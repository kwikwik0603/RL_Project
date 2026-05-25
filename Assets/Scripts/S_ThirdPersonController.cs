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

    private float turnSmoothTime = 0.1f;
    private float turnVelocity;

    //input
    private InputSystem_Actions actions;
    float horInput;
    float verInput;
    bool isSprinting;

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
        PlayerMovement();
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
        //get input from InputSystems and split into two floats
        Vector2 moveInput = actions.Player.Move.ReadValue<Vector2>();
        horInput = moveInput.x;
        verInput = moveInput.y;

        //getting sprint
        isSprinting = actions.Player.Dash.IsPressed();
    }

    private void PlayerMovement()
    {
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        float animMultiplier = isSprinting ? 2f : 1f;

        //getas cameras y rotation, smooths it and rotates transform
        float targetAngle = cam.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        //getting cameras directions as vectors, flattening and normoalizing them
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        //moving the transform according to the camera directions
        Vector3 moveDir = (camForward * verInput) + (camRight * horInput);

        if (moveDir.magnitude > 0.1f)
        {
            controller.Move(moveDir.normalized * currentSpeed * Time.deltaTime);

            Vector3 localMove = transform.InverseTransformDirection(moveDir.normalized);
            float animHor = localMove.x * animMultiplier;
            float animVer = localMove.z * animMultiplier;

            anim.SetFloat("Horizontal", animHor, 0.1f, Time.deltaTime);
            anim.SetFloat("Vertical", animVer, 0.1f, Time.deltaTime);
        }
        else
        {
            anim.SetFloat("Horizontal", horInput, 0.1f, Time.deltaTime);
            anim.SetFloat("Vertical", verInput, 0.1f, Time.deltaTime);
        }
    }
}
