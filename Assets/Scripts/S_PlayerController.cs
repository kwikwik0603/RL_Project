using UnityEngine;

public class S_PlayerController : MonoBehaviour
{
    [Header("Movement")]

    [SerializeField] private float moveSpeed;
    [SerializeField] private Transform orientation;

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
        MyInput();
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
}
 