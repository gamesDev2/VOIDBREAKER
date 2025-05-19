using System.Collections.Generic;
using UnityEngine;

public class CollectItemObjective : Objective
{
    [Tooltip("List of items the player must collect via interaction")]
    public List<GameObject> itemsToCollect = new List<GameObject>();
    private HashSet<GameObject> collectedItems = new HashSet<GameObject>();

    protected override void Initialize()
    {
        PlayerInteraction.OnItemInteracted += OnItemInteracted;
        collectedItems.Clear();
    }

    protected override void TearDown()
    {
        PlayerInteraction.OnItemInteracted -= OnItemInteracted;
    }

    private void OnItemInteracted(GameObject item)
    {
        if (!isActive || item == null || collectedItems.Contains(item)) return;

        if (itemsToCollect.Contains(item))
        {
            collectedItems.Add(item);
            if (collectedItems.Count >= itemsToCollect.Count)
                CompleteObjective();
        }
    }
}