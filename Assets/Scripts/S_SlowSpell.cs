using UnityEngine;

public class S_SlowSpell : MonoBehaviour
{
    [SerializeField] private AttackData attackData;
    [SerializeField] private float projectileLifetime = 6;
    private float baseDamage;
    private float explosionRadius;
    private float explosionForce;

    void Start()
    {
        Destroy(gameObject, projectileLifetime);
        baseDamage = attackData.slowBaseDamage;
        explosionRadius = attackData.slowExplosionRadius;
        explosionForce = attackData.slowExplosionForce;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    private void Explode()
    {
        Collider[] collidersInsideExplosion = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach(Collider nearbyObject in collidersInsideExplosion)
        {
            Rigidbody rbNearby = nearbyObject.GetComponent<Rigidbody>();
            if (rbNearby != null)
            {
                rbNearby.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            DealDamage(nearbyObject);
        }

        Destroy(gameObject);
    }

    private void DealDamage(Collider nearbyObject)
    {
        S_Health health = nearbyObject.GetComponent<S_Health>();
        if (health != null)
        {
            float dist = Vector3.Distance(transform.position, nearbyObject.transform.position);
            float damageProportion = 1 - (dist / explosionRadius);
            float finalDamage = damageProportion * Mathf.Max(0, damageProportion);

            health.TakeDamage(finalDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

}
