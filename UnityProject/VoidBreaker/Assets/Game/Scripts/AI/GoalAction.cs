using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A base ScriptableObject for GOAP-like actions.
/// </summary>
public abstract class GoalAction : ScriptableObject
{
    public float cost = 1f;

    // For simplicity, we store "effects" as a list of keys the action can satisfy
    public List<string> effects = new List<string>();

    /// <summary> Checks if this action can be run given the current state of the agent. </summary>
    public abstract bool CheckProceduralPrecondition(AI_Agent agent);

    /// <summary> Performs the action on the agent. </summary>
    public abstract bool Perform(AI_Agent agent);

    /// <summary> Returns whether this action is complete. </summary>
    public abstract bool IsDone();

    /// <summary> Resets any internal state of the action. </summary>
    public abstract void ResetAction();
}
