using UnityEngine;

public class PatrolAction : GOAPAction
{
    public Transform[] waypoints;
    public float waypointTolerance = 1f; // how close is "close enough" to a waypoint

    private int currentWaypointIndex = 0;
    private bool completed = false;
    private GOAPAgent goapAgent;

    void Start()
    {
        // Optionally load waypoints from a manager or define them in Inspector
    }

    public override bool IsDone()
    {
        return completed;
    }

    public override bool CheckProceduralPrecondition(GameObject agentObj)
    {
        // If we have at least one waypoint, the action is valid
        return (waypoints != null && waypoints.Length > 0);
    }

    public override bool Perform(GameObject agentObj)
    {
        if (goapAgent == null)
            goapAgent = agentObj.GetComponent<GOAPAgent>();

        if (!goapAgent) return false;

        // Move to current waypoint
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        goapAgent.MoveTo(targetWaypoint.position);

        // Check distance
        float dist = Vector3.Distance(agentObj.transform.position, targetWaypoint.position);
        if (dist <= waypointTolerance)
        {
            // Move to next waypoint
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }

        // If the player is spotted or in range, we can consider the patrol "done"
        // so the AI can switch to another action (follow, attack, etc.)
        bool playerSpotted = false;
        goapAgent.beliefs.TryGetValue("playerSpotted", out playerSpotted);
        if (playerSpotted)
        {
            completed = true;
        }

        return true;
    }

    public override bool RequiresInRange()
    {
        // We do not require a single target object for patrolling,
        // so this can be false (or return false).
        return false;
    }
}
