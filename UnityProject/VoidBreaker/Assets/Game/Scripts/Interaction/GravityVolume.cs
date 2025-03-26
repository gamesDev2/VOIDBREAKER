using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityVolume : MonoBehaviour
{
    private List<Rigidbody> mBodies = new List<Rigidbody>();

    [Header("How much and in what direction the volume should push objects in.")]
    [SerializeField] private Vector3 Acceleration = Vector3.zero;

    [SerializeField] private bool cancelEngineGravity = true;

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 accel = Acceleration;
        if (cancelEngineGravity)
        {
            accel -= Physics.gravity;
        }

        for (int i = 0; i < mBodies.Count; i++) 
        {
            if (mBodies[i] != null)
            {
                mBodies[i].AddForce(accel, ForceMode.Acceleration);
            }
            else
            {
                mBodies.Remove(mBodies[i]);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody otherBody = other.gameObject.GetComponent<Rigidbody>();
        if (otherBody != null)
        {
            mBodies.Add(otherBody);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody otherBody = other.gameObject.GetComponent<Rigidbody>();
        if (otherBody != null)
        {
            mBodies.Remove(otherBody);
        }
    }
}
