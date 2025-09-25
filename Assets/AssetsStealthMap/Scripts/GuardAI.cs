using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public enum GuardState { Patrol, Investigate, Pursue }

public class GuardAI : MonoBehaviour
{
    [Header("Patrol")]
    public List<Transform> waypoints = new List<Transform>();
    [SerializeField] float waypointTolerance = 0.4f;

    [Header("Investigate")]
    [SerializeField] float investigateDuration = 3f;
    [SerializeField] float turnSpeed = 360f;

    [Header("Pursue")]
    [SerializeField] float catchDistance = 1.1f;
    [SerializeField] float lostSightTimeout = 2.0f;

    [Header("Perception/LoS")]
    [SerializeField] float fieldOfView = 90f;
    public Transform eyes;
    public LayerMask obstacleMask;

    [Header("Placement")]
    [SerializeField] float snapToNavMeshDistance = 3f;

    [Header("Debug")]
    [SerializeField] GuardState state = GuardState.Patrol;

    NavMeshAgent guardAgent;
    Transform _player;
    int _wpIndex;
    Vector3 investigatePoint;
    float investigateUntil;
    float lostSightUntil;
    bool lostSight;

    void Awake()
    {
        guardAgent = GetComponent<NavMeshAgent>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) _player = p.transform;
        if (!eyes) eyes = transform;
    }

    void OnEnable()
    {
        EnsureOnNavMesh();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnsureOnNavMesh();
        if (waypoints.Count > 0 && IsAgentReady())
        {
            guardAgent.destination = waypoints[_wpIndex].position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsAgentReady()) return;

        switch (state)
        {
            case GuardState.Patrol: UpdatePatrol(); break;
            case GuardState.Investigate: UpdateInvestigate(); break;
            case GuardState.Pursue: UpdatePursue(); break;
        }
    }

    bool IsAgentReady()
    {
        return guardAgent != null && guardAgent.isActiveAndEnabled && guardAgent.isOnNavMesh;
    }

    void EnsureOnNavMesh()
    {
        if (guardAgent == null || !guardAgent.isActiveAndEnabled) return;

        if (guardAgent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, snapToNavMeshDistance, NavMesh.AllAreas))
        {
            guardAgent.Warp(hit.position);
        }
    }

    void UpdatePatrol()
    {
        if (!IsAgentReady()) return;
        guardAgent.isStopped = false;
        if (waypoints.Count == 0) return;

        if (!guardAgent.pathPending && guardAgent.remainingDistance <= waypointTolerance)
        {
            _wpIndex = (_wpIndex + 1) % waypoints.Count;
            guardAgent.SetDestination(waypoints[_wpIndex].position);
        }

        if (CanSeePlayer(out _))
        {
            BeginPursuit();
        }
    }

    void UpdateInvestigate()
    {
        if (!IsAgentReady()) return;
        guardAgent.isStopped = true;

        Vector3 dir = (investigatePoint - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        if (CanSeePlayer(out _))
        {
            BeginPursuit();
            return;
        }


        if (Time.time >= investigateUntil)
        {
            state = GuardState.Patrol;
            if (waypoints.Count > 0 && IsAgentReady())
                guardAgent.SetDestination(waypoints[_wpIndex].position);
        }
    }

    void UpdatePursue()
    {
        if (_player == null || !IsAgentReady()) return;
        guardAgent.isStopped = false;
        guardAgent.SetDestination(_player.position);

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= catchDistance)
        {
            GameManager.Instance.PlayerCaught();
            return;
        }

        lostSight = CanSeePlayer(out _);
        if (lostSight)
        {
            lostSightUntil = Time.time + lostSightTimeout;
        }
        else if (Time.time > lostSightUntil)
        {
            Vector3 lastPos;
            if (TryGetLastKnownPlayerPos(out lastPos))
            {
                if (IsAgentReady()) guardAgent.SetDestination(lastPos);
                BeginInvestigate(lastPos);
            }
            else
            {
                state = GuardState.Investigate;
                investigatePoint = transform.position + transform.forward;
                investigateUntil = Time.time + investigateDuration;
            }
        }
    }

    bool TryGetLastKnownPlayerPos(out Vector3 pos)
    {
        if (_player)
        {
            pos = _player.position;
            return true;
        }
        pos = Vector3.zero; return false;
    }

    public void OnHeard(Vector3 worldPoint)
    {
        if (state == GuardState.Pursue) return;
        BeginInvestigate(worldPoint);
    }

    public void OnSeen(Transform player)
    {
        BeginPursuit();
    }
    void BeginInvestigate(Vector3 point)
    {
        state = GuardState.Investigate;
        investigatePoint = point;
        investigateUntil = Time.time + investigateDuration;
    }

    void BeginPursuit()
    {
        EnsureOnNavMesh();
        if (!IsAgentReady()) return;
        state = GuardState.Pursue;
        lostSightUntil = Time.time + lostSightTimeout;
    }

    public bool CanSeePlayer(out float dot)
    {
        dot = -1f;
        if (_player == null) return false;

        Vector3 eyePos = eyes ? eyes.position : transform.position;
        Vector3 toPlayer = (_player.position - eyePos);
        Vector3 toPlayerXZ = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (toPlayerXZ.sqrMagnitude < 0.0001f) return false;

        Vector3 dirNorm = toPlayerXZ.normalized;
        Vector3 fwd = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        dot = Vector3.Dot(fwd, dirNorm);
        float cosHalfFOV = Mathf.Cos(fieldOfView * 0.5f * Mathf.Deg2Rad);
        if (dot < cosHalfFOV) return false;

        Vector3 playerChest = _player.position + Vector3.up * 0.8f;
        if (Physics.Linecast(eyePos, playerChest, out RaycastHit hit, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Transform origin = eyes ? eyes : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin.position, 0.1f);
        Vector3 fwd = transform.forward;
        Vector3 left = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * fwd;
        Vector3 right = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * fwd;
        Gizmos.DrawLine(origin.position, origin.position + left * 3f);
        Gizmos.DrawLine(origin.position, origin.position + right * 3f);
    }
}
