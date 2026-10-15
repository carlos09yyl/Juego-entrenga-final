using UnityEngine;

/// <summary>
/// Arquero. Cuando el balon entra en la zona de reaccion se lanza hacia el punto que eligio:
/// - la animacion (estirada, salto o atajada al centro) mueve el cuerpo,
/// - el script solo agrega el desplazamiento que le falta a la animacion para llegar,
///   con una curva suave (sale rapido y frena al final), y una elevacion en arco
///   cuando el tiro va alto.
/// Las manos, brazos, cabeza y cuerpo tienen colisionadores propios (ColisionadoresArquero),
/// asi el balon rebota en lo que realmente se ve.
/// </summary>
public class GoalkeeperController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform startPosition;

    [Header("Configuración")]
    [Tooltip("Velocidad maxima (m/s) con la que el arquero se desplaza ademas de la animacion. La sube el PenaltyTargetSelector en cada intento.")]
    public float moveSpeed = 5f;

    [Header("Animaciones")]
    public Animator animator;

    [Header("Debug / Ajustes")]
    [Tooltip("Si el punto esta a menos de esta distancia lateral, ataja al centro o salta en vez de estirarse.")]
    public float centerThreshold = 0.9f;
    public bool  autoAssignAnimator = true;

    [Header("Alcance de cada animacion (medido en el clip, escala 1)")]
    [Tooltip("Hasta donde llegan las manos hacia el lado en la estirada a la derecha.")]
    public float alcanceLateralDerecha = 1.5f;
    [Tooltip("Hasta donde llegan las manos hacia el lado en la estirada a la izquierda.")]
    public float alcanceLateralIzquierda = 1.1f;
    [Tooltip("Altura de las manos en la estirada.")]
    public float alturaManosEstirada = 0.75f;
    [Tooltip("Altura de las manos en el salto.")]
    public float alturaManosSalto = 1.25f;
    [Tooltip("Altura de las manos en la atajada al centro.")]
    public float alturaManosCentro = 0.6f;

    [Header("Tiempos")]
    [Tooltip("Segundos (a velocidad 1) hasta que la estirada llega a su punto maximo.")]
    public float tiempoEstirada = 1.05f;
    public float tiempoSalto = 0.8f;
    public float tiempoCentro = 0.4f;
    [Tooltip("Velocidad de las animaciones de reaccion. Mas alto = reacciona mas rapido.")]
    public float velocidadEstirada = 2.0f;
    public float velocidadSalto = 1.6f;
    public float velocidadCentro = 1.5f;

    [Header("Elevacion en tiros altos")]
    [Tooltip("Maximo que se eleva el cuerpo ademas de la animacion. Las esquinas de arriba quedan casi imposibles.")]
    public float elevacionMaxima = 1.0f;

    // ── Estado interno ────────────────────────────────────────────────
    private bool      isArmed  = false;
    private bool      hasDived = false;
    private bool      enMovimiento = false;
    private Transform pendingDivePoint;
    private Rigidbody rb;

    private Vector3 origen;            // donde estaba al lanzarse
    private Vector3 desplazamiento;    // cuanto se mueve de lado (ademas de la animacion)
    private float   elevacion;         // cuanto sube en el punto maximo
    private float   duracionSubida;    // segundos hasta el punto maximo
    private float   duracionBajada;    // segundos para volver al piso
    private float   tiempo;
    private bool    esperandoReaccion;
    private float   esperaReaccion;

    private const string IdleStateName = "Idle";

    // ── Posicion frente a la barrera ──────────────────────────────────
    private Vector3 posicionBase;          // donde arranca el arquero en la escena
    private float   desplazamientoLateral; // cuanto se corre hacia el lado que deja libre la barrera

    /// <summary>
    /// Corre al arquero hacia un lado (metros en X del mundo) para cubrir lo que la barrera deja libre.
    /// Se aplica de una vez si el arquero no se esta lanzando.
    /// </summary>
    public void Acomodar(float lateral)
    {
        desplazamientoLateral = lateral;
        if (!enMovimiento && !hasDived)
        {
            Vector3 p = posicionBase + Vector3.right * desplazamientoLateral;
            transform.position = p;
            if (rb != null) rb.position = p;
        }
    }
    private static readonly string[] Triggers = { "Left", "Right", "Center", "Jump" };

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        posicionBase = transform.position;

        if (animator == null && autoAssignAnimator)
            animator = GetComponent<Animator>();

        if (startPosition == null)
            Debug.LogWarning("[GoalkeeperController] startPosition NO asignado");
    }

    // ── API pública ───────────────────────────────────────────────────

    /// <summary>Arma al arquero con el punto que va a cubrir. Se lanza cuando la zona de reaccion llama a TriggerDive().</summary>
    public void PrepareReaction(Transform divePoint)
    {
        if (divePoint == null) return;
        pendingDivePoint = divePoint;
        hasDived = false;
        isArmed  = true;
    }

    /// <summary>Lo llama GoalkeeperReactionZone cuando el balon entra al area de reaccion.</summary>
    public void TriggerDive()
    {
        if (!isArmed || hasDived || pendingDivePoint == null) return;
        hasDived = true;
        isArmed  = false;
        ExecuteDive(pendingDivePoint.position);
    }

    /// <summary>
    /// Se lanza despues de 'segundos' (tiempo de reaccion). Lo llama el PenaltyTargetSelector
    /// en el momento en que el pie golpea el balon, como un arquero de verdad.
    /// </summary>
    public void ReaccionarEn(float segundos)
    {
        if (!isArmed || hasDived) return;
        esperaReaccion = Mathf.Max(0f, segundos);
        esperandoReaccion = true;
    }

    /// <summary>Desarma al arquero (gol o atajada). La animacion sigue hasta el final.</summary>
    public void Disarm()
    {
        isArmed = false;
        pendingDivePoint = null;
    }

    /// <summary>Vuelve al arquero a su posicion inicial y a la animacion de espera.</summary>
    public void ResetPosition(Vector3 position, Quaternion rotation)
    {
        // siempre vuelve a su sitio de inicio, corrido segun la barrera
        position = posicionBase + Vector3.right * desplazamientoLateral;
        isArmed = false;
        hasDived = false;
        enMovimiento = false;
        esperandoReaccion = false;
        pendingDivePoint = null;

        transform.SetPositionAndRotation(position, rotation);
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.position = position;
            rb.rotation = rotation;
        }

        if (animator != null)
        {
            ResetAllTriggers();
            animator.speed = 1f;
            animator.Play(IdleStateName, 0, Random.value);   // cada arquero arranca el idle en un punto distinto
            animator.Update(0f);
        }
    }

    public void ResetPosition()
    {
        Vector3    pos = startPosition != null ? startPosition.position : transform.position;
        Quaternion rot = startPosition != null ? startPosition.rotation : transform.rotation;
        ResetPosition(pos, rot);
    }

    // ── Lógica de la estirada ─────────────────────────────────────────

    private void ExecuteDive(Vector3 punto)
    {
        float escala = transform.lossyScale.y;
        Vector3 aPunto = punto - transform.position;
        float dx = Vector3.Dot(aPunto, transform.right);   // + = a la derecha del arquero
        float dy = aPunto.y;

        string trigger;
        float alcanceX, alturaManos, tSubida, velocidad;

        if (Mathf.Abs(dx) < centerThreshold * escala)
        {
            bool alto = dy > (alturaManosCentro + alturaManosSalto) * 0.5f * escala;
            trigger     = alto ? "Jump" : "Center";
            alcanceX    = 0f;
            alturaManos = alto ? alturaManosSalto : alturaManosCentro;
            tSubida     = alto ? tiempoSalto : tiempoCentro;
            velocidad   = alto ? velocidadSalto : velocidadCentro;
        }
        else
        {
            bool derecha = dx > 0f;
            trigger     = derecha ? "Right" : "Left";
            alcanceX    = derecha ? alcanceLateralDerecha : alcanceLateralIzquierda;
            alturaManos = alturaManosEstirada;
            tSubida     = tiempoEstirada;
            velocidad   = velocidadEstirada;
        }

        if (animator != null)
        {
            ResetAllTriggers();
            animator.speed = velocidad;
            animator.SetTrigger(trigger);
        }

        // lo que la animacion no alcanza lo pone el script
        float falta = Mathf.Max(0f, Mathf.Abs(dx) - alcanceX * escala) * Mathf.Sign(dx);
        duracionSubida = Mathf.Max(0.15f, tSubida / velocidad);

        // la dificultad del intento limita cuanto puede desplazarse en ese tiempo
        float maxDesplazamiento = moveSpeed * duracionSubida;
        falta = Mathf.Clamp(falta, -maxDesplazamiento, maxDesplazamiento);

        origen = transform.position;
        desplazamiento = transform.right * falta;
        elevacion = Mathf.Clamp(dy - alturaManos * escala, 0f, elevacionMaxima * escala);
        duracionBajada = Mathf.Max(0.2f, Mathf.Sqrt(2f * elevacion / 9.81f));
        tiempo = 0f;
        enMovimiento = true;
    }

    void Update()
    {
        if (esperandoReaccion)
        {
            esperaReaccion -= Time.deltaTime;
            if (esperaReaccion <= 0f)
            {
                esperandoReaccion = false;
                TriggerDive();
            }
        }

        if (!enMovimiento) return;

        tiempo += Time.deltaTime;
        float u = Mathf.Clamp01(tiempo / duracionSubida);

        // de lado: sale rapido y frena al llegar (ease-out)
        float lateral = 1f - (1f - u) * (1f - u);

        // altura: sube en arco hasta el punto maximo y luego cae como con gravedad
        float altura;
        if (tiempo <= duracionSubida)
            altura = elevacion * Mathf.Sin(u * Mathf.PI * 0.5f);
        else
        {
            float b = Mathf.Clamp01((tiempo - duracionSubida) / duracionBajada);
            altura = elevacion * (1f - b * b);
        }

        transform.position = origen + desplazamiento * lateral + Vector3.up * altura;

        if (tiempo >= duracionSubida + duracionBajada) enMovimiento = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    void ResetAllTriggers()
    {
        if (animator == null) return;
        foreach (var t in Triggers) animator.ResetTrigger(t);
    }
}
