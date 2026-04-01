using UnityEngine;

public class S_PlayerController : MonoBehaviour
{
    [Header("Movement")]

    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform orientation;

    [SerializeField] private float groundDrag;

    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask whatIsGround;
    private bool isGrounded;

    float horInput;
    float verInput;

    Vector3 moveDirection;

    Rigidbody rb;



    private InputSystem_Actions actions;

    void Awake()
    {
        actions = new InputSystem_Actions();
    }

    void OnEnable()
    {
        actions.Player.Enable();
    }

    void OnDisable()
    {
        actions.Player.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;   
    }

    void Update()
    {

        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();

        if (isGrounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;

        SpeedControl();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void MyInput()
    {
        Vector2 moveInput = actions.Player.Move.ReadValue<Vector2>();

        horInput = moveInput.x;
        verInput = moveInput.y;
    }

    void MovePlayer()
    {
        moveDirection = orientation.forward * verInput + orientation.right * horInput;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10, ForceMode.Force);
    }

    void SpeedControl()
    {
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if(flatVelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }
}
 