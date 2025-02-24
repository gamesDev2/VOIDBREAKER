using System.Collections.Generic;
using UnityEngine;

public class AI_Agent : MonoBehaviour
{
    public Transform player;

    // ScriptableObject actions assigned in the Inspector
    public List<GoalAction> availableActionsSO = new List<GoalAction>();

    // The current goal state
    public Dictionary<string, bool> goal = new Dictionary<string, bool>();

    // New: references to other agents (assigned by AIDirectorGoap)
    public List<AI_Agent> knownAllies = new List<AI_Agent>();

    private List<GoalAction> actions;
    private Queue<GoalAction> currentActions;
    private GoapPlanner planner;

    void Start()
    {
        planner = new GoapPlanner();
        // Copy from the assigned ScriptableObject list
        actions = new List<GoalAction>(availableActionsSO);
        Debug.Log($"{gameObject.name}: Found {actions.Count} GOAP actions (ScriptableObjects).");

        // Default goal if the director doesn't override
        goal.Clear();
        goal.Add("following", true);
    }

    void Update()
    {
        if (player == null)
        {
            // The director is supposed to assign this
            return;
        }

        // If no plan or we've finished the plan, plan again
        if (currentActions == null || currentActions.Count == 0)
        {
            currentActions = planner.Plan(gameObject, actions, goal);
            if (currentActions == null)
            {
                Debug.LogWarning($"{gameObject.name}: No valid plan found for goal(s) {string.Join(", ", goal.Keys)}");
                return;
            }
            else
            {
                Debug.Log($"{gameObject.name}: Plan found with {currentActions.Count} action(s).");
            }
        }

        // Execute the current action
        if (currentActions.Count > 0)
        {
            GoalAction action = currentActions.Peek();
            if (!action.Perform(this))
            {
                Debug.LogWarning($"{gameObject.name}: Action {action.name} failed to perform.");
            }
        }
    }
}
