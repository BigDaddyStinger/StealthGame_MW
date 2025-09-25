using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    SPlayerController playerController;

    public bool IsNoisy => playerController != null && playerController.IsNoisy;

    void Awake()
    {
        playerController = GetComponent<SPlayerController>();
    }
}
