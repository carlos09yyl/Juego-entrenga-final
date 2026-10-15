using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camara del tiro libre:
/// - Al patear gira y se acerca solo un poquito hacia el balon.
/// - En el gol da un empujon (zoom rapido) y una sacudida corta.
/// - Graba la jugada y despues del gol la repite en camara lenta desde un costado del arco.
/// Se agrega sola a la camara del juego desde PenaltyTargetSelector.
/// </summary>
public class CamaraTiro : MonoBehaviour
{
    [Header("Seguimiento (sutil)")]
    [Range(0f, 1f)] public float giroHaciaBalon = 0.22f;   // 0 = no gira, 1 = mira directo al balon
    public float acercamiento = 1.6f;                      // metros que avanza mientras vuela el balon
    public float zoomTiro = 3f;                            // grados de FOV que cierra
    public float suavidad = 0.18f;

    [Header("Gol")]
    public float zoomGol = 5f;
    public float sacudidaGol = 0.12f;

    [Header("Repeticion")]
    public bool repetirGoles = true;
    [Range(0.1f, 1f)] public float velocidadRepeticion = 0.35f;
    public float fovRepeticion = 34f;

    // referencias (las pone PenaltyTargetSelector)
    [HideInInspector] public Transform balon;
    [HideInInspector] public Transform arquero;
    [HideInInspector] public Transform pateador;
    [HideInInspector] public Vector3 bocaMin, bocaMax;

    public bool Repitiendo { get; private set; }
    public bool HayJugada => cuadros.Count > 10;

    private Camera cam;
    private Vector3 posBase;
    private Quaternion rotBase;
    private float fovBase;

    private bool siguiendo;
    private float tSeguir;
    private float pesoSeguir;          // 0..1 cuanto se aplica el seguimiento
    private float fovExtra;
    private float sacudida;

    // ── grabacion ─────────────────────────────────────────────────────
    private struct Cuadro
    {
        public float t;
        public Vector3 balonPos; public Quaternion balonRot;
        public Vector3 arqueroPos; public Quaternion arqueroRot;
        public int arqueroEstado; public float arqueroTiempo;
        public Vector3 pateadorPos; public Quaternion pateadorRot;
        public int pateadorEstado; public float pateadorTiempo;
    }
    private readonly List<Cuadro> cuadros = new List<Cuadro>();
    private bool grabando;
    private float tGrabacion;
    private float finGrabacion = -1f;
    private Animator animArquero, animPateador;

    void Awake()
    {
        cam = GetComponent<Camera>();
        posBase = transform.position;
        rotBase = transform.rotation;
        fovBase = cam != null ? cam.fieldOfView : 60f;
    }

    // ── API ───────────────────────────────────────────────────────────

    /// <summary>Empieza la carrera del pateador: arranca la grabacion.</summary>
    public void EmpezarJugada()
    {
        if (arquero != null && animArquero == null) animArquero = arquero.GetComponent<Animator>();
        if (pateador != null && animPateador == null) animPateador = pateador.GetComponent<Animator>();
        cuadros.Clear();
        tGrabacion = 0f;
        finGrabacion = -1f;
        grabando = true;
    }

    /// <summary>El balon sale: la camara empieza a acompañarlo.</summary>
    public void SeguirBalon()
    {
        siguiendo = true;
        tSeguir = 0f;
    }

    /// <summary>Gol: empujon, sacudida y la grabacion sigue un momento mas.</summary>
    public void Gol()
    {
        fovExtra = -zoomGol;
        sacudida = sacudidaGol;
        finGrabacion = tGrabacion + 0.9f;
    }

    /// <summary>Atajada, palo o barrera: sacudida pequeña y se deja de grabar.</summary>
    public void Golpe(float intensidad = 0.05f)
    {
        sacudida = Mathf.Max(sacudida, intensidad);
        finGrabacion = tGrabacion + 0.5f;
        siguiendo = false;      // no persigue un balon que ya se fue: vuelve suave a su lugar
    }

    /// <summary>Vuelve a la posicion de siempre (al reiniciar el intento).</summary>
    public void Volver()
    {
        siguiendo = false;
        grabando = false;
    }

    // ── actualizacion ─────────────────────────────────────────────────

    void LateUpdate()
    {
        if (Repitiendo) return;

        if (grabando) Grabar();

        float dt = Time.unscaledDeltaTime;
        pesoSeguir = Mathf.MoveTowards(pesoSeguir, siguiendo ? 1f : 0f, dt / (siguiendo ? 0.35f : 0.6f));
        if (siguiendo) tSeguir += Time.deltaTime;

        Vector3 pos = posBase;
        Quaternion rot = rotBase;
        if (pesoSeguir > 0f && balon != null)
        {
            float avance = Mathf.Clamp01(tSeguir / 0.9f);
            pos = posBase + rotBase * Vector3.forward * (acercamiento * Suave(avance));
            Quaternion haciaBalon = Quaternion.LookRotation(balon.position - pos);
            rot = Quaternion.Slerp(rotBase, haciaBalon, giroHaciaBalon);
            pos = Vector3.Lerp(posBase, pos, Suave(pesoSeguir));
            rot = Quaternion.Slerp(rotBase, rot, Suave(pesoSeguir));
        }

        // sacudida que se apaga sola
        if (sacudida > 0.0001f)
        {
            float s = sacudida;
            pos += new Vector3(Mathf.PerlinNoise(Time.unscaledTime * 31f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.unscaledTime * 29f) - 0.5f, 0f) * (2f * s);
            sacudida = Mathf.MoveTowards(sacudida, 0f, dt * 0.35f);
        }

        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, pos, 1f - Mathf.Pow(suavidad, dt * 10f)),
            Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Pow(suavidad, dt * 10f)));

        fovExtra = Mathf.MoveTowards(fovExtra, 0f, dt * 6f);
        if (cam != null) cam.fieldOfView = fovBase - zoomTiro * Suave(pesoSeguir) + fovExtra;
    }

    static float Suave(float t) => t * t * (3f - 2f * t);

    void Grabar()
    {
        tGrabacion += Time.deltaTime;
        if (finGrabacion > 0f && tGrabacion > finGrabacion) { grabando = false; return; }
        if (cuadros.Count > 1200) { grabando = false; return; }

        var c = new Cuadro { t = tGrabacion };
        if (balon != null) { c.balonPos = balon.position; c.balonRot = balon.rotation; }
        if (arquero != null) { c.arqueroPos = arquero.position; c.arqueroRot = arquero.rotation; }
        if (pateador != null) { c.pateadorPos = pateador.position; c.pateadorRot = pateador.rotation; }
        if (animArquero != null)
        {
            var e = animArquero.IsInTransition(0) ? animArquero.GetNextAnimatorStateInfo(0) : animArquero.GetCurrentAnimatorStateInfo(0);
            c.arqueroEstado = e.fullPathHash; c.arqueroTiempo = e.normalizedTime;
        }
        if (animPateador != null)
        {
            var e = animPateador.IsInTransition(0) ? animPateador.GetNextAnimatorStateInfo(0) : animPateador.GetCurrentAnimatorStateInfo(0);
            c.pateadorEstado = e.fullPathHash; c.pateadorTiempo = e.normalizedTime;
        }
        cuadros.Add(c);
    }

    // ── repeticion ────────────────────────────────────────────────────

    /// <summary>Repite la jugada grabada en camara lenta. Devuelve cuando termina.</summary>
    public IEnumerator Repetir(System.Action<bool> mostrarLetrero = null)
    {
        if (!repetirGoles || cuadros.Count < 10) yield break;
        grabando = false;
        Repitiendo = true;
        mostrarLetrero?.Invoke(true);

        var rb = balon != null ? balon.GetComponent<Rigidbody>() : null;
        bool eraKinematico = rb != null && rb.isKinematic;
        if (rb != null)
        {
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true;
        }
        float velArq = animArquero != null ? animArquero.speed : 1f;
        float velPat = animPateador != null ? animPateador.speed : 1f;
        if (animArquero != null) animArquero.speed = 0f;
        if (animPateador != null) animPateador.speed = 0f;

        // camara al costado del arco, del lado contrario a donde entro el balon
        Vector3 fin = cuadros[cuadros.Count - 1].balonPos;
        float centroX = (bocaMin.x + bocaMax.x) * 0.5f;
        float lado = fin.x < centroX ? 1f : -1f;
        // cerca de la linea de gol para que la barrera no tape la vista
        Vector3 posCam = new Vector3(centroX + lado * ((bocaMax.x - bocaMin.x) * 0.5f + 5.5f), 1.7f, bocaMin.z - 2.8f);
        if (cam != null) cam.fieldOfView = fovRepeticion;

        // empieza un poco antes de la patada para ver la carrera
        float inicio = Mathf.Max(0f, cuadros[cuadros.Count - 1].t - 2.6f);
        float t = inicio;
        int i = 0;
        while (i < cuadros.Count - 1 && cuadros[i + 1].t < t) i++;
        float total = cuadros[cuadros.Count - 1].t;

        while (t <= total)
        {
            while (i < cuadros.Count - 2 && cuadros[i + 1].t < t) i++;
            var a = cuadros[i]; var b = cuadros[i + 1];
            float u = Mathf.InverseLerp(a.t, b.t, t);
            Aplicar(a, b, u, rb);

            Vector3 objetivo = Vector3.Lerp(a.balonPos, b.balonPos, u);
            Vector3 mirar = Vector3.Lerp(new Vector3(centroX, 1.2f, bocaMin.z), objetivo, 0.75f);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, posCam, 0.15f),
                Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mirar - transform.position), 0.2f));

            t += Time.unscaledDeltaTime * velocidadRepeticion;
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.4f);

        // deja todo como estaba antes de la repeticion
        if (animArquero != null) animArquero.speed = velArq;
        if (animPateador != null) animPateador.speed = velPat;
        if (rb != null) rb.isKinematic = eraKinematico;
        mostrarLetrero?.Invoke(false);
        transform.SetPositionAndRotation(posBase, rotBase);
        if (cam != null) cam.fieldOfView = fovBase;
        pesoSeguir = 0f; siguiendo = false; sacudida = 0f; fovExtra = 0f;
        cuadros.Clear();
        Repitiendo = false;
    }

    void Aplicar(Cuadro a, Cuadro b, float u, Rigidbody rb)
    {
        if (balon != null)
        {
            Vector3 p = Vector3.Lerp(a.balonPos, b.balonPos, u);
            Quaternion r = Quaternion.Slerp(a.balonRot, b.balonRot, u);
            balon.SetPositionAndRotation(p, r);
            if (rb != null) { rb.position = p; rb.rotation = r; }
        }
        if (arquero != null) arquero.SetPositionAndRotation(Vector3.Lerp(a.arqueroPos, b.arqueroPos, u), Quaternion.Slerp(a.arqueroRot, b.arqueroRot, u));
        if (pateador != null) pateador.SetPositionAndRotation(Vector3.Lerp(a.pateadorPos, b.pateadorPos, u), Quaternion.Slerp(a.pateadorRot, b.pateadorRot, u));
        Pose(animArquero, a.arqueroEstado, b.arqueroEstado, a.arqueroTiempo, b.arqueroTiempo, u);
        Pose(animPateador, a.pateadorEstado, b.pateadorEstado, a.pateadorTiempo, b.pateadorTiempo, u);
    }

    static void Pose(Animator anim, int estadoA, int estadoB, float tA, float tB, float u)
    {
        if (anim == null || estadoA == 0) return;
        int estado = u < 0.5f ? estadoA : estadoB;
        float tiempo = estadoA == estadoB ? Mathf.Lerp(tA, tB, u) : (u < 0.5f ? tA : tB);
        anim.Play(estado, 0, tiempo);
        anim.Update(0f);
    }
}
