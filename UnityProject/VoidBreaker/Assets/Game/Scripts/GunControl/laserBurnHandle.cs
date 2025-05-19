using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Assertions;

public class laserBurnHandle : MonoBehaviour
{
    private LineRenderer burnMark;
    private ParentConstraint parentCon;
    private ConstraintSource cS;

    private Transform tester;
    private Vector3 Normal;
    private int ourObject;

    public beamImpactFX impactFX;

    private bool hasConstructed = false;

    void Awake()
    {
        burnMark = GetComponent<LineRenderer>();
        parentCon = GetComponent<ParentConstraint>();
        
    }

    private void Update()
    {
        // If our "parent" dies then we die
        if (tester == null)
        {
            if (impactFX != null)
            {
                impactFX.endFX();
            }
            Destroy(gameObject);
            return;
        }
    }

    // A function that allows the object to have a "constructor" even when it's being created through unities Instantiate()
    public void setObjParams(int _objID, Vector3 _localNormal, Vector3 _worldNomal, Transform _pT)
    {
        // Ensuring that the sudo constructor can only be called once
        Assert.IsTrue(!hasConstructed);
        hasConstructed = true;

        tester = _pT;
        cS.sourceTransform = _pT;
        cS.weight = 1;
        parentCon.AddSource(cS);

        Vector3 offset = Quaternion.LookRotation(-_localNormal).eulerAngles;
        Vector3[] rotRest = { (offset) };
        parentCon.rotationOffsets = rotRest;

        parentCon.constraintActive = true;
        parentCon.locked = true;
        ourObject = _objID;
        Normal = _localNormal;
    }

    public void AddBurnPosition(Vector3 _point)
    {
        Vector3 RotPoint = Quaternion.Inverse(gameObject.transform.rotation) * _point;
        burnMark.positionCount++;
        burnMark.SetPosition(burnMark.positionCount-1, RotPoint);

        impactFX.transform.position = _point + gameObject.transform.position;
    }

    public bool sameObject(int _objId, Vector3 _normal)
    {
        if (_objId == ourObject && _normal == Normal) return true;

        return false;
    }

    public void endBurn()
    {
        impactFX.endFX();
        if (!burnMark.enabled)
        {
            transform.DetachChildren();
            Destroy(gameObject);
        }
        impactFX = null;
        
    }

    
}
