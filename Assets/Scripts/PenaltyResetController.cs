using UnityEngine;

/// <summary>
/// Captures the initial world state of the ball, goalkeeper and player the moment the
/// scene starts, and exposes a single FullReset() that restores all three actors to
/// exactly those positions/rotations — including Rigidbody state and Animator pose.
///
/// Attach to any persistent GameObject (e.g. GameController).
/// Wire the public references in the Inspector.
/// </summary>
public class PenaltyResetController : MonoBehaviour
{
    [Header("Ball")]
    public Rigidbody ballRigidbody;

    [Header("Goalkeeper")]
    public Rigidbody  keeperRigidbody;
    public Animator   keeperAnimator;
    public GoalkeeperController keeperController;

    [Header("Player")]
    public Transform         playerTransform;
    public Rigidbody         playerRigidbody;   // optional — assign if the player has one
    public PlayerKickController playerKickController;

    // ── Animator idle state names ─────────────────────────────────────
    private const string KeeperIdleState = "mixamo_com";
    private const string PlayerIdleState = "Idle";

    // ── Snapshot taken at scene start ─────────────────────────────────
    private Vector3    ballStartPos;
    private Quaternion ballStartRot;

    private Vector3    keeperStartPos;
    private Quaternion keeperStartRot;

    private Vector3    playerStartPos;
    private Quaternion playerStartRot;

    private bool snapshotTaken = false;

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        TakeSnapshot();
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Saves the current world positions and rotations of all three actors.
    /// Called automatically in Awake; call again only if you need to redefine the baseline.
    /// </summary>
    public void TakeSnapshot()
    {
        if (ballRigidbody != null)
        {
            ballStartPos = ballRigidbody.position;
            ballStartRot = ballRigidbody.rotation;
        }

        if (keeperRigidbody != null)
        {
            keeperStartPos = keeperRigidbody.position;
            keeperStartRot = keeperRigidbody.rotation;
        }
        else if (keeperController != null)
        {
            keeperStartPos = keeperController.transform.position;
            keeperStartRot = keeperController.transform.rotation;
        }

        if (playerTransform != null)
        {
            playerStartPos = playerTransform.position;
            playerStartRot = playerTransform.rotation;
        }

        snapshotTaken = true;
        Debug.Log($"[PenaltyResetController] Snapshot — " +
                  $"Balón:{ballStartPos}  Portero:{keeperStartPos}  Jugador:{playerStartPos}");
    }

    /// <summary>
    /// Restores all actors to the snapshot positions/rotations and resets
    /// Rigidbody velocities + Animator states. Safe to call from a coroutine.
    /// </summary>
    public void FullReset()
    {
        if (!snapshotTaken)
        {
            Debug.LogWarning("[PenaltyResetController] FullReset() called before snapshot was taken.");
            TakeSnapshot();
        }

        ResetBall();
        ResetKeeper();
        ResetPlayer();

        Debug.Log("[PenaltyResetController] FullReset completado");
    }

    // ── Individual resets ─────────────────────────────────────────────

    private void ResetBall()
    {
        if (ballRigidbody == null) return;

        ballRigidbody.linearVelocity  = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.position        = ballStartPos;
        ballRigidbody.rotation        = ballStartRot;
    }

    private void ResetKeeper()
    {
        // Stop internal movement coroutines and clear flags via the controller
        keeperController?.ResetPosition(keeperStartPos, keeperStartRot);

        // Rigidbody hard-reset (position already applied inside ResetPosition,
        // but we also clear velocities here as a safety net)
        if (keeperRigidbody != null)
        {
            keeperRigidbody.linearVelocity  = Vector3.zero;
            keeperRigidbody.angularVelocity = Vector3.zero;
            keeperRigidbody.position        = keeperStartPos;
            keeperRigidbody.rotation        = keeperStartRot;
        }

        // Force idle animation
        if (keeperAnimator != null)
        {
            keeperAnimator.ResetTrigger("Left");
            keeperAnimator.ResetTrigger("Right");
            keeperAnimator.ResetTrigger("Center");
            keeperAnimator.ResetTrigger("Jump");
            keeperAnimator.Play(KeeperIdleState, 0, 0f);
            keeperAnimator.Update(0f);
        }
    }

    private void ResetPlayer()
    {
        if (playerTransform != null)
        {
            playerTransform.position = playerStartPos;
            playerTransform.rotation = playerStartRot;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity  = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position        = playerStartPos;
            playerRigidbody.rotation        = playerStartRot;
        }

        playerKickController?.ResetAnimator();
    }
}
