using UnityEngine;
using System.Collections;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Tooltip("Order in which objectives should *ideally* be completed.")]
    public Objective[] objectives;

    private int currentIndex = -1;

    // ------------------------------------------------------------ //

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    IEnumerator Start()           // slight delay so other singletons init
    {
        yield return null;
        BeginNextObjective();
    }

    // ------------------------------------------------------------ //
    //  called any time *any* objective (active or not) completes   //
    // ------------------------------------------------------------ //

    public void OnObjectiveCompleted(Objective obj)
    {
        int completedIdx = System.Array.IndexOf(objectives, obj);
        if (completedIdx < 0) return;             // safety

        // ---------- sequence-break handling ----------
        if (completedIdx > currentIndex)
        {
            // 1) Skip every objective from the current one up to (but not incl.) the one just finished
            for (int i = Mathf.Max(currentIndex, 0); i < completedIdx; i++)
            {
                if (!objectives[i].IsComplete)
                    objectives[i].SkipObjective();
            }
        }

        // Treat the newly-finished objective as the “current” one,
        // then advance to whatever comes after it.
        currentIndex = completedIdx;
        BeginNextObjective();
    }

    // ------------------------------------------------------------ //
    //  activate the next valid objective (or finish the game)      //
    // ------------------------------------------------------------ //

    private void BeginNextObjective()
    {
        // Cancel the old active objective (if any)
        if (currentIndex >= 0 && currentIndex < objectives.Length)
            objectives[currentIndex].CancelObjective();

        // Advance
        currentIndex++;

        // Auto-skip anything that’s already complete or already satisfied
        while (currentIndex < objectives.Length &&
               (objectives[currentIndex].IsComplete ||
                objectives[currentIndex].IsSatisfiedByGameState()))
        {
            objectives[currentIndex].SkipObjective();
            currentIndex++;
        }

        // Start the next real objective, or wrap up the game
        if (currentIndex < objectives.Length)
        {
            var obj = objectives[currentIndex];
            obj.StartObjective();
            Game_Manager.Instance?.on_objective_updated?.Invoke(obj.Title, obj.Description);
        }
        else
        {
            Debug.Log("All objectives resolved.");
            Game_Manager.Instance?.on_objective_updated?.Invoke("All objectives completed", "");
            LoadingScreen.LoadScene("CreditsScene");
        }
    }
}
