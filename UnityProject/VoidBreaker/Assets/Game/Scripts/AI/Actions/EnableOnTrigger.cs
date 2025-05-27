using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableOnTrigger : MonoBehaviour
{

    private AI_Movement_Controller aimove;
    private float moveSpeed;
    private void Start()
    {
        aimove = GetComponent<AI_Movement_Controller>();
        moveSpeed = aimove.moveSpeed;
        aimove.moveSpeed = 0;
    }
    public void Enable(float _delay)
    {
        Invoke("_EnableFunc", _delay);
    }

    private void _EnableFunc()
    {
        aimove.moveSpeed = moveSpeed;
    }
}
