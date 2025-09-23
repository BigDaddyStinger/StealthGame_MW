using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;


public class SPlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float crouchSpeed;
    [SerializeField] float noiseMoveThreshold = 0.1f;

    [SerializeField] bool isCrouched = false;
    public bool IsSneaking => isCrouched;
    public bool IsMoving => new Vector2(moveInput.x, moveInput.y).sqrMagnitude > (noiseMoveThreshold * noiseMoveThreshold);
    public bool IsNoisy => IsMoving && !IsSneaking; // Guards hear only if not sneaking

    [SerializeField] Rigidbody rb;

    [SerializeField] Vector2 moveInput;

    InputAction actMove;
    InputAction actCrouch;

    void Start()
    {
        actMove = InputSystem.actions.FindAction("Move");
        actCrouch = InputSystem.actions.FindAction("Crouch");

        rb = GetComponent<Rigidbody>();

        crouchSpeed = moveSpeed / 1.5f;

        if (actCrouch != null)
        {
            actCrouch.performed += _ => ToggleCrouch();
        }
    }

    void Update()
    {
        UpdateMovment();
    }

    public void UpdateMovment()
    {
        if (actMove == null) return;

        if (actCrouch == null) return;

        if (actMove != null && actCrouch != null)
        {
            if (!isCrouched)
            {
                moveInput = actMove.ReadValue<Vector2>();
                transform.Translate(moveInput.x * Time.deltaTime * moveSpeed, 0f, moveInput.y * Time.deltaTime * moveSpeed);
            }

            else
            {
                moveInput = actMove.ReadValue<Vector2>();
                transform.Translate(moveInput.x * Time.deltaTime * crouchSpeed, 0f, moveInput.y * Time.deltaTime * crouchSpeed);
            }

        }

    }
    public void ToggleCrouch()
    {
        isCrouched = !isCrouched;
    }

}
