using System.Collections;
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
    private Coroutine rearmCoroutine;

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

        penaltySelector?.OnGoalkeeperSaved();

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
