using UnityEngine;
using EzySlice;

public static class MeshCuttingSystem
{
    public static SlicedHull SliceObject(GameObject obj, Vector3 planePosition, Vector3 planeNormal, Material crossSectionMaterial = null)
    {
        var smr = obj.GetComponent<SkinnedMeshRenderer>();
        if (smr != null)
            return SliceSkinned(smr, planePosition, planeNormal, crossSectionMaterial);

        var mf = obj.GetComponent<MeshFilter>();
        if (mf == null)
            return null;

        return obj.Slice(planePosition, planeNormal, crossSectionMaterial);
    }
    public static GameObject FindSliceableRoot(GameObject obj)
    {
        // Traverse upward until we find a root that has either MeshFilter or SkinnedMeshRenderer
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.GetComponent<MeshFilter>() != null || current.GetComponent<SkinnedMeshRenderer>() != null)
                return current.gameObject;

            current = current.parent;
        }

        return obj; // fallback to the original object if nothing is found
    }

    private static SlicedHull SliceSkinned(SkinnedMeshRenderer skinned,
                                           Vector3 planePos,
                                           Vector3 planeNormal,
                                           Material crossSectionMat)
    {
        var bakedMesh = new Mesh();
#if UNITY_2020_1_OR_NEWER
        skinned.BakeMesh(bakedMesh, true);
#else
        skinned.BakeMesh(bakedMesh);
#endif

        var tmp = new GameObject($"{skinned.name}_BakedForSlice");
        tmp.transform.SetParent(skinned.transform.parent, false);
        tmp.transform.localPosition = skinned.transform.localPosition;
        tmp.transform.localRotation = skinned.transform.localRotation;
        tmp.transform.localScale = skinned.transform.localScale;

        var mf = tmp.AddComponent<MeshFilter>();
        var mr = tmp.AddComponent<MeshRenderer>();
        mf.sharedMesh = bakedMesh;
        mr.sharedMaterials = skinned.sharedMaterials;
        mr.shadowCastingMode = skinned.shadowCastingMode;
        mr.receiveShadows = skinned.receiveShadows;
        mr.lightProbeUsage = skinned.lightProbeUsage;
        mr.reflectionProbeUsage = skinned.reflectionProbeUsage;

        SlicedHull hull = tmp.Slice(planePos, planeNormal, crossSectionMat);

        Object.Destroy(tmp);
        Object.Destroy(bakedMesh);
        return hull;
    }

    public static void AddHullComponents(GameObject go, Vector3 originalPosition)
    {
        go.layer = 9;
        go.transform.position = originalPosition;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.convex = true;

        rb.AddExplosionForce(150, originalPosition, 5);
    }
}
