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

    [Header("Keypad UI Elements")]
    [Tooltip("The overall keypad panel (show/hide).")]
    public Image KeypadWindow;
    [Tooltip("Background behind the code‐input field (this flashes).")]
    public Image CodeInputBackground;
    [Tooltip("Text element that shows the digits the player has typed.")]
    public TextMeshProUGUI keypadInputText;
    [Tooltip("Buttons 0–9 in order (index 0 ⇒ digit “0”).")]
    public Button[] keypadButtons;
    public Button keypadDeleteButton;
    public Button keypadSubmitButton;
    public Color CodeWindowCorrectColor = Color.green;
    public Color CodeWindowIncorrectColor = Color.red;

    [Header("HUD Sway Settings")]
    [Tooltip("The RectTransform of the main HUD container.")]
    public RectTransform mainContainer;
    [Tooltip("Multiplier for how much the HUD sways in response to the player's velocity.")]
    public float swayIntensity = 5f;
    [Tooltip("Smoothing factor for the HUD sway movement.")]
    public float swaySmoothing = 3f;

    // Internal state for sway
    private Vector2 currentSwayOffset = Vector2.zero;

    // Internal state for flashing bars
    private float healthBarTargetValue = 100f;
    private float energyBarTargetValue = 100f;
    private bool isHealthFlashing = false;
    private bool isEnergyFlashing = false;
    public Color BarBackgroundColor = Color.gray;
    public Color FlashBackgroundColor = Color.red;

    // Keypad state
    private string _currentInput = "";
    private Color _defaultCodeWindowColor;

    void Start()
    {
        // ——— HUD setup ———
        if (healthBarImage == null || energyBarImage == null ||
            healthBackgroundImage == null || energyBackgroundImage == null ||
            interact_text == null || InteractWindow == null)
        {
            Debug.LogError("HUDController: Missing HUD references.");
            return;
        }

        if (Game_Manager.Instance != null)
        {
            Game_Manager.Instance.on_health_changed.AddListener(UpdateHealthBar);
            Game_Manager.Instance.on_energy_changed.AddListener(UpdateEnergyBar);
            Game_Manager.Instance.on_interact.AddListener(UpdateInteractText);
        }
        InteractWindow.gameObject.SetActive(false);

        // ——— Keypad setup ———
        if (KeypadWindow == null || CodeInputBackground == null ||
            keypadInputText == null || keypadButtons == null ||
            keypadButtons.Length == 0 ||
            keypadDeleteButton == null || keypadSubmitButton == null)
        {
            Debug.LogError("HUDController: Missing keypad references.");
            return;
        }

        // Cache default code‐window color
        _defaultCodeWindowColor = CodeInputBackground.color;

        // Hide keypad at start
        KeypadWindow.gameObject.SetActive(false);

        // Listen for show/hide keypad events
        Game_Manager.Instance.on_keypad_shown.AddListener(OnKeypadShown);

        // Wire up digit buttons
        for (int i = 0; i < keypadButtons.Length; i++)
        {
            int digit = i;  // capture in closure
            keypadButtons[i].onClick.AddListener(() => OnDigitPressed(digit));
        }
        keypadDeleteButton.onClick.AddListener(OnDeletePressed);
        keypadSubmitButton.onClick.AddListener(OnSubmitPressed);
    }

    void Update()
    {
        UpdateSway();
    }

    private void UpdateSway()
    {
        if (Game_Manager.Instance?.player != null && mainContainer != null)
        {
            Rigidbody rb = Game_Manager.Instance.player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 vel = rb.velocity;
                Vector2 desired = new Vector2(vel.x, vel.z) * swayIntensity;
                currentSwayOffset = Vector2.Lerp(currentSwayOffset, desired, Time.deltaTime * swaySmoothing);
                mainContainer.DOAnchorPos(currentSwayOffset, 0.1f).SetEase(Ease.Linear);
            }
        }
    }

    // ——— Health & Energy Bars ———

    public void UpdateHealthBar(float value)
    {
        healthBarTargetValue = value;
        healthBarImage.DOFillAmount(value / 100f, 0.5f).SetEase(Ease.OutCubic);

        if (value < 50f && !isHealthFlashing)
        {
            isHealthFlashing = true;
            StartCoroutine(FlashBarCoroutine(
                healthBackgroundImage,
                () => healthBarTargetValue < 50f,
                () => isHealthFlashing = false));
        }
        else if (value >= 50f)
        {
            isHealthFlashing = false;
            healthBackgroundImage.color = BarBackgroundColor;
        }
    }

    public void UpdateEnergyBar(float value)
    {
        energyBarTargetValue = value;
        energyBarImage.DOFillAmount(value / 100f, 0.5f).SetEase(Ease.OutCubic);

        if (value < 50f && !isEnergyFlashing)
        {
            isEnergyFlashing = true;
            StartCoroutine(FlashBarCoroutine(
                energyBackgroundImage,
                () => energyBarTargetValue < 50f,
                () => isEnergyFlashing = false));
        }
        else if (value >= 50f)
        {
            isEnergyFlashing = false;
            energyBackgroundImage.color = BarBackgroundColor;
        }
    }

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

    // ——— Interaction Prompt ———

    public void UpdateInteractText(bool show, string text)
    {
        InteractWindow.gameObject.SetActive(show);
        interact_text.text = text;
    }

    // ——— Keypad Handlers ———

    private void OnKeypadShown(bool show)
    {
        KeypadWindow.gameObject.SetActive(show);
        Game_Manager.SetCursorLocked(!show);

        // Reset input & code‐window color
        _currentInput = "";
        keypadInputText.text = "";
        CodeInputBackground.color = _defaultCodeWindowColor;
    }

    private void OnDigitPressed(int digit)
    {
        if (_currentInput.Length < 10)
        {
            _currentInput += digit.ToString();
            keypadInputText.text = _currentInput;
        }
    }

    private void OnDeletePressed()
    {
        if (_currentInput.Length > 0)
        {
            _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
            keypadInputText.text = _currentInput;
        }
    }

    private void OnSubmitPressed()
    {
        var gm = Game_Manager.Instance;
        var console = gm?.activeConsole;
        if (console == null) return;

        bool correct = console.ValidateCode(_currentInput);
        if (correct)
        {
            // Flash code‐window green, then hide keypad
            PulseCodeWindow(CodeWindowCorrectColor);
            DOVirtual.DelayedCall(1f, () =>
            {
                gm.on_keypad_shown.Invoke(false);
                gm.activeConsole = null;
            });
        }
        else
        {
            // Flash code‐window red, clear input
            PulseCodeWindow(CodeWindowIncorrectColor);
            _currentInput = "";
            keypadInputText.text = "";
        }
    }

    private void PulseCodeWindow(Color target)
    {
        CodeInputBackground
            .DOColor(target, 0.15f)
            .SetLoops(4, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}
