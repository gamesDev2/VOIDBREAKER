using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class beamImpactFX : MonoBehaviour
{
    private Light impactLight;
    private ParticleSystem impactParticle;
    private ParticleSystem smokeParticle;

    [SerializeField]
    private float decayRate = 1f;
    void Awake()
    {
        
        impactParticle = GetComponent<ParticleSystem>();
        smokeParticle = transform.GetChild(0).GetComponent<ParticleSystem>();
        impactLight = transform.GetChild(1).GetComponent<Light>();
    }
    
    public void endFX()
    {
        StartCoroutine(fadeFX());
    }

    IEnumerator fadeFX()
    {
        float delta = 1.0f;
        float brightness = impactLight.intensity;

        impactParticle.Stop();
        smokeParticle.Stop();

        while (delta > 0f)
        {
            impactLight.intensity = delta * brightness;
            delta -= decayRate * Time.deltaTime;
            yield return null;
        }

        impactLight.intensity = 0;

        while (impactParticle.IsAlive() || smokeParticle.IsAlive())
        {
            yield return null;
        }

        Destroy(gameObject);
    }
}
