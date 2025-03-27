using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GravityVolume : MonoBehaviour
{
    private Dictionary<Rigidbody, ParticleSystem> mBodies = new Dictionary<Rigidbody, ParticleSystem>();

    [Tooltip("How much and in what direction the volume should push objects in.")]
    [SerializeField] private Vector3 Acceleration = Vector3.zero;

    [SerializeField] private bool cancelEngineGravity = true;

    [Tooltip("The particle system that will spawn on objects within the gravity volume")]
    [SerializeField] private ParticleSystem particleFX;

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 accel = Acceleration;
        if (cancelEngineGravity)
        {
            accel -= Physics.gravity;
        }

        foreach (Rigidbody body in mBodies.Keys.ToArray()) 
        {
            if (body != null)
            {
                body.AddForce(accel, ForceMode.Acceleration);
                mBodies[body].transform.position = body.worldCenterOfMass - (mBodies[body].transform.forward * 2);
            }
            else
            {
                mBodies[body].Stop();
                Destroy(mBodies[body].gameObject, 1);
                mBodies.Remove(body);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody otherBody = other.gameObject.GetComponent<Rigidbody>();
        if (otherBody != null)
        {
            mBodies.Add(otherBody, Instantiate(particleFX));
            mBodies[otherBody].transform.rotation = Quaternion.LookRotation(Acceleration + (Vector3.up * 0.0001f));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody otherBody = other.gameObject.GetComponent<Rigidbody>();
        if (otherBody != null)
        {
            mBodies[otherBody].Stop();
            Destroy(mBodies[otherBody].gameObject, 1);
            mBodies.Remove(otherBody);
        }
    }
}
