using UnityEngine;

/// <summary>
/// Invisible trigger volume placed between the ball's starting position and the goal.
/// When the ball enters this zone, the goalkeeper executes its dive — if it's still armed.
/// This replaces the unreliable per-frame distance check in GoalkeeperController.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class GoalkeeperReactionZone : MonoBehaviour
{
    [Header("Reference")]
    public GoalkeeperController goalkeeperController;

    private BoxCollider zone;

    void Awake()
    {
        zone           = GetComponent<BoxCollider>();
        zone.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        goalkeeperController?.TriggerDive();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc != null)
            Gizmos.DrawCube(bc.center, bc.size);
    }
#endif
}
