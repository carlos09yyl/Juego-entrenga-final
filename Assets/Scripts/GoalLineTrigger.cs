using UnityEngine;

/// <summary>
/// Detects when the ball crosses the goal line and notifies PenaltyTargetSelector.
/// </summary>
public class GoalLineTrigger : MonoBehaviour
{
    [Header("References")]
    public PenaltyTargetSelector penaltySelector;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Ball")) return;

        triggered = true;
        Debug.Log("⚽ GOL!");

        ScoreData.WhiteScore++;

        penaltySelector?.OnGoalScored();

        // Re-arm after a moment (reset handled by PenaltyTargetSelector coroutine)
        Invoke(nameof(ReArm), 3f);
    }

    private void ReArm() => triggered = false;
}
