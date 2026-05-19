using UnityEngine;

/// <summary>
/// Placed on the GoalKeeper collider (or a child trigger volume).
/// Fires when the ball touches the goalkeeper, signalling a save.
/// </summary>
public class GoalKeeperSaveTrigger : MonoBehaviour
{
    [Header("References")]
    public PenaltyTargetSelector penaltySelector;

    private bool triggered = false;

    private void OnCollisionEnter(Collision collision)
    {
        HandleContact(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleContact(other.gameObject);
    }

    private void HandleContact(GameObject other)
    {
        if (triggered) return;
        if (!other.CompareTag("Ball")) return;

        triggered = true;
        Debug.Log("🧤 ¡El arquero atajó!");

        // PenaltyTargetSelector.OnGoalkeeperSaved() handles scoring, reset and turn advance.
        penaltySelector?.OnGoalkeeperSaved();

        Invoke(nameof(ReArm), 6f);
    }

    private void ReArm() => triggered = false;
}
