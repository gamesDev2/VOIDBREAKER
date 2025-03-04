using System.Collections;
using UnityEngine;
using DG.Tweening;
using EzySlice;

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

    // Optional: parameters for camera shake
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
        // Right mouse button toggles blade mode
        if (Input.GetMouseButtonDown(1))
        {
            ToggleBladeMode(true);
        }
        if (Input.GetMouseButtonUp(1))
        {
            ToggleBladeMode(false);
        }

        if (bladeMode)
        {
            // Smoothly align the player's rotation with the camera
            transform.rotation = Quaternion.Lerp(transform.rotation, mainCamera.transform.rotation, 0.2f);
            RotatePlane();

            // Left mouse button triggers the cut
            if (Input.GetMouseButtonDown(0))
            {
                // Only execute if there is a child available for animation
                if (cutPlane.childCount > 0)
                {
                    cutPlane.GetChild(0).DOComplete();
                    cutPlane.GetChild(0).DOLocalMoveX(cutPlane.GetChild(0).localPosition.x * -1, 0.05f).SetEase(Ease.OutExpo);
                }
                ShakeCamera();
                Slice();
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

        // Use the Camera_Controller to set the override FOV
        if (cameraController != null)
        {
            if (state)
                cameraController.SetOverrideFOV(zoomFOV);
            else
                cameraController.ClearOverrideFOV();
        }

        // Slow down or resume time
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
    /// Updated to recenter the cut pieces and apply a reduced explosion force.
    /// </summary>
    public void Slice()
    {
        Collider[] hits = Physics.OverlapBox(cutPlane.position, new Vector3(5, 0.1f, 5), cutPlane.rotation, layerMask);
        if (hits.Length <= 0)
            return;

        foreach (Collider col in hits)
        {
            SlicedHull hull = MeshCuttingSystem.SliceObject(col.gameObject, cutPlane.position, cutPlane.up, crossMaterial);
            if (hull != null)
            {
                GameObject lowerHull = hull.CreateLowerHull(col.gameObject, crossMaterial);
                GameObject upperHull = hull.CreateUpperHull(col.gameObject, crossMaterial);
                // Recenter the pieces using the original object's position and apply reduced explosion force
                MeshCuttingSystem.AddHullComponents(lowerHull, col.transform.position);
                MeshCuttingSystem.AddHullComponents(upperHull, col.transform.position);
                Destroy(col.gameObject);
            }
        }
    }

    /// <summary>
    /// Triggers a camera shake via the Camera_Controller.
    /// </summary>
    public void ShakeCamera()
    {
        if (cameraController != null)
        {
            cameraController.ShakeCamera(shakeStrength);
        }
    }
}
