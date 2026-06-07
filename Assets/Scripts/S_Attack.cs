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

    [SerializeField] private string pIsAttacking;
    [SerializeField] private string pFastSpell;
    [SerializeField] private string pSlowSpell;

    [SerializeField] private S_HudManager hudManager;

    [SerializeField] private int stamina;
    [SerializeField] private bool isAttacking;
    [SerializeField] private int attackType;
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
        stamina = attackData.playerStamina;
        if (stamina == 0) Debug.LogError("No Player Stamina");
        playerData.isAlive = true;
        readyToAttack = true;
        isAttacking = false;

        hudManager.SetMaxStamina(stamina);
    }

    void Update()
    {
        if (actions.Player.FastAttack.WasPressedThisFrame())
        {
            isAttacking = true;
            attackType = 1;
        }
        else if(actions.Player.SlowAttack.WasPressedThisFrame())
        {
            isAttacking = true;
            attackType = 2;
        }
        else if(actions.Player.AoEAttack.WasPressedThisFrame())
        {
            isAttacking = true;
            attackType = 3;
        }
        else if (actions.Player.ExitAttack.WasPressedThisFrame())
        {
            isAttacking = false;
        }



        if(isAttacking)
        {
            animator.SetBool(pIsAttacking, true);
            if (actions.Player.Fire.WasPressedThisFrame() && readyToAttack && playerData.isAlive)
            {
                playerData.canMove = false;
                switch (attackType)
                {
                    case 1:
                        if(stamina > 2)
                        {
                            animator.SetTrigger(pFastSpell);
                        }
                        break;
                    case 2:
                        if (stamina > 5)
                        {
                            animator.SetTrigger(pSlowSpell);
                        }
                        break;

                    case 3:
                        if (stamina > 10)
                        {
                            Debug.Log("AoE Attack");
                        }
                        break;
                    default:
                        Debug.Log("Invalid Attack");
                        break;
                }
                Invoke(nameof(ResetTrigger), 0.1f);
            }
        }
        else
        {
            animator.SetBool(pIsAttacking, false);
        }
    }

    //called from event trigger in anim clip
    private void FastAttack()
    {
        float coolDown = attackData.fastAttackCooldown;
        float launchForce = attackData.fastSpellThrowForce;
        int cost = attackData.fastSpellCost;

        readyToAttack = false;

        float cameraPitch = mainCamera.transform.eulerAngles.x;
        if (cameraPitch > 180f) cameraPitch -= 360f;
        float clampedPitch = Mathf.Clamp(cameraPitch, -5f, 5f); //+ve is down, -ve is up
        float horizontalFacing = transform.eulerAngles.y;
        Quaternion launchRotation = Quaternion.Euler(clampedPitch, horizontalFacing, 0f);

        GameObject projectile = Instantiate(fastSpellPrefab, attackPoint.position, launchRotation);
        Rigidbody projectileRB = projectile.GetComponent<Rigidbody>();

        Vector3 forceToAdd = projectile.transform.forward * launchForce + transform.up * 0f;
        projectileRB.AddForce(forceToAdd, ForceMode.Impulse);

        stamina -= cost;
        hudManager.SetStamina(stamina);

        Vector3 sightDirection = launchRotation * Vector3.forward;
        Vector3 debugEndPoint = throwPoint.position + (sightDirection * 50f);

        Debug.DrawLine(throwPoint.position, debugEndPoint, Color.green, 2.0f);

        Invoke(nameof(ResetThrow), coolDown);
    }

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

    //called from slow and fast attack
    private void ResetThrow()
    {
        readyToAttack = true;
    }

    //called from update
    private void ResetTrigger()
    {
        animator.ResetTrigger(pFastSpell);
        animator.ResetTrigger(pSlowSpell);
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