using UnityEngine;

public class S_Throwing : MonoBehaviour
{
    //references
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Animator animator;
    [SerializeField] private S_ThirdPersonController thridPersonController;
    [SerializeField] private AttackData attackData;
    [SerializeField] private PlayerData playerData;

    private int stamina;

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
        stamina = attackData.maxStamina;
        playerData.isAlive = true;
        readyToThrow = true;
    }

    void Update()
    {
        if (actions.Player.FastAttack.IsPressed() && readyToThrow && stamina > 0 && playerData.isAlive)
        {
            animator.SetTrigger("FastAttack");
            playerData.canMove = false;
            Invoke(nameof(ResetTrigger), 0.1f);
        }
        if(actions.Player.SlowAttack.IsPressed() && readyToThrow && stamina > 0 && playerData.isAlive)
        {
            animator.SetTrigger("SlowAttack");
            playerData.canMove = false;
            Invoke(nameof(ResetTrigger), 0.1f);
        }
    }

    //called from event trigger in anim clip
    private void LaunchProjectile(int type)
    {
        float coolDown = 0f;
        float launchForce = 0f;
        float launchUpwardForce = 0f;
        int cost = 0;

        switch (type)
        {
            case 1:
                coolDown            = attackData.fastAttackCooldown;
                launchForce         = attackData.fastThrowForce;
                launchUpwardForce   = attackData.fastThrowUpwardForce;
                cost                = attackData.fastThrowCost;

                break;
            case 2:
                coolDown            = attackData.slowThrowCooldown;
                launchForce         = attackData.slowThrowForce;
                launchUpwardForce   = attackData.slowThrowUpwardForce;
                cost                = attackData.slowThrowCost;

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
        animator.ResetTrigger("FastAttack");
        animator.ResetTrigger("SlowAttack");
    }

    //called from event trigger in anim clip
    private void ResetCharController()
    {

        Debug.Log("Bool reset");
        playerData.canMove = true;
    }

    private Quaternion GetCameraRotation()
    {
        float horizontalCamAngle = mainCamera.eulerAngles.y;
        Quaternion launchRotation = Quaternion.Euler(0f, horizontalCamAngle, 0f);

        return launchRotation;
    }
}
