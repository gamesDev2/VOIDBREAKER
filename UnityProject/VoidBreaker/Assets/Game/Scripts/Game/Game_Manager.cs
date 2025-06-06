using UnityEngine;
using UnityEngine.Events;

public class Game_Manager : MonoBehaviour
{
    public static Game_Manager Instance { get; private set; }

    [Header("Player Info")]
    public string playerName;

    private float levelStartTime;
    public float GetElapsedTime() => Time.time - levelStartTime;

    public void StartGameTimer()
    {
        levelStartTime = Time.time;
    }

    [System.Serializable]
    public class InteractEvent : UnityEvent<bool, string> { }

    [Header("Scene References")]
    public GameObject player;
    public UI_Manager uiManager;

    [Header("Gameplay Events")]
    public UnityEvent<bool> on_mesh_trail = new UnityEvent<bool>();
    public UnityEvent<float> on_health_changed = new UnityEvent<float>();
    public UnityEvent<float> on_energy_changed = new UnityEvent<float>();
    public InteractEvent on_interact = new InteractEvent();
    public UnityEvent<bool> on_keypad_shown = new UnityEvent<bool>();
    public UnityEvent<float> on_submit_keypad_code = new UnityEvent<float>();
    public UnityEvent<bool> on_game_over = new UnityEvent<bool>();
    public UnityEvent<bool> on_game_won = new UnityEvent<bool>();
    public UnityEvent<bool> on_game_paused = new UnityEvent<bool>();
    public UnityEvent<bool> on_game_started = new UnityEvent<bool>();
    public UnityEvent on_door_console_update = new UnityEvent();
    public UnityEvent<string, string> on_view_pda_entry = new UnityEvent<string, string>();
    public UnityEvent<string, string> on_objective_updated = new UnityEvent<string, string>();
    public UnityEvent on_empty_fire = new UnityEvent();
    public UnityEvent<float> on_fade_to_black = new UnityEvent<float>();
    public UnityEvent<string> on_weapon_equipped = new UnityEvent<string>();


    // The console currently requesting a code
    [HideInInspector] public DoorConsole activeConsole;

    // The current state of the mouse cursor
    [HideInInspector] public bool cursorLocked = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // ----------------------
    // Cursor Locking Helpers
    // ----------------------

    /// <summary>
    /// Lock the mouse cursor to the center and hide it.
    /// Call from any script via Game_Manager.Instance.LockCursor().
    /// </summary>
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorLocked = true;
    }

    /// <summary>
    /// Unlock the mouse cursor (make it visible and free).
    /// Call from any script via Game_Manager.Instance.UnlockCursor().
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorLocked = false;
    }

    /// <summary>
    /// Convenience static wrapper: 
    /// Game_Manager.SetCursorLocked(true) to lock, false to unlock.
    /// </summary>
    public static void SetCursorLocked(bool locked)
    {
        if (Instance == null) return;
        if (locked) Instance.LockCursor();
        else Instance.UnlockCursor();
    }

    /// <summary>
    /// gets the current state of the cursor lock
    /// </summary>
    public static bool IsCursorLocked()
    {
        if (Instance == null) return false;
        return Instance.cursorLocked;
    }
}
