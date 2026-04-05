using UnityEngine;

public class S_ThirdPersonCam : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private Transform orientation;
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerObj;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private float rotationSpeed;

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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Update()
    {
        CameraMovement();
    }

    void CameraMovement()
    {
        //rotation direction
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;

        Vector2 moveInput = actions.Player.Move.ReadValue<Vector2>();

        //rotate player object
        float horInput = moveInput.x;
        float verInput = moveInput.y;

        Vector3 inputDir = orientation.forward * verInput + orientation.right * horInput;

        if (inputDir != Vector3.zero)
        {
            playerObj.forward = Vector3.Slerp(playerObj.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
        }
    }
}
