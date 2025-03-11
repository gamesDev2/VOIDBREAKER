using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsMovable : BaseInteractable
{
    public float holdDistance = 2f;
    public float moveSpeed = 10f;

    private Rigidbody rb;
    private Transform carryParent;

    public bool IsBeingCarried { get; private set; } = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void Interact(GameObject interactor)
    {
        // If not being carried, pick up
        if (!IsBeingCarried)
        {
            PlayerInteraction pi = interactor.GetComponent<PlayerInteraction>();
            if (pi != null && pi.carryAnchor != null)
            {
                carryParent = pi.carryAnchor;
                IsBeingCarried = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
        else
        {
            // If already carried, just drop
            Drop();
        }
    }

    private void FixedUpdate()
    {
        if (IsBeingCarried && carryParent != null)
        {
            Vector3 targetPos = carryParent.position + carryParent.forward * holdDistance;
            Vector3 moveDir = targetPos - transform.position;
            rb.velocity = moveDir * moveSpeed;
        }
    }

    public void Drop()
    {
        IsBeingCarried = false;
        carryParent = null;
        rb.useGravity = true;
/*        rb.velocity = Vector3.zero;
*/    }

    public override string GetInteractionPrompt()
    {
        return IsBeingCarried ? "Press F to drop" : "Press F to pick up";
    }
}
