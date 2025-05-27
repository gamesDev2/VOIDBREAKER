using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using EzySlice;
using UnityEngine.VFX;

public class Sword : weaponBase
{
    #region Inspector Fields
    [Header("Sword Settings")]
    [Tooltip("Indicates whether the sword is currently in attack mode.")]
    public bool isAttacking;

    [Header("Camera Settings")]
    public float normalFOV = 60f;
    public float zoomFOV = 15f;
    public Camera mainCamera; // Assign in inspector or use Camera.main
    private Camera_Controller cameraController;

    [Header("Player controller")]
    [SerializeField] private Entity playerHandle;

    [Header("Cutting Settings")]
    [Tooltip("Transform used as the slicing plane.")]
    public Transform cutPlane;
    [Tooltip("Material for the default cut cross‑section.")]
    public Material crossMaterial;
    [Tooltip("Material used for enemy flesh cross‑sections.")]
    public Material enemyFleshMaterial;
    [Tooltip("Layer mask for sliceable objects.")]
    public LayerMask layerMask;

    [Header("Slash Cooldown")]
    [Tooltip("Time (in seconds) between slashes.")]
    public float slashCooldown = 0.4f;
    private float lastSlashTime = -Mathf.Infinity;

    [Header("Slash Effects")]
    [Tooltip("Visual Effect Graph to trigger on slash (should not auto‑play).")]
    public VisualEffect slashVFX;
    [Tooltip("Particle System to play on slash.")]
    public ParticleSystem slashParticles;

    [Header("Camera Shake Settings")]
    public float shakeStrength = 0.5f;

    [Header("Sword Stats")]
    public float energyUse = 40f;
    #endregion

    private AudioSource slashSound;

    #region Unity Callbacks
    protected override void Start()
    {
        base.Start();
        isMeleeWeapon = true;

        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera != null)
            cameraController = mainCamera.GetComponent<Camera_Controller>();

        if (cutPlane != null)
            cutPlane.gameObject.SetActive(false);

        slashSound = GetComponent<AudioSource>();
    }

    protected override void Update()
    {
        base.Update();

        if (isAttacking && mainCamera != null)
            RotatePlane();

        if (isAttacking && playerHandle != null && playerHandle.GetEnergy() <= 0.0f)
            stopAttack();
    }
    #endregion

    #region WeaponBase Overrides
    public override void startAttack()
    {
        if (playerHandle != null && playerHandle.GetEnergy() <= 0.0f)
            return;

        playerHandle.SetSpecialModeActive(true);
        isAttacking = true;
        if (cutPlane != null)
            cutPlane.gameObject.SetActive(true);
        if (cameraController != null)
        {
            cameraController.SetOverrideFOV(zoomFOV);
            cameraController.SetVolumeIntensity(1f);
        }
        Time.timeScale = (1.0f / playerHandle.timeFlow) * 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    public override void stopAttack()
    {
        isAttacking = false;
        playerHandle.SetSpecialModeActive(false);
        if (cutPlane != null)
            cutPlane.gameObject.SetActive(false);
        if (cameraController != null)
        {
            cameraController.ClearOverrideFOV();
            cameraController.SetVolumeIntensity(0.0f);
        }
        Time.timeScale = (1.0f / playerHandle.timeFlow);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    public override void select()
    {
        isSelectedWeapon = true;
        gameObject.SetActive(isSelectedWeapon);
    }

    public override void deselect()
    {
        stopAttack();
        isSelectedWeapon = false;
        gameObject.SetActive(isSelectedWeapon);
    }
    #endregion

    #region Plane Rotation
    public void RotatePlane()
    {
        if (cutPlane != null)
            cutPlane.eulerAngles += new Vector3(0, 0, -Input.GetAxis("Mouse X") * 5);
    }
    #endregion

    #region Slash Logic
    public void Slash()
    {
        if (Time.time < lastSlashTime + slashCooldown || playerHandle.GetEnergy() < energyUse)
            return;
        lastSlashTime = Time.time;

        bool didSlice = Slice();
        if (!didSlice) return;

        playerHandle.DrainEnergy(energyUse);

        if (cutPlane.childCount > 0)
        {
            cutPlane.GetChild(0).DOComplete();
            cutPlane.GetChild(0)
                     .DOLocalMoveX(cutPlane.GetChild(0).localPosition.x * -1, 0.05f)
                     .SetEase(Ease.OutExpo);
        }

        if (slashVFX != null)
        {
            slashVFX.Stop();
            slashVFX.Play();
        }
        if (slashParticles != null)
        {
            slashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slashParticles.Play();
        }
        if (slashSound != null)
            slashSound.Play();

        ShakeCamera();
    }

    public bool Slice()
    {
        if (cutPlane == null) return false;

        bool slicedSomething = false;
        Collider[] hits = Physics.OverlapBox(
            cutPlane.position,
            new Vector3(2, 0.1f, 2),
            cutPlane.rotation,
            layerMask);

        if (hits.Length == 0)
            return false;

        foreach (Collider col in hits)
        {
            bool isEnemy = col.GetComponentInParent<GOAPAgent>() != null;
            GameObject target = MeshCuttingSystem.FindSliceableRoot(col.gameObject);
            bool wasEnemy =
                col.GetComponentInParent<GOAPAgent>() != null ||
                col.GetComponentInParent<EnemyMarker>() != null;
            if (target == null) continue;

            Material sliceMat = wasEnemy && enemyFleshMaterial != null ? enemyFleshMaterial : crossMaterial;

            SlicedHull hull = MeshCuttingSystem.SliceObject(target, cutPlane.position, cutPlane.up, sliceMat);
            if (hull == null) continue;
            slicedSomething = true;

            GameObject lowerHull = hull.CreateLowerHull(target, sliceMat);
            GameObject upperHull = hull.CreateUpperHull(target, sliceMat);
            if (wasEnemy)
            {
                lowerHull.AddComponent<EnemyMarker>();
                upperHull.AddComponent<EnemyMarker>();
            }

            ApplyCrossSectionMaterial(lowerHull, sliceMat);
            ApplyCrossSectionMaterial(upperHull, sliceMat);

            MeshCuttingSystem.AddHullComponents(lowerHull, target.transform.position);
            MeshCuttingSystem.AddHullComponents(upperHull, target.transform.position);

            if (CutObjectManager.Instance != null)
            {
                CutObjectManager.Instance.RegisterCutObject(lowerHull);
                CutObjectManager.Instance.RegisterCutObject(upperHull);
            }

            Entity victim = col.GetComponentInParent<Entity>();
            if (victim != null)
                victim.Die();

            Destroy(target);
        }

        return slicedSomething;
    }
    #endregion

    #region Helpers
    private static void ApplyCrossSectionMaterial(GameObject hull, Material sliceMat)
    {
        if (hull == null || sliceMat == null) return;
        var renderer = hull.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        Material[] mats = renderer.sharedMaterials;
        if (mats == null || mats.Length == 0)
        {
            renderer.sharedMaterials = new[] { sliceMat };
            return;
        }

        int crossIndex = mats.Length - 1;
        if (mats[crossIndex] == sliceMat) return;

        mats[crossIndex] = sliceMat;
        renderer.sharedMaterials = mats;
    }

    public void ShakeCamera()
    {
        if (cameraController != null)
            cameraController.ShakeCamera(shakeStrength);
    }
    #endregion
}
