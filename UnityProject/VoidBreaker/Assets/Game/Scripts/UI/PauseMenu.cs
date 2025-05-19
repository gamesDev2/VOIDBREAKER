using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuPanel;
    public SettingsUI settingsUI;
    public bool settingsActive = false;

    private bool isPaused = false;
    

    private void Start()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1.0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            if (!isPaused)
            {
                PauseGame();
            }
            else if (pauseMenuPanel.activeSelf)
            {
                ResumeGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0.0f;
        pauseMenuPanel.SetActive(true);
        Game_Manager.Instance.UnlockCursor();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1.0f;
        pauseMenuPanel.SetActive(false);
        Game_Manager.Instance.LockCursor();
    }

    public void Settings()
    {
        settingsActive = true;
        settingsUI.ShowSettings(pauseMenuPanel);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("MainMenu");
    }
}
