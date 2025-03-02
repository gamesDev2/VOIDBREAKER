// GOAPGoal.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GOAPGoal
{
    public string name;
    public Dictionary<string, bool> goalState;
    public int priority;

    public GOAPGoal(string name, int priority, Dictionary<string, bool> goalState)
    {
        this.name = name;
        this.priority = priority;
        this.goalState = goalState;
    }
}
