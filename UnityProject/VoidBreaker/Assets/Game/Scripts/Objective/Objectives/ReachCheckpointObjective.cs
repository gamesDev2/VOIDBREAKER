using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ReachCheckpointObjective : Objective
{
    void Awake() => GetComponent<Collider>().isTrigger = true;

    protected override void Initialize() { }
    protected override void TearDown() { }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (other.CompareTag("Player"))
            CompleteObjective();
    }
}
