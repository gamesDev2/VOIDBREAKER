using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class gunShooting : weaponBase
{
    [Header("Gun Characteristics")]
    [Tooltip("Damage per ammo consumed")]
    [SerializeField] private float damage = 1.0f;
    [Tooltip("How many shots the gun fires in a second")]
    [SerializeField] private float fireRate = 2.0f;
    [Tooltip("Effectively makes the gun fire every frame. Ammo consumption is still controlled by fireRate")]
    [SerializeField] private bool beamWeapon = false;
    [Tooltip("The maximum shots the gun can fire before it has to reload")]
    [SerializeField] private int ammoCount = 10;
    [Tooltip("How long(in seconds) it takes to reload")]
    [SerializeField] private float reloadTime = 2;
    [Tooltip("How far the gun can hit targets from")]
    [SerializeField] private float range = 100.0f;

    [Header("Remaining Ammunition")]
    [SerializeField] private int ammoRemaining;

    [Header("Hit Effects")]
    [Tooltip("Prefab to a decal projector containing a bullet hole")]
    [SerializeField] private DecalProjector bulletHole;
    [Tooltip("Prefab to a line renderer that must contain a laserBurnHandle script")]
    [SerializeField] private laserBurnHandle laserBurn;
    [Tooltip("Continuous Impact Particle system goes here.")]
    [SerializeField] private beamImpactFX impactFX;

    [Header("Tracer Effect")]
    [SerializeField] private beamControl tracerFX;





    private float timeSinceLastShot = 0;
    private float timeSinceLastReload = 0;
    
    private bool reloading = false;
    private bool firing = false;

    private laserBurnHandle currentBurner = null;

    private RaycastHit hit;


    protected override void Start()
    {
        ammoRemaining = ammoCount;
        tracerFX.visible(false);
    }

    protected override void Update()
    {
        timeSinceLastShot += Time.deltaTime;
        timeSinceLastReload += Time.deltaTime;


        if (timeSinceLastReload > reloadTime / ammoCount &&
            ammoRemaining < ammoCount &&
            !firing &&
            timeSinceLastShot > 1 / fireRate)
        {
            ammoRemaining += (int)(timeSinceLastReload / (reloadTime / ammoCount));
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

    public override void startAttack()
    {
        if (!reloading)
        {
            firing = true;
            timeSinceLastShot = 0;
        }
    }

    public override void stopAttack()
    {
        if (currentBurner != null)
        {
            currentBurner.endBurn();
            currentBurner = null;
        }

        tracerFX.visible(false);

        firing = false;
        currentBurner = null;
        timeSinceLastReload = 0;
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
            stopAttack();
        }

        
        if (!((beamWeapon || (timeSinceLastShot > 1 / fireRate)) && !reloading))
        {
            return;
        }


        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range, -5, QueryTriggerInteraction.Ignore))
        {
            handleHit(ref hit);
            tracerFX.updateDirection(hit.point);
        }
        // TODO: Make this shit not the ugliest piece of shit code you've ever written. Actually that kinda applies to most of this script. GET ON IT ME!!!
        else if (currentBurner != null)
        {
            currentBurner.endBurn();
            currentBurner = null;
            tracerFX.updateDirection(playerCamera.transform.position + (playerCamera.transform.forward * range));
        }
        else
        {
            tracerFX.updateDirection(playerCamera.transform.position + (playerCamera.transform.forward * range));
        }

        if (timeSinceLastShot > 1 / fireRate)
        {
            ammoRemaining -= (int)(timeSinceLastShot / (1 / fireRate));
            timeSinceLastShot = 0;
        }

        tracerFX.visible(true);
    }



    private void handleHit(ref RaycastHit _hit)
    {
        int objId = hit.collider.gameObject.GetInstanceID();

        Entity entity = _hit.collider.GetComponent<Entity>();
        if (entity != null)
        {
            entity.TakeDamage(damage * fireRate * Time.deltaTime);
        }



        if (!beamWeapon)
        {
            Instantiate(bulletHole, hit.point, Quaternion.LookRotation(hit.normal), hit.transform);
            return;
        }

        if(currentBurner != null && currentBurner.sameObject(objId, hit.normal))
        {
            currentBurner.AddBurnPosition(_hit.point + (hit.normal / 10000));
        }
        else if(currentBurner != null)
        {
            currentBurner.endBurn();
            currentBurner = null;
        }

        if (currentBurner == null && !hit.collider.isTrigger)
        {
            currentBurner = Instantiate(laserBurn, hit.point + (hit.normal / 10000), Quaternion.LookRotation(-hit.normal));
            currentBurner.impactFX = Instantiate(impactFX, hit.point + (hit.normal / 10000), Quaternion.LookRotation(hit.normal), currentBurner.transform);
            currentBurner.setObjParams(objId, hit.normal);

        }
    }
}