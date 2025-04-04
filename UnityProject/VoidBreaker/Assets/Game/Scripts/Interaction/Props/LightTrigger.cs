using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTrigger : MonoBehaviour
{
    [SerializeField] private FlickerLight[] Lights;

    [SerializeField] private float SequenceDuration;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            for (int i = 0; i < Lights.Length; i++) 
            {
                Lights[i].startFlicker();

            }
            Invoke("StopSequence", SequenceDuration);
        }
    }

    private void StopSequence()
    {
        for (int i = 0; i < Lights.Length; i++)
        {
            Lights[i].stopFlicker();
        }
    }
}
