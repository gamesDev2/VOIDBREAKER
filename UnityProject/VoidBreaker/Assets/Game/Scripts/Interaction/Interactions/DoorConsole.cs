using UnityEngine;

public class DoorConsole : BaseInteractable
{
    [Header("Security")]
    [Tooltip("The 4-digit PIN required to open this door")]
    [SerializeField] private string securityCode = "1234";

    public bool mActivated = false;
    public bool Activated
    {
        get => mActivated; // this is used by the door to check if it should open
        set => mActivated = value; // this is used by the keypad to set the console as activated
    }

    /// <summary>
    /// Player pressed F on this console to interact with it.
    /// </summary>
    public override void Interact(GameObject interactor)
    {
        // register ourselves as the active console
        if (Game_Manager.Instance != null)
        {
            Game_Manager.Instance.activeConsole = this;
            // tell the HUDController to show the keypad
            Game_Manager.Instance.on_keypad_shown.Invoke(true);
        }
    }

    /// <summary>
    /// Check if the entered code matches.
    /// </summary>
    public bool ValidateCode(string entered)
    {
        bool ok = entered == securityCode;
        Debug.Log($"[Console:{name}] ValidateCode(“{entered}”) → {ok}");
        if (ok) mActivated = true;
        Game_Manager.Instance.on_door_console_update.Invoke();
        return ok;
    }

    /// <summary>
    /// Resets this console
    /// </summary>
    public void ResetDoorControl()
    {
        mActivated = false;
        Game_Manager.Instance.on_door_console_update.Invoke();
    }
}
