using UnityEngine;

public class HideInShadowsAction : GOAPAction
{
    private bool completed = false;
    private Vector3 hidePosition;

    // Called once when the action starts
    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        // Check that the agent has heard a sound
        GOAPAgent goap = agent.GetComponent<GOAPAgent>();
        if (goap == null || !goap.beliefs.ContainsKey("heardSound") || !goap.beliefs["heardSound"])
            return false;

        // Find all potential shadow spots in the scene
        GameObject[] shadowSpots = GameObject.FindGameObjectsWithTag("ShadowSpot");
        if (shadowSpots.Length == 0)
            return false;

        // Select the nearest shadow spot
        float bestDist = Mathf.Infinity;
        GameObject bestSpot = null;
        foreach (GameObject spot in shadowSpots)
        {
            float dist = Vector3.Distance(agent.transform.position, spot.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestSpot = spot;
            }
        }

        if (bestSpot != null)
        {
            hidePosition = bestSpot.transform.position;
            target = bestSpot; // Optional: for visualization or further logic
            return true;
        }
        return false;
    }

    // Called every frame to perform the action
    public override bool Perform(GameObject agent)
    {
        GOAPAgent goap = agent.GetComponent<GOAPAgent>();
        if (goap == null)
            return false;

        // Command the agent to move toward the hide position
        goap.MoveTo(hidePosition);

        // Check if the agent is near the hiding spot
        float distance = Vector3.Distance(agent.transform.position, hidePosition);
        if (distance < 1.0f)
        {
            // Once in position, mark the action as complete
            completed = true;

            // Clear the heard sound belief so the agent does not continuously hide
            if (goap.beliefs.ContainsKey("heardSound"))
                goap.beliefs["heardSound"] = false;
        }
        return true;
    }

    // The action requires the agent to be in range of the target
    public override bool RequiresInRange()
    {
        return true;
    }

    public override bool IsDone()
    {
        return completed;
    }
}
