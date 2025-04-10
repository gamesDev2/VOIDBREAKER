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

    //constructing a public class that inherits from unity event and holds 2 data types,both string because unity events can only hold 1 data type(for some reason)
    [System.Serializable] public class InteractEvent : UnityEvent<bool, string> { }

    public GameObject player;
    public UI_Manager uiManager;

    public UnityEvent<bool> on_mesh_trail = new UnityEvent<bool>();
    public UnityEvent<float> on_health_changed = new UnityEvent<float>();
    public UnityEvent<float> on_energy_changed = new UnityEvent<float>();
    public InteractEvent on_interact = new InteractEvent();
    public UnityEvent<bool> on_game_over = new UnityEvent<bool>();
    public UnityEvent<bool> on_game_won = new UnityEvent<bool>();
    public UnityEvent<bool> on_game_paused = new UnityEvent<bool>();
    public UnityEvent<bool> on_game_started = new UnityEvent<bool>();
}
