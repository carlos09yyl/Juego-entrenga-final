using UnityEngine;

/// <summary>
/// DEPRECATED — GoalArea reaction logic is now handled by GoalkeeperReactionZone.
/// Self-disables on Awake to never interfere with the current penalty system.
/// </summary>
public class GoalAreaTrigger : MonoBehaviour
{
    void Awake()
    {
        enabled = false;
        Debug.LogWarning("[GoalAreaTrigger] Deshabilitado — usa GoalkeeperReactionZone.");
    }
}
