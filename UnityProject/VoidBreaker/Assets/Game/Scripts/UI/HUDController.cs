using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HUDController : MonoBehaviour
{
    // ---------------- HUD Elements ----------------
    [Header("HUD Elements")]
    public Image healthBarImage;
    public Image energyBarImage;
    public Image healthBackgroundImage;
    public Image energyBackgroundImage;
    public TextMeshProUGUI interact_text;
    public Image InteractWindow;
    public Image gunOverheatWindow;

    // ---------------- Keypad UI Elements ----------------
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

    // ---------------- Objective UI Elements ----------------
    [Header("Objective UI Elements")]
    public GameObject objectiveWindow;
    public TextMeshProUGUI objectiveTitleText;
    public TextMeshProUGUI objectiveDescriptionText;

    // ---------------- PDA Viewer Elements ----------------
    [Header("PDA UI Elements")]
    [Tooltip("Main PDA window to show/hide")] public Image pdaWindow;
    public TextMeshProUGUI pdaTitle;
    public TextMeshProUGUI pdaEntry;

    [Header("PDA Entry List UI")]
    [Tooltip("Container that will hold the generated entry buttons (vertical layout recommended)")]
    [SerializeField] private Transform pdaEntryButtonContainer;
    [Tooltip("Prefab used to generate buttons for each collected entry")]
    [SerializeField] private Button pdaEntryButtonPrefab;
    [Tooltip("Color for the currently‑selected entry button")] public Color selectedEntryColor = new Color(0.6f, 0f, 0f, 1f);
    [Tooltip("Color for idle / non‑selected entry buttons")] public Color normalEntryColor = Color.white;

    // ---------------- HUD Sway Settings ----------------
    [Header("HUD Sway Settings")]
    public RectTransform mainContainer;
    public float swayIntensity = 5f;
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

    // ---------------- PDA Internal State ----------------
    private class LocalPDAEntry
    {
        public string title;
        public string entry;
        public Button button;
    }

    private readonly Dictionary<string, LocalPDAEntry> _pdaEntries = new Dictionary<string, LocalPDAEntry>();
    private Button _currentSelectedButton;

    // ---------------- Fade To Black ----------------
    private float fadeTime;
    public CanvasGroup BlackScreen;

    // ---------------- MonoBehaviour ----------------
    void Start()
    {
        // —— HUD setup checks ——
        if (healthBarImage == null || energyBarImage == null ||
            healthBackgroundImage == null || energyBackgroundImage == null ||
            interact_text == null || InteractWindow == null)
        {
            Debug.LogError("HUDController: Missing HUD references.");
            return;
        }

        // —— Hook up Game‑Manager events ——
        if (Game_Manager.Instance != null)
        {
            Game_Manager.Instance.on_health_changed.AddListener(UpdateHealthBar);
            Game_Manager.Instance.on_energy_changed.AddListener(UpdateEnergyBar);
            Game_Manager.Instance.on_interact.AddListener(UpdateInteractText);
            Game_Manager.Instance.on_view_pda_entry.AddListener(OnViewPDALog);
            Game_Manager.Instance.on_objective_updated.AddListener(UpdateObjectiveText);
            Game_Manager.Instance.on_empty_fire.AddListener(OnEmptyFire);
            Game_Manager.Instance.on_fade_to_black.AddListener(FadeToBlack);
        }
        InteractWindow.gameObject.SetActive(false);

        // —— Keypad references check ——
        if (KeypadWindow == null || CodeInputBackground == null ||
            keypadInputText == null || keypadButtons == null || keypadButtons.Length == 0 ||
            keypadDeleteButton == null || keypadSubmitButton == null)
        {
            Debug.LogError("HUDController: Missing keypad references.");
            return;
        }

        // Cache default code‑window color
        _defaultCodeWindowColor = CodeInputBackground.color;

        // Hide keypad at start
        KeypadWindow.gameObject.SetActive(false);

        // Listen for show/hide keypad events
        Game_Manager.Instance?.on_keypad_shown.AddListener(OnKeypadShown);

        // Wire up digit buttons
        for (int i = 0; i < keypadButtons.Length; i++)
        {
            int digit = i; // capture in closure
            keypadButtons[i].onClick.AddListener(() => OnDigitPressed(digit));
        }
        keypadDeleteButton.onClick.AddListener(OnDeletePressed);
        keypadSubmitButton.onClick.AddListener(OnSubmitPressed);

        // Make sure PDA window starts hidden
        if (pdaWindow != null)
            pdaWindow.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateSway();

        // Handle ESC to close PDA viewer
        if (pdaWindow != null && pdaWindow.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePDAWindow();
        }
    }

    // ---------------- HUD Sway ----------------
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

    // ---------------- Health & Energy Bars ----------------
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

    // ---------------- Interaction Prompt ----------------
    public void UpdateInteractText(bool show, string text)
    {
        if (KeypadWindow.gameObject.activeSelf) return; // Don't show interact text if keypad is open
        InteractWindow.gameObject.SetActive(show);
        interact_text.text = text;
    }

    // ---------------- Keypad Handlers ----------------
    private void OnKeypadShown(bool show)
    {
        KeypadWindow.gameObject.SetActive(show);
        Game_Manager.SetCursorLocked(!show);

        // Reset input & code‑window color
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
            // Flash code‐window red, clear input and hide the keypad
            PulseCodeWindow(CodeWindowIncorrectColor);
            _currentInput = "";
            keypadInputText.text = "";
            DOVirtual.DelayedCall(1f, () =>
            {
                gm.on_keypad_shown.Invoke(false);
                gm.activeConsole = null;
            });
        }
    }

    private void PulseCodeWindow(Color target)
    {
        CodeInputBackground
            .DOColor(target, 0.15f)
            .SetLoops(4, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    // ---------------- Objective Text ----------------
    public void UpdateObjectiveText(string title, string description)
    {
        if (objectiveWindow != null) objectiveWindow.SetActive(true);
        if (objectiveTitleText != null) objectiveTitleText.text = title;
        if (objectiveDescriptionText != null) objectiveDescriptionText.text = description;
        Debug.Log($"Objective Updated: {title} - {description}");
    }

    // ---------------- Gun Overheat Handlers -------
    private bool WarningActive = false;
    public void OnEmptyFire()
    {
        if (!WarningActive)
        {
            // I hate myself for writing this
            WarningActive = true;
            Invoke("GunWarningOn", 0f);
            Invoke("GunWarningOff", 0.2f);
            Invoke("GunWarningOn", 0.4f);
            Invoke("GunWarningOff", 0.6f);
            Invoke("GunWarningOn", 0.8f);
            Invoke("GunWarningOff", 1f);
            Invoke("GunWarningFinish", 1f);
        }
    }
    private void GunWarningOn()
    {
        gunOverheatWindow.gameObject.SetActive(true);
    }

    private void GunWarningOff()
    {
        gunOverheatWindow.gameObject.SetActive(false);
    }

    private void GunWarningFinish()
    {
        WarningActive = false;
    }

    // ---------------- PDA Handlers ----------------
    private void OnViewPDALog(string title, string entry)
    {
        if (pdaWindow == null) return;

        AddOrUpdateEntry(title, entry);
        ShowEntry(title);

        pdaWindow.gameObject.SetActive(true);
        Game_Manager.SetCursorLocked(false);
    }

    private void AddOrUpdateEntry(string title, string entry)
    {
        if (_pdaEntries.TryGetValue(title, out LocalPDAEntry existing))
        {
            existing.entry = entry; // Update text if needed
            return;
        }

        // Create new button for this entry
        if (pdaEntryButtonPrefab == null || pdaEntryButtonContainer == null)
        {
            Debug.LogWarning("HUDController: Entry button prefab/container not assigned.");
            return;
        }

        Button newBtn = Instantiate(pdaEntryButtonPrefab, pdaEntryButtonContainer);
        TextMeshProUGUI txt = newBtn.GetComponentInChildren<TextMeshProUGUI>();
        //shorten the title to 20 characters and add ... if it is longer
        if (txt != null)
        {
            txt.text = title.Length > 20 ? title.Substring(0, 20) + "..." : title;
        }
        else
        {
            Debug.LogWarning("HUDController: Button prefab does not have a TextMeshProUGUI component.");
            return;
        }

        newBtn.onClick.AddListener(() => ShowEntry(title));

        LocalPDAEntry newEntry = new LocalPDAEntry
        {
            title = title,
            entry = entry,
            button = newBtn
        };
        _pdaEntries.Add(title, newEntry);

        // Reset colors so newly‑added buttons start unselected
        SetButtonColors(newBtn, normalEntryColor);
    }

    private void ShowEntry(string title)
    {
        if (!_pdaEntries.TryGetValue(title, out LocalPDAEntry data)) return;

        pdaTitle.text = data.title;
        pdaEntry.text = data.entry;

        // Highlight selected button
        if (_currentSelectedButton != null && _currentSelectedButton != data.button)
        {
            SetButtonColors(_currentSelectedButton, normalEntryColor);
        }
        _currentSelectedButton = data.button;
        SetButtonColors(_currentSelectedButton, selectedEntryColor);
    }

    private void SetButtonColors(Button btn, Color c)
    {
        if (btn == null) return;
        ColorBlock cb = btn.colors;
        cb.normalColor = c;
        cb.highlightedColor = c;
        cb.selectedColor = c;
        cb.pressedColor = c;
        btn.colors = cb;
    }

    private void ClosePDAWindow()
    {
        pdaWindow.gameObject.SetActive(false);
        Game_Manager.SetCursorLocked(true);
    }


    private void FadeToBlack(float Time)
    {
        fadeTime = Time;

        StartCoroutine(fading());
    }

    private IEnumerator fading()
    {
        float fade = 1f;
        float originalTime = fadeTime;

        while (fadeTime >= 0)
        {
            fadeTime -= Time.deltaTime;
            fade = 1f - (fadeTime / originalTime);
            BlackScreen.alpha = fade;

            yield return null;
        }
    }
}
