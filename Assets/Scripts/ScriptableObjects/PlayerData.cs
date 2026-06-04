using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Title ("Player Data")]
    public int maxHealth = 100;
    public float walkSpeed = 1.2f;
    public float sprintSpeed = 4.5f;
    public float jumpHeight = 1.5f;
    public LayerMask GroundLayerMask;
    public bool canMove = true;
    public bool isAlive = true;

}
