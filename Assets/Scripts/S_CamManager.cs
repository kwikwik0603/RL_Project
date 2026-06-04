using Unity.Cinemachine;
using UnityEngine;

public class S_CamManager : MonoBehaviour
{

    [SerializeField] private CinemachineCamera thirdPersonCam;
    [SerializeField] private CinemachineCamera shoulderCam;
    [SerializeField] private CinemachineInputAxisController inputController;

    [Header("Settings")]
    private int activePriority = 15;
    private int inactivePriority = 10;

    private InputSystem_Actions actions;
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
        ResetCameras();
    }

    void Update()
    {
        
    }

    public void EnableShoulderCam()
    {
        
        thirdPersonCam.Priority = inactivePriority;
        inputController.Controllers[1].Enabled = false;
        shoulderCam.Priority = activePriority;
    }

    public void ResetCameras()
    {
        thirdPersonCam.Priority = activePriority;
        inputController.Controllers[1].Enabled = true;
        shoulderCam.Priority = inactivePriority;
    }
}
