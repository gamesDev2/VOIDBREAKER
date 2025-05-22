using System.Collections;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Plays the intro cut-scene, fades in from black, waits until the
/// cut-scene is ~90 % finished, fades back to black, then hands control
/// to the global LoadingScreen to bring in the next scene.
/// </summary>
public class Intro : MonoBehaviour
{
    [Header("Scene / Cut-scene")]
    [Tooltip("Animator that drives the opening cut-scene (layer 0).")]
    [SerializeField] private Animator cutsceneAnimator;

    [Tooltip("The exact state name that’s playing (if empty, we just read the first clip).")]
    [SerializeField] private string cutsceneStateName = "";

    [Header("Flow")]
    [Tooltip("Scene to load after the intro.")]
    [SerializeField] private string nextScene = "Dev_2";

    [Tooltip("Seconds for the initial fade-in from black.")]
    [SerializeField] private float fadeInDuration = 1.5f;

    [Tooltip("Seconds for the fade-out to black at the end.")]
    [SerializeField] private float fadeOutDuration = 1.5f;

    // ────────────────────────────────────────────────────────────────
    private HUDController hud;          // grabbed at runtime
    [SerializeField] private CanvasGroup blackScreen; // grabbed at runtime, but can be set in the inspector

    private void Start()
    {

        // Grab HUD & black-screen overlay
        hud = FindObjectOfType<HUDController>(true);

        if (blackScreen == null)
        {
            // If no black-screen overlay was provided, try to find it
            blackScreen = FindObjectOfType<CanvasGroup>(true);
        }



        // Make sure we start fully black
        blackScreen.alpha = 1f;

        // Kick off the sequence
        StartCoroutine(RunIntroSequence());
    }

    private IEnumerator RunIntroSequence()
    {
        // 1) Fade **from** black
        blackScreen.DOFade(0f, fadeInDuration).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(fadeInDuration);

        // 2) Wait until the cut-scene is 90 % done
        if (cutsceneAnimator != null)
        {
            // If no explicit state name was provided, grab the first state
            if (string.IsNullOrEmpty(cutsceneStateName))
            {
                var info = cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
                cutsceneStateName = info.IsName("") ? info.shortNameHash.ToString() : info.fullPathHash.ToString();
            }

            // Poll until the state’s normalized time reaches 0.9
            while (cutsceneAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.8f)
            {
                yield return null;
            }
        }

        // 3) Fade **to** black
        blackScreen.DOFade(1f, fadeOutDuration).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(fadeOutDuration);

        // 4) Fire off the loading screen
        LoadingScreen.LoadScene(nextScene);
    }
}
