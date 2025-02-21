using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoapPlanner
{
    public Queue<GoapAction> Plan(
        Dictionary<string, GoapBelief> beliefs,
        List<GoapAction> availableActions,
        GoapGoal goal)
    {
        // Validate inputs
        if (beliefs == null || availableActions == null || goal == null)
        {
            Debug.LogError("Invalid input: beliefs, availableActions, or goal is null.");
            return null;
        }

        // Gather all the belief keys that must be satisfied
        var desiredKeys = goal.desiredBeliefKeys
            .Where(k => beliefs.ContainsKey(k) && !beliefs[k].Evaluate())
            .ToHashSet();

        if (desiredKeys.Count == 0)
        {
            // Goal is already satisfied
            return new Queue<GoapAction>();
        }

        // BFS to find a sequence of actions that can fulfill these beliefs
        var plan = new List<GoapAction>();
        if (!BuildPlan(desiredKeys, availableActions, new HashSet<GoapAction>(), plan, beliefs))
        {
            // No plan found
            Debug.LogWarning($"No plan found for goal: {goal.goalName}");
            return null;
        }

        // Reverse the plan (we built it backwards) and return as a queue
        plan.Reverse();
        return new Queue<GoapAction>(plan);
    }

    private bool BuildPlan(
        HashSet<string> desiredKeys,
        List<GoapAction> availableActions,
        HashSet<GoapAction> usedActions,
        List<GoapAction> plan,
        Dictionary<string, GoapBelief> beliefs)
    {
        // If no more desired keys to fulfill, we are done
        if (desiredKeys.Count == 0) return true;

        // Try to find an action that provides at least one of the desired keys
        foreach (var action in availableActions)
        {
            if (usedActions.Contains(action)) continue;

            // Check if this action's effects intersect with the desired keys
            var matchingEffects = action.effects.Intersect(desiredKeys).ToList();
            if (matchingEffects.Count == 0) continue;

            // Check if action's preconditions are all satisfied
            bool preconditionsMet = action.preconditions.All(p => beliefs.ContainsKey(p) && beliefs[p].Evaluate());
            if (!preconditionsMet) continue;

            // Also check if the action is feasible in code (e.g. path available)
            // For brevity, we skip that check, but you can call action.CheckProceduralPrecondition()

            // Mark action as used
            usedActions.Add(action);

            // We'll remove these matched effects from the desired set
            var newDesiredKeys = new HashSet<string>(desiredKeys);
            foreach (var eff in matchingEffects)
            {
                newDesiredKeys.Remove(eff);
            }

            // Recurse to see if we can fulfill the rest of the desired keys
            if (BuildPlan(newDesiredKeys, availableActions, usedActions, plan, beliefs))
            {
                plan.Add(action); // add this action to the plan
                return true;
            }

            // If we failed, revert usage
            usedActions.Remove(action);
        }

        return false;
    }
}
