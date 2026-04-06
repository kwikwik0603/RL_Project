using UnityEngine;

public class S_NPCInteraction : MonoBehaviour
{
    [Header ("Player Detect")]
    [SerializeField] private float detectRange;
    [SerializeField] private LayerMask playerLayer;


    [Header ("Player Facing")]
    [SerializeField] private float maxLookAngle;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private GameObject NPCBody;


    [Header("Player Interaction")]
    [SerializeField] private GameObject UICanvas;


    private Collider[] results = new Collider[1];


    void Awake()
    {
        UICanvas.SetActive(false);
    }
    void Start()
    {
        Debug.Log("Started");
    }


    void Update()
    {
        DetectPlayer();
    }

    void DetectPlayer()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectRange, results, playerLayer);

        if (count > 0)
        {
            Collider col = results[0];
            Debug.Log("Detected");

            S_PlayerController pController = col.GetComponentInParent<S_PlayerController>();

            if(pController != null)
            {
                Vector3 direction = pController.transform.position - transform.position;
                Vector3 npcForward = transform.forward;
                float angle = Vector3.Angle(npcForward, direction);

                if (angle < maxLookAngle && angle != 0)
                {
                    Debug.Log(angle);
                    
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    NPCBody.transform.rotation = Quaternion.Slerp(NPCBody.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

                    UICanvas.SetActive(true);
                    UICanvas.transform.LookAt(col.transform.position);
                }
                else
                {
                    NPCBody.transform.rotation = Quaternion.Slerp(NPCBody.transform.rotation, transform.rotation, Time.deltaTime * rotationSpeed);
                    UICanvas.SetActive(false);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectRange);

    }
}
