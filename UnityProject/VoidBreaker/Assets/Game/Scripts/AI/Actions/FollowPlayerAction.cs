using UnityEngine;

public class FollowPlayerAction : GOAPAction
{
    private bool completed = false;
    private Transform player;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    public override bool IsDone() { return completed; }
    public override bool CheckProceduralPrecondition(GameObject agent) { return player != null; }
    public override bool Perform(GameObject agentObj)
    {
        if (player == null) return false;
        GOAPAgent goap = agentObj.GetComponent<GOAPAgent>();
        if (!goap) return false;

        // Move to player
        goap.MoveTo(player.position);

        // Mark done if close enough
        float dist = Vector3.Distance(agentObj.transform.position, player.position);
        if (dist < 1.0f)
            completed = true;
        return true;
    }

    public override bool RequiresInRange() { return false; }
}
