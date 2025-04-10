using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public class HUDController : MonoBehaviour
{
    [Header("HUD Elements")]
    public Image healthBarImage;
    public Image energyBarImage;
    public Image healthBackgroundImage;
    public Image energyBackgroundImage;
    public TextMeshProUGUI interact_text;
    public Image InteractWindow;
    public Color BarBackgroundColor = Color.gray;
    public Color FlashBackgroundColor = Color.red;


    private float healthBarValue = 100f; // 100 means full health
    private float energyBarValue = 100f; // 100 means full energy
    private float healthBarTargetValue = 100f; // 100 means full health
    private float staminaBarTargetValue = 100f; // 100 means full energy
    private bool isHealthBarFlashing = false;
    private bool isEnergyBarFlashing = false;

    // Start is called before the first frame update
    void Start()
    {
        if (healthBarImage == null || energyBarImage == null || interact_text == null)
        {
            Debug.LogError("HUDController: Missing references in the inspector.");
            return;
        }
        if (Game_Manager.Instance != null)
        {
            // Subscribe to the events from the Game_Manager
            Game_Manager.Instance.on_health_changed.AddListener(UpdateHealthBar);
            Game_Manager.Instance.on_energy_changed.AddListener(UpdateEnergyBar);
            Game_Manager.Instance.on_interact.AddListener(UpdateInteractText);

            //set the interact window to inactive
            InteractWindow.gameObject.SetActive(false);
        }
    }

    //function that uses the DOTween library to animate the health bar image type fill amount given a value
    public void UpdateHealthBar(float value)
    {
        healthBarTargetValue = value;
        healthBarImage.DOFillAmount(healthBarTargetValue / 100f, 0.5f).SetEase(Ease.OutCubic);

        if (healthBarTargetValue < 50f && !isHealthBarFlashing)
        {
            StartCoroutine(FlashBar(healthBackgroundImage, healthBarTargetValue, isHealthBarFlashing));
        }
        else if (healthBarTargetValue >= 50f && isHealthBarFlashing)
        {
            StopCoroutine(FlashBar(healthBackgroundImage, healthBarTargetValue, isHealthBarFlashing));
            healthBackgroundImage.DOColor(BarBackgroundColor, 0.5f).SetEase(Ease.OutCubic);
            isHealthBarFlashing = false;
        }
    }

    //function that uses the DOTween library to animate the energy bar image type fill amount given a value
    public void UpdateEnergyBar(float value)
    {
        energyBarValue = value;
        energyBarImage.DOFillAmount(energyBarValue / 100f, 0.5f).SetEase(Ease.OutCubic);

        if (energyBarValue < 50f && !isEnergyBarFlashing)
        {
            StartCoroutine(FlashBar(energyBackgroundImage, energyBarValue, isEnergyBarFlashing));
        }
        else if (energyBarValue >= 50f && isEnergyBarFlashing)
        {
            StopCoroutine(FlashBar(energyBackgroundImage, energyBarValue, isEnergyBarFlashing));
            energyBackgroundImage.DOColor(BarBackgroundColor, 0.5f).SetEase(Ease.OutCubic);
            isEnergyBarFlashing = false;
        }
    }

    //function that updates the interact text and button text
    public void UpdateInteractText(bool show, string text)
    {
        if (interact_text != null)
        {
            InteractWindow.gameObject.SetActive(show);

            interact_text.text = text;
        }
        else
        {
            Debug.LogError("HUDController: Missing references in the inspector.");
        }
    }

    private IEnumerator FlashBar(Image barImage, float targetValue,bool isFlashing)
    {
        while (targetValue < 50f && isFlashing)
        {
            //tween the color of the bar image to the flash color and back to the original color
            barImage.DOColor(FlashBackgroundColor, 0.5f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.5f);
            barImage.DOColor(BarBackgroundColor, 0.5f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.5f);
        }
        isFlashing = false;
    }
}
