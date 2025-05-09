using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class beamControl : MonoBehaviour
{
    private LineRenderer beam;

    private Vector3 startPoint;
    private Vector3 endPoint;

    void Awake()
    {
        beam = GetComponent<LineRenderer>();
    }

    public void updateDirection(Vector3 _end)
    {
        startPoint = gameObject.transform.position;
        endPoint = _end;

        beam.SetPosition(0, startPoint);
        beam.SetPosition(1, endPoint);
    }

    public void visible(bool _toggle)
    {
        gameObject.SetActive(_toggle);
        if (beam == null)
        {
            beam = GetComponent<LineRenderer>();
        }
    }    
}
