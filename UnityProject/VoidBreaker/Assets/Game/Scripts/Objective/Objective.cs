using UnityEngine;
using UnityEngine.Events;

public abstract class Objective : MonoBehaviour
{
    public string Title;
    [TextArea] public string Description;

    public UnityEvent OnObjectiveStarted;
    public UnityEvent OnObjectiveCompleted;
    public UnityEvent OnObjectiveSkipped;      // optional UI hook

    protected bool isActive = false;
    protected bool isComplete = false;

    public bool IsActive => isActive;
    public bool IsComplete => isComplete;

    // ------------------------------------------------------------------ //
    // life-cycle                                                         //
    // ------------------------------------------------------------------ //

    internal void StartObjective()
    {
        isActive = true;
        isComplete = false;
        OnObjectiveStarted?.Invoke();
        Initialize();
    }

    internal void CancelObjective()          // called by the manager
    {
        isActive = false;
        TearDown();
    }

    public void SkipObjective()             // used by the manager
    {
        if (isComplete) return;
        isActive = false;
        isComplete = true;
        TearDown();
        OnObjectiveSkipped?.Invoke();
    }

    protected void CompleteObjective()       // normal in-order completion
    {
        if (isComplete) return;
        isActive = false;
        isComplete = true;
        TearDown();
        OnObjectiveCompleted?.Invoke();
        ObjectiveManager.Instance.OnObjectiveCompleted(this);
    }

    /// <summary>Call this when the game fulfils the objective while it is NOT active.</summary>
    public void ForceCompleteObjective()
    {
        if (isComplete) return;
        isActive = false;
        isComplete = true;
        TearDown();
        OnObjectiveCompleted?.Invoke();
        ObjectiveManager.Instance.OnObjectiveCompleted(this);
    }

    // ------------------------------------------------------------------ //
    // override points                                                    //
    // ------------------------------------------------------------------ //

    /// <summary>Return true if this objective is *already* satisfied by the current game state.</summary>
    public virtual bool IsSatisfiedByGameState() => false;

    protected abstract void Initialize();
    protected abstract void TearDown();
}
