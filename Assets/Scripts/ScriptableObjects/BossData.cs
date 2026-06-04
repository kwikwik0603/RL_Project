using UnityEngine;

[CreateAssetMenu(fileName = "BossData", menuName = "Scriptable Objects/BossData")]
public class BossData : ScriptableObject
{
    public int maxHealth = 100;
    public float walkSpeed = 1.2f;
    public float sprintSpeed = 4.5f;

    public float jumpHeight;
    public LayerMask GroundLayerMask;
    public bool canMove = true;
    public bool isAlive = true;
}
