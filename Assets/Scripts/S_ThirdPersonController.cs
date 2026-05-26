using UnityEngine;

public class S_ThirdPersonController : MonoBehaviour
{
    //player controller
    [SerializeField] private CharacterController controller;

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

    //input
    private InputSystem_Actions actions;
    bool isSprinting;
    float horInput;
    float verInput;

    private void Awake()
    {
        actions = new InputSystem_Actions();
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
    }

    private void Update()
    {
        MyInput();
        MovementController();
        AnimatorController();
    }
    
    private void MyInput2()
    {
        Vector2 moveInput = actions.Player.Move.ReadValue<Vector2>();

        horInput = moveInput.x;
        verInput = moveInput.y;

        Debug.Log("Hor Input" + horInput);
        Debug.Log("Ver Input" + verInput);

        Vector3 direction = new Vector3(horInput, 0f, verInput).normalized;

        if(direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;  
            controller.Move(moveDir * walkSpeed * Time.deltaTime);
        }
    }

    private void MyInput()
    {
        //input from system
        isSprinting = actions.Player.Sprint.IsPressed();
        Vector2 moveInput = actions.Player.Move.ReadValue<Vector2>();
        horInput = moveInput.x;
        verInput = moveInput.y;
    }

    private void MovementController()
    {
        //walk and run speed
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Smoothly interpolate physical speed
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

        Vector3 moveDir = (camForward * verInput) + (camRight * horInput);

        //dead zone check
        if (moveDir.magnitude > 0.1f)
        { 
            controller.Move(moveDir.normalized * movementSpeed * Time.deltaTime);
        }
        else
        {
            // Smoothly decay to zero when you release the keys
            movementSpeed = Mathf.MoveTowards(movementSpeed, 0f, Time.deltaTime * 10f);
        }
    }

    private void AnimatorController()
    {
        if (moveDir.magnitude > 0.1f)
        {
            float animMultiplier = isSprinting ? 2.0f : 1.0f;

            float targetHorAnim = horInput * animMultiplier;
            float targetVerAnim = verInput * animMultiplier;

            anim.SetFloat("Horizontal", targetHorAnim, 0.1f, Time.deltaTime);
            anim.SetFloat("Vertical", targetVerAnim, 0.1f, Time.deltaTime);
        }
        else
        {
            anim.SetFloat("Horizontal", 0f, 0.1f, Time.deltaTime);
            anim.SetFloat("Vertical", 0f, 0.1f, Time.deltaTime);
        }
    }
}
