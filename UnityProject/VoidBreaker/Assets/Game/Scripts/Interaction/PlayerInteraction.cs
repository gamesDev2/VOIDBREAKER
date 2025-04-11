using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public LayerMask interactableMask;

    // Key is now F
    public KeyCode interactKey = KeyCode.F;

    [Header("References")]
    public Camera playerCamera;
    public Transform carryAnchor; // in case you are carrying objects

    private PhysicsMovable carriedObject;
    private IInteractable currentInteractable;

    void Update()
    {
        // Ensure playerCamera reference is valid.
        if (playerCamera == null)
            playerCamera = Camera.main;

        // If an object is being carried, show "Press F to drop" in the HUD.
        if (carriedObject != null)
        {
            if (Game_Manager.Instance != null)
                Game_Manager.Instance.on_interact.Invoke(true, "Press F to drop");

            if (Input.GetKeyDown(interactKey))
            {
                carriedObject.Drop();
                carriedObject = null;
            }
            return;
        }

        // Check for any interactable in front of the player.
        CheckForInteractable();

        // If an interactable is found, notify the HUD to display its prompt.
        if (currentInteractable != null)
        {
            if (Game_Manager.Instance != null)
                Game_Manager.Instance.on_interact.Invoke(true, currentInteractable.GetInteractionPrompt());

            // When the interact key is pressed, trigger the interaction.
            if (Input.GetKeyDown(interactKey))
            {
                currentInteractable.Interact(gameObject);

                // If the interactable is a movable object that can be carried, save the reference.
                if (currentInteractable is PhysicsMovable movable)
                {
                    if (movable.IsBeingCarried)
                        carriedObject = movable;
                }
            }
        }
        else
        {
            // No interactable present: ensure the HUD hides the interact prompt.
            if (Game_Manager.Instance != null)
                Game_Manager.Instance.on_interact.Invoke(false, "");
        }
    }

    private void CheckForInteractable()
    {
        currentInteractable = null;
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactDistance, interactableMask))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            currentInteractable = interactable;
        }
    }
}
