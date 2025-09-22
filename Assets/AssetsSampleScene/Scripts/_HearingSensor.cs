using UnityEngine;

public class HearingSensor : MonoBehaviour
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
        // Only react to objects that can make noise
        NoiseEmitter emitter = other.GetComponentInParent<NoiseEmitter>();
        if (emitter != null && emitter.IsNoisy)
        {
            guard.OnHeard(other.transform.position);
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
