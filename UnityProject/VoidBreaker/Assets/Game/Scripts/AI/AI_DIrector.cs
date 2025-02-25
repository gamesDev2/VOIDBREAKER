using System.Collections.Generic;
using UnityEngine;

public class AIDirectorGoap : MonoBehaviour
{
    [Header("Agents Under Director Control")]
    public List<AI_Agent> agents;

    [Header("Player Reference")]
    public GameObject player;

    [Header("Director Logic")]
    public float groupAttackDistance = 15f;
    public bool forceGroupAttack; // For debug toggling in the Inspector

    void Awake()
    {
        // For each agent, build a list of the other agents (allies)
        for (int i = 0; i < agents.Count; i++)
        {
            List<AI_Agent> allyList = new List<AI_Agent>(agents);
            allyList.RemoveAt(i); // remove self
            agents[i].knownAllies = allyList;
        }
    }

    void Update()
    {
        // If the player is invalid or not tagged as "Player," do nothing
        if (player == null || !player.CompareTag("Player"))
            return;

        // Decide the global goal for each agent
        foreach (var agent in agents)
        {
            if (agent == null) continue;
            agent.player = player.transform;

            float distance = Vector3.Distance(agent.transform.position, player.transform.position);

            // If "forceGroupAttack" is true or player is close, set goal to "coordinatedAttack"
            // otherwise default to "following"
            if (forceGroupAttack || distance < groupAttackDistance)
            {
                SetAgentGoal(agent, "coordinatedAttack");
            }
            else
            {
                SetAgentGoal(agent, "following");
            }
        }
    }

    private void SetAgentGoal(AI_Agent agent, string goalKey)
    {
        agent.goal.Clear();
        agent.goal.Add(goalKey, true);
    }
}
