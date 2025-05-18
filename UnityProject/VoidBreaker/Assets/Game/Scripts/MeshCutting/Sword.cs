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

    [Header("Player controller")]
    [SerializeField] private Entity playerHandle;

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
        // This is a melee weapon.
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
        base.Update(); // Now ProcessInput() is not used here.

        if (isAttacking && mainCamera != null)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, mainCamera.transform.rotation, 0.2f);
            RotatePlane();
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, 0), 0.2f);
        }

        // Stop attack if energy is depleted.
        if (isAttacking && playerHandle != null && playerHandle.GetEnergy() <= 0.0f)
        {
            stopAttack();
        }
    }


    // Remove internal input handling. All input is now handled by the weapon handler.

    public override void startAttack()
    {
        // Check if the player has enough energy to attack.
        if (playerHandle != null && playerHandle.GetEnergy() <= 0.0f)
            return;
        playerHandle.SetSpecialModeActive(true); // Set special mode active
        isAttacking = true;
        if (cutPlane != null)
            cutPlane.gameObject.SetActive(true);
        if (cameraController != null)
            cameraController.SetOverrideFOV(zoomFOV);
            cameraController.SetVolumeIntensity(1f);
        // Slow down time for dramatic effect.
        //DOTween.To(() => Time.timeScale, x => Time.timeScale = x, (1.0f / playerHandle.timeFlow) * 0.2f, 0.02f);
        Time.timeScale = (1.0f / playerHandle.timeFlow) * 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    public override void stopAttack()
    {

        isAttacking = false;
        playerHandle.SetSpecialModeActive(false); // Clear special mode
        if (cutPlane != null)
            cutPlane.gameObject.SetActive(false);
        if (cameraController != null)
            cameraController.ClearOverrideFOV();
            cameraController.SetVolumeIntensity(0.0f);

        // Resume normal time.
        //DOTween.To(() => Time.timeScale, x => Time.timeScale = x, (1.0f / playerHandle.timeFlow) * 1f, 0.02f);
        Time.timeScale = (1.0f / playerHandle.timeFlow) * 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    /// <summary>
    /// Rotates the slicing plane based on horizontal mouse movement.
    /// </summary>
    public void RotatePlane()
    {
        if (cutPlane != null)
            cutPlane.eulerAngles += new Vector3(0, 0, -Input.GetAxis("Mouse X") * 5);
    }

    /// <summary>
    /// Performs a single slash.
    /// </summary>
    public void Slash()
    {
        // Check cooldown.
        if (Time.time < lastSlashTime + slashCooldown)
            return;
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
            // Trigger slash VFX.
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
