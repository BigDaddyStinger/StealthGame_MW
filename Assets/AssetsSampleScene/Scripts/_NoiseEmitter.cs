using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{

    PlayerController controller;

    public bool IsNoisy => controller != null && controller.IsNoisy;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
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
