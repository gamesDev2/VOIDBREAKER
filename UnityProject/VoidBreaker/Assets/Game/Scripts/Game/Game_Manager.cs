using UnityEngine;
using UnityEngine.Events;
public class Game_Manager : MonoBehaviour
{
    public static Game_Manager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Add your Singleton methods and properties below.
    // Store the reference to the player and UI manager.
    public GameObject player;
    public UI_Manager uiManager;

    public UnityEvent<bool> on_mesh_trail = new UnityEvent<bool>();
}
