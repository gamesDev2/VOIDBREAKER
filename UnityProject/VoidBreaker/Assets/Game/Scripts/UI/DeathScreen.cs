using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeathScene : MonoBehaviour
{
    [SerializeField] private Button loadLastCheckpointButton;
    [SerializeField] private Button loadMainMenuButton;
    [SerializeField] private CanvasGroup DeathCanvas;

    public float fadeInTime;

    void Start()
    {
        loadLastCheckpointButton.onClick.AddListener(LoadLastCheckpoint);
        loadMainMenuButton.onClick.AddListener(LoadMainMenu);
        StartCoroutine(FadeIn());
    }


    private void LoadLastCheckpoint()
    {
        LoadingScreen.LoadScene("Dev_2");
    }

    private void LoadMainMenu()
    {
        LoadingScreen.LoadScene("MainMenu");
    }


    IEnumerator FadeIn()
    {
        float time = 0f;
        float fade = 0f;

        while (time < fadeInTime)
        {
            time += Time.deltaTime;

            fade = time / fadeInTime;

            DeathCanvas.alpha = fade;

            yield return null;
        }

        Game_Manager.SetCursorLocked(false);
    }
}
