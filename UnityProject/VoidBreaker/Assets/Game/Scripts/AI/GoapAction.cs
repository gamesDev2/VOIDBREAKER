using System.Collections.Generic;
using UnityEngine;

public abstract class GoapAction : ScriptableObject // Inherit from ScriptableObject so we can create assets in the editor
{
    [Header("Basic Info")]
    public string actionName;
    public float cost = 1f;

    [Header("Beliefs")]
    public List<string> preconditions; // Keys that must be true before this action can run
    public List<string> effects;       // Keys that become true after this action finishes

    // Called once when the action starts
    public virtual void OnActionStart(GameObject agent) { }

    // Called each frame while the action is active
    public virtual void OnActionUpdate(GameObject agent, float deltaTime) { }

    // Called once when the action stops (either done or aborted)
    public virtual void OnActionStop(GameObject agent) { }

    // Whether the action is finished
    public abstract bool IsDone { get; }

    // Check if this action can run at all under current conditions
    // (For example, do we have a valid path?)
    public virtual bool CheckProceduralPrecondition(GameObject agent) => true;
}
