using UnityEngine;
using UnityEngine.UI;

public class S_StaminaBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public void SetMaxStamina(int maxStamina)
    {
        slider.maxValue = maxStamina;
        slider.value = maxStamina;
    }
    public void SetStamina(int stamina)
    {
        slider.value = stamina;
    }
}
