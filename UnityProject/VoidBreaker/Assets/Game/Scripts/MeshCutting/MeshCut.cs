using UnityEngine;
using EzySlice;

public static class MeshCuttingSystem
{
    /// <summary>
    /// Attempts to slice the provided GameObject using the specified plane.
    /// </summary>
    public static SlicedHull SliceObject(GameObject obj, Vector3 planePosition, Vector3 planeNormal, Material crossSectionMaterial = null)
    {
        if (obj.GetComponent<MeshFilter>() == null)
            return null;

        return obj.Slice(planePosition, planeNormal, crossSectionMaterial);
    }

    /// <summary>
    /// Adds physics components to the sliced piece, re-centering it at the original position and applying a milder explosion force.
    /// </summary>
    /// <param name="go">The sliced GameObject.</param>
    /// <param name="originalPosition">The original object's position.</param>
    public static void AddHullComponents(GameObject go, Vector3 originalPosition)
    {
        // Set the layer as needed.
        go.layer = 9;
        // Recenter the piece to the original object's position.
        go.transform.position = originalPosition;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.convex = true;

        // Apply a reduced explosion force.
        rb.AddExplosionForce(150, originalPosition, 5);
    }
}
