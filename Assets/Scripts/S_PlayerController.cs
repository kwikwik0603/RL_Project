using UnityEditor.Build;
using UnityEngine;

public class S_PlayerController : MonoBehaviour
{
    [Header("Movement Toggles")]

    [SerializeField] private bool canMove = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canDash = true;
    [SerializeField] private bool canAttack = true;


    [Header("Movement")]

    private float moveSpeed = 7.0f;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;

    [SerializeField] private Transform orientation;

    [SerializeField] private float groundDrag;

    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    private bool readyToJump = true;

    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask whatIsGround;

    private bool isGrounded;

    float horInput;
    float verInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public enum MovementState
    {
        walking,
        sprinting,
        air
    }

    public MovementState state;

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

    //update for physics based movement
    void FixedUpdate()
    {
        MovePlayer();
    }

    //getting player input
    void MyInput()
    {
        Vector2 moveInput = actions.Player.Move.ReadValue<Vector2>();

        horInput = moveInput.x;
        verInput = moveInput.y;

        if(actions.Player.Jump.IsPressed() && isGrounded && readyToJump)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    //handles state for movement
    private void StateHandler()
    {
        //Mode - sprint

    }

    //player movement
    void MovePlayer()
    {
        if (canMove)
        {
            moveDirection = orientation.forward * verInput + orientation.right * horInput;

            if (isGrounded)
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            else if (!isGrounded)
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    //capping max movement speed
    void SpeedControl()
    {
        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if(flatVelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    void Jump()
    {
        if ((canJump))
        {
            //Debug.Log("Jump");

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

            
        }
    }

    void ResetJump()
    {
        readyToJump = true;
    }
}
 