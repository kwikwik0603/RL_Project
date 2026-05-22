using UnityEditor.Build;
using UnityEngine;

public class S_PlayerController : MonoBehaviour
{
    //movement Toggles
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canDash = true;
    [SerializeField] private bool canAttack = true;

    //movement 
    private float moveSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;

    [SerializeField] private Transform orientation;

    [SerializeField] private float groundDrag;

    //jump and air movement
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    private bool readyToJump;

    //crouch
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float crouchYScale;
    private float startYScale;

    //ground check
    [SerializeField] private float playerHeight;
    [SerializeField] private LayerMask whatIsGround;
    private bool isGrounded;

    //input
    private InputSystem_Actions actions;
    float horInput;
    float verInput;
    Vector3 moveDirection;

    //rigidbody
    Rigidbody rb;

    
    //movement states
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }
    public MovementState state;

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

        readyToJump = true;

        startYScale = transform.localScale.y;

    }

    void Update()
    {

        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        if (isGrounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;
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

        if (actions.Player.Jump.IsPressed() && isGrounded && readyToJump)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if(canCrouch)
        {
            if (actions.Player.Crouch.IsInProgress())
            {
                transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
                //rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

                if (actions.Player.Crouch.WasPressedThisFrame())
                {
                    rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
                }
            }
            else
            {
                transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
            }
        }
    }

    //handles state for movement
    private void StateHandler()
    {
        //crouching
        if (actions.Player.Crouch.IsInProgress())
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        //Mode - sprint
        else if (isGrounded && actions.Player.Sprint.IsPressed())
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        //walking
        else if (isGrounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;   
        }

        //in air
        else
        {
            state = MovementState.air;
        }
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

        if (flatVelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    //handles jumping
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
