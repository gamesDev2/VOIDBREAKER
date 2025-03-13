using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HUDController : MonoBehaviour
{
    [Header("HUD Elements")]
    public Image[] healthSegments;
    public Image[] staminaSegments;

    private int maxHealth = 10;
    private int maxStamina = 10;
    private int currentHealth;
    private int currentStamina;

    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int> OnStaminaChanged;

    private void OnEnable()
    {
        OnHealthChanged.AddListener(SetHealth);
        OnStaminaChanged.AddListener(SetStamina);
    }

    private void OnDisable()
    {
        OnHealthChanged.RemoveListener(SetHealth);
        OnStaminaChanged.RemoveListener(SetStamina);
    }

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        UpdateBar(healthSegments, currentHealth, maxHealth);
        UpdateBar(staminaSegments, currentStamina, maxStamina);

    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateBar(healthSegments, currentHealth, maxHealth);
    }

    public void SetStamina(int value)
    {
        currentStamina = Mathf.Clamp(value, 0, maxStamina);
        UpdateBar(staminaSegments, currentStamina, maxStamina);
    }

    void UpdateBar(Image[] segments, int currentValue, int maxValue)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].enabled = (i < currentValue);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
