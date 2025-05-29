using UnityEngine;

public class PDAInstance : BaseInteractable
{
    public int EntryNumber = 0;

    public override void Interact(GameObject interactor)
    {
        PDAData dataInstance = PDAManager.pdaEntries[EntryNumber];

        if (Game_Manager.Instance != null)
        {
            Game_Manager.Instance.on_view_pda_entry.Invoke(dataInstance.Title, dataInstance.Entry);
            PDAManager.CollectedEntries[EntryNumber] = true;
        }
    }
}
