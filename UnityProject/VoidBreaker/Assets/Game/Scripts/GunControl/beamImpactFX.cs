using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class beamImpactFX : MonoBehaviour
{
    private Light impactLight;
    private ParticleSystem impactParticle;

    [SerializeField]
    private float decayRate = 1f;
    void Awake()
    {
        impactLight = GetComponent<Light>();
        impactParticle = GetComponent<ParticleSystem>();
    }
    
    public void endFX()
    {
        StartCoroutine(fadeFX());
    }

    IEnumerator fadeFX()
    {
        float delta = 1.0f;
        float brightness = impactLight.intensity;

        var main = impactParticle.main;
        main.loop = false;

        while (delta > 0f)
        {
            impactLight.intensity = delta * brightness;
            delta -= decayRate * Time.deltaTime;
            yield return null;
        }

        impactLight.intensity = 0;

        while (impactParticle.IsAlive())
        {
            yield return null;
        }

        Destroy(this);
    }
}
