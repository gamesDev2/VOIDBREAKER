using UnityEngine;

public class Game_Manager : MonoBehaviour
{
    public static Game_Manager Instance { get; private set; }

    private void Awake()
    {
        // If an instance already exists and it's not this, destroy this duplicate.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Otherwise, assign this as the Singleton instance.
        Instance = this;

        // Optional: Make this object persist across scene loads.
        DontDestroyOnLoad(gameObject);
    }

    // Add your Singleton methods and properties below.
    //store the reference to the player and ui manager
    public GameObject player;
    public UI_Manager uiManager;

}
