using System.Collections;
using UnityEngine;
using DG.Tweening;
using EzySlice;
using UnityEngine.VFX;

public class BladeModeScript : MonoBehaviour
{
    [Header("Blade Mode Settings")]
    public bool bladeMode;

    [Header("Camera Settings")]
    public float normalFOV = 60f;
    public float zoomFOV = 15f;
    public Camera mainCamera; // Assign in inspector or get in Start
    private Camera_Controller cameraController; // Reference to your Camera_Controller

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
    [Tooltip("VFX Graph to trigger when a slash occurs (set not to auto-play).")]
    public VisualEffect slashVFX;
    [Tooltip("Particle System to play when a slash occurs.")]
    public ParticleSystem slashParticles;

    [Header("Optional: Camera Shake Settings")]
    public float shakeStrength = 0.5f;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (cameraController == null)
            cameraController = mainCamera.GetComponent<Camera_Controller>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cutPlane.gameObject.SetActive(false);
    }

    void Update()
    {
        // Right mouse button toggles blade mode.
        if (Input.GetMouseButtonDown(1))
            ToggleBladeMode(true);
        if (Input.GetMouseButtonUp(1))
            ToggleBladeMode(false);

        if (bladeMode)
        {
            // Smoothly align the player's rotation with the camera.
            transform.rotation = Quaternion.Lerp(transform.rotation, mainCamera.transform.rotation, 0.2f);
            RotatePlane();

            // Left mouse button triggers a slash if the cooldown has elapsed.
            if (Input.GetMouseButtonDown(0) && Time.time >= lastSlashTime + slashCooldown)
            {
                lastSlashTime = Time.time;  // Update last slash time.
                bool didSlice = Slice();
                if (didSlice)
                {
                    // Only execute if there is a child available for animation.
                    if (cutPlane.childCount > 0)
                    {
                        cutPlane.GetChild(0).DOComplete();
                        cutPlane.GetChild(0).DOLocalMoveX(cutPlane.GetChild(0).localPosition.x * -1, 0.05f)
                            .SetEase(Ease.OutExpo);
                    }
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
    }

    /// <summary>
    /// Toggles blade mode on or off.
    /// </summary>
    public void ToggleBladeMode(bool state)
    {
        bladeMode = state;
        cutPlane.localEulerAngles = Vector3.zero;
        cutPlane.gameObject.SetActive(state);

        // Use the Camera_Controller to set the override FOV.
        if (cameraController != null)
        {
            if (state)
                cameraController.SetOverrideFOV(zoomFOV);
            else
                cameraController.ClearOverrideFOV();
        }

        // Slow down or resume time.
        float targetTimeScale = state ? 0.2f : 1f;
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, targetTimeScale, 0.02f);
    }

    /// <summary>
    /// Rotates the slicing plane based on horizontal mouse movement.
    /// </summary>
    public void RotatePlane()
    {
        cutPlane.eulerAngles += new Vector3(0, 0, -Input.GetAxis("Mouse X") * 5);
    }

    /// <summary>
    /// Slices nearby objects using the MeshCuttingSystem.
    /// Returns true if at least one object was sliced.
    /// </summary>
    public bool Slice()
    {
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

                // Register cut pieces with the manager.
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

    /// <summary>
    /// Triggers a camera shake via the Camera_Controller.
    /// </summary>
    public void ShakeCamera()
    {
        if (cameraController != null)
            cameraController.ShakeCamera(shakeStrength);
    }
}
