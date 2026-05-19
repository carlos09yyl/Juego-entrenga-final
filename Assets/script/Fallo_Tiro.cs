using UnityEngine;

/// <summary>
/// DEPRECATED — replaced by PenaltyTargetSelector's BallTimeout coroutine.
/// Self-disables on Awake to never interfere with the current penalty system.
/// </summary>
public class MissTrigger : MonoBehaviour
{
    void Awake()
    {
        enabled = false;
        Debug.LogWarning("[MissTrigger] Deshabilitado — la lógica de fallo la gestiona PenaltyTargetSelector.BallTimeout.");
    }
}
