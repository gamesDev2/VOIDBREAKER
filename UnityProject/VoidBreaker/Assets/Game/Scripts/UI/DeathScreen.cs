using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathScene : MonoBehaviour
{
    [SerializeField] private Button loadLastCheckpointButton;
    [SerializeField] private Button loadMainMenuButton;

    void Start()
    {
        loadLastCheckpointButton.onClick.AddListener(LoadLastCheckpoint);
        loadMainMenuButton.onClick.AddListener(LoadMainMenu);
    }


    private void LoadLastCheckpoint()
    {
        LoadingScreen.LoadScene("Dev_2");
    }

    private void LoadMainMenu()
    {
        LoadingScreen.LoadScene("MainMenu");
    }
}
