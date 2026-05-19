using UnityEngine;
using System.Collections;

public class GoalkeeperController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform startPosition;

    [Header("Configuración")]
    public float moveSpeed = 5f;

    [Header("Animaciones")]
    public Animator animator;

    [Header("Debug / Ajustes")]
    public float centerThreshold = 1.2f;
    public bool autoAssignAnimator = true;

    // ── Estado interno ────────────────────────────────────────────────
    private bool      isMoving        = false;
    private bool      isArmed         = false;
    private bool      hasDived        = false;
    private Vector3   targetPosition;
    private Transform pendingDivePoint;
    private Coroutine movementRoutine;
    private Rigidbody rb;

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null && autoAssignAnimator)
        {
            animator = GetComponent<Animator>();
            if (animator != null)
                Debug.Log("[GoalkeeperController] Animator auto-asignado");
        }

        if (startPosition == null)
            Debug.LogWarning("[GoalkeeperController] startPosition NO asignado");
    }

    // ── API pública ───────────────────────────────────────────────────

    /// <summary>
    /// Arma al portero con el punto de destino elegido al azar.
    /// No anima ni mueve — espera a que GoalkeeperReactionZone llame a TriggerDive().
    /// </summary>
    public void PrepareReaction(Transform divePoint)
    {
        if (divePoint == null)
        {
            Debug.LogWarning("[GoalkeeperController] PrepareReaction: divePoint es NULL");
            return;
        }

        pendingDivePoint = divePoint;
        hasDived         = false;
        isArmed          = true;

        Debug.Log($"[GoalkeeperController] Armado → {divePoint.name}");
    }

    /// <summary>
    /// Llamado por GoalkeeperReactionZone cuando el balón entra al área de reacción.
    /// Solo ejecuta si el portero está armado y no ha buceado todavía.
    /// </summary>
    public void TriggerDive()
    {
        if (!isArmed || hasDived || pendingDivePoint == null)
        {
            Debug.Log("[GoalkeeperController] TriggerDive ignorado (no armado o ya buceó)");
            return;
        }

        hasDived = true;
        isArmed  = false;

        Debug.Log($"[GoalkeeperController] ¡Buceo! → {pendingDivePoint.name}");
        ExecuteDive(pendingDivePoint);
    }

    /// <summary>
    /// Desarma al portero sin animarlo. Llamar inmediatamente al detectar gol o atajada,
    /// antes de que la zona de reacción pueda disparar un buceo tardío.
    /// </summary>
    public void Disarm()
    {
        isArmed          = false;
        pendingDivePoint = null;
        Debug.Log("[GoalkeeperController] Desarmado");
    }

    // Name of the default/idle state in the goalkeeper Animator Controller.
    private const string IdleStateName = "mixamo_com";

    /// <summary>
    /// Resetea al portero a la posición/rotación indicadas (snapshot del inicio de escena).
    /// Detiene movimiento, limpia flags y fuerza idle en el animator.
    /// </summary>
    public void ResetPosition(Vector3 position, Quaternion rotation)
    {
        // 1. Detener coroutine de movimiento
        if (movementRoutine != null)
        {
            StopCoroutine(movementRoutine);
            movementRoutine = null;
        }
        isMoving = false;

        // 2. Limpiar estado lógico
        isArmed          = false;
        hasDived         = false;
        pendingDivePoint = null;

        // 3. Reposicionar: primero Transform, luego Rigidbody para que física coincida
        transform.position = position;
        transform.rotation = rotation;

        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position        = position;
            rb.rotation        = rotation;
        }

        // 4. Forzar idle en el animator
        if (animator != null)
        {
            ResetAllTriggers();
            animator.Play(IdleStateName, 0, 0f);
            animator.Update(0f);
        }

        Debug.Log($"[GoalkeeperController] Reset a {position}");
    }

    /// <summary>Sobrecarga que usa el Transform startPosition del Inspector.</summary>
    public void ResetPosition()
    {
        Vector3    pos = startPosition != null ? startPosition.position : transform.position;
        Quaternion rot = startPosition != null ? startPosition.rotation : transform.rotation;
        ResetPosition(pos, rot);
    }

    // ── Lógica de buceo ───────────────────────────────────────────────

    private void ExecuteDive(Transform divePoint)
    {
        targetPosition = divePoint.position;

        // Use WORLD-space X difference instead of local-space projection.
        // The goalkeeper faces the player (rotated ~180° on Y), so from the keeper's
        // own perspective its left/right are swapped relative to the world.
        // A positive worldDeltaX means the target is to the RIGHT in world space,
        // which is to the keeper's LEFT from its facing direction.
        // We flip the sign so "Right" animation = keeper dives to world +X,
        // matching the side the ball is actually coming from.
        float worldDeltaX = divePoint.position.x - transform.position.x;

        Debug.Log($"[GoalkeeperController] worldDeltaX={worldDeltaX:F2}  threshold={centerThreshold}");

        if (animator != null)
        {
            ResetAllTriggers();

            if (Mathf.Abs(worldDeltaX) < centerThreshold)
                animator.SetTrigger("Center");
            else if (worldDeltaX > 0f)
                animator.SetTrigger("Left");   // keeper dives toward world +X → its own Left
            else
                animator.SetTrigger("Right");  // keeper dives toward world -X → its own Right
        }

        // Start moving immediately — do not wait for animator state names.
        if (movementRoutine != null)
            StopCoroutine(movementRoutine);

        isMoving        = true;
        movementRoutine = null;
    }

    void LateUpdate()
    {
        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            isMoving = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    void ResetAllTriggers()
    {
        animator.ResetTrigger("Left");
        animator.ResetTrigger("Right");
        animator.ResetTrigger("Center");
        animator.ResetTrigger("Jump");
    }

    // ── Gizmos ────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!isMoving) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawSphere(targetPosition, 0.05f);
    }
}
