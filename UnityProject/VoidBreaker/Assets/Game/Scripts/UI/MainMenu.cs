using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public SettingsUI settingsUI;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel == null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    public void StartGame()
    {
        LoadingScreen.LoadScene("Dev_2");
    }

    public void NewGame()
    {
        LoadingScreen.LoadScene("Dev_2");
    }

    public void Settings()
    {
        mainMenuPanel.SetActive(false);
        settingsUI.ShowSettings(mainMenuPanel);
    }

    public void Quit()
    {
        Application.Quit();
    }

    
    
}
