using System.Collections.Generic;
using UnityEngine;

public class CollectItemObjective : Objective
{
    [Tooltip("List of items the player must collect via interaction")]
    public List<GameObject> itemsToCollect = new List<GameObject>();
    private HashSet<GameObject> collectedItems = new HashSet<GameObject>();

    private void OnEnable()
    {
        PlayerInteraction.OnItemInteracted += OnItemInteracted;
    }

    private void OnDisable()
    {
        PlayerInteraction.OnItemInteracted -= OnItemInteracted;
    }

    protected override void Initialize()
    {
        collectedItems.Clear();
    }

    protected override void TearDown() { }

    private void OnItemInteracted(GameObject item)
    {
        if (item == null || collectedItems.Contains(item)) return;
        if (!itemsToCollect.Contains(item)) return;

        collectedItems.Add(item);

        if (collectedItems.Count >= itemsToCollect.Count)
        {
            if (IsActive)
                CompleteObjective();
            else
                ForceCompleteObjective();
        }
    }

    public override bool IsSatisfiedByGameState()
    {
        int collected = 0;
        foreach (var item in itemsToCollect)
        {
            if (item == null || !item.activeInHierarchy)
                collected++;
        }
        return collected >= itemsToCollect.Count;
    }
}
