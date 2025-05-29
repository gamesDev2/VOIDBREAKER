using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class SecurityDoor : MonoBehaviour
{
    [SerializeField, Tooltip("All Doors that this script controls")] private DoorMovement[] Doors;
    [Tooltip("The Consoles that control this door")] public DoorConsole[] doorConsoles;
    [SerializeField, Header("Events that the door triggers on open")] private EventTrigger[] additionalTriggers;

    [SerializeField, Tooltip("How long it takes for the door to open/close.")] private float transitionSpeed = 2.0f;
    
    private bool doorMoving = false;
    private float doorPosition = 0f;

    public bool open = false;

    void Start()
    {
        Assert.IsNotNull(Doors);
        Game_Manager.Instance.on_door_console_update.AddListener(OnConsoleUpdate);
    }

    // Update is called once per frame
    void OnConsoleUpdate()
    {
        open = isActivated();

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

            foreach (DoorMovement d in Doors)
            {
                d.setDoorPosition(doorPosition);
            }
            Debug.Log(doorPosition);
            yield return null;
        }
        while (doorPosition <= 1f && doorPosition >= 0);

        if (transitionSpeed < 0f)
        {
            doorPosition = 0f;
        }
        else 
        {
            doorPosition = 1f;
        }

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

        foreach (EventTrigger e in additionalTriggers)
        {
            if (e != null)
                e.TriggerEvent.Invoke();
        }

        return true;
    }
}
