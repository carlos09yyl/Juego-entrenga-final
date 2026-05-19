using System.Collections;
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
    private Coroutine rearmCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Ball")) return;

        triggered = true;
        Debug.Log("⚽ GOL DETECTADO");

        penaltySelector?.OnGoalScored();

        if (rearmCoroutine != null) StopCoroutine(rearmCoroutine);
        rearmCoroutine = StartCoroutine(ReArmDelayed());
    }

    private IEnumerator ReArmDelayed()
    {
        yield return new WaitForSeconds(6f);
        triggered = false;
        rearmCoroutine = null;
    }
}