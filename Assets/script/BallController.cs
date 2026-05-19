using UnityEngine;

/// <summary>
/// Controls the soccer ball: launching toward a target and resetting to home position.
/// Shot input is handled by PenaltyTargetSelector.
/// </summary>
public class BallController : MonoBehaviour
{
    [Header("References")]
    public Transform startPosition;
    public Rigidbody rb;

    [Header("Configuration")]
    public float launchSpeed = 6f;

    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Launches the ball toward a world-space target position.
    /// The ball passes through that point and continues along the same trajectory.
    /// </summary>
    public void LaunchToward(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        rb.linearVelocity = direction * launchSpeed;
        Debug.Log($"[BallController] Lanzado → dir={direction}  speed={launchSpeed}");
    }

    /// <summary>Launches the ball in a raw world-space direction.</summary>
    public void Launch(Vector3 direction)
    {
        rb.linearVelocity = direction.normalized * launchSpeed;
    }

    /// <summary>Resets the ball to its home position with no velocity.</summary>
    public void ResetBall()
    {
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position        = startPosition.position;
        rb.rotation        = Quaternion.identity;
    }
}
