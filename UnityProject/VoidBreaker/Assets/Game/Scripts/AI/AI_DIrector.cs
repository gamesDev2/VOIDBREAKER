using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs; // for RaycastCommand
using System.Collections.Generic;

/// <summary>
/// Message types exchanged between agents via the Director.
/// </summary>
public enum MessageType
{
    Attack,
    Retreat,
    Regroup,
    Alert
}

/// <summary>
/// Broad‑stroke plan categories.
/// </summary>
public enum PlanType
{
    Attack,
    Flank,
    Defend,
    Retreat
}

/// <summary>
/// The central "AI Director" – aware of player stats and each agent's state.
/// It periodically builds an AIPlan  that contains *per‑agent* GOAP action
/// overrides based on:
///   • Player position / health / energy
///   • Each agent's distance to the player & own health
///   • The agent's EnemyType (Simple ▸ hit‑&‑run,  Brute ▸ frontal assault,  Stalker ▸ flank / hide)
/// The plan is then dispatched; each GOAPAgent replaces its current plan
/// with the provided override list.
/// </summary>
public class AIDirector : MonoBehaviour
{
    // ────────────────────────────────────────────────────────────────────────────────
    #region Singleton
    public static AIDirector Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        DisposeHPCArrays();
        if (Instance == this) Instance = null;
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────────
    #region Inspector‑Tweaks
    [Header("Agent Management")]
    public List<GOAPAgent> agents = new List<GOAPAgent>();

    [Header("Plan Generation")]
    [Tooltip("Seconds between adaptive re‑planning.")]
    public float planningInterval = 3f;

    [Header("HPC Line‑of‑Sight Settings")]
    public float maxLoSDistance = 30f;
    public LayerMask obstacleMask;

    [Header("Player Thresholds (percent)")]
    public float playerLowHealthPct = 0.35f;
    public float playerLowEnergyPct = 0.25f;

    [Header("Distance Thresholds (metres)")]
    public float meleeDistance = 4f;
    public float engageDistance = 15f;
    public float stalkDistance = 22f;
    #endregion

    // ────────────────────────────────────────────────────────────────────────────────
    #region Private‑State
    private float _nextPlanTime = 0f;

    // HPC arrays
    private NativeArray<RaycastCommand> _raycastCommands;
    private NativeArray<RaycastHit> _raycastHits;

    // Player references
    private Transform _playerTf;
    private Entity _playerEntity;   // gives us health + energy
    #endregion

    // ────────────────────────────────────────────────────────────────────────────────
    #region Lifecycle
    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTf = playerObj.transform;
            _playerEntity = playerObj.GetComponent<Entity>();
        }

        if (agents.Count > 0) AllocateHPCArrays(agents.Count);
    }

    void Update()
    {
        DoHPCLineOfSight();

        if (Time.time >= _nextPlanTime)
        {
            AIPlan plan = BuildAdaptivePlan();
            DistributeAdaptivePlan(plan);
            _nextPlanTime = Time.time + planningInterval;
        }
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────────
    #region Adaptive‑Planning
    /// <summary>
    /// Construct a brand‑new <see cref="AIPlan"/> containing individual GOAP action
    /// lists for *every* registered agent.
    /// </summary>
    private AIPlan BuildAdaptivePlan()
    {
        AIPlan plan = new AIPlan(PlanType.Attack); // the specific PlanType is mostly cosmetic here

        // If we do not have the player, bail out – every agent will patrol.
        if (_playerTf == null || _playerEntity == null)
        {
            foreach (var ag in agents)
            {
                var patrol = ag.availableActions.Find(a => a is PatrolAction);
                plan.agentPlans[ag] = patrol != null ? new List<GOAPAction> { patrol } : new List<GOAPAction>();
            }
            return plan;
        }

        float playerHealthPct = _playerEntity.GetHealth() / _playerEntity.MaxHealth;
        float playerEnergyPct = (_playerEntity.GetType().GetMethod("GetEnergy") != null)
                               ? _playerEntity.GetEnergy() / _playerEntity.MaxEnergy
                               : 1f;   // if energy not implemented, treat as full

        foreach (var agent in agents)
        {
            if (agent == null) continue;

            List<GOAPAction> list = new List<GOAPAction>();

            // Grab agent stats
            Entity ent = agent.GetComponent<Entity>();
            float agentHPpct = ent != null ? ent.GetHealth() / ent.MaxHealth : 1f;
            float distToPlayer = Vector3.Distance(agent.transform.position, _playerTf.position);

            switch (agent.enemyType)
            {
                case EnemyType.Brute:
                    EvaluateBrute(agent, list, agentHPpct, distToPlayer);
                    break;

                case EnemyType.Simple:
                    EvaluateSimple(agent, list, agentHPpct, distToPlayer, playerEnergyPct);
                    break;

                case EnemyType.Stalker:
                    EvaluateStalker(agent, list, distToPlayer, playerHealthPct);
                    break;
            }

            // Always fall back to patrol if we somehow produced nothing.
            if (list.Count == 0)
            {
                GOAPAction patrol = agent.availableActions.Find(a => a is PatrolAction);
                if (patrol != null) list.Add(patrol);
            }

            plan.agentPlans[agent] = list;
        }

        return plan;
    }

    // ────────────────────────────────────────────────────────────────────────────────
    #region Per‑type Evaluators
    private void EvaluateBrute(GOAPAgent ag, List<GOAPAction> list, float hp, float dist)
    {
        if (hp < 0.30f)
        {
            // Low HP ▸ flee / return to post
            var flee = ag.availableActions.Find(a => a is ReturnToPostAction);
            if (flee != null) list.Add(flee);
            return;
        }

        // In range? attack, otherwise push forward.
        if (dist <= meleeDistance)
        {
            var atk = ag.availableActions.Find(a => a is AttackAction);
            if (atk != null) list.Add(atk);
        }
        else
        {
            var follow = ag.availableActions.Find(a => a is FollowPlayerAction);
            if (follow != null) list.Add(follow);
        }
    }

    private void EvaluateSimple(GOAPAgent ag, List<GOAPAction> list, float hp, float dist, float playerEnergy)
    {
        if (hp < 0.20f)
        {
            var flee = ag.availableActions.Find(a => a is ReturnToPostAction);
            if (flee != null) list.Add(flee);
            return;
        }

        // Simple enemies become aggressive when the player is exhausted.
        if (playerEnergy < playerLowEnergyPct)
        {
            var atk = ag.availableActions.Find(a => a is AttackAction);
            if (atk != null) list.Add(atk);
        }
        else
        {
            var follow = ag.availableActions.Find(a => a is FollowPlayerAction);
            if (follow != null) list.Add(follow);
        }
    }

    private void EvaluateStalker(GOAPAgent ag, List<GOAPAction> list, float dist, float playerHP)
    {
        // If too close, disappear / hide.
        if (dist < meleeDistance)
        {
            var hide = ag.availableActions.Find(a => a is HideFromPlayerAction);
            if (hide != null) list.Add(hide);
            return;
        }

        // If the player is weak, circle in for a kill.
        if (playerHP < playerLowHealthPct && dist < engageDistance)
        {
            var atk = ag.availableActions.Find(a => a is AttackAction);
            if (atk != null) list.Add(atk);
        }
        else
        {
            // Maintain stalking distance
            var stalk = ag.availableActions.Find(a => a is FollowPlayerAction);
            if (stalk != null) list.Add(stalk);
        }
    }
    #endregion
    #endregion

    // ────────────────────────────────────────────────────────────────────────────────
    #region Plan Distribution
    private void DistributeAdaptivePlan(AIPlan plan)
    {
        foreach (var kvp in plan.agentPlans)
        {
            kvp.Key.ReceivePlan(plan);
        }
    }
    #endregion

    // ────────────────────────────────────────────────────────────────────────────────
    #region HPC Line‑of‑Sight
    private void AllocateHPCArrays(int count)
    {
        _raycastCommands = new NativeArray<RaycastCommand>(count, Allocator.Persistent);
        _raycastHits = new NativeArray<RaycastHit>(count, Allocator.Persistent);
    }

    private void DisposeHPCArrays()
    {
        if (_raycastCommands.IsCreated) _raycastCommands.Dispose();
        if (_raycastHits.IsCreated) _raycastHits.Dispose();
    }

    private void ReallocateHPCArraysIfNeeded()
    {
        if (agents.Count == 0) { DisposeHPCArrays(); return; }
        if (!_raycastCommands.IsCreated || _raycastCommands.Length != agents.Count)
        {
            DisposeHPCArrays();
            AllocateHPCArrays(agents.Count);
        }
    }

    public void RegisterAgent(GOAPAgent ag) { if (!agents.Contains(ag)) agents.Add(ag); ReallocateHPCArraysIfNeeded(); }
    public void UnregisterAgent(GOAPAgent ag) { if (agents.Remove(ag)) ReallocateHPCArraysIfNeeded(); }

    private void DoHPCLineOfSight()
    {
        if (agents.Count == 0 || _playerTf == null) return;
        ReallocateHPCArraysIfNeeded();

        // 1) Build RaycastCommand array
        for (int i = 0; i < agents.Count; i++)
        {
            Vector3 aPos = agents[i].transform.position;
            Vector3 dir = (_playerTf.position - aPos);
            float dist = dir.magnitude;
            float used = Mathf.Min(dist, maxLoSDistance);
#pragma warning disable CS0618
            _raycastCommands[i] = new RaycastCommand(aPos, dir.normalized, used, obstacleMask);
#pragma warning restore CS0618
        }

        // 2) Schedule & wait
        JobHandle handle = RaycastCommand.ScheduleBatch(_raycastCommands, _raycastHits, 64);
        handle.Complete();

        // 3) Process hits – tell each agent whether it can see the player
        for (int i = 0; i < agents.Count; i++)
        {
            bool canSee = true;
            RaycastHit hit = _raycastHits[i];

            float realDist = Vector3.Distance(agents[i].transform.position, _playerTf.position);
            if (realDist > maxLoSDistance)
            {
                canSee = false;
            }
            else if (hit.collider != null && !hit.collider.CompareTag("Player"))
            {
                canSee = false;
            }

            agents[i].ExternalSetPlayerSpotted(canSee);
        }
    }
    #endregion
}
