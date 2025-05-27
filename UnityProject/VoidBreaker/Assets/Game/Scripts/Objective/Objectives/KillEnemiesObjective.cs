using System.Collections.Generic;
using UnityEngine;

public class KillEnemiesObjective : Objective
{
    [Tooltip("List of enemies to track")]
    public List<GameObject> enemiesToTrack = new();

    private int _kills;
    private int _total;

    private void OnEnable()
    {
        Entity.OnDeath += OnAnyEntityDied;
    }

    private void OnDisable()
    {
        Entity.OnDeath -= OnAnyEntityDied;
    }

    protected override void Initialize()
    {
        enemiesToTrack.RemoveAll(e => e == null);
        _total = enemiesToTrack.Count;
        _kills = 0;

        PushUI();
    }

    protected override void TearDown() { }

    private void OnAnyEntityDied(GameObject deadGo)
    {
        if (!enemiesToTrack.Remove(deadGo)) return;

        _kills++;
        PushUI();

        if (_kills >= _total)
        {
            if (IsActive)
                CompleteObjective();
            else
                ForceCompleteObjective();
        }
    }

    private void PushUI()
    {
        Description = $"Kill the kthar ({_kills}/{_total})";
        if (IsActive)
            Game_Manager.Instance?.on_objective_updated?.Invoke(Title, Description);
    }

    public override bool IsSatisfiedByGameState()
    {
        int alive = 0;
        foreach (var enemy in enemiesToTrack)
        {
            if (enemy != null)
                alive++;
        }
        return alive == 0;
    }
}
