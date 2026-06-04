using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    [Title("Attack Data")]
    public int maxStamina;

    [Title("Fast Attack")]
    public float fastDamage;
    public float fastThrowForce;
    public float fastThrowUpwardForce;
    public int   fastThrowCost;
    public float fastAttackCooldown;

    [Title("Slow Attack")]
    public float slowBaseDamage;
    public float slowExplosionRadius;
    public float slowExplosionForce;
    public float slowThrowForce;
    public float slowThrowUpwardForce;
    public int   slowThrowCost;
    public float slowThrowCooldown;

}
