using UnityEngine;

public class SightSensor : MonoBehaviour
{

    public GuardAI guard;


    void Reset()
    {
        GetComponent<SphereCollider>().isTrigger = true;
        if (!guard) guard = GetComponentInParent<GuardAI>();
    }


    void OnTriggerStay(Collider other)
    {
        if (guard == null) return;
        if (!other.CompareTag("Player")) return;
        if (guard.CanSeePlayer(out _))
        {
            guard.OnSeen(other.transform);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
