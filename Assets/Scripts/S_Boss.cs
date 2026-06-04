using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class S_Boss : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private BossData bossData;
    [SerializeField] private AttackData attackData;
    [SerializeField] private CharacterController charC;

    public float currentHealth {  get; private set; }
    public int heavyCooldown { get; private set; }
    public int DodgeFrames { get; private set; }
    public bool isBlocking { get; private set; }

    private float maxHealth;

    private void Start()
    {
        currentHealth = bossData.maxHealth;
    }

    public void ApplyAction(int action)
    {
        isBlocking = false;
    }
}
