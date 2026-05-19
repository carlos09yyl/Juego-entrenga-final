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
    public bool  autoAssignAnimator = true;

    // ── Estado interno ────────────────────────────────────────────────
    private bool      isMoving        = false;
    private bool      isArmed         = false;
    private bool      hasDived        = false;
    private Vector3   targetPosition;
    private Transform pendingDivePoint;
    private Rigidbody rb;

    // ── Intercepción ──────────────────────────────────────────────────
    // El portero se mueve al punto objetivo de la portería (mismo Z que él mismo),
    // ajustado para estar en la línea de portería — maximiza la chance de bloqueo.
    private Vector3 ComputeInterceptPosition(Vector3 divePoint)
    {
        // Mantener Z del portero (su plano de portería) y tomar X/Y del target seleccionado.
        // Esto pone al portero exactamente en la trayectoria de la pelota dentro de su plano.
        return new Vector3(divePoint.x, divePoint.y, transform.position.z);
    }

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
    /// Arma al portero con el punto destino elegido.
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
    /// Desarma al portero sin animarlo. Llamar al detectar gol o atajada definitiva.
    /// </summary>
    public void Disarm()
    {
        isArmed          = false;
        pendingDivePoint = null;
        isMoving         = false;
        Debug.Log("[GoalkeeperController] Desarmado");
    }

    private const string IdleStateName = "mixamo_com";

    /// <summary>Resetea al portero al snapshot de inicio de escena.</summary>
    public void ResetPosition(Vector3 position, Quaternion rotation)
    {
        isMoving = false;
        isArmed          = false;
        hasDived         = false;
        pendingDivePoint = null;

        transform.position = position;
        transform.rotation = rotation;

        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position        = position;
            rb.rotation        = rotation;
        }

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
        // Calcular posición de intercepción: portero se mueve a la X/Y del target
        // pero permanece en su propio plano Z (línea de portería).
        targetPosition = ComputeInterceptPosition(divePoint.position);

        // Proyectar el delta sobre los ejes LOCALES del portero.
        // Usando transform.right en lugar de worldDeltaX se corrige automáticamente
        // la rotación del modelo (el portero mira ~180° hacia el jugador).
        Vector3 toTarget    = divePoint.position - transform.position;
        float   localDeltaX = Vector3.Dot(toTarget, transform.right);
        float   localDeltaY = divePoint.position.y - transform.position.y;

        Debug.Log($"[GoalkeeperController] Intercepción → {targetPosition} | localX={localDeltaX:F2} | localY={localDeltaY:F2}");

        if (animator != null)
        {
            ResetAllTriggers();

            // Tiros altos: fila superior de la portería
            if (localDeltaY > centerThreshold)
                animator.SetTrigger("Jump");
            else if (Mathf.Abs(localDeltaX) < centerThreshold)
                animator.SetTrigger("Center");
            else if (localDeltaX > 0f)
                animator.SetTrigger("Right");  // target a la derecha local del portero
            else
                animator.SetTrigger("Left");   // target a la izquierda local del portero
        }

        isMoving = true;
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

    void OnDrawGizmosSelected()
    {
        if (!isMoving) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawSphere(targetPosition, 0.1f);
    }
}
