using UnityEngine;

/// <summary>
/// A simple abstract class that implements IInteractable.
/// Subclass this for things like a Switch, Door, Console, etc.
/// </summary>
public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    [SerializeField, Tooltip("Prompt text shown to the player when in range.")]
    protected string interactionPrompt = "Press E to interact";

    /// <summary>
    /// Called when the player interacts with this object.
    /// </summary>
    /// <param name="interactor">The GameObject doing the interacting (usually the player).</param>
    public abstract void Interact(GameObject interactor);

    /// <summary>
    /// Returns a short string displayed on screen when in range.
    /// </summary>
    /// <returns></returns>
    public virtual string GetInteractionPrompt()
    {
        return interactionPrompt;
    }
}
