using System.Collections.Generic;
using UnityEngine;

public abstract class GoalAction : ScriptableObject
{
    public float cost = 1f;
    public List<string> effects = new List<string>();

    public abstract bool CheckProceduralPrecondition(AI_Agent agent);
    public abstract bool ResetAction();
    public abstract bool IsDone();
    public abstract bool Perform(AI_Agent agent);
}
