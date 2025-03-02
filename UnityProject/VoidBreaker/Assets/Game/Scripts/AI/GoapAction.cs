using System.Collections.Generic;
using UnityEngine;

public abstract class GOAPAction : MonoBehaviour
{
    public float cost = 1.0f;
    public Dictionary<string, bool> preconditions = new Dictionary<string, bool>();
    public Dictionary<string, bool> effects = new Dictionary<string, bool>();

    // The target GameObject for this action (if needed)
    public GameObject target;
    // Indicates whether the agent is within range of the target to perform the action
    public bool inRange = false;

    // Called before the action starts to reset its state
    public virtual void Reset()
    {
        inRange = false;
    }

    // Returns true when the action has been completed
    public abstract bool IsDone();

    // Checks whether the action can run (i.e. are any procedural preconditions met)
    public abstract bool CheckProceduralPrecondition(GameObject agent);

    // Performs the action. Return false if something fails.
    public abstract bool Perform(GameObject agent);

    // Does this action require the agent to be near its target?
    public virtual bool RequiresInRange()
    {
        return target != null;
    }

    // Set the target for the action
    public virtual void SetTarget(GameObject target)
    {
        this.target = target;
    }
}
