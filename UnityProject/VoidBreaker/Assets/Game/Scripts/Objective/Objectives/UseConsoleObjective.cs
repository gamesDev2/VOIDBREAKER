using UnityEngine;

public class UseConsoleObjective : Objective
{
    public DoorConsole targetConsole;

    protected override void Initialize()
    {
        Game_Manager.Instance.on_door_console_update.AddListener(CheckConsole);
        CheckConsole();
    }

    protected override void TearDown()
    {
        Game_Manager.Instance.on_door_console_update.RemoveListener(CheckConsole);
    }

    private void CheckConsole()
    {
        if (targetConsole != null && targetConsole.Activated)
            CompleteObjective();
    }
}
