using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum GuardState { Patrol, Investigate, Pursue }

public class GuardAI : MonoBehaviour
{
    [Header("Patrol")]
    public List<Transform> waypoints = new List<Transform>();
    public float waypointTolerance = 0.4f;


    [Header("Investigate")]
    public float investigateDuration = 3f; // seconds to look around
    public float turnSpeed = 360f; // deg/sec when turning to face event


    [Header("Pursue")]
    public float catchDistance = 1.1f; // if closer than this => caught
    public float lostSightTimeout = 2.0f; // seconds after losing LOS before switching to Investigate


    [Header("Perception/LOS")]
    public float fieldOfView = 90f; // degrees total FOV
    public Transform eyes; // origin of sight raycasts
    public LayerMask obstacleMask; // set to "Obstacles" layer


    [Header("Debug")]
    public GuardState state = GuardState.Patrol;


    NavMeshAgent _agent;
    Transform _player;
    int _wpIndex;
    Vector3 _investigatePoint;
    float _investigateUntil;
    float _lostSightUntil;
    bool _hasLOS;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) _player = p.transform;
        if (!eyes) eyes = transform; // fallback
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (waypoints.Count > 0)
            _agent.destination = waypoints[_wpIndex].position;
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case GuardState.Patrol: UpdatePatrol(); break;
            case GuardState.Investigate: UpdateInvestigate(); break;
            case GuardState.Pursue: UpdatePursue(); break;
        }
    }



    void UpdatePatrol()
    {
        _agent.isStopped = false;
        if (waypoints.Count == 0) return;


        if (!_agent.pathPending && _agent.remainingDistance <= waypointTolerance)
        {
            _wpIndex = (_wpIndex + 1) % waypoints.Count;
            _agent.SetDestination(waypoints[_wpIndex].position);
        }


        // Opportunistic sight check
        if (CanSeePlayer(out _))
        {
            BeginPursuit();
        }
    }

    void UpdateInvestigate()
    {
        _agent.isStopped = true; // look around


        // Turn to face the investigate point
        Vector3 dir = (_investigatePoint - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }


        // If player becomes visible during investigation, pursue
        if (CanSeePlayer(out _))
        {
            BeginPursuit();
            return;
        }


        if (Time.time >= _investigateUntil)
        {
            // Resume patrol
            state = GuardState.Patrol;
            if (waypoints.Count > 0)
                _agent.SetDestination(waypoints[_wpIndex].position);
        }
    }


    void UpdatePursue()
    {
        if (_player == null) return;
        _agent.isStopped = false;
        _agent.SetDestination(_player.position);


        // Check catch
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= catchDistance)
        {
            GameManager.Instance.PlayerCaught();
            return;
        }


        // Maintain LOS timer
        _hasLOS = CanSeePlayer(out _);
        if (_hasLOS)
        {
            _lostSightUntil = Time.time + lostSightTimeout;
        }
        else if (Time.time > _lostSightUntil)
        {
            // Lost the player: go to last known position and investigate
            Vector3 lastPos;
            if (TryGetLastKnownPlayerPos(out lastPos))
            {
                _agent.SetDestination(lastPos);
                BeginInvestigate(lastPos);
            }
            else
            {
                state = GuardState.Investigate; // fallback: look around current spot
                _investigatePoint = transform.position + transform.forward;
                _investigateUntil = Time.time + investigateDuration;
            }
        }
    }

    bool TryGetLastKnownPlayerPos(out Vector3 pos)
    {
        // Eyes to player projection at the moment LOS was last confirmed
        if (_player)
        {
            pos = _player.position;
            return true;
        }
        pos = Vector3.zero; return false;
    }

    public void OnHeard(Vector3 worldPoint)
    {
        if (state == GuardState.Pursue) return; // hearing irrelevant if already chasing
        BeginInvestigate(worldPoint);
    }


    public void OnSeen(Transform player)
    {
        BeginPursuit();
    }


    void BeginInvestigate(Vector3 point)
    {
        state = GuardState.Investigate;
        _investigatePoint = point;
        _investigateUntil = Time.time + investigateDuration;
    }


    void BeginPursuit()
    {
        state = GuardState.Pursue;
        _lostSightUntil = Time.time + lostSightTimeout;
    }

    // Line of Sight using dot product + raycast block check
    public bool CanSeePlayer(out float dot)
    {
        dot = -1f;
        if (_player == null) return false;


        Vector3 eyePos = eyes.position;
        Vector3 toPlayer = (_player.position - eyePos);
        Vector3 toPlayerXZ = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (toPlayerXZ.sqrMagnitude < 0.0001f) return false;


        Vector3 dirNorm = toPlayerXZ.normalized;
        Vector3 fwd = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        dot = Vector3.Dot(fwd, dirNorm);
        float cosHalfFOV = Mathf.Cos(fieldOfView * 0.5f * Mathf.Deg2Rad);
        if (dot < cosHalfFOV) return false; // outside FOV


        // Raycast for obstacles between eyes and player
        Vector3 playerChest = _player.position + Vector3.up * 0.8f;
        if (Physics.Linecast(eyePos, playerChest, out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Something on obstacleMask blocked the view
            return false;
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        // Draw FOV lines from eyes
        Transform origin = eyes ? eyes : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin.position, 0.1f);
        float half = fieldOfView * 0.5f * Mathf.Deg2Rad;
        Vector3 fwd = transform.forward;
        Vector3 left = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * fwd;
        Vector3 right = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * fwd;
        Gizmos.DrawLine(origin.position, origin.position + left * 3f);
        Gizmos.DrawLine(origin.position, origin.position + right * 3f);
    }



}
