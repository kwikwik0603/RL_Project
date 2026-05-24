using UnityEngine;

public class S_Dashing : MonoBehaviour
{
    //References
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerCam;
    private Rigidbody rb;
    private S_PlayerController pc;

    //Dash movement
    [SerializeField] private float dashForce;
    [SerializeField] private float dashUpwardForce;
    [SerializeField] private float dashDuration;
    [SerializeField] private float dashCooldown;
    private Vector3 delayedForceToApply;
    private float dashCooldownTimer;


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
        rb = GetComponent<Rigidbody>();
        pc = GetComponent<S_PlayerController>();
    }


    void Update()
    {
        if (actions.Player.Dash.IsPressed())
        {
            Dash();
        }

        if(dashCooldownTimer > 0)
        {
            dashCooldownTimer = -Time.deltaTime;
        }
    }

    private void Dash()
    {
        if (dashCooldownTimer > 0) return;
        else dashCooldownTimer = dashCooldown;
            pc.dashing = true;
        Vector3 forceToAdd = pc.moveDirection * dashForce + orientation.up * dashUpwardForce;
        delayedForceToApply = forceToAdd;

        Invoke(nameof(DelayedDashForce), 0.025f);
        Invoke(nameof(ResetDash), dashDuration);
    }

    private void DelayedDashForce()
    {
        rb.AddForce(delayedForceToApply, ForceMode.Impulse);
    }

    private void ResetDash()
    {
        pc.dashing = false;
    }
}
