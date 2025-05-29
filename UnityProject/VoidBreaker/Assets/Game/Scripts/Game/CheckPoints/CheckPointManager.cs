using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;

public class CheckPointManager : MonoBehaviour
{
    public static int checkPointIndex = -1;

    public CheckPoint[] checkPointsInstance;

    public static CheckPoint[] checkPoints;

    private static LevelState levelStateManager;
    private static string settingsPath;

    void Start()
    {
        levelStateManager = GetComponent<LevelState>();

        checkPoints = new CheckPoint[checkPointsInstance.Length];
        for (int i = 0; i < checkPointsInstance.Length; i++) 
        {
            checkPoints[i] = checkPointsInstance[i];
            checkPoints[i].CheckPointIndex = i;
        }

        settingsPath = Path.Combine(Application.persistentDataPath, "checkPoint.json");
        Debug.Log(settingsPath);
        if (File.Exists(settingsPath))
        {
            string json = File.ReadAllText(settingsPath);
            levelStateManager.convertJsonToLevel(json);
        }
        else
        {
            var sr = File.CreateText(settingsPath);
            sr.Close();
            string json = levelStateManager.convertLevelToJSON();
            File.WriteAllTextAsync(settingsPath, json);
        }
    }
    
    static public void updateCheckPoint(int checkPoint)
    {
        if (checkPoint > checkPointIndex)
        {
            checkPointIndex = checkPoint;
            string json = levelStateManager.convertLevelToJSON();
            File.WriteAllTextAsync(settingsPath, json);
        }
    }

    static public void newGame()
    {
        File.Delete(Path.Combine(Application.persistentDataPath, "checkPoint.json"));
    }
}
