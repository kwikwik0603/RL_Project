using UnityEngine;

public class S_NPCInteraction : MonoBehaviour
{
    [SerializeField] private float detectRange;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private LayerMask playerLayer;

    private Collider[] results = new Collider[1];
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

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
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
