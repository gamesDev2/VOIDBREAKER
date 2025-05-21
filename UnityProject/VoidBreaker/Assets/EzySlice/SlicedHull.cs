using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EzySlice
{

    /**
	 * The final generated data structure from a slice operation. This provides easy access
	 * to utility functions and the final Mesh data for each section of the HULL.
	 */
    public sealed class SlicedHull
    {
        private Mesh upper_hull;
        private Mesh lower_hull;

        public SlicedHull(Mesh upperHull, Mesh lowerHull)
        {
            this.upper_hull = upperHull;
            this.lower_hull = lowerHull;
        }

        public GameObject CreateUpperHull(GameObject original)
        {
            return CreateUpperHull(original, null);
        }

        public GameObject CreateUpperHull(GameObject original, Material crossSectionMat)
        {
            GameObject newObject = CreateUpperHull();

            if (newObject != null)
            {
                CopyTransform(original, newObject);
                AssignMaterials(newObject, original, crossSectionMat, upper_hull);
            }

            return newObject;
        }

        public GameObject CreateLowerHull(GameObject original)
        {
            return CreateLowerHull(original, null);
        }

        public GameObject CreateLowerHull(GameObject original, Material crossSectionMat)
        {
            GameObject newObject = CreateLowerHull();

            if (newObject != null)
            {
                CopyTransform(original, newObject);
                AssignMaterials(newObject, original, crossSectionMat, lower_hull);
            }

            return newObject;
        }

        /**
		 * Generate a new GameObject from the upper hull of the mesh
		 * This function will return null if upper hull does not exist
		 */
        public GameObject CreateUpperHull()
        {
            return CreateEmptyObject("Upper_Hull", upper_hull);
        }

        /**
		 * Generate a new GameObject from the Lower hull of the mesh
		 * This function will return null if lower hull does not exist
		 */
        public GameObject CreateLowerHull()
        {
            return CreateEmptyObject("Lower_Hull", lower_hull);
        }

        public Mesh upperHull
        {
            get { return this.upper_hull; }
        }

        public Mesh lowerHull
        {
            get { return this.lower_hull; }
        }

        private static GameObject CreateEmptyObject(string name, Mesh hull)
        {
            if (hull == null)
            {
                return null;
            }

            GameObject newObject = new GameObject(name);
            newObject.AddComponent<MeshRenderer>();
            MeshFilter filter = newObject.AddComponent<MeshFilter>();
            filter.mesh = hull;
            return newObject;
        }

        private static void CopyTransform(GameObject source, GameObject dest)
        {
            if (source == null || dest == null) return;
            dest.transform.localPosition = source.transform.localPosition;
            dest.transform.localRotation = source.transform.localRotation;
            dest.transform.localScale = source.transform.localScale;
        }

        private static void AssignMaterials(GameObject newObject, GameObject original, Material crossSectionMat, Mesh hullMesh)
        {
            if (newObject == null || original == null || hullMesh == null) return;

            Material[] shared = null;
            Mesh mesh = null;

            var meshRenderer = original.GetComponent<MeshRenderer>();
            var meshFilter = original.GetComponent<MeshFilter>();
            var skinnedRenderer = original.GetComponent<SkinnedMeshRenderer>();

            if (meshRenderer != null)
            {
                shared = meshRenderer.sharedMaterials;
                mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            }
            else if (skinnedRenderer != null)
            {
                shared = skinnedRenderer.sharedMaterials;
                mesh = skinnedRenderer.sharedMesh;
            }

            // If mesh/materials were not found, search children
            if ((shared == null || mesh == null) && original.transform.childCount > 0)
            {
                foreach (Transform child in original.transform)
                {
                    if (shared == null)
                    {
                        var childRenderer = child.GetComponent<Renderer>();
                        if (childRenderer != null)
                            shared = childRenderer.sharedMaterials;
                    }

                    if (mesh == null)
                    {
                        var childFilter = child.GetComponent<MeshFilter>();
                        if (childFilter != null)
                            mesh = childFilter.sharedMesh;

                        var childSkin = child.GetComponent<SkinnedMeshRenderer>();
                        if (childSkin != null)
                            mesh = childSkin.sharedMesh;
                    }
                }
            }

            // Exit if still missing data
            if (shared == null || mesh == null)
            {
                if (crossSectionMat != null)
                    newObject.GetComponent<Renderer>().sharedMaterial = crossSectionMat;
                return;
            }

            if (mesh.subMeshCount == hullMesh.subMeshCount)
            {
                newObject.GetComponent<Renderer>().sharedMaterials = shared;
                return;
            }

            // Append cross-section material
            Material[] newShared = new Material[shared.Length + 1];
            System.Array.Copy(shared, newShared, shared.Length);
            newShared[shared.Length] = crossSectionMat;
            newObject.GetComponent<Renderer>().sharedMaterials = newShared;
        }

    }
}