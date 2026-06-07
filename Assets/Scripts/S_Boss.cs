
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class S_Boss : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private BossData bossData;
    [SerializeField] private AttackData attackData;
    [SerializeField] private CharacterController charC;
    [SerializeField] private float walkRange = 6f;
    [SerializeField] private float orbitRadius = 5f;
    [SerializeField] private float directionChangeCoolDown = 4;

    [Header("Debug - Change this while playing!")]
    [Tooltip("0 = Move Towards, 1 = Move Away")]
    public int currentAction = 0;

    [SerializeField] private Vector3 moveDelta;

    private Animator anim;
    private float walkSpeed;
    private float sprintSpeed;
    private float movementSpeed;
    private float orbitDir = 1f;
    private float timer = 0;

    public float currentHealth {  get; private set; }
    public int heavyCooldown { get; private set; }
    public int DodgeFrames { get; private set; }
    public bool isBlocking { get; private set; }

    private float maxHealth;

    private void Start()
    {
        anim = GetComponent<Animator>();
        currentHealth = bossData.maxHealth;
        walkSpeed = bossData.walkSpeed;
        sprintSpeed = bossData.sprintSpeed;
    }

    private void Update()
    {
        ApplyActions();
    }

    private void ApplyActions()
    {
        //distance to player
        Vector3 bossPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(player.position.x, 0, player.position.z);
        float distanceToPlayer = Vector3.Distance(bossPos, playerPos);
        //movementSpeed = distanceToPlayer >= walkRange ? sprintSpeed : walkSpeed;

        //direction to player
        Vector3 directionToPlayer = (playerPos - bossPos).normalized;
        if(directionToPlayer.sqrMagnitude == 0)
        {
            directionToPlayer = transform.forward;
        }

        //rotate boss to player
        Quaternion targetRot = Quaternion.LookRotation(directionToPlayer);
        float angleDiff = Quaternion.Angle(transform.rotation, targetRot);
        if(angleDiff > 5)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, Time.deltaTime * 90f);
            return;
        }
        transform.rotation = targetRot;

        moveDelta = Vector3.zero;

        switch (currentAction)
        {
            case 0: //idle - stay in one place
                moveDelta = Vector3.zero;
                anim.SetFloat("Ver", 0f);
                anim.SetFloat("Hor", 0f);
                break;
            case 1: //move towards player
                moveDelta = MoveTowardsPlayer(distanceToPlayer);
                anim.SetFloat("Hor", 0f);

                break;
            case 2: // move away from player
                moveDelta = MoveAwayFromPlayer();
                anim.SetFloat("Hor", 0f);
                break;
            case 3:
                moveDelta = OrbitPlayer(distanceToPlayer, directionToPlayer);

                break;
            default: // default case
                Debug.Log("No Action");
                anim.SetFloat("Ver", 0f);
                anim.SetFloat("Hor", 0f);
                break;
        }

        if(moveDelta != Vector3.zero)
        {
            charC.Move(moveDelta * Time.deltaTime);
        }
    }

    private Vector3 OrbitPlayer(float distanceToPlayer, Vector3 directionToPlayer)
    {

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            if (Random.value < 0.01f)
            {
                orbitDir *= -1f;
                timer = directionChangeCoolDown;
            }
        }


        if(orbitDir < 0f)
        {
            anim.SetFloat("Hor", 1f);
        }
        else if(orbitDir > 0f)
        {
            anim.SetFloat("Hor", -1f);
        }

        Vector3 tangent = new Vector3(-directionToPlayer.z, 0, directionToPlayer.x) * orbitDir;

        float distanceError = distanceToPlayer - orbitRadius;
        Vector3 radialCorrection = directionToPlayer * distanceError * 2f;

        Vector3 desiredVelocity = (tangent * walkSpeed) + radialCorrection;

        if (desiredVelocity.magnitude > walkSpeed)
        {
            desiredVelocity = desiredVelocity.normalized * walkSpeed;
        }

        return desiredVelocity;
    }

    private Vector3 MoveAwayFromPlayer()
    {
        anim.SetFloat("Ver", -1f);
        return -transform.forward * walkSpeed;
        
    }

    private Vector3 MoveTowardsPlayer(float distanceToPlayer)
    {
        anim.SetFloat("Ver", 1f);
        if (distanceToPlayer > attackData.attackRange)
        {
            return transform.forward * walkSpeed;
        }
        return Vector3.zero;
    }


    private void OnDrawGizmos()
    {
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, walkRange);
        }
    }
}
