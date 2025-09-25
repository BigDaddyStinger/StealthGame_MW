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

        NoiseEmitter emitter = other.GetComponentInParent<NoiseEmitter>();
        if (emitter != null && emitter.IsNoisy)
        {
            guard.OnHeard(other.transform.position);
        }
    }
}
