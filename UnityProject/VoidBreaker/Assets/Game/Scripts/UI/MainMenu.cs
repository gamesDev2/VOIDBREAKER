using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    public void NewGame()
    {
        SceneManager.LoadScene(1);
    }

    public void Settings()
    {

    }

    public void Quit()
    {
        Application.Quit();
    }

    
}
