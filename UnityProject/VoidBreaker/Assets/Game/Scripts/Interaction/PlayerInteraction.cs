using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public LayerMask interactableMask;

    // Key is now F
    public KeyCode interactKey = KeyCode.F;

    [Header("References")]
    public Camera playerCamera;
    public Transform carryAnchor;
    public Text promptText;

    // Track if we're carrying an object
    private PhysicsMovable carriedObject;
    private IInteractable currentInteractable;

    void Update()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // 1) If we are currently carrying something,
        //    show "Press F to drop", and drop on F press
        if (carriedObject != null)
        {
            // Optional: show prompt
            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = "Press F to drop";
            }

            if (Input.GetKeyDown(interactKey))
            {
                carriedObject.Drop();
                carriedObject = null; // Clear reference
            }

            return;
        }

        // 2) If not carrying anything, do the normal raycast
        CheckForInteractable();

        // 3) Show prompt if we see something
        if (currentInteractable != null)
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(true);
                promptText.text = currentInteractable.GetInteractionPrompt();
            }

            // 4) Press F to interact (pick up, open door, etc.)
            if (Input.GetKeyDown(interactKey))
            {
                currentInteractable.Interact(gameObject);

                // If it's a PhysicsMovable, store a reference so we know we're carrying it
                if (currentInteractable is PhysicsMovable movable)
                {
                    if (movable.IsBeingCarried)
                        carriedObject = movable;
                }
            }
        }
        else
        {
            // Hide prompt if nothing is hit
            if (promptText != null)
                promptText.gameObject.SetActive(false);
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
            {
                // Maybe the script is on a parent
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }
            currentInteractable = interactable;
        }
    }
}
