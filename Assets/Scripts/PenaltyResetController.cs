using System.Collections;
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
    public Transform            playerTransform;
    public Rigidbody            playerRigidbody;   // optional — assign if the player has one
    public PlayerKickController playerKickController;

    // ── Animator idle state names ─────────────────────────────────────
    private const string KeeperIdleState = "Idle";

    // ── Snapshot taken at scene start ─────────────────────────────────
    private Vector3    ballStartPos;
    private Quaternion ballStartRot;

    private Vector3    keeperStartPos;
    private Quaternion keeperStartRot;

    private Vector3    playerStartPos;
    private Quaternion playerStartRot;

    private bool snapshotTaken = false;

    // ─────────────────────────────────────────────────────────────────

    void Start()
    {
        // Take the snapshot in Start (not Awake) so all Rigidbodies have settled
        // into their initial positions before we record them.
        TakeSnapshot();
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Saves the current world positions and rotations of all three actors.
    /// Called automatically in Start; call again only if you need to redefine the baseline.
    /// </summary>
    public void TakeSnapshot()
    {
        if (ballRigidbody != null)
        {
            ballStartPos = ballRigidbody.position;
            ballStartRot = ballRigidbody.rotation;
        }

        // Goalkeeper — prefer Rigidbody, fall back to Transform
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

        // Player — prefer Rigidbody, fall back to Transform
        if (playerRigidbody != null)
        {
            playerStartPos = playerRigidbody.position;
            playerStartRot = playerRigidbody.rotation;
        }
        else if (playerTransform != null)
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

        // el BallController tambien apaga el vuelo con efecto y la gravedad
        var control = ballRigidbody.GetComponent<BallController>();
        if (control != null && control.startPosition != null)
        {
            control.ResetBall();
            return;
        }

        ballRigidbody.useGravity      = false;
        ballRigidbody.linearVelocity  = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
        // Move via MovePosition so the physics engine acknowledges the teleport
        ballRigidbody.MovePosition(ballStartPos);
        ballRigidbody.MoveRotation(ballStartRot);
    }

    private void ResetKeeper()
    {
        // El controlador del arquero deja posicion, Rigidbody y animacion como al inicio
        if (keeperController != null)
        {
            keeperController.ResetPosition(keeperStartPos, keeperStartRot);
            return;
        }

        if (keeperRigidbody != null)
        {
            if (!keeperRigidbody.isKinematic)
            {
                keeperRigidbody.linearVelocity  = Vector3.zero;
                keeperRigidbody.angularVelocity = Vector3.zero;
            }
            keeperRigidbody.position = keeperStartPos;
            keeperRigidbody.rotation = keeperStartRot;
        }

        if (keeperAnimator != null)
        {
            keeperAnimator.speed = 1f;
            keeperAnimator.Play(KeeperIdleState, 0, 0f);
            keeperAnimator.Update(0f);
        }
    }

    private void ResetPlayer()
    {
        if (playerRigidbody != null)
        {
            if (!playerRigidbody.isKinematic)
            {
                playerRigidbody.linearVelocity  = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
            playerRigidbody.position = playerStartPos;
            playerRigidbody.rotation = playerStartRot;
            playerTransform?.SetPositionAndRotation(playerStartPos, playerStartRot);
        }
        else if (playerTransform != null)
        {
            playerTransform.position = playerStartPos;
            playerTransform.rotation = playerStartRot;
        }

        playerKickController?.ResetAnimator();
    }
}
