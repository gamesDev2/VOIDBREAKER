using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class MeshTrail : MonoBehaviour
{
    [Header("Tag Settings")]
    [Tooltip("All GameObjects with these tags will receive a trail.")]
    public List<string> Tags = new List<string>();

    [Header("Timing")]
    [Tooltip("Time interval between trail piece spawns")]
    public float spawnInterval = 0.1f;
    [Tooltip("Duration for each trail piece to fade out")]
    public float fadeDuration = 0.5f;

    [Header("Color & Material")]
    [Tooltip("Gradient used to pick a color for each new trail piece")]
    public Gradient colorGradient; // pretty cool not gonna lie
    [Tooltip("Material to apply to the trail pieces (must have a float property named _Alpha)")]
    public Material trailMaterial;

    [Header("Color Cycling")]
    [Tooltip("Increment in [0..1] for each spawned trail piece to sample the gradient.\n" +
             "Example: 0.1 means each piece steps 10% further along the gradient.")]
    public float colorTimeIncrement = 0.1f;

    // Internal
    private float colorTime;              // Tracks our position along the gradient [0..1]
    private Transform[] trailTargets;     // Populated automatically based on tags

    private void Start()
    {
        // Find all objects with the specified tags and store their transforms
        List<Transform> newTargets = new List<Transform>();
        foreach (string tag in Tags)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in taggedObjects)
            {
                newTargets.Add(obj.transform);
            }
        }
        trailTargets = newTargets.ToArray();

        if (trailTargets.Length == 0)
        {
            Debug.LogWarning("No GameObjects found with the specified tags for MeshTrail.");
        }
        if (Game_Manager.Instance != null)
        {
            Game_Manager.Instance.on_mesh_trail.AddListener(ToggleMeshTrail);
        }

        ToggleMeshTrail(true);
    }


    /// <summary>
    /// Called by the Game_Manager event: enable or disable the mesh trail.
    /// </summary>
    private void ToggleMeshTrail(bool enable)
    {
        if (enable)
        {
            // Only start if not already invoking
            if (!IsInvoking(nameof(SpawnTrails)))
            {
                InvokeRepeating(nameof(SpawnTrails), 0f, spawnInterval);
            }
        }
        else
        {
            // Cancel any active spawning
            CancelInvoke(nameof(SpawnTrails));
        }
    }

    /// <summary>
    /// Spawns the actual trail pieces for each target.
    /// </summary>
    private void SpawnTrails()
    {
        if (trailMaterial == null)
        {
            Debug.LogWarning("MeshTrail: Trail material is not assigned.");
            return;
        }

        // Advance through the gradient
        colorTime = Mathf.Repeat(colorTime + colorTimeIncrement, 1f);
        Color currentColor = colorGradient.Evaluate(colorTime);

        // Spawn a trail piece for each target
        foreach (Transform target in trailTargets)
        {
            if (target == null) continue;

            // Ensure the target has a MeshFilter and MeshRenderer
            MeshFilter targetMeshFilter = target.GetComponent<MeshFilter>();
            MeshRenderer targetMeshRenderer = target.GetComponent<MeshRenderer>();
            if (targetMeshFilter == null || targetMeshRenderer == null)
            {
                continue;
            }

            // Create a new GameObject for the trail piece
            GameObject trailPiece = new GameObject($"TrailPiece_{target.name}");
            trailPiece.transform.position = target.position;
            trailPiece.transform.rotation = target.rotation;
            trailPiece.transform.localScale = target.localScale;

            // Add a MeshFilter and MeshRenderer
            MeshFilter trailMeshFilter = trailPiece.AddComponent<MeshFilter>();
            MeshRenderer trailMeshRenderer = trailPiece.AddComponent<MeshRenderer>();

            // Duplicate the mesh
            trailMeshFilter.mesh = targetMeshFilter.mesh;

            // Create a new material instance for fading
            Material matInstance = new Material(trailMaterial);
            matInstance.SetColor("_Color", currentColor);
            matInstance.SetFloat("_Alpha", 1f);
            trailMeshRenderer.material = matInstance;

            // Tween the _Alpha property from 1 to 0 over fadeDuration
            matInstance
                .DOFloat(0f, "_Alpha", fadeDuration)
                .OnComplete(() => Destroy(trailPiece));
        }
    }
}
