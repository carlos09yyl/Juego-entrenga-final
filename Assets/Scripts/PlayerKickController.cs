using System;
using UnityEngine;

/// <summary>
/// Drives the player kicker's Animator.
/// Fires OnKickImpact at the right frame via Animation Event so the ball launches in sync.
/// </summary>
public class PlayerKickController : MonoBehaviour
{
    private static readonly int KickTrigger = Animator.StringToHash("Kick");

    // Name of the idle state in the Animator Controller (Base Layer).
    private const string IdleStateName = "Idle";

    [Header("References")]
    public Animator animator;

    /// <summary>
    /// Raised by an Animation Event on the Kick clip at the moment of ball impact.
    /// Subscribe from PenaltyTargetSelector to launch the ball.
    /// </summary>
    public event Action OnKickImpact;

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>Plays the kick animation. Ball launch is triggered via the OnKickImpact event.</summary>
    public void PlayKick()
    {
        animator.SetTrigger(KickTrigger);
    }

    /// <summary>
    /// Resets the animator back to the Idle state immediately.
    /// Called after each attempt reset so the player returns to their standing pose.
    /// </summary>
    public void ResetAnimator()
    {
        if (animator == null) return;

        animator.ResetTrigger(KickTrigger);
        // Force-play Idle from the start, bypassing any in-progress transition
        animator.Play(IdleStateName, 0, 0f);
        animator.Update(0f);   // flush immediately so the pose snaps in the same frame
    }

    /// <summary>
    /// Called by an Animation Event placed on the Kick clip at the impact frame.
    /// Notifies subscribers (PenaltyTargetSelector) to launch the ball.
    /// </summary>
    public void KickImpactEvent()
    {
        OnKickImpact?.Invoke();
    }
}

