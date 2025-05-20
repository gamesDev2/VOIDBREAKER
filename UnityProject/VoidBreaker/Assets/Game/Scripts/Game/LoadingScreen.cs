using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class LoadingScreen : MonoBehaviour
{
    // ───────────────────────── CONFIG ───────────────────────────────
    [Header("UI")]
    [SerializeField] private CanvasGroup rootCanvasGroup; // whole screen
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressPercent;
    [SerializeField] private TextMeshProUGUI tipText;

    [Header("Behaviour")]
    [TextArea] public string[] tips;
    [Range(1f, 10f)] public float tipChangeInterval = 3f;
    public Ease progressEase = Ease.OutQuad;
    public Ease tipFadeEase = Ease.InOutQuad;
    public float minVisibleTime = 1.0f;            // Seconds

    // ───────────────────────── STATIC API ───────────────────────────
    private static string _targetScene;
    public static void LoadScene(string sceneName)
    {
        _targetScene = sceneName;
        SceneManager.LoadScene("LoadingScreen", LoadSceneMode.Single);
    }

    // ───────────────────────── INTERNAL ─────────────────────────────
    private void Awake()
    {
        // Ensure there's a CanvasGroup so we can fade the whole screen
        if (rootCanvasGroup == null)
            rootCanvasGroup = GetComponentInChildren<CanvasGroup>();

        if (rootCanvasGroup == null)
            rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        EventSystem es = FindObjectOfType<EventSystem>();
        if (!es)
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
        else if (es.transform.parent != null && es.transform.parent != transform)
        {
            Destroy(es.gameObject);
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);                 // survive scene hop
        StartCoroutine(LoadRoutine());
        if (tips.Length > 0) StartCoroutine(TipCycler());
    }

    private IEnumerator LoadRoutine()
    {
        float startTime = Time.unscaledTime;
        AsyncOperation op = SceneManager.LoadSceneAsync(_targetScene);
        op.allowSceneActivation = false;

        // Animate until op.progress hits 0.9 (Unity’s async ceiling)
        while (op.progress < 0.9f)
        {
            AnimateProgress(op.progress);
            yield return null;
        }

        // Fill bar to 100 %
        AnimateProgress(1f);

        // Wait for bar tween AND minimum visible time
        float wait = Mathf.Max(minVisibleTime - (Time.unscaledTime - startTime), 0f);
        yield return new WaitForSecondsRealtime(wait);
        yield return new WaitUntil(() => Mathf.Abs(progressBar.value - 1f) < 0.01f);

        // Let Unity activate the target scene, then fade-out our UI
        op.allowSceneActivation = true;
        yield return new WaitForSecondsRealtime(0.1f); // one frame

        rootCanvasGroup.DOFade(0f, 0.35f)
                       .SetEase(Ease.InQuad)
                       .SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.4f);

        Destroy(gameObject);                           // clean-up
    }

    // ───────────────────────── Helpers ──────────────────────────────
    private void AnimateProgress(float raw)
    {
        float pct = Mathf.Clamp01(raw / 0.9f);
        progressBar.DOValue(pct, 0.25f).SetEase(progressEase).SetUpdate(true);
        if (progressPercent) progressPercent.text = Mathf.RoundToInt(pct * 100) + "%";
    }

    private IEnumerator TipCycler()
    {
        int i = 0;
        CanvasGroup cg = tipText.GetComponent<CanvasGroup>() ??
                         tipText.gameObject.AddComponent<CanvasGroup>();

        while (true)
        {
            // fade-out
            cg.DOFade(0f, 0.25f).SetEase(tipFadeEase).SetUpdate(true);
            yield return new WaitForSecondsRealtime(0.25f);

            tipText.text = tips[i];
            i = (i + 1) % tips.Length;

            // fade-in
            cg.DOFade(1f, 0.25f).SetEase(tipFadeEase).SetUpdate(true);
            yield return new WaitForSecondsRealtime(tipChangeInterval);
        }
    }
}
