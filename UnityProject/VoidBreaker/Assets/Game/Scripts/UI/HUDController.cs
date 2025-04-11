using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
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

    [Header("HUD Sway Settings")]
    [Tooltip("The RectTransform of the main HUD container.")]
    public RectTransform mainContainer;
    [Tooltip("Multiplier for how much the HUD sways in response to the player's horizontal velocity.")]
    public float swayIntensity = 5f;
    [Tooltip("Smoothing factor for the HUD sway movement.")]
    public float swaySmoothing = 3f;

    // Internal state for sway offset.
    private Vector2 currentSwayOffset = Vector2.zero;

    private float healthBarTargetValue = 100f;
    private float energyBarTargetValue = 100f;
    private bool isHealthFlashing = false;
    private bool isEnergyFlashing = false;

    void Start()
    {
        if (healthBarImage == null || energyBarImage == null || interact_text == null)
        {
            Debug.LogError("HUDController: Missing references in the inspector.");
            return;
        }
        if (Game_Manager.Instance != null)
        {
            Game_Manager.Instance.on_health_changed.AddListener(UpdateHealthBar);
            Game_Manager.Instance.on_energy_changed.AddListener(UpdateEnergyBar);
            Game_Manager.Instance.on_interact.AddListener(UpdateInteractText);
            InteractWindow.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Handle HUD sway logic each frame.
        UpdateSway();
    }

    private void UpdateSway()
    {
        // Ensure we have a reference to the player and a main container to move.
        if (Game_Manager.Instance != null && Game_Manager.Instance.player != null && mainContainer != null)
        {
            Rigidbody playerRb = Game_Manager.Instance.player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // Use the player's horizontal velocity (x and z) as the basis for sway,apply it to the main container using the dotween library.
                Vector3 velocity = playerRb.velocity;
                Vector2 desiredOffset = new Vector2(velocity.x, velocity.z) * swayIntensity;
                currentSwayOffset = Vector2.Lerp(currentSwayOffset, desiredOffset, Time.deltaTime * swaySmoothing);
                // Apply the sway offset to the main container.
                mainContainer.DOAnchorPos(currentSwayOffset, 0.1f).SetEase(Ease.Linear);
            }
        }
    }

    // Update the health bar fill and start/stop flashing as needed.
    public void UpdateHealthBar(float value)
    {
        healthBarTargetValue = value;
        healthBarImage.DOFillAmount(healthBarTargetValue / 100f, 0.5f).SetEase(Ease.OutCubic);

        if (healthBarTargetValue < 50f && !isHealthFlashing)
        {
            isHealthFlashing = true;
            StartCoroutine(FlashBarCoroutine(
                healthBackgroundImage,
                () => healthBarTargetValue < 50f,
                () => isHealthFlashing = false));
        }
        else if (healthBarTargetValue >= 50f)
        {
            isHealthFlashing = false;
            healthBackgroundImage.color = BarBackgroundColor;
        }
    }

    // Update the energy bar fill and start/stop flashing as needed.
    public void UpdateEnergyBar(float value)
    {
        energyBarTargetValue = value;
        energyBarImage.DOFillAmount(energyBarTargetValue / 100f, 0.5f).SetEase(Ease.OutCubic);

        if (energyBarTargetValue < 50f && !isEnergyFlashing)
        {
            isEnergyFlashing = true;
            StartCoroutine(FlashBarCoroutine(
                energyBackgroundImage,
                () => energyBarTargetValue < 50f,
                () => isEnergyFlashing = false));
        }
        else if (energyBarTargetValue >= 50f)
        {
            isEnergyFlashing = false;
            energyBackgroundImage.color = BarBackgroundColor;
        }
    }

    // Update the interaction text.
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

    // A generic coroutine that flashes a given Image between a flash color and the default background color.
    private IEnumerator FlashBarCoroutine(Image bar, Func<bool> condition, Action onExit)
    {
        while (condition())
        {
            bar.DOColor(FlashBackgroundColor, 0.5f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.5f);
            bar.DOColor(BarBackgroundColor, 0.5f).SetEase(Ease.OutCubic);
            yield return new WaitForSeconds(0.5f);
        }
        bar.DOColor(BarBackgroundColor, 0.5f).SetEase(Ease.OutCubic);
        onExit?.Invoke();
    }
}
