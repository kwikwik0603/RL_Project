using UnityEngine;

public class S_ThirdPersonController : MonoBehaviour
{
    //references
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerData playerData;

    [SerializeField] private string pHorizontal;
    [SerializeField] private string pVertical;
    [SerializeField] private string pIsCrouching;
    [SerializeField] private string pIsJumping;

    //movement
    private float walkSpeed;
    private float sprintSpeed;
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
    [SerializeField] private bool holdCrouch;
    private bool isCrouching; //is used

    private float jumpHeight;
    private LayerMask whatIsGround;
    private float gravity = -9.81f;
    private float vertVelocity;
    private bool isGrounded;

    //input
    private InputSystem_Actions actions;
    bool isSprinting;
    float horInput;
    float verInput;


    private void Awake()
    {
        actions = new InputSystem_Actions();

        standingHeight = characterController.height;
        standingCentre = characterController.center;
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
        walkSpeed = playerData.walkSpeed;
        sprintSpeed = playerData.sprintSpeed;
        jumpHeight = playerData.jumpHeight;
        whatIsGround = playerData.GroundLayerMask;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        isCrouching = false;
        playerData.canMove = true;
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


        if (!holdCrouch)
        {
            if (actions.Player.Crouch.WasPressedThisFrame())
            {
                isCrouching = !isCrouching;
                if (isCrouching) Crouch();
                else ResetCrouch();
            }
        }
        else
        {
            if (actions.Player.Crouch.IsPressed())
            {
                Crouch();
            }
            else
            {
                ResetCrouch();
            }
        }


        if (actions.Player.Jump.WasPressedThisFrame() && isGrounded && playerData.canMove)
        {
            animator.SetTrigger(pIsJumping);
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

        if (playerData.isAlive)
        {
            //gets camera angle and lerps transform accordingly
            float targetAngle = mainCamera.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        //world space processing of camera direction
        Vector3 camForward = mainCamera.forward;
        Vector3 camRight = mainCamera.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        moveDir = (camForward * verInput) + (camRight * horInput);
        Vector3 horzVelocity = Vector3.zero;

        //dead zone check
        if (playerData.canMove && moveDir.magnitude > 0.1f)
        {
            //charController.Move(moveDir.normalized * movementSpeed * Time.deltaTime);

            horzVelocity = moveDir.normalized * movementSpeed;

            float animMultiplier = isSprinting ? 2.0f : 1.0f;
            float targetHorAnim = horInput * animMultiplier;
            float targetVerAnim = verInput * animMultiplier;
            animator.SetFloat(pHorizontal, targetHorAnim, 0.1f, Time.deltaTime);
            animator.SetFloat(pVertical, targetVerAnim, 0.1f, Time.deltaTime);
        }
        else
        {
            movementSpeed = Mathf.MoveTowards(movementSpeed, 0f, Time.deltaTime * 10f);

            animator.SetFloat(pHorizontal, 0f, 0.1f, Time.deltaTime);
            animator.SetFloat(pVertical, 0f, 0.1f, Time.deltaTime);
        }
        vertVelocity += gravity * Time.deltaTime;

        Vector3 finalDisplacement = horzVelocity;
        finalDisplacement.y = vertVelocity;

        
        characterController.Move(finalDisplacement * Time.deltaTime);
    }

    private void Crouch()
    {
        isCrouching = true;
        characterController.height = crouchingHeight;
        characterController.center = crouchingCentre;

        if (actions.Player.Crouch.WasPressedThisFrame())
        {
            characterController.Move(Vector3.down * 0.2f);
        }

        animator.SetBool(pIsCrouching, true);
    }

    private void ResetCrouch()
    {
        isCrouching = false;
        characterController.height = standingHeight;
        characterController.center = standingCentre;

        animator.SetBool(pIsCrouching, false);
    }

    private void Jump()
    {
        vertVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
}
