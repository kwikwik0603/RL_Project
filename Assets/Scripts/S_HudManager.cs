using UnityEngine;
using UnityEngine.UI;


public class S_HudManager : MonoBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider staminaBar;

    public void SetMaxHealth(int maxHealth)
    {
        healthBar.maxValue = maxHealth;
        healthBar.value = maxHealth;
    }
    public void SetHealth(int health)
    {
        healthBar.value = health;
    }
    public void SetMaxStamina(int maxStamina)
    {
        staminaBar.maxValue = maxStamina;
        staminaBar.value = maxStamina;
    }
    public void SetStamina(int stamina)
    {
        staminaBar.value = stamina;
    }
}
