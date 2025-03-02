using System.Collections.Generic;

public class AIPlan
{
    public PlanType planType;
    public Dictionary<GOAPAgent, List<GOAPAction>> agentPlans;

    public AIPlan(PlanType type)
    {
        planType = type;
        agentPlans = new Dictionary<GOAPAgent, List<GOAPAction>>();
    }
}