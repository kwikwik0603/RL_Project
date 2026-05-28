using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    [Title("Attack Data")]
    public int maxStamina;

    [Title("Fast Attack")]
    public float fastAttackCooldown;
    public float fastThrowForce;
    public float fastThrowUpwardForce;
    public int fastThrowCost;

    [Title("Slow Attack")]
    public float slowThrowCooldown;
    public float slowThrowForce;
    public float slowThrowUpwardForce;
    public int slowThrowCost;

}
