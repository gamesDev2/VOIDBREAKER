using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CheckPointManager
{
    static public int checkPointIndex = 0;

    public CheckPoint[] checkPoints;

    static public void updateCheckPoint(int checkPoint)
    {
        checkPointIndex = checkPoint > checkPointIndex ? checkPoint : checkPointIndex; 
    }
}
