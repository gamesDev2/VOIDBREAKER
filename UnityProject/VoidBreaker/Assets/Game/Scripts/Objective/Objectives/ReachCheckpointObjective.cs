using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ReachCheckpointObjective : Objective
{
    private Collider checkpointCollider;

    void Awake()
    {
        checkpointCollider = GetComponent<Collider>();
        checkpointCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (IsActive)
            CompleteObjective();
        else
            ForceCompleteObjective();
    }

    protected override void Initialize() { }
    protected override void TearDown() { }

    public override bool IsSatisfiedByGameState()
    {
        Collider[] hits = Physics.OverlapBox(
            checkpointCollider.bounds.center,
            checkpointCollider.bounds.extents,
            checkpointCollider.transform.rotation,
            LayerMask.GetMask("Player")
        );
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
                return true;
        }
        return false;
    }
}
