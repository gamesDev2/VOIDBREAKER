using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class gunShooting : MonoBehaviour
{
    [Header("Hierachy References")]
    public Camera playerCamera;

    [Header("Gun Characteristics")]
    [Tooltip("How many shots the gun fires in a second")]
    public float fireRate = 2.0f;
    [Tooltip("The maximum shots the gun can fire before it has to reload")]
    public int ammoCount = 10;
    [Tooltip("How long(in seconds) it takes to reload")]
    public float reloadTime = 2;
    [Tooltip("How far the gun can hit targets from")]
    public float range = 100.0f;
    [Header("Input")]
    public KeyCode shootButton = KeyCode.Mouse0;
    [Header("Hit Decal")]
    [Tooltip("Prefab to a decal projector containing a bullet hole")]
    public DecalProjector bulletHole;
    [Header("Tag")]
    [Tooltip("Anything with this tag should have a \"OnHit\" method")]
    public string tagToHit;

    private float timeSinceLastShot;
    private float timeSinceLastReload;
    public int ammoRemaining;
    private bool reloading;

    void Start()
    {
        ammoRemaining = ammoCount;
    }

    void Update()
    {
        timeSinceLastShot += Time.deltaTime;
        timeSinceLastReload += Time.deltaTime;


        if (timeSinceLastReload > reloadTime && ammoRemaining < ammoCount)
        {
            ammoRemaining++;
            timeSinceLastReload = 0;
        }
    }

    public void Shoot()
    {
        if (ammoRemaining < 1)
        {
            reloading = true;
        }
        else if (ammoRemaining >= ammoCount)
        {
            reloading = false;
            ammoRemaining = ammoCount;
        }

        RaycastHit hit;
        if ((timeSinceLastShot > 1 / fireRate) && !reloading)
        {

            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
            {
                Instantiate(bulletHole, hit.point, Quaternion.LookRotation(hit.normal), hit.transform);
                handleHit(ref hit);
            }

            ammoRemaining--;
            timeSinceLastShot = 0;
        }

        
    }

    private void handleHit(ref RaycastHit _hit)
    {
        if(_hit.collider.tag == tagToHit)
        {
            // Do something idk what yet
        }
    }
}