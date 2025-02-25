using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "GoalActions/FollowPlayerAction")]
public class FollowPlayerAction : GoalAction
{
    [Header("NavMesh Pathfinding")]
    public float pathRefreshInterval = 1.0f;

    [Tooltip("Distance at which the AI stops walking toward the player.")]
    public float stopDistance = 1f;

    [Header("Ally Separation")]
    public float allyAvoidDistance = 3f;
    public float separationWeight = 0.5f;

    [Header("Spherecast Avoidance")]
    public float spherecastRadius = 0.5f;
    public float spherecastDistance = 2f;
    [Tooltip("Layers for environment/walls. EXCLUDE your AI layer!")]
    public LayerMask obstacleLayers;

    [Header("Movement Speeds")]
    public float sprintDistance = 10f;

    // Internal
    private NavMeshPath path;
    private float nextPathRefreshTime;
    private int currentPathIndex;

    private void OnEnable()
    {
        effects.Clear();
        effects.Add("following");
    }

    public override bool CheckProceduralPrecondition(AI_Agent agent)
    {
        if (agent.player == null) return false;
        return (agent.GetComponent<NavMeshAgent>() != null);
    }

    public override bool ResetAction()
    {
        path = null;
        nextPathRefreshTime = 0f;
        currentPathIndex = 0;
        return true;
    }

    public override bool IsDone() { return false; }

    public override bool Perform(AI_Agent agent)
    {
        if (agent.player == null) return false;

        // 1) Movement & NavMeshAgent
        AI_Movement_Controller movement = agent.GetComponent<AI_Movement_Controller>();
        if (!movement) return false;

        NavMeshAgent navAgent = agent.GetComponent<NavMeshAgent>();
        if (!navAgent) return false;

        // 2) Recalc path occasionally
        float now = Time.time;
        if (path == null || now >= nextPathRefreshTime)
        {
            path = new NavMeshPath();
            bool gotPath = navAgent.CalculatePath(agent.player.position, path);
            Debug.Log($"{agent.name}: Calculating path... success={gotPath}");
            nextPathRefreshTime = now + pathRefreshInterval;
            currentPathIndex = 0;
        }

        if (path.corners.Length == 0)
        {
            Debug.Log($"{agent.name}: No path => stop.");
            movement.SetAIInput(0f, 0f, false, false, false, false, 0f);
            return true;
        }

        // 3) Next corner logic
        Vector3 currentCorner = path.corners[Mathf.Clamp(currentPathIndex, 0, path.corners.Length - 1)];
        float distToCorner = Vector3.Distance(agent.transform.position, currentCorner);
        if (distToCorner < 1f && currentPathIndex < path.corners.Length - 1)
        {
            Debug.Log($"{agent.name}: Reached corner {currentPathIndex}, next corner.");
            currentPathIndex++;
            currentCorner = path.corners[currentPathIndex];
        }

        // 4) Movement inputs
        float distanceToPlayer = Vector3.Distance(agent.transform.position, agent.player.position);
        float verticalInput = (distanceToPlayer > stopDistance) ? 1f : 0f;
        bool wantSprint = (distanceToPlayer > sprintDistance);

        // 5) Direction to corner
        Vector3 toCorner = currentCorner - agent.transform.position;
        toCorner.y = 0f;
        Vector3 moveDir = toCorner.normalized;

        // 6) Spherecast for obstacles
        Vector3 sphereOrigin = agent.transform.position + Vector3.up * 0.5f;
        if (Physics.SphereCast(sphereOrigin, spherecastRadius, moveDir,
                               out RaycastHit hit, spherecastDistance, obstacleLayers))
        {
            Debug.Log($"{agent.name}: Obstacle => side-step");
            Vector3 left = Quaternion.Euler(0, -45, 0) * moveDir;
            Vector3 right = Quaternion.Euler(0, 45, 0) * moveDir;

            bool leftBlocked = Physics.SphereCast(sphereOrigin, spherecastRadius, left, out _, spherecastDistance, obstacleLayers);
            bool rightBlocked = Physics.SphereCast(sphereOrigin, spherecastRadius, right, out _, spherecastDistance, obstacleLayers);

            if (!leftBlocked && rightBlocked)
            {
                moveDir = left.normalized;
            }
            else if (!rightBlocked && leftBlocked)
            {
                moveDir = right.normalized;
            }
            else if (!rightBlocked && !leftBlocked)
            {
                moveDir = ((left + right) * 0.5f).normalized;
            }
            else
            {
                // Both sides blocked => slow down
                verticalInput = 0.5f;
            }
        }

        // 7) Ally separation
        Vector3 separationDir = Vector3.zero;
        foreach (var ally in agent.knownAllies)
        {
            if (!ally) continue;
            float dist = Vector3.Distance(agent.transform.position, ally.transform.position);
            if (dist < allyAvoidDistance && dist > 0.01f)
            {
                // Repel away from ally
                Vector3 repulsion = (agent.transform.position - ally.transform.position).normalized
                                    * (allyAvoidDistance - dist);
                separationDir += repulsion;
            }
        }

        if (separationDir.sqrMagnitude > 0.001f)
        {
            separationDir.Normalize();
            Vector3 combined = moveDir + (separationDir * separationWeight);
            Debug.Log($"{agent.name}: separationDir={separationDir}, baseDir={moveDir}, final={combined}");
            moveDir = combined.normalized;
        }

        // 8) Final rotation
        float angle = 0f;
        if (moveDir.sqrMagnitude > 0.001f)
        {
            angle = Vector3.SignedAngle(agent.transform.forward, moveDir, Vector3.up);
        }

        if (verticalInput == 0f)
            Debug.Log($"{agent.name}: verticalInput=0 => stopping. distToPlayer={distanceToPlayer}");

        // 9) Apply to AI_Movement_Controller
        movement.SetAIInput(
            horizontal: 0f,
            vertical: verticalInput,
            wantSprint: wantSprint,
            wantCrouch: false,
            wantJump: false,
            wantDash: false,
            lookYaw: angle
        );

        return true;
    }
}
