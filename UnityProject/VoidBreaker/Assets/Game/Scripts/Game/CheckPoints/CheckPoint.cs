using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckPoint : MonoBehaviour
{
    public Transform PlayerSpawnLocation;
    public int CheckPointIndex;

    public EventTrigger checkPointTrigger;

    private void Start()
    {
        checkPointTrigger.TriggerEvent.AddListener(CheckPointTriggered);
    }

    public void CheckPointTriggered()
    {
        CheckPointManager.updateCheckPoint(CheckPointIndex);
    }
}
