using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    SPlayerController playerController;

    public bool IsNoisy => playerController != null && playerController.IsNoisy;

    void Awake()
    {
        playerController = GetComponent<SPlayerController>();
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
