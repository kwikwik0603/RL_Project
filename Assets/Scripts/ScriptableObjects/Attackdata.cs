using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    [Title("Stamina")]
    public int playerStamina = 100;
    public int bossStamina = 100;

    [Title("Fast Spell")]
    public float fastSpellDamage = 5f;
    public float fastSpellThrowForce = 50f;
    public int   fastSpellCost = 2;
    public float fastAttackCooldown = 1f;

    [Title("Slow Spell")]
    public float slowBaseDamage = 15f;
    public float slowExplosionRadius = 5f;
    public float slowExplosionForce = 1000f;
    public float slowThrowForce = 7.5f;
    public float slowThrowUpwardForce = 10f;
    public int   slowThrowCost = 5;
    public float slowThrowCooldown = 3f;

    [Title("Boss Attacks")]
    public float attackRange = 2f; //need to increase, 2 is too small
    public float lightAttackDamage = 5f;
    public float heavyAttackDamage = 15f;
    public int heavyAttackCooldown = 3;

}
