using UnityEngine;

public class InitManager : MonoBehaviour
{
    private static CutObjectManager cutManager;
    private static Game_Manager gameManager;
    private static PDAManager pdaManager;
    private static HighScoreManager scoreManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeManager()
    {
        if (cutManager == null && gameManager == null &&
            pdaManager == null && scoreManager == null)
        {
            GameObject go = new GameObject("Managers");

            cutManager = go.AddComponent<CutObjectManager>();
            gameManager = go.AddComponent<Game_Manager>();
            pdaManager = go.AddComponent<PDAManager>();
            scoreManager = go.AddComponent<HighScoreManager>();

            DontDestroyOnLoad(go);
        }
    }
}
