using System.Collections.Generic;
using UnityEngine;

public class KillEnemiesObjective : Objective
{
    [Tooltip("List of enemies to track")]
    public List<GameObject> enemiesToTrack = new();

    private int _kills;
    private int _total;

    protected override void Initialize()
    {
        // Snapshot total once; we’ll mutate the list afterwards
        enemiesToTrack.RemoveAll(e => e == null);   // safety
        _total = enemiesToTrack.Count;
        _kills = 0;

        // Subscribe only once (static event, no need to loop)
        Entity.OnDeath += OnAnyEntityDied;

        PushUI();   // show “0 / X” immediately
    }
    protected override void TearDown()
    {
        Entity.OnDeath -= OnAnyEntityDied;
    }

    private void OnAnyEntityDied(GameObject deadGo)
    {
        if (!isActive) return;                 // ignore if objective not running
        if (!enemiesToTrack.Remove(deadGo))    // was this one of *our* targets?
            return;

        _kills++;
        PushUI();

        if (_kills >= _total)
            CompleteObjective();
    }

    private void PushUI()
    {
        Description = $"Kill the kthar ({_kills}/{_total})";
        Game_Manager.Instance?.on_objective_updated
            ?.Invoke(Title, Description);
    }
}
