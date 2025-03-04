using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class laserBurnHandle : MonoBehaviour
{
    private LineRenderer burnMark;


    private Vector3 Normal;
    private int ourObject;

    private Light impactLight;
    public beamImpactFX impactFX;

    private void Awake()
    {
        burnMark = GetComponent<LineRenderer>();
        impactLight = GetComponent<Light>();
    }

    public void setObjParams(int _objID, Vector3 _normal)
    {
        ourObject = _objID;
        Normal = _normal;
    }

    public void AddBurnPosition(Vector3 _point)
    {
        burnMark.positionCount++;
        burnMark.SetPosition(burnMark.positionCount-1, _point);

        impactFX.transform.position = _point;
    }

    public bool sameObject(int _objId, Vector3 _normal)
    {
        if (_objId == ourObject && _normal == Normal) return true;

        return false;
    }

    public void endBurn()
    {
        impactFX.endFX();
        impactFX = null;
    }

    
}
