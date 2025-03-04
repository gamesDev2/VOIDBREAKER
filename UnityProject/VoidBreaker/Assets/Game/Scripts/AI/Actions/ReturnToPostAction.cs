using UnityEngine;
using UnityEngine.AI;

public class ReturnToPostAction : GOAPAction
{
    private bool reachedPost = false;
    private GOAPAgent goapAgent;

    public override bool CheckProceduralPrecondition(GameObject agentObj)
    {
        goapAgent = agentObj.GetComponent<GOAPAgent>();
        return goapAgent != null;
    }

    public override bool IsDone()
    {
        return reachedPost;
    }

    public override bool Perform(GameObject agentObj)
    {
        if (goapAgent == null)
        {
            goapAgent = agentObj.GetComponent<GOAPAgent>();
            if (goapAgent == null) return false;
        }

        Vector3 postPosition = goapAgent.GetAgentOriginalPosition();
        goapAgent.MoveTo(postPosition);

        // Check if the agent is near enough to consider the action complete.
        if (Vector3.Distance(agentObj.transform.position, postPosition) <= 0.5f)
        {
            reachedPost = true;
        }
        return true;
    }

    public override bool RequiresInRange()
    {
        // This action does not require an additional target reference.
        return false;
    }
}
