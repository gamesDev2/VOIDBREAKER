using UnityEngine;
using UnityEngine.Assertions;

public class DoorMovement : MonoBehaviour
{
    [SerializeField, Tooltip("This is where the door will lerp to when opening")] private Transform OpenPosition;
    [SerializeField, Tooltip("This is where the door will lerp to when closing")] private Transform ClosedPosition;

    void Start()
    {
        Assert.IsNotNull(OpenPosition);
        Assert.IsNotNull(ClosedPosition);
    }

    public void setDoorPosition(float position)
    {
        transform.position = Vector3.Lerp(ClosedPosition.position, OpenPosition.position, position);
    }
}