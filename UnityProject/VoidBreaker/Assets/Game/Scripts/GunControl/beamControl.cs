using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class beamControl : MonoBehaviour
{
    private LineRenderer beam;
    private Light illumination;

    private Vector3 startPoint;
    private Vector3 endPoint;

    private void Awake()
    {
        beam = GetComponent<LineRenderer>();
        illumination = transform.GetChild(0).GetComponent<Light>();
    }

    public void updateDirection(Vector3 _start, Vector3 _end)
    {
        startPoint = _start;
        endPoint = _end;
        Vector3 AB = _end - _start;

        beam.SetPosition(0, startPoint);
        beam.SetPosition(1, endPoint);

        illumination.transform.position = (startPoint + endPoint) / 2;
        illumination.transform.rotation = Quaternion.LookRotation(AB);
        illumination.areaSize.Set(0.5f, AB.magnitude);
    }

    public void visible(bool _toggle)
    {
        gameObject.SetActive(_toggle);
    }    
}
