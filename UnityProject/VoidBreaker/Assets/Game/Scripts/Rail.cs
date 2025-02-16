using UnityEngine;

public class Rail : MonoBehaviour
{
    [Tooltip("Start point of the rail in world space.")]
    public Transform railStart;
    [Tooltip("End point of the rail in world space.")]
    public Transform railEnd;
    [Tooltip("Speed at which the player rides the rail.")]
    public float railSpeed = 50f;

    // Returns the direction along the rail.
    public Vector3 GetRailDirection()
    {
        return (railEnd.position - railStart.position).normalized;
    }

    // Given a world position, returns the closest point on the rail line segment.
    public Vector3 GetClosestPointOnRail(Vector3 position)
    {
        Vector3 a = railStart.position;
        Vector3 ab = railEnd.position - a;
        float t = Mathf.Clamp01(Vector3.Dot(position - a, ab) / ab.sqrMagnitude);
        return a + t * ab;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FPS_Controller controller = other.GetComponent<FPS_Controller>();
            if (controller != null)
            {
                controller.EnterRail(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FPS_Controller controller = other.GetComponent<FPS_Controller>();
            if (controller != null)
            {
                controller.ExitRail();
            }
        }
    }
}
