using UnityEngine;

/// <summary>
/// Balon. El tiro se calcula para que llegue exacto al punto elegido aunque tenga
/// gravedad y efecto (curva): se resuelve la velocidad inicial con
///     destino = inicio + v0·T + ½·a·T²
/// donde a = gravedad del vuelo + empuje lateral del efecto.
/// Mientras vuela se aplica esa aceleracion; al primer choque (arquero, palo, red, piso)
/// vuelve a la fisica normal y rebota.
/// </summary>
public class BallController : MonoBehaviour
{
    [Header("References")]
    public Transform startPosition;
    public Rigidbody rb;

    [Header("Configuration")]
    [Tooltip("Velocidad del tiro en m/s. La pone el PenaltyTargetSelector segun la potencia.")]
    public float launchSpeed = 12f;

    [Header("Vuelo")]
    [Tooltip("Que tanto pesa la gravedad durante el tiro (1 = real). Menos = tiro mas tenso.")]
    public float gravedadVuelo = 0.75f;
    [Tooltip("Giro visual del balon (radianes por segundo por cada m/s de velocidad).")]
    public float giroVisual = 0.35f;
    [Tooltip("Rozamiento cuando ya rebota o rueda.")]
    public float rozamientoSuelto = 0.25f;

    /// <summary>True desde que se patea hasta el primer choque.</summary>
    public bool EnVuelo { get; private set; }
    /// <summary>True desde que se patea hasta que se reinicia.</summary>
    public bool Lanzado { get; private set; }
    /// <summary>Segundos que tarda en llegar al punto, calculados al patear.</summary>
    public float TiempoDeVuelo { get; private set; }

    private Vector3 aceleracionVuelo;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = 60f;       // deja girar el balon rapido (Unity limita a 7 por defecto)

        // punto de penal: si BallHome es hijo del balon se mueve con el, asi que se guarda la posicion inicial
        bool homeValido = startPosition != null && !startPosition.IsChildOf(transform);
        puntoPenal = homeValido ? startPosition.position : transform.position;
    }

    private Vector3 puntoPenal;

    /// <summary>Tiro recto (sin efecto) hacia un punto. Devuelve el tiempo de vuelo.</summary>
    public float LaunchToward(Vector3 targetPosition)
    {
        return LaunchToward(targetPosition, launchSpeed, 0f);
    }

    /// <summary>
    /// Patea el balon para que pase por targetPosition.
    /// efecto: aceleracion lateral en m/s² (+ curva hacia la derecha del pateador, - hacia la izquierda).
    /// Devuelve el tiempo de vuelo hasta el punto.
    /// </summary>
    public float LaunchToward(Vector3 targetPosition, float velocidad, float efecto)
    {
        Vector3 inicio = rb.position;
        Vector3 recorrido = targetPosition - inicio;
        Vector3 plano = Vector3.ProjectOnPlane(recorrido, Vector3.up);
        Vector3 lateral = plano.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, plano.normalized) : Vector3.right;

        float T = Mathf.Max(0.12f, recorrido.magnitude / Mathf.Max(0.1f, velocidad));
        aceleracionVuelo = Physics.gravity * gravedadVuelo + lateral * efecto;
        Vector3 v0 = (recorrido - 0.5f * aceleracionVuelo * T * T) / T;

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearDamping = 0f;              // sin rozamiento en el aire: asi llega exacto
        rb.angularDamping = 0.05f;
        rb.linearVelocity = v0;

        // giro: hacia adelante segun la velocidad y sobre el eje vertical segun el efecto
        Vector3 haciaAdelante = Vector3.Cross(Vector3.up, plano.normalized);
        rb.angularVelocity = haciaAdelante * (velocidad * giroVisual) - Vector3.up * efecto * 2.5f;

        TiempoDeVuelo = T;
        EnVuelo = true;
        Lanzado = true;
        return T;
    }

    /// <summary>
    /// Puntos de la trayectoria que tendria un tiro (misma formula que LaunchToward),
    /// desde el balon hasta 'fraccion' del recorrido. Sirve para dibujar la linea de tiro.
    /// </summary>
    public Vector3[] Trayectoria(Vector3 destino, float velocidad, float efecto, int cantidad, float fraccion)
    {
        Vector3 inicio = rb != null ? rb.position : transform.position;
        Vector3 recorrido = destino - inicio;
        Vector3 plano = Vector3.ProjectOnPlane(recorrido, Vector3.up);
        Vector3 lateral = plano.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, plano.normalized) : Vector3.right;
        float T = Mathf.Max(0.12f, recorrido.magnitude / Mathf.Max(0.1f, velocidad));
        Vector3 a = Physics.gravity * gravedadVuelo + lateral * efecto;
        Vector3 v0 = (recorrido - 0.5f * a * T * T) / T;

        var puntos = new Vector3[Mathf.Max(2, cantidad)];
        for (int i = 0; i < puntos.Length; i++)
        {
            float t = T * fraccion * i / (puntos.Length - 1);
            puntos[i] = inicio + v0 * t + 0.5f * a * t * t;
        }
        return puntos;
    }

    /// <summary>Lanza en una direccion cruda (sin calcular destino).</summary>
    public void Launch(Vector3 direction)
    {
        rb.useGravity = true;
        rb.linearDamping = rozamientoSuelto;
        rb.linearVelocity = direction.normalized * launchSpeed;
        Lanzado = true;
    }

    void FixedUpdate()
    {
        if (EnVuelo) rb.AddForce(aceleracionVuelo, ForceMode.Acceleration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // golpe en un palo o el travesaño: suena mas fuerte cuanto mas rapido venga el balon
        var mat = collision.collider.sharedMaterial;
        float impacto = collision.relativeVelocity.magnitude;
        if (Lanzado && mat != null && mat.name.StartsWith("Poste") && impacto > 2f)
            SonidosPenales.Reproducir("palo", Mathf.Clamp01(impacto / 14f));

        if (!EnVuelo) return;
        Soltar();
    }

    /// <summary>Termina el vuelo calculado y deja el balon a la fisica normal (gravedad y rozamiento).</summary>
    public void Soltar()
    {
        EnVuelo = false;
        if (rb.isKinematic) return;
        rb.useGravity = true;
        rb.linearDamping = rozamientoSuelto;
    }

    /// <summary>Vuelve el balon al punto de penal, quieto.</summary>
    public void ResetBall()
    {
        EnVuelo = false;
        Lanzado = false;
        rb.useGravity = false;
        rb.linearDamping = rozamientoSuelto;
        if (!rb.isKinematic)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Vector3 pos = startPosition != null && !startPosition.IsChildOf(transform) ? startPosition.position : puntoPenal;
        rb.position = pos;
        rb.rotation = Quaternion.identity;
        transform.SetPositionAndRotation(pos, Quaternion.identity);
    }
}
