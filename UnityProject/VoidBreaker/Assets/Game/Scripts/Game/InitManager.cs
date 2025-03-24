// Automatically initialize the managers on game start.
using UnityEngine;

public class InitManager : MonoBehaviour
{
    private static CutObjectManager instance;
    private static Game_Manager gameManager;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeManager()
    {
        if (instance == null && gameManager == null)
        {
            GameObject go = new GameObject("Managers");
            instance = go.AddComponent<CutObjectManager>();
            DontDestroyOnLoad(go);

            gameManager = go.AddComponent<Game_Manager>();
            DontDestroyOnLoad(go);

        }
    }
}
