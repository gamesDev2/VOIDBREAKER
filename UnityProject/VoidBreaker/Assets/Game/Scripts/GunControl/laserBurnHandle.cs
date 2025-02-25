using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class laserBurnHandle : MonoBehaviour
{
    private LineRenderer burnMark;

    private Vector3 Normal;
    private int ourObject;

    private void Awake()
    {
        burnMark = GetComponent<LineRenderer>();
        burnMark.positionCount++;
        burnMark.SetPosition(0, burnMark.transform.position);
    }

    public void setObjNorm(int _objID, Vector3 _normal)
    {
        ourObject = _objID;
        Normal = _normal;
    }

    public void AddBurnPosition(Vector3 _point)
    {
        if ((_point - burnMark.GetPosition(burnMark.positionCount - 1)).magnitude < 0.1)
        {
            return;
        }

        burnMark.positionCount++;
        burnMark.SetPosition(burnMark.positionCount-1, _point);  
    }

    public bool sameObject(int _objId, Vector3 _normal)
    {
        if (_objId == ourObject && _normal == Normal) return true;

        return false;
    }

}
