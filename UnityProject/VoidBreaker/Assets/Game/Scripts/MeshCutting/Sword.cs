using System.Collections;
using UnityEngine;
using DG.Tweening;
using EzySlice;
using UnityEngine.VFX;

public class Sword : weaponBase
{
    [Header("Sword Settings")]
    [Tooltip("Indicates whether the sword is currently in attack mode.")]
    public bool isAttacking;

    [Header("Camera Settings")]
    public float normalFOV = 60f;
    public float zoomFOV = 15f;
    public Camera mainCamera; // Assign in inspector or use Camera.main
    private Camera_Controller cameraController;

    [Header("Cutting Settings")]
    [Tooltip("Transform used as the slicing plane.")]
    public Transform cutPlane;
    [Tooltip("Material for the cut cross-section.")]
    public Material crossMaterial;
    [Tooltip("Layer mask for sliceable objects.")]
    public LayerMask layerMask;

    [Header("Slash Cooldown")]
    [Tooltip("Time (in seconds) between slashes.")]
    public float slashCooldown = 0.4f;
    private float lastSlashTime = -Mathf.Infinity;

    [Header("Slash Effects")]
    [Tooltip("Visual Effect Graph to trigger on slash (should not auto-play).")]
    public VisualEffect slashVFX;
    [Tooltip("Particle System to play on slash.")]
    public ParticleSystem slashParticles;

    [Header("Camera Shake Settings")]
    public float shakeStrength = 0.5f;

    protected override void Start()
    {
        base.Start();
        // Mark this weapon as melee so it handles its own input.
        isMeleeWeapon = true;

        if (mainCamera == null)
            mainCamera = Camera.main;
        if (mainCamera != null)
            cameraController = mainCamera.GetComponent<Camera_Controller>();

        if (cutPlane != null)
            cutPlane.gameObject.SetActive(false);
    }

    protected override void Update()
    {
        base.Update(); // Calls ProcessInput() if isMeleeWeapon is true.
        // While attacking, update the sword's rotation to align with the camera.
        if (isAttacking && mainCamera != null)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, mainCamera.transform.rotation, 0.2f);
            RotatePlane();
        }
    }

    public override void ProcessInput()
    {
        // Use right mouse button to engage/disengage blade mode.
        if (Input.GetMouseButtonDown(1))
        {
            startAttack();
        }
        if (Input.GetMouseButtonUp(1))
        {
            stopAttack();
        }
        // When in attack mode, a left mouse click (Mouse0) triggers one slash.
        if (isAttacking && Input.GetMouseButtonDown(0) && Time.time >= lastSlashTime + slashCooldown)
        {
            lastSlashTime = Time.time;
            bool didSlice = Slice();
            if (didSlice)
            {
                // Optional: animate the slicing plane's child if available.
                if (cutPlane.childCount > 0)
                {
                    cutPlane.GetChild(0).DOComplete();
                    cutPlane.GetChild(0)
                        .DOLocalMoveX(cutPlane.GetChild(0).localPosition.x * -1, 0.05f)
                        .SetEase(Ease.OutExpo);
                }
                // Trigger VFX: reset and send the event.
                if (slashVFX != null)
                {
                    slashVFX.Stop();
                    slashVFX.Play();
                }
                // Restart the particle system.
                if (slashParticles != null)
                {
                    slashParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    slashParticles.Play();
                }
                ShakeCamera();
            }
        }
    }

    public override void startAttack()
    {
        isAttacking = true;
        if (cutPlane != null)
            cutPlane.gameObject.SetActive(true);
        if (cameraController != null)
            cameraController.SetOverrideFOV(zoomFOV);
        // Slow down time for dramatic effect.
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0.2f, 0.02f);
    }

    public override void stopAttack()
    {
        isAttacking = false;
        if (cutPlane != null)
            cutPlane.gameObject.SetActive(false);
        if (cameraController != null)
            cameraController.ClearOverrideFOV();
        // Resume normal time.
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1f, 0.02f);
    }

    public void RotatePlane()
    {
        if (cutPlane != null)
            cutPlane.eulerAngles += new Vector3(0, 0, -Input.GetAxis("Mouse X") * 5);
    }

    /// <summary>
    /// Attempts to slice nearby objects using the MeshCuttingSystem.
    /// Returns true if at least one object was sliced.
    /// </summary>
    public bool Slice()
    {
        if (cutPlane == null) return false;
        bool slicedSomething = false;
        Collider[] hits = Physics.OverlapBox(cutPlane.position, new Vector3(5, 0.1f, 5), cutPlane.rotation, layerMask);
        if (hits.Length <= 0)
            return false;
        foreach (Collider col in hits)
        {
            SlicedHull hull = MeshCuttingSystem.SliceObject(col.gameObject, cutPlane.position, cutPlane.up, crossMaterial);
            if (hull != null)
            {
                slicedSomething = true;
                GameObject lowerHull = hull.CreateLowerHull(col.gameObject, crossMaterial);
                GameObject upperHull = hull.CreateUpperHull(col.gameObject, crossMaterial);
                MeshCuttingSystem.AddHullComponents(lowerHull, col.transform.position);
                MeshCuttingSystem.AddHullComponents(upperHull, col.transform.position);
                if (CutObjectManager.Instance != null)
                {
                    CutObjectManager.Instance.RegisterCutObject(lowerHull);
                    CutObjectManager.Instance.RegisterCutObject(upperHull);
                }
                Destroy(col.gameObject);
            }
        }
        return slicedSomething;
    }

    public void ShakeCamera()
    {
        if (cameraController != null)
            cameraController.ShakeCamera(shakeStrength);
    }
}
