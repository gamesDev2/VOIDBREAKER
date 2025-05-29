using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardScene : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Container that holds all the leaderboard entries.")]
    public Transform entryContainer;

    [Tooltip("Prefab for a single leaderboard entry (must have two TMP_Text components).")]
    public GameObject entryPrefab;

    [Header("Scene Transition Settings")]
    [Tooltip("Name of the scene to load after the leaderboard.")]
    public string nextSceneName = "CreditsScene";

    [Tooltip("Time in seconds before automatically transitioning.")]
    public float delayBeforeTransition = 10f;

    private float timer = 0f;
    private bool transitioning = false;

    private void Start()
    {
        PopulateLeaderboard();
    }

    private void Update()
    {
        if (transitioning) return;

        timer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || timer >= delayBeforeTransition)
        {
            transitioning = true;
            LoadingScreen.LoadScene(nextSceneName);
        }
    }

    private void PopulateLeaderboard()
    {
        if (HighScoreManager.Instance == null || HighScoreManager.Instance.highScores == null)
            return;

        // Clear any existing entries
        foreach (Transform child in entryContainer)
        {
            Destroy(child.gameObject);
        }

        List<HighScoreEntry> scores = HighScoreManager.Instance.highScores.scores;

        foreach (HighScoreEntry entry in scores)
        {
            GameObject newEntry = Instantiate(entryPrefab, entryContainer);

            TMP_Text[] texts = newEntry.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = entry.playerName;
                texts[1].text = FormatTime(entry.completionTime);
            }
        }
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        int millis = Mathf.FloorToInt((seconds * 1000f) % 1000f);
        return $"{minutes:00}:{secs:00}.{millis:000}";
    }
}
