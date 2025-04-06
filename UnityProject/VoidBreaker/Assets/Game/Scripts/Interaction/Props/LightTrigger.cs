using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTrigger : MonoBehaviour
{
    [SerializeField] private FlickerLight[] Lights;
    [SerializeField] private float SequenceDuration;
    [SerializeField] private bool TriggerOnce = true;

    private bool HasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if(TriggerOnce && HasTriggered)
        {
            return;
        }

        if (other.tag == "Player")
        {
            for (int i = 0; i < Lights.Length; i++) 
            {
                Lights[i].startFlicker();

            }
            HasTriggered = true;
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

    public void resetTrigger()
    {
        HasTriggered = false;
    }
}
