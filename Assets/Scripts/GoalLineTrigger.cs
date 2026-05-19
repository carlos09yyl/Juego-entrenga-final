using UnityEngine;

/// <summary>
/// Detects when the ball crosses the goal line and delegates all scoring
/// and reset logic to PenaltyTargetSelector.OnGoalScored().
/// </summary>
public class GoalLineTrigger : MonoBehaviour
{
    [Header("References")]
    public PenaltyTargetSelector penaltySelector;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Ball")) return;

        triggered = true;
        Debug.Log("⚽ GOL DETECTADO");

        // PenaltyTargetSelector.OnGoalScored() handles scoring, reset and turn advance.
        penaltySelector?.OnGoalScored();

        Invoke(nameof(ReArm), 6f);
    }

    private void ReArm() => triggered = false;
}