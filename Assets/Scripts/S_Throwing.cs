using UnityEngine;

public class S_Throwing : MonoBehaviour
{
    //references
    [SerializeField] private Transform cam;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Animator anim;
    [SerializeField] private S_ThirdPersonController tpController;

    //
    [SerializeField] private int stamina;

    [SerializeField] private float fastThrowCooldown;
    [SerializeField] private float fastThrowForce;
    [SerializeField] private float fastThrowUpwardForce;
    [SerializeField] private int fastThrowCost;

    [SerializeField] private float slowThrowCooldown;
    [SerializeField] private float slowThrowForce;
    [SerializeField] private float slowThrowUpwardForce;
    [SerializeField] private int slowThrowCost; 


    private bool readyToThrow;

    //input actions
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
        readyToThrow = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (actions.Player.FastAttack.IsPressed() && readyToThrow && stamina > 0)
        {
            anim.SetTrigger("FastAttack");
            tpController.canMove = false;
            Invoke(nameof(ResetTrigger), 0.1f);
        }
        if(actions.Player.SlowAttack.IsPressed() && readyToThrow && stamina > 0)
        {
            anim.SetTrigger("SlowAttack");
            tpController.canMove = false;
            Invoke(nameof(ResetTrigger), 0.1f);
        }
    }


    private void FastAttack()
    {
        Debug.Log("Function called");
        
        readyToThrow = false;
        
        GameObject projectile = Instantiate(projectilePrefab, attackPoint.position, GetCameraRotation());

        Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();

        Vector3 forceToAdd = projectile.transform.forward * fastThrowForce + transform.up * fastThrowUpwardForce;

        projectileRB.AddForce(forceToAdd, ForceMode.Impulse);

        stamina -= fastThrowCost;

        Invoke(nameof(ResetThrow), fastThrowCooldown);
    }

    private void SlowAttack()
    {
        Debug.Log("Function called");

        readyToThrow = false;
        
        GameObject projectile = Instantiate(projectilePrefab, attackPoint.position, GetCameraRotation());

        Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();

        Vector3 forceToAdd = projectile.transform.forward * slowThrowForce + transform.up * slowThrowUpwardForce;

        projectileRB.AddForce(forceToAdd, ForceMode.Impulse);

        stamina -= slowThrowCost;

        Invoke(nameof(ResetThrow), slowThrowCooldown);
    }

    private void LaunchProjectile(int type)
    {
        float coolDown = 0f;
        float launchForce = 0f;
        float launchUpwardForce = 0f;
        int cost = 0;

        switch (type)
        {
            case 1:
                coolDown = fastThrowCooldown;
                launchForce = fastThrowForce;
                launchUpwardForce = fastThrowUpwardForce;
                cost = fastThrowCost;

                break;
            case 2:
                coolDown = slowThrowCooldown;
                launchForce = slowThrowForce;
                launchUpwardForce = slowThrowUpwardForce;
                cost = slowThrowCost;

                break;
            default:
                Debug.Log("No attackType");
                break;
        }

        readyToThrow = false;

        GameObject projectile = Instantiate(projectilePrefab, attackPoint.position, GetCameraRotation());
        Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();
        Vector3 forceToAdd = projectile.transform.forward * launchForce + transform.up * launchUpwardForce;
        projectileRB.AddForce(forceToAdd, ForceMode.Impulse);

        stamina -= cost;

        Invoke(nameof(ResetThrow), coolDown);

    }

    private void ResetThrow()
    {
        readyToThrow = true;
    }

    private void ResetTrigger()
    {
        anim.ResetTrigger("FastAttack");
        anim.ResetTrigger("SlowAttack");
    }

    private void ResetCharController()
    {

        Debug.Log("Bool reset");
        tpController.canMove = true;
    }

    private Quaternion GetCameraRotation()
    {
        float horizontalCamAngle = cam.eulerAngles.y;
        Quaternion launchRotation = Quaternion.Euler(0f, horizontalCamAngle, 0f);

        return launchRotation;
    }
}
