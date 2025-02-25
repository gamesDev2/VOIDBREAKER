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
    [Tooltip("Effectively makes the gun fire every frame. Ammo consumption is still controlled by fireRate")]
    public bool beamWeapon = false;
    [Tooltip("The maximum shots the gun can fire before it has to reload")]
    public int ammoCount = 10;
    [Tooltip("How long(in seconds) it takes to reload")]
    public float reloadTime = 2;
    [Tooltip("How far the gun can hit targets from")]
    public float range = 100.0f;


    [Header("Remaining Ammunition")]
    [SerializeField]
    private int ammoRemaining;

    [Header("Hit Decal")]
    [Tooltip("Prefab to a decal projector containing a bullet hole")]
    public DecalProjector bulletHole;
    [Tooltip("Prefab to a line renderer that must contain a laserBurnHandle script")]
    public laserBurnHandle laserBurn;

    [Header("Tag")]
    [Tooltip("Anything with this tag should have a \"OnHit\" method")]
    public string tagToHit;

    

    private float timeSinceLastShot = 0;
    private float timeSinceLastReload = 0;
    
    private bool reloading = false;
    private bool firing = false;

    private laserBurnHandle currentBurner = null;

    private RaycastHit hit;
    void Start()
    {
        ammoRemaining = ammoCount;
    }

    void Update()
    {
        timeSinceLastShot += Time.deltaTime;
        timeSinceLastReload += Time.deltaTime;


        if (timeSinceLastReload > reloadTime / ammoCount &&
            ammoRemaining < ammoCount &&
            !firing &&
            timeSinceLastShot > 1 / fireRate)
        {
            ammoRemaining++;
            timeSinceLastReload = 0;
        }

        if (ammoRemaining >= ammoCount)
        {
            reloading = false;
            ammoRemaining = ammoCount;
        }

        if (firing)
        {
            Shoot();
        }
    }

    public void startFire()
    {
        if (!reloading)
        {
            firing = true;
        }
    }

    public void stopFire()
    {
        firing = false;
        currentBurner = null;
    }


    private void Shoot()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (ammoRemaining < 1)
        {
            reloading = true;
            stopFire();
        }

        
        if (!((beamWeapon || (timeSinceLastShot > 1 / fireRate)) && !reloading))
        {
            return;
        }


        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            handleHit(ref hit);
        }

        if (timeSinceLastShot > 1 / fireRate)
        {
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


        if (!beamWeapon)
        {
            Instantiate(bulletHole, hit.point, Quaternion.LookRotation(hit.normal), hit.transform);
            return;
        }

        if(currentBurner != null)
        {
            currentBurner.AddBurnPosition(_hit.point);
        }
        else
        {
            currentBurner = Instantiate(laserBurn, hit.point, Quaternion.LookRotation(hit.normal), hit.transform);
        }
    }
}