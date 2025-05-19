using UnityEngine;
using UnityEngine.Events;

public abstract class Objective : MonoBehaviour
{
    public string Title;
    [TextArea] public string Description;
    public UnityEvent OnObjectiveStarted;
    public UnityEvent OnObjectiveCompleted;

    protected bool isActive = false;
    protected bool isComplete = false;

    internal void StartObjective()
    {
        isActive = true;
        isComplete = false;
        gameObject.SetActive(true);
        OnObjectiveStarted?.Invoke();
        Initialize();
    }

    internal void CancelObjective()
    {
        isActive = false;
        TearDown();
        gameObject.SetActive(false);
    }

    protected void CompleteObjective()
    {
        if (!isActive || isComplete) return;
        isComplete = true;
        OnObjectiveCompleted?.Invoke();
        isActive = false;
        TearDown();
        ObjectiveManager.Instance.OnObjectiveCompleted(this);
    }

    protected abstract void Initialize();
    protected abstract void TearDown();
}
