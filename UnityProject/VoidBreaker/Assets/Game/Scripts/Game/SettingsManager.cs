using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Rendering;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    public UserSettings userSettings;

    private string settingsPath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            settingsPath = Path.Combine(Application.persistentDataPath, "usersettings.json");
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadSettings()
    {
        if (File.Exists(settingsPath))
        {
            string json = File.ReadAllText(settingsPath);
            userSettings = JsonUtility.FromJson<UserSettings>(json);
        }
        else
        {
            userSettings = new UserSettings();
        }
    }

    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(userSettings, true);
        File.WriteAllText(settingsPath, json);
    }
}
