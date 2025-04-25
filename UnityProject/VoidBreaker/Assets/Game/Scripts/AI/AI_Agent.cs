using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Note: MessageType is defined in AI_Director.cs
public enum EnemyType
{
    Simple,
    Brute,
    Stalker
}

[RequireComponent(typeof(NavMeshAgent), typeof(AI_Movement_Controller))]
public class GOAPAgent : MonoBehaviour
{
    // ---------------------- Enemy Configuration ----------------------
    [Header("Enemy Type")]
    public EnemyType enemyType = EnemyType.Simple;
    [Tooltip("For Simple enemies: health ≤ this value triggers a retreat.")]
    public float fleeHealthThreshold = 30f;

    // ---------------------- GOAP-Related Fields ----------------------
    public List<GOAPAction> availableActions;
    private Queue<GOAPAction> currentActions;
    public Dictionary<string, bool> beliefs = new Dictionary<string, bool>();
    private Queue<GOAPAction> previousActions = new Queue<GOAPAction>();

    // ---------------------- Movement & NavMesh -----------------------
    private AI_Movement_Controller movementController;
    private NavMeshAgent navAgent;

    [Header("Movement Settings")]
    [SerializeField] private float stoppingDistance = 1.0f;

    // Stuck detection
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
    private Vector3 originalPosition;
    [SerializeField] private bool isReturningToPost = false;

    // ---------------------- Communication ----------------------------
    [Header("Communication")]
    public float informRange = 10f;
    private bool hasInformedTeammates = false;

    // For smoothing movement direction
    private Vector3 lastMoveDir = Vector3.zero;

    // Track the last-known ground position of the player
    public Vector3 lastKnownPlayerGroundPos { get; private set; }

    // ---------------------- Steering Limits -------------------------
    [Header("Steering Limits")]
    [Tooltip("Max permitted angle (degrees) between direct‑to‑target direction and actual steering.")]
    public float maxSteerDeviation = 45f;

    // ---------------------- Health Reference ------------------------
    private Entity entity;

    void Start()
    {
        movementController = GetComponent<AI_Movement_Controller>();
        navAgent = GetComponent<NavMeshAgent>();
        entity = GetComponent<Entity>();

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
        navAgent.nextPosition = transform.position;

        // Plan or execute actions
        if (currentActions.Count == 0)
        {
            PlanActions("AttackPlayer");
        }
        else
        {
            var action = currentActions.Peek();
            if (action.IsDone())
                currentActions.Dequeue();
            else
            {
                if (action.RequiresInRange() && !action.inRange && action.target != null)
                {
                    Vector3 finalPos;
                    if (TryGetGroundPosition(action.target.transform.position, out finalPos))
                    {
                        var path = new NavMeshPath();
                        navAgent.CalculatePath(finalPos, path);
                        if (path.status == NavMeshPathStatus.PathPartial ||
                            path.status == NavMeshPathStatus.PathInvalid)
                        {
                            Debug.Log(name + " => partial path, fallback");
                            currentActions.Clear();
                        }
                        else
                        {
                            MoveTo(finalPos);
                        }
                    }
                    else
                    {
                        Debug.Log(name + " => no valid ground, fallback");
                        currentActions.Clear();
                    }
                }
                action.Perform(gameObject);
            }
        }

        // Update last-known ground position
        bool spotted = false;
        beliefs.TryGetValue("playerSpotted", out spotted);
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (spotted && playerObj != null)
        {
            Vector3 p = playerObj.transform.position, g;
            lastKnownPlayerGroundPos = TryGetGroundPosition(p, out g) ? g : p;
        }

        CheckStuck();

        // Movement & steering
        Vector3 desiredVel = navAgent.desiredVelocity;
        if (desiredVel.sqrMagnitude < 0.01f)
        {
            movementController.SetAIInput(0f, 0f, false, false, false, false, transform.eulerAngles.y);
        }
        else
        {
            // Base steering
            Vector3 flat = new Vector3(desiredVel.x, 0f, desiredVel.z);
            Vector3 dir = flat.normalized;
            if (lastMoveDir == Vector3.zero) lastMoveDir = dir;
            Vector3 blended = Vector3.Lerp(lastMoveDir, dir, 0.1f).normalized;
            Vector3 avoid = SphereCastAvoidObstacle(blended);
            Vector3 finalDir = AdjustForAgentSpacing(avoid);

            // Clamp to maxSteerDeviation
            Vector3 toTarget = (lastTargetPos - transform.position).normalized;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                float maxRad = maxSteerDeviation * Mathf.Deg2Rad;
                finalDir = Vector3.RotateTowards(toTarget, finalDir, maxRad, 0f).normalized;
            }

            lastMoveDir = finalDir;

            // Jump logic
            float jumpUp = 1.5f, jumpDown = -3f;
            float horiz = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.z),
                new Vector2(lastTargetPos.x, lastTargetPos.z)
            );
            float vert = lastTargetPos.y - transform.position.y;
            bool jumpFlag = (horiz < 3f && vert > jumpUp) || (horiz < 5f && vert < jumpDown);

            float yaw = Mathf.Atan2(finalDir.x, finalDir.z) * Mathf.Rad2Deg;
            movementController.SetAIInput(finalDir.x, finalDir.z, false, false, jumpFlag, false, yaw);
        }

        navAgent.nextPosition = transform.position;
    }

    public void ExternalSetPlayerSpotted(bool spotted)
    {
        bool was = beliefs.ContainsKey("playerSpotted") && beliefs["playerSpotted"];
        beliefs["playerSpotted"] = spotted;
        if (!was && spotted)
        {
            StorePreviousActions();
            InformNearbyAgents(MessageType.Alert);
        }
        else if (was && !spotted)
        {
            RestoreLowestCostAction();
        }
    }

    private void InformNearbyAgents(MessageType msgType)
    {
        if (hasInformedTeammates) return;
        var hits = Physics.OverlapSphere(transform.position, informRange, agentLayerMask);
        foreach (var c in hits)
        {
            if (c.gameObject == gameObject) continue;
            var other = c.GetComponent<GOAPAgent>();
            if (other != null)
                other.ReceiveMessage(new AIDirectorMessage(msgType, this, other,
                    msgType == MessageType.Alert ? "Player spotted!" : "Retreat!"));
        }
        hasInformedTeammates = true;
    }

    public void ReceiveMessage(AIDirectorMessage msg)
    {
        switch (msg.msgType)
        {
            case MessageType.Alert:
                StorePreviousActions();
                beliefs["playerSpotted"] = true;
                break;
            case MessageType.Retreat:
                StorePreviousActions();
                beliefs["retreat"] = true;
                break;
        }
    }

    private void StorePreviousActions()
    {
        if (currentActions.Count > 0)
            previousActions = new Queue<GOAPAction>(currentActions);
        else
            previousActions.Clear();
    }

    private void RestoreLowestCostAction()
    {
        if (previousActions.Count == 0) return;
        GOAPAction best = null;
        float bestCost = float.MaxValue;
        foreach (var a in previousActions)
            if (a.cost < bestCost) { bestCost = a.cost; best = a; }
        currentActions.Clear();
        if (best != null) currentActions.Enqueue(best);
        previousActions.Clear();
    }

    private void PlanActions(string goal)
    {
        currentActions.Clear();
        bool spotted = beliefs.ContainsKey("playerSpotted") && beliefs["playerSpotted"];

        // Simple: low health => retreat
        if (enemyType == EnemyType.Simple && entity != null && entity.GetHealth() <= fleeHealthThreshold)
        {
            InformNearbyAgents(MessageType.Retreat);
            var flee = availableActions.Find(a => a is ReturnToPostAction);
            if (flee != null) { currentActions.Enqueue(flee); return; }
        }

        // Brute
        if (enemyType == EnemyType.Brute)
        {
            if (spotted)
            {
                var atk = availableActions.Find(a => a is AttackAction);
                if (atk != null) currentActions.Enqueue(atk);
            }
            else
            {
                var pat = availableActions.Find(a => a is PatrolAction);
                if (pat != null) currentActions.Enqueue(pat);
            }
            return;
        }

        // Stalker
        if (enemyType == EnemyType.Stalker)
        {
            if (spotted)
            {
                var hide = availableActions.Find(a => a is HideFromPlayerAction);
                if (hide != null) currentActions.Enqueue(hide);
            }
            else
            {
                var pat = availableActions.Find(a => a is ReturnToPostAction);
                if (pat != null) currentActions.Enqueue(pat);
            }
            return;
        }

        // Default: attack or patrol
        if (spotted)
        {
            var atk = availableActions.Find(a => a is AttackAction);
            if (atk != null) currentActions.Enqueue(atk);
        }
        else
        {
            var pat = availableActions.Find(a => a is PatrolAction);
            if (pat != null) currentActions.Enqueue(pat);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (navAgent != null && navAgent.hasPath)
        {
            Gizmos.color = Color.cyan;
            var corners = navAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            Gizmos.color = Color.blue;
            foreach (var c in corners)
                Gizmos.DrawSphere(c, 0.1f);
        }

        if (hasTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastTargetPos, 0.2f);
        }

        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            Vector3 p = pObj.transform.position;
            Vector3 ground;
            if (TryGetGroundPosition(p, out ground) && Mathf.Abs(ground.y - p.y) > 0.5f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(ground, 0.2f);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(lastKnownPlayerGroundPos, 0.2f);
    }

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

    private Vector3 SphereCastAvoidObstacle(Vector3 dir)
    {
        Vector3 start = transform.position + Vector3.up * visionRayHeight;
        if (Physics.SphereCast(start, 0.5f, dir, out RaycastHit hit, obstacleAvoidanceDistance, visionObstacleLayers))
        {
            if (hit.collider.CompareTag("Player")) return dir;
            float f = Mathf.Clamp((obstacleAvoidanceDistance - hit.distance) / obstacleAvoidanceDistance, 0f, 0.3f);
            var n = hit.normal; n.y = 0f;
            return Vector3.Lerp(dir, Vector3.Reflect(dir, n), f).normalized;
        }
        return dir;
    }

    private Vector3 AdjustForAgentSpacing(Vector3 moveDir)
    {
        var hits = Physics.OverlapSphere(transform.position, minAgentSpacing, agentLayerMask);
        if (hits.Length <= 1) return moveDir;
        Vector3 sep = Vector3.zero; int cnt = 0;
        foreach (var c in hits)
        {
            if (c.gameObject == gameObject) continue;
            var off = transform.position - c.transform.position;
            float d = off.magnitude;
            if (d > 0.01f) { sep += off.normalized / d; cnt++; }
        }
        if (cnt > 0) sep /= cnt;
        moveDir += sep * 0.7f;
        return moveDir.normalized;
    }

    public void MoveTo(Vector3 destination)
    {
        lastTargetPos = destination;
        hasTarget = true;
        navAgent.SetDestination(destination);
    }

    private void CheckStuck()
    {
        float moved = Vector3.Distance(transform.position, lastPosition);
        if (moved < stuckThreshold) stuckTimer += Time.deltaTime;
        else { stuckTimer = 0f; stuckAttempts = 0; }

        if (stuckTimer >= stuckTime)
        {
            stuckTimer = 0f;
            stuckAttempts++;
            StartCoroutine(AdvancedStuckRecoveryMultiPhase());
            if (stuckAttempts >= maxStuckAttempts) stuckAttempts = 0;
        }
        lastPosition = transform.position;
    }

    private IEnumerator AdvancedStuckRecoveryMultiPhase()
    {
        yield return ReverseAndRotate(0.5f, 90f);
        yield return new WaitForSeconds(0.4f);
        if (!IsStillStuck()) yield break;
        yield return FullTurn();
        yield return new WaitForSeconds(0.4f);
        if (!IsStillStuck()) yield break;
        yield return RandomRoam();
        yield return new WaitForSeconds(0.4f);
    }

    private bool IsStillStuck()
    {
        return Vector3.Distance(transform.position, lastPosition) < stuckThreshold;
    }

    private IEnumerator ReverseAndRotate(float time, float angle)
    {
        float t = 0f;
        while (t < time)
        {
            Vector3 back = -transform.forward;
            movementController.SetAIInput(back.x, back.z, false, false, false, false, transform.eulerAngles.y);
            t += Time.deltaTime;
            yield return null;
        }
        float a = Random.value < 0.5f ? -angle : angle;
        movementController.SetAIInput(0, 0, false, false, false, false, transform.eulerAngles.y + a);
        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator FullTurn()
    {
        movementController.SetAIInput(0, 0, false, false, false, false, transform.eulerAngles.y + 180f);
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator RandomRoam()
    {
        Vector3 rnd = Random.insideUnitSphere * 5f;
        rnd.y = transform.position.y;
        rnd += transform.position;
        if (NavMesh.SamplePosition(rnd, out NavMeshHit hit, 5f, NavMesh.AllAreas))
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
