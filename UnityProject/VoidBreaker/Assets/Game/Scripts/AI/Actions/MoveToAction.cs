using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "GOAP/Actions/MoveToTargetAction")]
public class MoveToTargetAction : GoapAction
{
    private bool reachedDestination;
    private NavMeshAgent navAgent;

    public override bool IsDone => reachedDestination;

    public override void OnActionStart(GameObject agent)
    {
        navAgent = agent.GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            reachedDestination = true;
            return;
        }

        // Example: move 5 units in front of the agent
        Vector3 dest = agent.transform.position + agent.transform.forward * 5f;
        navAgent.SetDestination(dest);
        reachedDestination = false;
    }

    public override void OnActionUpdate(GameObject agent, float deltaTime)
    {
        if (navAgent == null) return;

        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            reachedDestination = true;
        }
    }

    public override void OnActionStop(GameObject agent)
    {
        // Optionally reset or stop the NavMeshAgent
        // navAgent.ResetPath();
    }
}
