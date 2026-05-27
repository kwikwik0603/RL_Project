using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Title ("Player Data")]
    public int maxHealth;
    public float walkSpeed;
    public float sprintSpeed;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;


}
