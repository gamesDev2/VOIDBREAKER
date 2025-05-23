using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class SecurityDoor : MonoBehaviour
{
    [SerializeField, Tooltip("All Doors that this script controls")] private DoorMovement[] Doors;
    [SerializeField, Tooltip("The Consoles that control this door")] private DoorConsole[] doorConsoles;

    [SerializeField, Tooltip("How long it takes for the door to open/close.")] private float transitionSpeed = 2.0f;

    [SerializeField] private EnableOnTrigger[] additionalTriggers;
    
    private bool doorMoving = false;
    private float doorPosition = 0f;

    void Start()
    {
        Assert.IsNotNull(Doors);
        Game_Manager.Instance.on_door_console_update.AddListener(OnConsoleUpdate);
    }

    // Update is called once per frame
    void OnConsoleUpdate()
    {
        bool open = isActivated();

        if (open)
        {
            transitionSpeed = math.abs(transitionSpeed);
        }
        else
        {
            transitionSpeed = -math.abs(transitionSpeed);
        }

        if (!doorMoving)
        {
            StartCoroutine(DoorTransition());
        }

    }

    private IEnumerator DoorTransition()
    {
        doorMoving = true;

        do
        {
            doorPosition += Time.deltaTime * transitionSpeed;
            doorPosition = math.clamp(doorPosition, 0f, 1f);

            foreach (DoorMovement d in Doors)
            {
                d.setDoorPosition(doorPosition);
            }

            yield return null;
        }
        while (doorPosition != 1f || doorPosition != 0);

        doorMoving = false;
    }


    private bool isActivated()
    {
        foreach (DoorConsole console in doorConsoles)
        {
            if (!console.Activated)
            {
                return false;
            }
        }
        if (doorConsoles.Length == 0)
            return false;

        foreach (EnableOnTrigger e in additionalTriggers)
        {
            e.Enable();
        }

        return true;
    }
}
