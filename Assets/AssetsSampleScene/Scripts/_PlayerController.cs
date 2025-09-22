using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sneakSpeed = 2f;
    public bool holdToSneak = true; // if false, toggle


    [Header("Noise")]
    public float noiseMoveThreshold = 0.1f; // >0 => considered moving


    Rigidbody _rb;
    Vector3 _input;
    bool _sneakHeld;
    bool _sneakToggled;


    public bool IsSneaking => holdToSneak ? _sneakHeld : _sneakToggled;
    public bool IsMoving => new Vector2(_input.x, _input.z).sqrMagnitude > (noiseMoveThreshold * noiseMoveThreshold);
    public bool IsNoisy => IsMoving && !IsSneaking; // Guards hear only if not sneaking


    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation; // top-down
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }



    void Update()
    {
        // Top-down camera assumed looking down - use X/Z plane
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _input = new Vector3(h, 0f, v).normalized;


        // Sneak handling
        if (holdToSneak)
        {
            _sneakHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                _sneakToggled = !_sneakToggled;
        }
    }


    void FixedUpdate()
    {
        float speed = IsSneaking ? sneakSpeed : moveSpeed;
        Vector3 targetVel = _input * speed;
        Vector3 newPos = _rb.position + targetVel * Time.fixedDeltaTime;
        _rb.MovePosition(newPos);


        // Face movement direction if moving (nice feel)
        if (_input.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_input, Vector3.up);
            _rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, 0.2f));
        }
    }
}
