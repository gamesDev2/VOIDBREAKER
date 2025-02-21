using System;
using System.Collections.Generic;

[Serializable]
public class GoapGoal
{
    public string goalName;                   // e.g. "AttackPlayer"
    public float priority;                    // Higher means more important
    public HashSet<string> desiredBeliefKeys; // Belief keys that must become true

    public GoapGoal(string name, float priority, params string[] desiredKeys)
    {
        this.goalName = name;
        this.priority = priority;
        this.desiredBeliefKeys = new HashSet<string>(desiredKeys);
    }
}
