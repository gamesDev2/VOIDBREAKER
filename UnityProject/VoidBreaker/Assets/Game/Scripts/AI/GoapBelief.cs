using System;

[Serializable]
public class GoapBelief // Represents a single belief that the agent can have about the world
{
    public string key;         // Unique identifier (e.g. "HealthLow")
    public Func<bool> test;    // Function returning true if this belief is satisfied

    public GoapBelief(string key, Func<bool> test)
    {
        this.key = key;
        this.test = test;
    }

    public bool Evaluate() // calls the test function to determine if the belief is true
    {
        return test.Invoke();
    }
}
