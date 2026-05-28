using System;
using UnityEngine;
using UnityEngine.UI;
public class S_PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private Animator animator;
    

    [SerializeField] private S_HealthBar healthBar;
    [SerializeField] private S_StaminaBar staminaBar;

    private int maxHealth;
    [SerializeField] private int currentHealth;

    private InputSystem_Actions actions;
    private void Awake()
    {
        actions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        actions.Player.Enable();
    }

    private void OnDisable()
    {
        actions.Player.Disable();
    }

    private void Start()
    {
        maxHealth = playerData.maxHealth;

        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    private void Update()
    {
        if (actions.Player.Debug.WasPressedThisFrame())
        {
            Takedamage(20);
        }
    }

    private void Takedamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);

        if(currentHealth <= 0)
        {
            animator.SetBool("IsDead", true);
            playerData.canMove = false;
            playerData.isAlive = false;
        }
    }
}
