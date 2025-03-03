using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;   // for RaycastCommand
using System.Collections.Generic;
/// <summary>
/// The various message types an AI agent might handle.
/// </summary>
public enum MessageType
{
    Attack,
    Retreat,
    Regroup,
    Alert
}

/// <summary>
/// The various plan types we might generate.
/// </summary>
public enum PlanType
{
    Attack,
    Flank,
    Defend,
    Retreat
}
public class AIDirector : MonoBehaviour
{
    public static AIDirector Instance;

    [Header("Agent Management")]
    public List<GOAPAgent> agents = new List<GOAPAgent>();

    [Header("Plan Generation")]
    public float planningInterval = 5f;
    private float nextPlanTime = 0f;

    [Header("HPC Line-of-Sight Settings")]
    public float maxLoSDistance = 10f;
    public LayerMask obstacleMask;

    private NativeArray<RaycastCommand> raycastCommands;
    private NativeArray<RaycastHit> raycastHits;

    private Transform player;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
            player = pObj.transform;

        if (agents.Count > 0)
        {
            AllocateHPCArrays(agents.Count);
        }
    }

    void OnDestroy()
    {
        DisposeHPCArrays();
        if (Instance == this) Instance = null;
    }

    public void RegisterAgent(GOAPAgent agent)
    {
        if (!agents.Contains(agent))
            agents.Add(agent);
        ReallocateHPCArraysIfNeeded();
    }

    public void UnregisterAgent(GOAPAgent agent)
    {
        if (agents.Contains(agent))
            agents.Remove(agent);
        ReallocateHPCArraysIfNeeded();
    }

    private void ReallocateHPCArraysIfNeeded()
    {
        if (agents.Count == 0)
        {
            DisposeHPCArrays();
        }
        else
        {
            if (!raycastCommands.IsCreated || raycastCommands.Length != agents.Count)
            {
                DisposeHPCArrays();
                AllocateHPCArrays(agents.Count);
            }
        }
    }

    private void AllocateHPCArrays(int count)
    {
        raycastCommands = new NativeArray<RaycastCommand>(count, Allocator.Persistent);
        raycastHits = new NativeArray<RaycastHit>(count, Allocator.Persistent);
    }

    private void DisposeHPCArrays()
    {
        if (raycastCommands.IsCreated) raycastCommands.Dispose();
        if (raycastHits.IsCreated) raycastHits.Dispose();
    }

    void Update()
    {
        DoHPCLineOfSight();

        if (Time.time >= nextPlanTime)
        {
            AIPlan plan = new AIPlan(PlanType.Attack); // example
            DistributeAdvancedPlan(plan);
            nextPlanTime = Time.time + planningInterval;
        }
    }

    /// <summary>
    /// HPC line-of-sight check using RaycastCommand.
    /// 
    /// 1) Build RaycastCommands for each agent->player
    /// 2) Schedule HPC job
    /// 3) On main thread, process hits to check collider & distance
    /// </summary>
    private void DoHPCLineOfSight()
    {
        if (agents.Count == 0 || !player) return;
        ReallocateHPCArraysIfNeeded();

        // 1) Build HPC commands
        for (int i = 0; i < agents.Count; i++)
        {
            Vector3 agentPos = agents[i].transform.position;
            Vector3 dir = (player.position - agentPos);
            float dist = dir.magnitude;
            float usedDist = Mathf.Min(dist, maxLoSDistance);

#pragma warning disable CS0618
            raycastCommands[i] = new RaycastCommand(
                agentPos,
                dir.normalized,
                usedDist,
                obstacleMask
            );
#pragma warning restore CS0618
        }

        // 2) Schedule HPC
        JobHandle handle = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 64);
        handle.Complete(); // Wait for HPC job

        // 3) MAIN THREAD: process hits
        for (int i = 0; i < agents.Count; i++)
        {
            bool canSeePlayer = true;
            RaycastHit hit = raycastHits[i];

            // Check actual distance
            float realDist = Vector3.Distance(agents[i].transform.position, player.position);
            if (realDist > maxLoSDistance)
            {
                canSeePlayer = false;
            }
            else
            {
                // If HPC ray hits an obstacle that isn't the player => blocked
                if (hit.collider != null && !hit.collider.CompareTag("Player"))
                {
                    canSeePlayer = false;
                }
            }

            // Tell the agent
            agents[i].ExternalSetPlayerSpotted(canSeePlayer);
        }
    }

    // ------------------ Plan Generation & Distribution ------------------

    public void DistributeAdvancedPlan(AIPlan plan)
    {
        foreach (var kvp in plan.agentPlans)
        {
            kvp.Key.ReceivePlan(plan);
        }
    }
}
