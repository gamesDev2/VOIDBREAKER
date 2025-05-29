using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableOnTrigger : MonoBehaviour
{
    [SerializeField] private EventTrigger Trigger;
    [SerializeField] private float Delay = 2f;

    private AI_Movement_Controller aimove;
    private float moveSpeed;
    private void Start()
    {
        aimove = GetComponent<AI_Movement_Controller>();
        moveSpeed = aimove.moveSpeed;
        aimove.moveSpeed = 0;

        Trigger.TriggerEvent.AddListener(Enable);
    }
    public void Enable()
    {
        Invoke("_EnableFunc", Delay);
    }

    private void _EnableFunc()
    {
        aimove.moveSpeed = moveSpeed;
    }
}
