using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableOnTrigger : MonoBehaviour
{

    private AI_Movement_Controller aimove;

    private void Start()
    {
        aimove = GetComponent<AI_Movement_Controller>();
    }
    public void Enable()
    {
        aimove.moveSpeed = 12;
    }
}
