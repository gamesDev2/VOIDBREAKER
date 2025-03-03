using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A GOAPAgent that:
/// 1) Uses NavMeshAgent + physics-based AI_Movement_Controller.
/// 2) Removes local line-of-sight checks; HPC manager calls ExternalSetPlayerSpotted(bool).
/// 3) Reverts to the single lowest-cost action if it loses line of sight.
/// 4) Has advanced stuck recovery, midair player handling, obstacle avoidance, etc.
/// 5) Now predicts the player landing position, tracks the last known ground position,
///    and initiates a jump when needed.
/// </summary>
[RequireComponent(typeof(NavMeshAgent), typeof(AI_Movement_Controller))]
public class GOAPAgent : MonoBehaviour
{
    // ---------------------- GOAP-Related Fields ----------------------
    public List<GOAPAction> availableActions;  // All GOAP actions on this AI
    private Queue<GOAPAction> currentActions;  // The current action plan
    public Dictionary<string, bool> beliefs = new Dictionary<string, bool>();

    // We'll store the old plan in a queue.
    // When we revert, we pick the single action with the lowest cost.
    private Queue<GOAPAction> previousActions = new Queue<GOAPAction>();

    // ---------------------- Movement & NavMesh -----------------------
    private AI_Movement_Controller movementController;
    private NavMeshAgent navAgent;

    [Header("Movement Settings")]
    [SerializeField] private float stoppingDistance = 1.0f;

    // --- Stuck detection ---
    public float stuckThreshold = 0.1f;
    public float stuckTime = 2f;
    public int maxStuckAttempts = 3;

    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private int stuckAttempts = 0;

    // ---------------------- Obstacle Avoidance -----------------------
    [Header("Obstacle Avoidance")]
    public float visionRayHeight = 0.5f;
    public float obstacleAvoidanceDistance = 1.0f;
    public float sideRayAngle = 25f;
    public LayerMask visionObstacleLayers;

    // ---------------------- Agent Spacing ----------------------------
    [Header("Agent Spacing")]
    public float minAgentSpacing = 1.5f;
    public LayerMask agentLayerMask;

    // ---------------------- Midair Player Handling -------------------
    [Header("Midair Player Handling")]
    public float maxRaycastDown = 100f;
    public float sampleRadius = 2f;

    private Vector3 lastTargetPos;
    [SerializeField] private bool hasTarget = false;

    // Original position for returning to post
    private Vector3 originalPosition;
    [SerializeField] private bool isReturningToPost = false;

    // ---------------------- Communication ----------------------------
    [Header("Communication")]
    public float informRange = 10f;
    private bool hasInformedTeammates = false;

    // For tracking line-of-sight changes
    private bool wasSpottedLastFrame = false;

    // NEW: Track the last-known ground position of the player
    public Vector3 lastKnownPlayerGroundPos { get; private set; }

    // NEW: For smoothing movement direction
    private Vector3 lastMoveDir = Vector3.zero;

    void Start()
    {
        movementController = GetComponent<AI_Movement_Controller>();
        navAgent = GetComponent<NavMeshAgent>();

        // Configure NavMeshAgent for physics-based movement
        navAgent.updatePosition = false;
        navAgent.updateRotation = false;
        navAgent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        navAgent.avoidancePriority = 50;
        navAgent.autoBraking = false;
        navAgent.stoppingDistance = stoppingDistance;

        availableActions = new List<GOAPAction>(GetComponents<GOAPAction>());
        currentActions = new Queue<GOAPAction>();

        lastPosition = transform.position;
        originalPosition = transform.position;
    }

    void OnEnable()
    {
        if (AIDirector.Instance != null)
            AIDirector.Instance.RegisterAgent(this);
    }

    void OnDisable()
    {
        if (AIDirector.Instance != null)
            AIDirector.Instance.UnregisterAgent(this);
    }

    void Update()
    {
        // Sync the NavMeshAgent’s internal position first
        navAgent.nextPosition = transform.position;

        // Execute or plan GOAP actions
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
                if (action.RequiresInRange() && !action.inRange && action.target != null)
                {
                    Vector3 finalPos;
                    if (TryGetGroundPosition(action.target.transform.position, out finalPos))
                    {
                        NavMeshPath pathCheck = new NavMeshPath();
                        navAgent.CalculatePath(finalPos, pathCheck);

                        if (pathCheck.status == NavMeshPathStatus.PathPartial ||
                            pathCheck.status == NavMeshPathStatus.PathInvalid)
                        {
                            Debug.Log(name + " => partial path, fallback to patrol or idle");
                            currentActions.Clear();
                        }
                        else
                        {
                            MoveTo(finalPos);
                        }
                    }
                    else
                    {
                        Debug.Log(name + " => no valid ground, fallback to patrol or idle");
                        currentActions.Clear();
                    }
                }
                action.Perform(gameObject);
            }
        }

        // Update last-known player ground position when player is visible
        bool playerSpotted = false;
        beliefs.TryGetValue("playerSpotted", out playerSpotted);
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerSpotted && playerObj != null)
        {
            Vector3 playerPos = playerObj.transform.position;
            Vector3 predictedGround;
            if (TryGetGroundPosition(playerPos, out predictedGround))
            {
                lastKnownPlayerGroundPos = predictedGround;
            }
            else
            {
                lastKnownPlayerGroundPos = playerPos;
            }
        }

        // Stuck detection, etc.
        CheckStuck();

        // --- Movement Logic with Jump and Smooth Steering ---
        Vector3 desiredVel = navAgent.desiredVelocity;
        if (desiredVel.sqrMagnitude < 0.01f)
        {
            // No movement: if jump is needed, check below.
            movementController.SetAIInput(0f, 0f, false, false, false, false, transform.eulerAngles.y);
        }
        else
        {
            // Remove vertical component from desired velocity.
            Vector3 flatVel = new Vector3(desiredVel.x, 0f, desiredVel.z);
            Vector3 desiredDir = flatVel.normalized;
            // Smooth steering using last frame’s direction.
            if (lastMoveDir == Vector3.zero)
                lastMoveDir = desiredDir;
            Vector3 blendedDir = Vector3.Lerp(lastMoveDir, desiredDir, 0.1f).normalized;
            Vector3 avoidanceDir = SphereCastAvoidObstacle(blendedDir);
            Vector3 finalDir = AdjustForAgentSpacing(avoidanceDir);
            lastMoveDir = finalDir;

            // --- Jump Logic ---
            // Use lastTargetPos (set via MoveTo) as the destination.
            float jumpUpThreshold = 1.5f;    // Trigger jump if target is significantly higher.
            float jumpDownThreshold = -3.0f; // Trigger jump if target is significantly lower.
            float horizontalDist = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(lastTargetPos.x, lastTargetPos.z)
            );
            float verticalDiff = lastTargetPos.y - transform.position.y;
            bool jumpFlag = false;
            if (horizontalDist < 3f && verticalDiff > jumpUpThreshold)
                jumpFlag = true;
            if (horizontalDist < 5f && verticalDiff < jumpDownThreshold)
                jumpFlag = true;

            float yaw = Mathf.Atan2(finalDir.x, finalDir.z) * Mathf.Rad2Deg;
            movementController.SetAIInput(finalDir.x, finalDir.z, false, false, jumpFlag, false, yaw);
        }

        // Optional second sync:
        navAgent.nextPosition = transform.position;
    }

    // ----------------------------------------------------------------
    // ---------------------- HPC Approach: ExternalSetPlayerSpotted ----------------------
    // ----------------------------------------------------------------

    /// <summary>
    /// Called by the HPC line-of-sight manager to tell this agent whether it sees the player.
    /// This replaces any local distance-based spotting logic.
    /// </summary>
    public void ExternalSetPlayerSpotted(bool spotted)
    {
        bool wasSpotted = beliefs.ContainsKey("playerSpotted") && beliefs["playerSpotted"];
        beliefs["playerSpotted"] = spotted;

        // If we just spotted the player
        if (!wasSpotted && spotted)
        {
            StorePreviousActions();
            InformNearbyAgents();
        }
        // If we had line-of-sight but lost it
        else if (wasSpotted && !spotted)
        {
            RestoreLowestCostAction();
        }

        wasSpottedLastFrame = spotted;
    }

    // ----------------------------------------------------------------
    // ---------------------- Debug Drawing -----------------------------
    // ----------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        // Draw the NavMesh path (if any)
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
            // Mark path corners
            Gizmos.color = Color.blue;
            foreach (Vector3 corner in corners)
            {
                Gizmos.DrawSphere(corner, 0.1f);
            }
        }

        // Draw the current target position
        if (hasTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastTargetPos, 0.2f);
        }

        // Draw predicted player landing point (if player is in air)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Vector3 playerPos = playerObj.transform.position;
            Vector3 predictedGround;
            if (TryGetGroundPosition(playerPos, out predictedGround))
            {
                // If there is a significant difference, assume player is airborne.
                if (Mathf.Abs(predictedGround.y - playerPos.y) > 0.5f)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(predictedGround, 0.2f);
                }
            }
        }

        // Draw last known player ground position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(lastKnownPlayerGroundPos, 0.2f);
    }

    // ----------------------------------------------------------------
    // ---------------------- Movement & Steering Helpers -------------
    // ----------------------------------------------------------------

    // Changed to public so it can be used by other scripts (e.g., FollowPlayerAction)
    public bool TryGetGroundPosition(Vector3 rawPos, out Vector3 groundPos)
    {
        groundPos = Vector3.zero;
        if (Physics.Raycast(rawPos, Vector3.down, out RaycastHit hit, maxRaycastDown))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, sampleRadius, NavMesh.AllAreas))
            {
                groundPos = navHit.position;
                return true;
            }
        }
        return false;
    }

    private Vector3 SphereCastAvoidObstacle(Vector3 forwardDir)
    {
        Vector3 start = transform.position + Vector3.up * visionRayHeight;
        float detectionRadius = 0.5f;
        float detectionDist = obstacleAvoidanceDistance;

        if (Physics.SphereCast(start, detectionRadius, forwardDir, out RaycastHit centerHit, detectionDist, visionObstacleLayers))
        {
            // If the detected obstacle is the player, ignore it.
            if (centerHit.collider != null && centerHit.collider.CompareTag("Player"))
            {
                return forwardDir;
            }

            // Calculate avoidance factor, but clamp its maximum influence.
            float factor = Mathf.Clamp((detectionDist - centerHit.distance) / detectionDist, 0f, 0.3f);
            Vector3 hitNormal = centerHit.normal;
            hitNormal.y = 0; // limit adjustments to horizontal plane

            // Blend the forward direction with a reflected vector off the obstacle
            Vector3 avoidanceDir = Vector3.Lerp(forwardDir, Vector3.Reflect(forwardDir, hitNormal), factor);
            return avoidanceDir.normalized;
        }
        return forwardDir;
    }

    private Vector3 AdjustForAgentSpacing(Vector3 moveDir)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, minAgentSpacing, agentLayerMask);
        if (hits.Length <= 1) return moveDir;

        Vector3 separation = Vector3.zero;
        int count = 0;
        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject) continue;

            Vector3 offset = transform.position - col.transform.position;
            float dist = offset.magnitude;
            if (dist > 0.01f)
            {
                separation += offset.normalized / dist;
                count++;
            }
        }

        if (count > 0)
        {
            separation /= count;
            moveDir += separation * 0.7f;
        }
        return moveDir.normalized;
    }

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

            Debug.Log(name + " => Stuck attempt " + stuckAttempts + "/" + maxStuckAttempts);
            StartCoroutine(AdvancedStuckRecoveryMultiPhase());

            if (stuckAttempts >= maxStuckAttempts)
            {
                stuckAttempts = 0;
            }
        }
        lastPosition = transform.position;
    }

    private IEnumerator AdvancedStuckRecoveryMultiPhase()
    {
        Debug.Log(name + " => Starting advanced multi-phase unstuck routine.");

        // Phase 1: Reverse + rotate
        yield return StartCoroutine(ReverseAndRotate(0.5f, 90f));
        yield return new WaitForSeconds(0.4f);
        if (!IsStillStuck()) yield break;

        // Phase 2: full 180
        yield return StartCoroutine(FullTurn());
        yield return new WaitForSeconds(0.4f);
        if (!IsStillStuck()) yield break;

        // Phase 3: random roam
        yield return StartCoroutine(RandomRoam());
        yield return new WaitForSeconds(0.4f);
    }

    private bool IsStillStuck()
    {
        float dist = Vector3.Distance(transform.position, lastPosition);
        return dist < stuckThreshold;
    }

    private IEnumerator ReverseAndRotate(float reverseTime, float angle)
    {
        Debug.Log(name + " => ReverseAndRotate for " + reverseTime + "s ±" + angle + " deg.");

        float t = 0f;
        while (t < reverseTime)
        {
            Vector3 backward = -transform.forward;
            float yaw = transform.eulerAngles.y;
            movementController.SetAIInput(backward.x, backward.z, false, false, false, false, yaw);
            t += Time.deltaTime;
            yield return null;
        }

        float randomAngle = (Random.value < 0.5f) ? -angle : angle;
        float finalYaw = transform.eulerAngles.y + randomAngle;
        movementController.SetAIInput(0, 0, false, false, false, false, finalYaw);
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator FullTurn()
    {
        Debug.Log(name + " => Full 180 turn.");
        float finalYaw = transform.eulerAngles.y + 180f;
        movementController.SetAIInput(0, 0, false, false, false, false, finalYaw);
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator RandomRoam()
    {
        Debug.Log(name + " => Random roam attempt.");
        Vector3 randomDir = Random.insideUnitSphere * 5f;
        randomDir += transform.position;
        randomDir.y = transform.position.y;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDir, out hit, 5f, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);

            float t = 0f;
            while (t < 2f)
            {
                if (!IsStillStuck()) yield break;
                t += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            Debug.Log(name + " => Random roam point invalid, skipping.");
        }
    }

    // ----------------------------------------------------------------
    // ---------------------- Planning & Reverting ---------------------
    // ----------------------------------------------------------------

    /// <summary>
    /// Saves the current plan as "previous" so we can revert if we lose sight.
    /// </summary>
    private void StorePreviousActions()
    {
        if (currentActions.Count > 0)
        {
            previousActions = new Queue<GOAPAction>(currentActions);
        }
        else
        {
            previousActions.Clear();
        }
    }

    /// <summary>
    /// Reverts to the single action with the lowest cost from the old plan.
    /// </summary>
    private void RestoreLowestCostAction()
    {
        if (previousActions.Count > 0)
        {
            // Find the single action with the lowest cost
            GOAPAction bestAction = null;
            float bestCost = float.MaxValue;

            foreach (GOAPAction a in previousActions)
            {
                if (a.cost < bestCost)
                {
                    bestCost = a.cost;
                    bestAction = a;
                }
            }

            currentActions.Clear();

            if (bestAction != null)
            {
                Debug.Log(name + " => Lost line of sight, picking lowest-cost action: "
                          + bestAction.name + " cost=" + bestCost);
                currentActions.Enqueue(bestAction);
            }

            previousActions.Clear();
        }
    }

    private void PlanActions(string goal)
    {
        currentActions.Clear();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            isReturningToPost = true;
            MoveTo(originalPosition);
            return;
        }

        bool playerSpotted = false;
        beliefs.TryGetValue("playerSpotted", out playerSpotted);

        if (playerSpotted)
        {
            isReturningToPost = false;
            GOAPAction follow = availableActions.Find(a => a is FollowPlayerAction);
            if (follow != null) currentActions.Enqueue(follow);
        }
        else
        {
            isReturningToPost = false;
            GOAPAction patrol = availableActions.Find(a => a is PatrolAction);
            if (patrol != null) currentActions.Enqueue(patrol);
        }
    }

    // ----------------------------------------------------------------
    // ---------------------- Communication ----------------------------
    // ----------------------------------------------------------------

    private void InformNearbyAgents()
    {
        if (hasInformedTeammates) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, informRange, agentLayerMask);
        foreach (Collider col in hits)
        {
            if (col.gameObject == this.gameObject) continue;

            GOAPAgent otherAgent = col.GetComponent<GOAPAgent>();
            if (otherAgent != null)
            {
                AIDirectorMessage msg = new AIDirectorMessage(
                    MessageType.Alert,
                    this,
                    otherAgent,
                    "Player spotted!"
                );
                otherAgent.ReceiveMessage(msg);
            }
        }

        hasInformedTeammates = true;
    }

    public void ReceiveMessage(AIDirectorMessage message)
    {
        Debug.Log(gameObject.name + " received message: " + message.msgContent
                  + " (type " + message.msgType + ")");
        if (message.msgType == MessageType.Alert)
        {
            beliefs["playerSpotted"] = true;
        }
    }

    public void ReceivePlan(AIPlan plan)
    {
        List<GOAPAction> assignedActions;
        if (plan.agentPlans.TryGetValue(this, out assignedActions))
        {
            Debug.Log(gameObject.name + " received advanced plan: "
                      + plan.planType + ". Overriding current actions...");
            currentActions.Clear();
            foreach (var action in assignedActions)
            {
                currentActions.Enqueue(action);
            }
        }
    }

    public Vector3 GetAgentOriginalPosition()
    {
        return originalPosition;
    }
}
