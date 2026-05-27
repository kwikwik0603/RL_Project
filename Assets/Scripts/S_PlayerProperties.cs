using System;
using UnityEngine;
using UnityEngine.UI;
public class S_PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;

    [SerializeField] private S_HealthBar healthBar;
    [SerializeField] private S_StaminaBar staminaBar;

    private void Start()
    {
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Takedamage(20);
        }
    }

    private void Takedamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
    }
}
