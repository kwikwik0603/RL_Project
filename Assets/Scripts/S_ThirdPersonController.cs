using UnityEngine;

public class S_ThirdPersonController : MonoBehaviour
{
    //player controller
    [SerializeField] private CharacterController charController;

    //camera
    [SerializeField] private Transform cam;

    //animator
    [SerializeField] private Animator anim;

    //movement
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 12f;
    private Vector3 moveDir;
    private float movementSpeed;
    private float turnSmoothTime = 0.1f;
    private float turnVelocity;
    [HideInInspector] public bool canMove;

    //crouch movement
    private float crouchingHeight = 1f;
    private float standingHeight;
    private Vector3 standingCentre;
    private Vector3 crouchingCentre;
    private bool isCrouching;

    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private LayerMask whatIsGround;
    private float vertVelocity;
    private bool isGrounded;

    //input
    private InputSystem_Actions actions;
    bool isSprinting;
    float horInput;
    float verInput;

    public enum MovementState
    {
        walking,
        running,
        crouching,
        air
    }
    public MovementState state;

    private void Awake()
    {
        actions = new InputSystem_Actions();

        standingHeight = charController.height;
        standingCentre = charController.center;
        crouchingCentre = standingCentre;
        crouchingCentre.y = crouchingCentre.y - ((standingHeight - crouchingHeight) / 2f);
    }

    private void OnEnable()
    {
        actions.Player.Enable();
    }

    private void OnDisable()
    {
        actions.Player.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isCrouching = false;
        canMove = true;
    }

    private void Update()
    {
        MyInput();
        MovementController();
    }

    private void MyInput()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, standingHeight * 0.5f + 0.2f, whatIsGround);

        //input from system
        isSprinting = actions.Player.Sprint.IsPressed();
        Vector2 moveInput = actions.Player.Move.ReadValue<Vector2>();
        horInput = moveInput.x;
        verInput = moveInput.y;

        if(actions.Player.Crouch.IsPressed())
        {
            Crouch();
        }
        else
        {
            ResetCrouch();
        }

        if(actions.Player.Jump.WasPressedThisFrame() && isGrounded && canMove)
        {
            anim.SetTrigger("Jumping");
        }
    }

    private void MovementController()
    {
        if(isGrounded && vertVelocity < 0)
        {
            vertVelocity = -2f;
        }


        //walk and run speed
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        movementSpeed = Mathf.MoveTowards(movementSpeed, targetSpeed, Time.deltaTime * 8f);

        //gets camera angle and lerps transform accordingly
        float targetAngle = cam.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        //world space processing of camera direction
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDir = (camForward * verInput) + (camRight * horInput);
        Vector3 horzVelocity = Vector3.zero;

        //dead zone check
        if (canMove && moveDir.magnitude > 0.1f)
        {
            //charController.Move(moveDir.normalized * movementSpeed * Time.deltaTime);

            horzVelocity = moveDir.normalized * movementSpeed;

            float animMultiplier = isSprinting ? 2.0f : 1.0f;
            float targetHorAnim = horInput * animMultiplier;
            float targetVerAnim = verInput * animMultiplier;
            anim.SetFloat("Horizontal", targetHorAnim, 0.1f, Time.deltaTime);
            anim.SetFloat("Vertical", targetVerAnim, 0.1f, Time.deltaTime);
        }
        else
        {
            movementSpeed = Mathf.MoveTowards(movementSpeed, 0f, Time.deltaTime * 10f);

            anim.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
            anim.SetFloat("Vertical", 0f, 0.1f, Time.deltaTime);
        }
        vertVelocity += gravity * Time.deltaTime;

        Vector3 finalDisplacement = horzVelocity;
        finalDisplacement.y = vertVelocity;

        
        charController.Move(finalDisplacement * Time.deltaTime);
    }

    private void Crouch()
    {
        isCrouching = true;
        charController.height = crouchingHeight;
        charController.center = crouchingCentre;

        if (actions.Player.Crouch.WasPressedThisFrame())
        {
            charController.Move(Vector3.down * 0.2f);
        }

        anim.SetBool("Crouching", true);
    }

    private void ResetCrouch()
    {
        isCrouching = false;
        charController.height = standingHeight;
        charController.center = standingCentre;

        anim.SetBool("Crouching", false);
    }

    private void Jump()
    {
        vertVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
}
