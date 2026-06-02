using UnityEngine;

public class S_FastSpell : MonoBehaviour
{
    [SerializeField] private AttackData attackData;
    [SerializeField] private float projectileLifetime = 6;
    private float baseDamage;

    void Start()
    {
        Destroy(gameObject, projectileLifetime);
        baseDamage = attackData.fastDamage;

    }

    private void OnCollisionEnter(Collision collision)
    {
        DealDamage(collision.gameObject);
    }

    private void DealDamage(GameObject hitObject)
    {
        S_Health health = hitObject.GetComponent<S_Health>();
        if(health != null)
        {
            health.TakeDamage(baseDamage);
        }
        Destroy(gameObject, 2f);
    }
}
