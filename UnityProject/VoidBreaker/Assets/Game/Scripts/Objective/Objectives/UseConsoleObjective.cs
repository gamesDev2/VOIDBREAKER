using UnityEngine;

public class UseConsoleObjective : Objective
{
    public DoorConsole targetConsole;

    // ------------------------------------------------------------ //
    //  permanent subscription (objective listens even if inactive) //
    // ------------------------------------------------------------ //

    void OnEnable()
    {
        if (Game_Manager.Instance != null)
            Game_Manager.Instance.on_door_console_update.AddListener(CheckConsole);
    }

    void OnDisable()
    {
        if (Game_Manager.Instance != null)
            Game_Manager.Instance.on_door_console_update.RemoveListener(CheckConsole);
    }

    // ------------------------------------------------------------ //
    //  standard objective hooks                                    //
    // ------------------------------------------------------------ //

    protected override void Initialize() { CheckConsole(); }
    protected override void TearDown() { /* nothing extra */ }

    // ------------------------------------------------------------ //
    //  event reaction                                              //
    // ------------------------------------------------------------ //

    private void CheckConsole()
    {
        if (targetConsole != null && targetConsole.Activated)
        {
            if (IsActive)
                CompleteObjective();        // normal, in-order finish
            else
                ForceCompleteObjective();   // out-of-order finish (triggers skip)
        }
    }

    public override bool IsSatisfiedByGameState()
    {
        return targetConsole != null && targetConsole.Activated;
    }
}
