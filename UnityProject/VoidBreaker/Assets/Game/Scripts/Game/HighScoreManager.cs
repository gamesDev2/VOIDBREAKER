using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class HighScoreEntry
{
    public string playerName;
    public float completionTime;
}

[System.Serializable]
public class HighScoreList
{
    public List<HighScoreEntry> scores = new List<HighScoreEntry>();
}

public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance;

    private static readonly string resourcePath = "HighScoreData/HighScore"; // inside Resources
    private string filePath;

    public HighScoreList highScores = new HighScoreList();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            filePath = Path.Combine(Application.persistentDataPath, "HighScore/HighScore.json");

            if (!LoadFromFile())
            {
                LoadFromResources();
                SaveToFile(); // persist initial resource data to disk
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddScore(string name, float time)
    {
        highScores.scores.Add(new HighScoreEntry { playerName = name, completionTime = time });
        highScores.scores.Sort((a, b) => a.completionTime.CompareTo(b.completionTime));
        SaveToFile();
    }

    private bool LoadFromFile()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            highScores = JsonUtility.FromJson<HighScoreList>(json);
            return true;
        }
        return false;
    }

    private void LoadFromResources()
    {
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset != null)
        {
            highScores = JsonUtility.FromJson<HighScoreList>(asset.text);
        }
        else
        {
            Debug.LogWarning("HighScoreManager: No HighScoreData found in Resources. Initializing empty list.");
            highScores = new HighScoreList(); // fallback
        }
    }

    private void SaveToFile()
    {
        string dir = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        File.WriteAllText(filePath, JsonUtility.ToJson(highScores, true));
    }
}
