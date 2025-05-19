using UnityEngine;
using DG.Tweening;
using System.Collections;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }
    public Objective[] objectives;

    private int currentIndex = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(StartCoroutineMethod());
    }

    private IEnumerator StartCoroutineMethod()
    {
        yield return null;
        BeginNextObjective();
    }

    public void OnObjectiveCompleted(Objective obj)
    {
        BeginNextObjective();
    }

    private void BeginNextObjective()
    {
        if (currentIndex >= 0 && currentIndex < objectives.Length)
            objectives[currentIndex].CancelObjective();

        currentIndex++;

        if (currentIndex < objectives.Length)
        {
            var obj = objectives[currentIndex];
            obj.StartObjective();
            Game_Manager.Instance?.on_objective_updated.Invoke(obj.Title, obj.Description);
        }
        else
        {
            Debug.Log("All objectives completed.");

            Game_Manager.Instance?.on_objective_updated.Invoke("All objectives completed", "");
/*            Game_Manager.Instance?.on_game_won.Invoke(true);
*/        }
    }
}
