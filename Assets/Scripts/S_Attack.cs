using Unity.VisualScripting;
using UnityEngine;

public class S_Attack : MonoBehaviour
{
    //references
    [SerializeField] private Transform mainCamera;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private GameObject slowSpellPrefab;
    [SerializeField] private Animator animator;
    [SerializeField] private S_ThirdPersonController thridPersonController;
    [SerializeField] private AttackData attackData;
    [SerializeField] private PlayerData playerData;
    [SerializeField] private GameObject fastSpellPrefab;

    [SerializeField] private S_HudManager hudManager;

    private int stamina;

    private bool readyToAttack;

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
        readyToAttack = true;

        hudManager.SetMaxStamina(stamina);
    }

    void Update()
    {
        if (actions.Player.FastAttack.IsPressed() && readyToAttack && stamina > 2 && playerData.isAlive)
        {
            animator.SetTrigger("FastAttack");
            playerData.canMove = false;
            Invoke(nameof(ResetTrigger), 0.1f);
        }
        if(actions.Player.SlowAttack.IsPressed() && readyToAttack && stamina > 5 && playerData.isAlive)
        {
            animator.SetTrigger("SlowAttack");
            playerData.canMove = false;
            Invoke(nameof(ResetTrigger), 0.1f);
        }
    }

    //called from event trigger in anim clip
    private void FastAttack()
    {
        float coolDown = attackData.fastAttackCooldown;
        float launchForce = attackData.fastThrowForce;
        float launchUpwardForce = attackData.fastThrowUpwardForce;
        int cost = attackData.fastThrowCost;

        readyToAttack = false;

        float cameraPitch = mainCamera.transform.eulerAngles.x;
        if (cameraPitch > 180f) cameraPitch -= 360f;
        float clampedPitch = Mathf.Clamp(cameraPitch, -5f, 5f); //+ve is down, -ve is up
        float horizontalFacing = transform.eulerAngles.y;
        Quaternion launchRotation = Quaternion.Euler(clampedPitch, horizontalFacing, 0f);

        GameObject projectile = Instantiate(fastSpellPrefab, attackPoint.position, launchRotation);
        Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();

        Vector3 forceToAdd = projectile.transform.forward * launchForce + transform.up * launchUpwardForce;
        projectileRB.AddForce(forceToAdd, ForceMode.Impulse);

        stamina -= cost;
        hudManager.SetStamina(stamina);

        Vector3 sightDirection = launchRotation * Vector3.forward;
        Vector3 debugEndPoint = throwPoint.position + (sightDirection * 50f);

        Debug.DrawLine(throwPoint.position, debugEndPoint, Color.green, 2.0f);

        Invoke(nameof(ResetThrow), coolDown);
    }


    /*
    private void FastAttack()
    {
        float coolDown = attackData.fastAttackCooldown;
        float launchForce = attackData.fastThrowForce;
        float launchUpwardForce = attackData.fastThrowUpwardForce;
        int cost = attackData.fastThrowCost;

        readyToAttack = false;

        Vector3 camForward = mainCamera.transform.forward;

        float pitch = Mathf.Asin(camForward.y) * Mathf.Rad2Deg;
        float clampedPitch = Mathf.Clamp(pitch, -30f, 60f);
        float horizontalFacing = transform.eulerAngles.y;

        Quaternion launchRotation = Quaternion.Euler(-clampedPitch, horizontalFacing, 0f);
        GameObject projectile = Instantiate(projectilePrefab, attackPoint.position, launchRotation);
        Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();

        Vector3 forceToAdd = projectile.transform.forward * launchForce + transform.up * launchUpwardForce;
        projectileRB.AddForce(forceToAdd, ForceMode.Impulse);

        stamina -= cost;

        Invoke(nameof(ResetThrow), coolDown);
    }*/

    //called from event trigger in anim clip
    private void SlowAttack()
    {
        float coolDown = attackData.slowThrowCooldown;
        float launchForce = attackData.slowThrowForce;
        float launchUpwardForce = attackData.slowThrowUpwardForce;
        int cost = attackData.slowThrowCost;

        readyToAttack = false;

        GameObject projectile = Instantiate(slowSpellPrefab, attackPoint.position, GetCameraRotation());
        Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();
        Vector3 forceToAdd = projectile.transform.forward * launchForce + transform.up * launchUpwardForce;
        projectileRB.AddForce(forceToAdd, ForceMode.Impulse);

        stamina -= cost;
        hudManager.SetStamina(stamina);

        Invoke(nameof(ResetThrow), coolDown);
    }

    private void ResetThrow()
    {
        readyToAttack = true;
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
