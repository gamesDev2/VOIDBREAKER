using UnityEngine;

/// <summary>
/// Handles input on the credits screen and transitions back to the Main Menu.
/// </summary>
public class CreditsScene : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "MainMenu";

    private bool isExiting = false;

    private void Update()
    {
        if (!isExiting && Input.GetKeyDown(KeyCode.Space))
        {
            isExiting = true;
            LoadingScreen.LoadScene(menuSceneName);
        }
    }
}
