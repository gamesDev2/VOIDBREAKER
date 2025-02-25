using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class laserBurnHandle : MonoBehaviour
{
    private LineRenderer burnMark;

    private void Awake()
    {
        burnMark = GetComponent<LineRenderer>();
    }

    
    public void AddBurnPosition(Vector3 _point)
    {
        burnMark.positionCount++;
        burnMark.SetPosition(burnMark.positionCount-1, _point);
    }
}
