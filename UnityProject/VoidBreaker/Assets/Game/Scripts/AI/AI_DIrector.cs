using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum MessageType
{
    Attack,
    Retreat,
    Regroup,
    Alert
}

public enum PlanType
{
    Attack,
    Flank,
    Defend,
    Retreat
}

public class AIDirector : MonoBehaviour
{
    public static AIDirector Instance;
    public List<GOAPAgent> agents = new List<GOAPAgent>();

    public float planningInterval = 5f;
    private float nextPlanTime = 0f;

    [Header("Player Health Threshold")]
    public float healthThreshold = 30f;  // Only give orders if player health < this

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterAgent(GOAPAgent agent)
    {
        if (!agents.Contains(agent))
            agents.Add(agent);
    }

    public void UnregisterAgent(GOAPAgent agent)
    {
        if (agents.Contains(agent))
            agents.Remove(agent);
    }

    public void SendMessage(AIDirectorMessage message)
    {
        if (message.receiver != null)
            message.receiver.ReceiveMessage(message);
    }

    public void BroadcastMessage(AIDirectorMessage message)
    {
        foreach (GOAPAgent agent in agents)
        {
            if (agent != message.sender)
                agent.ReceiveMessage(message);
        }
    }

    // Inform only the nearest agent within maxDistance
    public void InformNearestAgent(AIDirectorMessage message, float maxDistance)
    {
        GOAPAgent nearest = null;
        float bestDistance = float.MaxValue;
        foreach (GOAPAgent agent in agents)
        {
            if (agent == message.sender)
                continue;
            float dist = Vector3.Distance(message.sender.transform.position, agent.transform.position);
            if (dist < bestDistance && dist <= maxDistance)
            {
                bestDistance = dist;
                nearest = agent;
            }
        }
        if (nearest != null)
        {
            nearest.ReceiveMessage(message);
            Debug.Log("Director: " + message.sender.name + " informed " + nearest.name + " with message: " + message.content);
        }
        else
        {
            Debug.Log("Director: No nearby agent found to inform.");
        }
    }

    // Creates an  plan for all agents
    public AIPlan GeneratePlan()
    {
        AIPlan plan;
        if (agents.Count >= 3)
        {
            plan = new AIPlan(PlanType.Attack);
            foreach (GOAPAgent agent in agents)
            {
                List<GOAPAction> actions = new List<GOAPAction>();
                GOAPAction follow = agent.availableActions.Find(a => a is FollowPlayerAction);
                GOAPAction attack = agent.availableActions.Find(a => a is AttackAction);
                if (follow != null && attack != null)
                {
                    actions.Add(follow);
                    actions.Add(attack);
                }
                plan.agentPlans.Add(agent, actions);
            }
        }
        else
        {
            plan = new AIPlan(PlanType.Attack);
            foreach (GOAPAgent agent in agents)
            {
                List<GOAPAction> actions = new List<GOAPAction>();
                GOAPAction attack = agent.availableActions.Find(a => a is AttackAction);
                if (attack != null)
                    actions.Add(attack);
                plan.agentPlans.Add(agent, actions);
            }
        }
        return plan;
    }

    // Distribute plan to each agent
    public void DistributePlan(AIPlan plan)
    {
        foreach (var kv in plan.agentPlans)
        {
            kv.Key.ReceivePlan(plan);
        }
    }

    void Update()
    {
        // Only generate a plan if enough time passed
        if (Time.time >= nextPlanTime)
        {
            // Check if the player's health is below threshold
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // We assume the player has an Entity script or something with a GetHealth() method
                Entity playerEntity = player.GetComponent<Entity>();
                if (playerEntity != null && playerEntity.GetHealth() < healthThreshold)
                {
                    // Player is low on health => issue advanced plan
                    AIPlan plan = GeneratePlan();
                    DistributePlan(plan);
                    Debug.Log("AIDirector: Player health is low, distributing plan.");
                }
            }

            // Schedule next check
            nextPlanTime = Time.time + planningInterval;
        }
    }
}
