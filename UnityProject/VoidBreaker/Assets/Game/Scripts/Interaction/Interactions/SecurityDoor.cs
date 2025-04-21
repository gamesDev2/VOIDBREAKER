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

    

    private float doorPosition = 0f;

    void Start()
    {
        Assert.IsNotNull(OpenPosition);
        Assert.IsNotNull(ClosedPosition);
    }

    // Update is called once per frame
    void Update()
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

        DoorTransition();

    }

    private void DoorTransition()
    {
        doorPosition += Time.deltaTime * transitionSpeed;
        doorPosition = math.clamp(doorPosition, 0f, 1f);
        transform.position = Vector3.Lerp(ClosedPosition.position, OpenPosition.position, doorPosition);
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
