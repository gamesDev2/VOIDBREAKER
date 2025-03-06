// Automatically initialize the manager on game load.
using UnityEngine;

public class InitManager : MonoBehaviour
{
    private static CutObjectManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeManager()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("CutObjectManager");
            instance = go.AddComponent<CutObjectManager>();
            DontDestroyOnLoad(go);
        }
    }
}
