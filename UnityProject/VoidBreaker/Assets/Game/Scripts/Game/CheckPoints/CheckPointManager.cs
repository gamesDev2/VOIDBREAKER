using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckPointManager : MonoBehaviour
{
    static public int checkPointIndex = 0;

    public CheckPoint[] checkPoints;

    void Start()
    {
        for (int i = 0; i < checkPoints.Length; i++) 
        {
            checkPoints[i].CheckPointIndex = i;
        }
    }

    static public void updateCheckPoint(int checkPoint)
    {
        if (checkPoint > checkPointIndex)
        {
            checkPointIndex = checkPoint;
        }
    }
}
