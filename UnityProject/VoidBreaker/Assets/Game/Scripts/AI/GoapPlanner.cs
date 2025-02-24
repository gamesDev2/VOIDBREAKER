using System.Collections.Generic;
using UnityEngine;

public class GoapPlanner
{
    public Queue<GoalAction> Plan(GameObject agentObj, List<GoalAction> availableActions, Dictionary<string, bool> goal)
    {
        AI_Agent agent = agentObj.GetComponent<AI_Agent>();
        List<GoalAction> usableActions = new List<GoalAction>();
        foreach (var action in availableActions)
        {
            if (action.CheckProceduralPrecondition(agent))
                usableActions.Add(action);
        }

        // Simple search: if an action’s effects list contains any goal key with a true value, return that action.
        foreach (var action in usableActions)
        {
            foreach (var effect in action.effects)
            {
                if (goal.ContainsKey(effect) && goal[effect])
                {
                    Queue<GoalAction> plan = new Queue<GoalAction>();
                    plan.Enqueue(action);
                    return plan;
                }
            }
        }
        return null;
    }
}
