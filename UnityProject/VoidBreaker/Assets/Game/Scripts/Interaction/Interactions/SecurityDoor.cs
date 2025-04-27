using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class SecurityDoor : MonoBehaviour
{
    [SerializeField, Tooltip("This is where the door will lerp to when opening")] private Transform OpenPosition;
    [SerializeField, Tooltip("This is where the door will lerp to when closing")] private Transform ClosedPosition;
    [SerializeField, Tooltip("The Consoles that control this door")] private DoorConsole[] doorConsoles;

    [SerializeField, Tooltip("How long it takes for the door to open/close.")] private float transitionSpeed = 2.0f;

    
    private bool doorMoving = false;
    private float doorPosition = 0f;

    void Start()
    {
        Assert.IsNotNull(OpenPosition);
        Assert.IsNotNull(ClosedPosition);
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
            transform.position = Vector3.Lerp(ClosedPosition.position, OpenPosition.position, doorPosition);

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
        return true;
    }
}
