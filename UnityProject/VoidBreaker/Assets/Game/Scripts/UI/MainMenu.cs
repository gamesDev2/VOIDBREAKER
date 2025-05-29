using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainMenuPanel;
    public GameObject enterNameSection;
    public TMP_InputField nameInputField;
    public Button startGameConfirmButton;
    public SettingsUI settingsUI;

    [Header("Idle Video Settings")]
    public GameObject videoPlayerObject; // Contains the VideoPlayer component (do NOT disable this)
    public GameObject videoScreenObject; // The RawImage or canvas showing the video (enable/disable this)
    public float idleThreshold = 20f;

    private float idleTimer = 0f;
    private bool videoPlaying = false;
    private VideoPlayer videoPlayer;

    private void Start()
    {
        if (Game_Manager.Instance != null)
            Game_Manager.SetCursorLocked(false);

        ShowMainMenu();

        if (enterNameSection != null)
            enterNameSection.SetActive(false);

        if (startGameConfirmButton != null)
            startGameConfirmButton.onClick.AddListener(OnNameConfirmed);

        if (videoPlayerObject != null)
        {
            videoPlayer = videoPlayerObject.GetComponent<VideoPlayer>();
        }

        if (videoScreenObject != null)
        {
            videoScreenObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (videoPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StopIdleVideo();
            }
            return;
        }

        if (Input.anyKey || Input.mouseScrollDelta != Vector2.zero)
        {
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.unscaledDeltaTime;

            if (idleTimer >= idleThreshold)
            {
                PlayIdleVideo();
            }
        }
    }

    private void PlayIdleVideo()
    {
        videoPlaying = true;

        enterNameSection?.SetActive(false); // Hide name entry if visible

        if (videoScreenObject != null)
            videoScreenObject.SetActive(true);

        if (videoPlayer != null)
        {
            videoPlayer.Stop(); // Reset video
            videoPlayer.Play();
        }
    }

    private void StopIdleVideo()
    {
        videoPlaying = false;
        idleTimer = 0f;

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoScreenObject != null)
            videoScreenObject.SetActive(false);

        enterNameSection?.SetActive(false); // Keep name entry hidden after video
        ShowMainMenu(); // Show/honor regular menu state
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void StartGame()
    {
        if (enterNameSection != null)
            enterNameSection.SetActive(true);
        idleTimer = 0f;
    }

    private void OnNameConfirmed()
    {
        string enteredName = nameInputField?.text.Trim();

        if (!string.IsNullOrEmpty(enteredName))
        {
            Game_Manager.Instance.playerName = enteredName;
            LoadingScreen.LoadScene("IntroCutscene");
        }
        else
        {
            Debug.LogWarning("Player name is empty. Cannot start game.");
        }
    }

    public void Settings()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        settingsUI.ShowSettings(mainMenuPanel);
        idleTimer = 0f;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
