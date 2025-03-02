using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    // ---------------------- Communication ----------------------------
    [Header("Communication")]
    public float informRange = 10f;     // radius in which we inform nearby agents
    private bool hasInformedTeammates = false; // to avoid spamming

    void Start()
    {
        movementController = GetComponent<AI_Movement_Controller>();
        navAgent = GetComponent<NavMeshAgent>();

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
                    // Attempt to find ground if player is midair
                    Vector3 finalPos;
                    if (TryGetGroundPosition(action.target.transform.position, out finalPos))
                    {
                        NavMeshPath pathCheck = new NavMeshPath();
                        navAgent.CalculatePath(finalPos, pathCheck);

                        if (pathCheck.status == NavMeshPathStatus.PathPartial ||
                            pathCheck.status == NavMeshPathStatus.PathInvalid)
                        {
                            Debug.Log(name + " => path partial to midair player, fallback to patrol or idle");
                            currentActions.Clear();
                        }
                        else
                        {
                            MoveTo(finalPos);
                        }
                    }
                    else
                    {
                        Debug.Log(name + " => no valid ground under midair player, fallback to patrol or idle");
                        currentActions.Clear();
                    }
                }
                action.Perform(gameObject);
            }
        }

        UpdateBeliefs();
        CheckStuck();

        // Movement: read agent's desiredVelocity, do obstacle/spacing, feed to physics
        Vector3 desiredVel = navAgent.desiredVelocity;
        if (desiredVel.sqrMagnitude < 0.01f)
        {
            movementController.SetAIInput(0f, 0f, false, false, false, false, transform.eulerAngles.y);
        }
        else
        {
            Vector3 flatVel = new Vector3(desiredVel.x, 0f, desiredVel.z);

            Vector3 finalDir = SphereCastAvoidObstacle(flatVel.normalized);
            finalDir = AdjustForAgentSpacing(finalDir);

            float yaw = Mathf.Atan2(finalDir.x, finalDir.z) * Mathf.Rad2Deg;
            movementController.SetAIInput(finalDir.x, finalDir.z, false, false, false, false, yaw);
        }

        navAgent.nextPosition = transform.position;
    }

    // ----------------------------------------------------------------
    // ---------------------- Movement & Steering ----------------------
    // ----------------------------------------------------------------

    private bool TryGetGroundPosition(Vector3 rawPos, out Vector3 groundPos)
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

        bool centerBlocked = Physics.SphereCast(start, detectionRadius, forwardDir, out RaycastHit centerHit, detectionDist, visionObstacleLayers);
        if (!centerBlocked) return forwardDir;

        Vector3 leftDir = Quaternion.Euler(0, -sideRayAngle, 0) * forwardDir;
        Vector3 rightDir = Quaternion.Euler(0, sideRayAngle, 0) * forwardDir;

        bool leftBlocked = Physics.SphereCast(start, detectionRadius, leftDir, out _, detectionDist, visionObstacleLayers);
        bool rightBlocked = Physics.SphereCast(start, detectionRadius, rightDir, out _, detectionDist, visionObstacleLayers);

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
            return leftDir.normalized; // both open, pick left
        }
        else
        {
            // both blocked => small sideways offset
            Vector3 sideStep = Vector3.Cross(forwardDir, Vector3.up).normalized;
            if (Random.value < 0.5f) sideStep = -sideStep;
            return (forwardDir + sideStep * 0.5f).normalized;
        }
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
                // Could do something more drastic
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
    // ---------------------- Beliefs & Planning -----------------------
    // ----------------------------------------------------------------

    private void UpdateBeliefs()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            bool wasSpotted = beliefs.ContainsKey("playerSpotted") && beliefs["playerSpotted"];

            bool inRange = (dist <= 1.5f);
            bool spottedNow = (dist < 10f);

            beliefs["playerInAttackRange"] = inRange;
            beliefs["playerSpotted"] = spottedNow;

            // If we just spotted the player and haven't informed teammates, do so
            if (!wasSpotted && spottedNow)
            {
                InformNearbyAgents();
            }
        }
        else
        {
            // No player => they're presumably dead/gone
            beliefs["playerSpotted"] = false;
            hasInformedTeammates = false;
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
                // AIDirectorMessage typically: public AIDirectorMessage(MessageType type, GOAPAgent sender, GOAPAgent receiver, string content)
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
        Debug.Log(gameObject.name + " received message: " + message.content + " (type " + message.type + ")");
        if (message.type == MessageType.Alert)
        {
            beliefs["playerSpotted"] = true;
        }
    }

    public void ReceivePlan(AIPlan plan)
    {
        List<GOAPAction> assignedActions;
        if (plan.agentPlans.TryGetValue(this, out assignedActions))
        {
            Debug.Log(gameObject.name + " received advanced plan: " + plan.planType + ". Overriding current actions...");
            currentActions.Clear();
            foreach (var action in assignedActions)
            {
                currentActions.Enqueue(action);
            }
        }
    }
}
