using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A fully-featured AI GOAPAgent that:
/// 1) Uses NavMeshAgent for pathfinding & local avoidance, 
///    while still letting a physics-based controller move the AI.
/// 2) Implements shape casts (sphere casts) for better obstacle detection.
/// 3) Keeps agents spaced out so they don't cluster.
/// 4) Handles midair players by sampling the ground under them.
/// 5) Returns to the agent's original post after combat if the player is gone.
/// 6) Includes advanced stuck recovery with multiple phases.
/// 
/// Make sure you have:
///  - AI_Movement_Controller + your Entity script on the same object.
///  - A baked NavMesh covering the floor.
///  - "AI" or a suitable layer assigned to your agents if you want them
///    to avoid each other with the agent spacing approach.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(AI_Movement_Controller))]
public class GOAPAgent : MonoBehaviour
{
    // ---------------------- GOAP-Related Fields ----------------------
    public List<GOAPAction> availableActions;      // All GOAP actions on this AI
    private Queue<GOAPAction> currentActions;      // The current action plan
    public Dictionary<string, bool> beliefs = new Dictionary<string, bool>();

    // ---------------------- Movement & NavMesh -----------------------
    private AI_Movement_Controller movementController;
    private NavMeshAgent navAgent;

    [Header("Movement Settings")]
    [SerializeField] private float stoppingDistance = 1.0f;

    // --- Stuck detection ---
    public float stuckThreshold = 0.1f;  // minimal movement to not be "stuck"
    public float stuckTime = 2f;         // how many seconds of near-zero movement => stuck
    public int maxStuckAttempts = 3;     // how many times we attempt recovery

    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private int stuckAttempts = 0;

    // ---------------------- Obstacle Avoidance -----------------------
    [Header("Obstacle Avoidance")]
    public float visionRayHeight = 0.5f;         // cast from near the agent's center
    public float obstacleAvoidanceDistance = 1.0f;
    public float sideRayAngle = 25f;             // angle for left/right checks
    public LayerMask visionObstacleLayers;       // layers for walls/obstacles

    // ---------------------- Agent Spacing ----------------------------
    [Header("Agent Spacing")]
    public float minAgentSpacing = 1.5f; // how far to keep from other AI
    public LayerMask agentLayerMask;     // layer used by other AI agents (e.g. "AI")

    // ---------------------- Midair Player Handling -------------------
    [Header("Midair Player Handling")]
    public float maxRaycastDown = 100f;  // how far we raycast down from the player
    public float sampleRadius = 2f;      // how wide we sample the NavMesh near that point

    // For re-issuing the last MoveTo if stuck
    private Vector3 lastTargetPos;
    private bool hasTarget = false;

    // ---------------------- Return to Original Post ------------------
    private Vector3 originalPosition;   // Where the agent started
    private bool isReturningToPost = false;

    // ----------------------------------------------------------------
    // ---------------------- Unity Lifecycle --------------------------
    // ----------------------------------------------------------------

    void Start()
    {
        // 1) Setup references
        movementController = GetComponent<AI_Movement_Controller>();
        navAgent = GetComponent<NavMeshAgent>();

        // 2) Disable NavMeshAgent's built-in movement
        navAgent.updatePosition = false;
        navAgent.updateRotation = false;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        navAgent.avoidancePriority = 50;
        navAgent.autoBraking = false;
        navAgent.stoppingDistance = stoppingDistance;

        // 3) Gather GOAP actions
        availableActions = new List<GOAPAction>(GetComponents<GOAPAction>());
        currentActions = new Queue<GOAPAction>();

        // 4) Initialize position tracking
        lastPosition = transform.position;
        originalPosition = transform.position; // store the agent's starting "post"
    }

    void OnEnable()
    {
        // Register with AIDirector if it exists
        if (AIDirector.Instance != null)
            AIDirector.Instance.RegisterAgent(this);
    }

    void OnDisable()
    {
        // Unregister from AIDirector if needed
        if (AIDirector.Instance != null)
            AIDirector.Instance.UnregisterAgent(this);
    }

    void Update()
    {
        // ----------------- GOAP Plan Execution -----------------
        if (currentActions.Count == 0)
        {
            PlanActions("AttackPlayer");
        }
        else
        {
            GOAPAction action = currentActions.Peek();
            if (action.IsDone())
            {
                currentActions.Dequeue();
            }
            else
            {
                // If action needs to be in range, we set the NavMeshAgent destination
                if (action.RequiresInRange() && !action.inRange && action.target != null)
                {
                    // If the target is midair, find the ground below them
                    Vector3 finalPos = GetGroundPositionIfMidair(action.target.transform.position);
                    MoveTo(finalPos);
                }
                action.Perform(gameObject);
            }
        }

        UpdateBeliefs();
        CheckStuck();   // see if we've moved recently

        // ----------------- Movement Integration -----------------
        // Let the NavMeshAgent do pathfinding & local avoidance,
        // but we do additional shape cast & agent spacing before feeding physics-based movement.

        Vector3 desiredVel = navAgent.desiredVelocity;
        if (desiredVel.sqrMagnitude < 0.01f)
        {
            // No movement needed
            movementController.SetAIInput(0f, 0f, false, false, false, false, transform.eulerAngles.y);
        }
        else
        {
            // Convert desired velocity to (horizontal, vertical)
            Vector3 flatVel = new Vector3(desiredVel.x, 0f, desiredVel.z);

            // 1) Sphere-cast to detect obstacles
            Vector3 finalDir = SphereCastAvoidObstacle(flatVel.normalized);

            // 2) Agent spacing
            finalDir = AdjustForAgentSpacing(finalDir);

            // 3) Convert finalDir to inputs
            float yaw = Mathf.Atan2(finalDir.x, finalDir.z) * Mathf.Rad2Deg;
            movementController.SetAIInput(finalDir.x, finalDir.z, false, false, false, false, yaw);
        }

        // Keep NavMeshAgent "in sync" with the actual position
        navAgent.nextPosition = transform.position;
    }

    // ----------------------------------------------------------------
    // ---------------------- Movement & Steering ----------------------
    // ----------------------------------------------------------------

    /// <summary>
    /// Sphere cast forward to detect obstacles. If blocked, we try left/right angles 
    /// to pick a better direction. This helps avoid corners or small objects.
    /// </summary>
    private Vector3 SphereCastAvoidObstacle(Vector3 forwardDir)
    {
        Vector3 start = transform.position + Vector3.up * visionRayHeight;
        float detectionRadius = 0.5f;  // radius for sphere cast
        float detectionDist = obstacleAvoidanceDistance;

        // Center cast
        bool centerBlocked = Physics.SphereCast(start, detectionRadius, forwardDir,
            out RaycastHit centerHit, detectionDist, visionObstacleLayers);

        if (!centerBlocked)
            return forwardDir;

        // If center is blocked, check left & right
        Vector3 leftDir = Quaternion.Euler(0, -sideRayAngle, 0) * forwardDir;
        Vector3 rightDir = Quaternion.Euler(0, sideRayAngle, 0) * forwardDir;

        bool leftBlocked = Physics.SphereCast(start, detectionRadius, leftDir,
            out _, detectionDist, visionObstacleLayers);
        bool rightBlocked = Physics.SphereCast(start, detectionRadius, rightDir,
            out _, detectionDist, visionObstacleLayers);

        if (!leftBlocked && rightBlocked)
        {
            return leftDir.normalized;
        }
        else if (!rightBlocked && leftBlocked)
        {
            return rightDir.normalized;
        }
        else if (!leftBlocked && !rightBlocked)
        {
            // Both open, pick left
            return leftDir.normalized;
        }
        else
        {
            // Both sides blocked => small sideways offset
            Vector3 sideStep = Vector3.Cross(forwardDir, Vector3.up).normalized;
            if (Random.value < 0.5f) sideStep = -sideStep;
            return (forwardDir + sideStep * 0.5f).normalized;
        }
    }

    /// <summary>
    /// Keeps AI agents spaced out by checking if other agents are within minAgentSpacing
    /// and steering away if needed.
    /// </summary>
    private Vector3 AdjustForAgentSpacing(Vector3 moveDir)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, minAgentSpacing, agentLayerMask);
        if (hits.Length <= 1) // includes self
            return moveDir;

        Vector3 separation = Vector3.zero;
        int count = 0;

        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject) continue;

            Vector3 offset = transform.position - col.transform.position;
            float dist = offset.magnitude;
            if (dist > 0.01f)
            {
                // Weighted separation (closer agents => stronger push)
                separation += offset.normalized / dist;
                count++;
            }
        }

        if (count > 0)
        {
            separation /= count;
            // Blend separation into moveDir
            moveDir += separation * 0.7f;
        }

        return moveDir.normalized;
    }

    /// <summary>
    /// If the target position is midair, we raycast down to find ground 
    /// and sample the NavMesh near that point.
    /// </summary>
    private Vector3 GetGroundPositionIfMidair(Vector3 rawPos)
    {
        if (Physics.Raycast(rawPos, Vector3.down, out RaycastHit hit, maxRaycastDown))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, sampleRadius, NavMesh.AllAreas))
            {
                return navHit.position;
            }
        }
        return rawPos; // fallback if no ground found
    }

    /// <summary>
    /// Tells the NavMeshAgent to path to 'destination', 
    /// storing it in case we need to re-issue on stuck.
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        lastTargetPos = destination;
        hasTarget = true;

        navAgent.SetDestination(destination);
    }

    // ----------------------------------------------------------------
    // ---------------------- Advanced Stuck Recovery ------------------
    // ----------------------------------------------------------------

    private void CheckStuck()
    {
        float movedDist = Vector3.Distance(transform.position, lastPosition);
        if (movedDist < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
        }
        else
        {
            stuckTimer = 0f;
            stuckAttempts = 0;
        }

        if (stuckTimer >= stuckTime)
        {
            stuckTimer = 0f;
            stuckAttempts++;

            Debug.Log($"{name} => Stuck attempt {stuckAttempts}/{maxStuckAttempts}.");

            StartCoroutine(AdvancedStuckRecoveryMultiPhase());

            if (stuckAttempts >= maxStuckAttempts)
            {
                // Could do something more drastic (teleport, bigger roam, etc.)
                stuckAttempts = 0;
            }
        }
        lastPosition = transform.position;
    }

    /// <summary>
    /// Multi-phase approach:
    /// 1) Reverse + small rotation
    /// 2) 180 turn
    /// 3) Random roam
    /// Check if freed after each phase.
    /// </summary>
    private IEnumerator AdvancedStuckRecoveryMultiPhase()
    {
        Debug.Log($"{name} => Starting advanced multi-phase unstuck routine.");

        // Phase 1: Reverse + rotate
        yield return ReverseAndRotate(0.5f, 90f);
        yield return new WaitForSeconds(0.4f);
        if (!IsStillStuck()) yield break;

        // Phase 2: full 180
        yield return FullTurn();
        yield return new WaitForSeconds(0.4f);
        if (!IsStillStuck()) yield break;

        // Phase 3: random roam
        yield return RandomRoam();
        yield return new WaitForSeconds(0.4f);
    }

    private bool IsStillStuck()
    {
        float dist = Vector3.Distance(transform.position, lastPosition);
        return dist < stuckThreshold;
    }

    private IEnumerator ReverseAndRotate(float reverseTime, float angle)
    {
        Debug.Log($"{name} => ReverseAndRotate for {reverseTime}s, ±{angle} deg.");

        // 1) Reverse
        float t = 0f;
        while (t < reverseTime)
        {
            Vector3 backward = -transform.forward;
            float yaw = transform.eulerAngles.y;
            movementController.SetAIInput(backward.x, backward.z, false, false, false, false, yaw);
            t += Time.deltaTime;
            yield return null;
        }

        // 2) Random rotation
        float randomAngle = (Random.value < 0.5f) ? -angle : angle;
        float finalYaw = transform.eulerAngles.y + randomAngle;
        movementController.SetAIInput(0, 0, false, false, false, false, finalYaw);
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator FullTurn()
    {
        Debug.Log($"{name} => Full 180 turn.");

        float finalYaw = transform.eulerAngles.y + 180f;
        movementController.SetAIInput(0, 0, false, false, false, false, finalYaw);
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator RandomRoam()
    {
        Debug.Log($"{name} => Random roam attempt.");

        Vector3 randomDir = Random.insideUnitSphere * 5f;
        randomDir += transform.position;
        randomDir.y = transform.position.y;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);

            float t = 0f;
            while (t < 2f)
            {
                if (!IsStillStuck())
                    yield break;
                t += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            Debug.Log($"{name} => Random roam point invalid, skipping.");
        }
    }

    // ----------------------------------------------------------------
    // ---------------------- Beliefs & Planning -----------------------
    // ----------------------------------------------------------------

    private void UpdateBeliefs()
    {
        // Example detection logic
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            beliefs["playerInAttackRange"] = (dist <= 1.5f);
            beliefs["playerSpotted"] = (dist < 10f);
        }
        else
        {
            // No player => they're presumably dead/gone
            beliefs["playerSpotted"] = false;
        }
    }

    /// <summary>
    /// If the player is alive and spotted, we follow. 
    /// Otherwise, we patrol or return to post if the player is gone.
    /// </summary>
    private void PlanActions(string goal)
    {
        currentActions.Clear();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            // Player is gone/dead → return to post
            isReturningToPost = true;
            MoveTo(originalPosition);
            return;
        }

        bool playerSpotted = false;
        beliefs.TryGetValue("playerSpotted", out playerSpotted);

        if (playerSpotted)
        {
            isReturningToPost = false;
            // Example: just follow the player
            GOAPAction follow = availableActions.Find(a => a is FollowPlayerAction);
            if (follow != null) currentActions.Enqueue(follow);
        }
        else
        {
            // No player spotted => patrol
            isReturningToPost = false;
            GOAPAction patrol = availableActions.Find(a => a is PatrolAction);
            if (patrol != null) currentActions.Enqueue(patrol);
        }
    }

    // ----------------------------------------------------------------
    // ---------------------- Director Messaging -----------------------
    // ----------------------------------------------------------------

    public void ReceiveMessage(AIDirectorMessage message)
    {
        Debug.Log($"{gameObject.name} received message: {message.content} (type {message.type})");
    }

    public void ReceivePlan(AIPlan plan)
    {
        if (plan.agentPlans.TryGetValue(this, out List<GOAPAction> assignedActions))
        {
            Debug.Log($"{gameObject.name} received advanced plan: {plan.planType}. Overriding current actions...");
            currentActions.Clear();
            foreach (var action in assignedActions)
            {
                currentActions.Enqueue(action);
            }
        }
    }
}
