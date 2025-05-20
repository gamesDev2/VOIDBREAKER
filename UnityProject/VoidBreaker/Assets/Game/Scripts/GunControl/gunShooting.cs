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
    [Tooltip("Prefab to a line renderer that must contain a laserBurnHandle script")]
    [SerializeField] private laserBurnHandle laserBurn;
    [Tooltip("Continuous Impact Particle system goes here.")]
    [SerializeField] private beamImpactFX impactFX;

    [Header("Tracer Effect")]
    [SerializeField] private beamControl tracerFX;

    [Header("SoundFX")]
    [SerializeField] private AudioClip overheat;
    [SerializeField] private AudioClip warningSounds;
    [SerializeField] private AudioSource oneShotAudio;

    private LayerMask PlayerMask;

    private float timeSinceLastShot = 0;
    private float timeSinceLastReload = 0;

    private bool reloading = false;
    private bool firing = false;

    private laserBurnHandle currentBurner = null;
    private AudioSource audioPlayer;

    private RaycastHit hit;
    private bool hitting;


    protected override void Start()
    {
        audioPlayer = GetComponent<AudioSource>();
        ammoRemaining = ammoCount;
        tracerFX.visible(false);
        PlayerMask = -1;
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

        if (ammoRemaining / fireRate < 5 && !oneShotAudio.isPlaying)
        {
            oneShotAudio.PlayOneShot(warningSounds);
        }

        if (firing)
        {
            ShootFx();
        }
    }

    private void FixedUpdate()
    {
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
            audioPlayer.Play();
        }
        else 
        {
            Game_Manager.Instance.on_empty_fire.Invoke();
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
        audioPlayer.Stop();

        hitting = false;
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


        if (hitting)
        {
            handleHit();
        }
        else if (currentBurner != null)
        {
            currentBurner.endBurn();
            currentBurner = null;
        }

        if (timeSinceLastShot > 1 / fireRate)
        {
            ammoRemaining -= (int)(timeSinceLastShot / (1 / fireRate));
            timeSinceLastShot = 0;
        }
    }

    private void ShootFx()
    {
        if (playerCamera == null)
        {
            return;
        }

        if (ammoRemaining < 1)
        {
            reloading = true;
            oneShotAudio.PlayOneShot(overheat);
            stopAttack();
        }

        if (!((beamWeapon || (timeSinceLastShot > 1 / fireRate)) && !reloading))
        {
            return;
        }

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range, PlayerMask, QueryTriggerInteraction.Ignore))
        {
            tracerFX.updateDirection(hit.point);
            hitting = true;
        }
        else
        {
            tracerFX.updateDirection(playerCamera.transform.position + (playerCamera.transform.forward * range));
            hitting = false;
        }

        tracerFX.visible(true);
    }

    private void handleHit()
    {
        int objId = hit.collider.gameObject.GetInstanceID();

        Vector3 localNormal = hit.collider.transform.InverseTransformVector(hit.normal);

        Entity entity = hit.collider.GetComponent<Entity>();
        if (entity != null)
        {
            entity.TakeDamage(damage * fireRate * Time.deltaTime);
        }

        if (currentBurner != null && currentBurner.sameObject(objId, localNormal))
        {
            currentBurner.AddBurnPosition((hit.point - currentBurner.transform.position) + (hit.normal / 1000));
        }
        else if (currentBurner != null)
        {
            currentBurner.endBurn();
            currentBurner = null;
        }

        if (currentBurner == null && !hit.collider.isTrigger)
        {
            currentBurner = Instantiate(laserBurn, hit.point + (hit.normal / 1000), Quaternion.LookRotation(localNormal));
            currentBurner.impactFX = Instantiate(impactFX, (hit.point) + (hit.normal / 1000), Quaternion.LookRotation(localNormal));
            currentBurner.setObjParams(objId, localNormal, hit.normal, hit.collider.gameObject.transform);
        }
    }

    public int maxAmmo
    {
        get { return ammoCount; }
    }

    public int remainingAmmo
    {
        get { return ammoRemaining; }
    }
}