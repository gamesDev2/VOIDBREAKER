using UnityEngine;
using UnityEngine.AI;

public static class NavMeshHelper
{
    /// <summary>
    /// Attempts to find a valid NavMesh position near 'worldPos'. 
    /// Typically used for the player's ground location if the player is in midair.
    /// </summary>
    public static bool GetGroundNavmeshPosition(Vector3 worldPos, float sampleRadius, out Vector3 navmeshPos)
    {
        navmeshPos = Vector3.zero;

        // Raycast down from worldPos to find ground
        if (Physics.Raycast(worldPos, Vector3.down, out RaycastHit hit, 100f))
        {
            Vector3 groundPos = hit.point;
            // Sample the NavMesh near groundPos
            if (NavMesh.SamplePosition(groundPos, out NavMeshHit navHit, sampleRadius, NavMesh.AllAreas))
            {
                navmeshPos = navHit.position;
                return true;
            }
        }
        return false;
    }
}
