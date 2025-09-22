using UnityEngine;
using UnityEngine.InputSystem;


public class SPlayerController : MonoBehaviour
{
    [SerializeField] Vector2 moveInput;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // MUST match: On<ActionName>(InputValue)
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void Update()
    {
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        transform.Translate(move * Time.deltaTime * moveSpeed);
    }
}
