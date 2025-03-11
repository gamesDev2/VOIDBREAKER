using UnityEngine;

/// <summary>
/// Basic interface for any interactive object in the game.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when the player interacts with this object.
    /// </summary>
    /// <param name="interactor">The GameObject doing the interacting (usually the player).</param>
    void Interact(GameObject interactor);

    /// <summary>
    /// Returns a short string (e.g., "Press E to open") to display on screen.
    /// </summary>
    /// <returns></returns>
    string GetInteractionPrompt();
}
