using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    [SerializeField] private float maxFlickerDuration = 1.0f;
    [SerializeField] private float minFlickerDuration = 0.1f;

    [SerializeField] private float sparkProbability = 1.0f;
    [SerializeField] private float sparkLength = 0.5f;
    [SerializeField] private int sparkEmmissionCount = 1;
    [SerializeField] private float EmmissionRate = 0.1f;

    private bool active;

    private float lightMaxIntensity;

    private ParticleSystem Sparks;
    private AudioSource flickerSound;
    private Light lightSource;

    // Start is called before the first frame update
    void Start()
    {
        Sparks = GetComponent<ParticleSystem>();
        flickerSound = GetComponent<AudioSource>();
        lightSource = GetComponent<Light>();
    }

    public void startFlicker()
    {
        active = true;

        Flicker();
    }


    public void stopFlicker()
    {
        active = false;
    }

    void Flicker()
    {
        if (active)
        {
            BeginFlicker();
        }
    }

    void BeginFlicker()
    {
        lightSource.enabled = false;
        Invoke("EndFlicker", minFlickerDuration + (Random.value * (maxFlickerDuration - minFlickerDuration)));
    }    

    void EndFlicker()
    {
        flickerSound.Play();
        lightSource.enabled = true;

        if (Random.value <= sparkProbability)
        {
            StartCoroutine(emitSparks());
        }
        

        Invoke("Flicker", minFlickerDuration + (Random.value * (maxFlickerDuration - minFlickerDuration)));
    }

    IEnumerator emitSparks()
    {
        float sparkTime = sparkLength;
        float timeTillSpark = 0;
        while (sparkTime > 0f)
        {
            if (timeTillSpark <= 0f)
            {
                Sparks.Emit(sparkEmmissionCount);
                timeTillSpark = EmmissionRate;
                yield return null;
            }
            yield return null;
            timeTillSpark -= Time.deltaTime;
            sparkTime -= Time.deltaTime;
        }
    }

}
