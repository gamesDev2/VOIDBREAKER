using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GoapAgent : MonoBehaviour
{
    [Header("Local Goals")]
    public List<GoapGoal> localGoals = new List<GoapGoal>();

    [Header("Actions (Scriptable Objects)")]
    public List<GoapAction> actionAssets = new List<GoapAction>();

    [HideInInspector]
    public GoapGoal currentDirectorGoal; // Assigned by AIDirector

    private Dictionary<string, GoapBelief> beliefs;
    private GoapPlanner planner;
    private Queue<GoapAction> actionPlan;
    private GoapAction currentAction;

    private NavMeshAgent navAgent;

    // Example stat
    public float Health = 100f;

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        planner = new GoapPlanner();

        // Initialize beliefs
        beliefs = new Dictionary<string, GoapBelief>
        {
            ["HealthLow"] = new GoapBelief("HealthLow", () => Health < 30),
            ["HealthHigh"] = new GoapBelief("HealthHigh", () => Health >= 80),
            // Add more as needed...
        };
    }

    void Update()
    {
        // If we're not executing an action, plan or re-plan
        if (currentAction == null)
        {
            var bestGoal = ChooseBestGoal();
            if (bestGoal != null)
            {
                actionPlan = planner.Plan(beliefs, actionAssets, bestGoal);
                if (actionPlan != null && actionPlan.Count > 0)
                {
                    currentAction = actionPlan.Dequeue();
                    currentAction.OnActionStart(gameObject);
                }
            }
        }
        else
        {
            // Execute current action
            currentAction.OnActionUpdate(gameObject, Time.deltaTime);

            // If done, move to the next action
            if (currentAction.IsDone)
            {
                currentAction.OnActionStop(gameObject);
                currentAction = null;

                if (actionPlan != null && actionPlan.Count > 0)
                {
                    currentAction = actionPlan.Dequeue();
                    currentAction.OnActionStart(gameObject);
                }
            }
        }
    }

    private GoapGoal ChooseBestGoal()
    {
        // 1) If Director assigned a goal and it's not satisfied, use that
        if (currentDirectorGoal != null && !IsGoalSatisfied(currentDirectorGoal))
        {
            return currentDirectorGoal;
        }

        // 2) Otherwise, pick the highest-priority local goal that is unsatisfied
        GoapGoal bestGoal = null;
        float bestPriority = 0f;

        foreach (var g in localGoals)
        {
            if (!IsGoalSatisfied(g) && g.priority > bestPriority)
            {
                bestGoal = g;
                bestPriority = g.priority;
            }
        }

        return bestGoal;
    }

    private bool IsGoalSatisfied(GoapGoal goal)
    {
        // If all desired beliefs are true, the goal is satisfied
        if (goal == null || goal.desiredBeliefKeys == null || goal.desiredBeliefKeys.Count == 0)
        {
            Debug.LogWarning("Goal is null or has no desired beliefs.");
            return false;
        }

        foreach (var key in goal.desiredBeliefKeys)
        {
            if (!beliefs.ContainsKey(key))
            {
                Debug.LogWarning($"Belief '{key}' is missing.");
                return false;
            }
            if (!beliefs[key].Evaluate())
            {
                Debug.Log($"Belief '{key}' is not satisfied.");
                return false;
            }
        }
        return true;
    }
}
