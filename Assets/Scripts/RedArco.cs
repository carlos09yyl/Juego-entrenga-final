using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Red del arco con fisica: cuando el balon entra, la red se estira hacia afuera,
/// frena el balon como un resorte y despues tiembla hasta quedar quieta.
///
/// Como funciona:
/// 1. Al iniciar subdivide la malla de la red (la original tiene muy pocos vertices).
/// 2. Detecta los bordes que van pegados al marco (palos, travesaño, piso) y los deja fijos;
///    cuanto mas lejos del marco, mas se puede mover la red.
/// 3. Cada punto de la red es un resorte con amortiguacion, unido a sus vecinos,
///    asi el golpe se reparte como una onda.
/// 4. El balon empuja los puntos cercanos y la red le devuelve una fuerza que lo frena.
///
/// Va en el objeto de la malla de la red. El modelo debe tener Read/Write activado.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class RedArco : MonoBehaviour
{
    [Header("Balon")]
    public Rigidbody balon;
    [Tooltip("Radio del balon en metros.")]
    public float radioBalon = 0.15f;

    [Header("Malla")]
    [Tooltip("La red se subdivide hasta que cada arista mida menos que esto (m).")]
    public float largoMaximoArista = 0.3f;
    public int nivelesMaximos = 6;
    [Tooltip("Distancia desde el marco en la que la red pasa de fija a libre (m).")]
    public float distanciaFijacion = 0.5f;
    [Tooltip("Angulo minimo entre caras para considerar una arista como pegada al marco.")]
    public float anguloPliegue = 25f;

    [Header("Movimiento de la red")]
    [Tooltip("Que tan rapido vuelve cada punto a su lugar.")]
    public float rigidez = 70f;
    [Tooltip("Que tan rapido deja de temblar.")]
    public float amortiguacion = 6f;
    [Tooltip("Que tanto arrastra cada punto a sus vecinos (hace la onda).")]
    public float tension = 35f;
    [Tooltip("Radio alrededor del balon que se hunde con el (m).")]
    public float radioInfluencia = 0.9f;
    [Tooltip("Lo maximo que se puede estirar la red hacia afuera (m).")]
    public float hundimientoMaximo = 1.1f;

    [Header("Freno del balon")]
    [Tooltip("Fuerza con la que la red frena el balon por cada metro que se estira.")]
    public float rigidezFreno = 420f;
    public float amortiguacionFreno = 14f;
    [Tooltip("Que tanto empuja la red el balon de regreso (0 = nada, 1 = como un trampolin).")]
    [Range(0f, 1f)] public float devolucion = 0.12f;
    [Tooltip("Escribe en la consola cuando el balon toca la red.")]
    public bool registrar = true;

    // ── datos de la malla ─────────────────────────────────────────────
    private Mesh malla;
    private Vector3[] restLocal;
    private Vector3[] vertsLocal;
    private int[][] verticesDe;        // por punto soldado: los vertices de la malla que lo comparten
    private Vector3[] restMundo;
    private Vector3[] normalAfuera;    // normal que apunta hacia afuera del arco
    private float[] peso;              // 0 = pegado al marco, 1 = libre
    private int[][] vecinos;
    private Vector3[] desplazamiento;
    private Vector3[] velocidad;
    private Bounds caja;               // volumen del arco (en el mundo)

    private bool listo;
    private bool dentro;               // el balon entro al arco
    private bool activa;               // la red se esta moviendo
    private bool tocada;
    private BallController controlBalon;
    private float picoEstirado;

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        var mf = GetComponent<MeshFilter>();
        Mesh original = mf.sharedMesh;
        if (original == null || !original.isReadable)
        {
            Debug.LogWarning("[RedArco] La malla de la red no tiene Read/Write activado; la red no se movera.");
            enabled = false;
            return;
        }

        var segmentos = AristasDelMarco(original);
        malla = Subdividir(original);
        malla.MarkDynamic();
        mf.mesh = malla;

        PrepararPuntos(segmentos);
        if (balon != null) controlBalon = balon.GetComponent<BallController>();
        listo = true;

        int libres = 0;
        foreach (float p in peso) if (p > 0.5f) libres++;
        Debug.Log($"[RedArco] {restMundo.Length} puntos ({libres} libres), {segmentos.Count} aristas del marco, volumen {caja.center:F2} / {caja.size:F2}");
    }

    /// <summary>Deja la red quieta en su forma original.</summary>
    public void Reiniciar()
    {
        if (!listo) return;
        for (int w = 0; w < desplazamiento.Length; w++)
        {
            desplazamiento[w] = Vector3.zero;
            velocidad[w] = Vector3.zero;
        }
        dentro = false;
        tocada = false;
        activa = true;      // escribe una vez la forma original
    }

    // ── simulacion ────────────────────────────────────────────────────

    void FixedUpdate()
    {
        if (!listo) return;
        float dt = Time.fixedDeltaTime;

        // 1) resortes de la red
        float energia = 0f;
        if (activa)
        {
            for (int w = 0; w < desplazamiento.Length; w++)
            {
                if (peso[w] <= 0f) continue;
                Vector3 promedio = Vector3.zero;
                var vs = vecinos[w];
                for (int k = 0; k < vs.Length; k++) promedio += desplazamiento[vs[k]];
                if (vs.Length > 0) promedio /= vs.Length;

                Vector3 a = -rigidez * desplazamiento[w] - amortiguacion * velocidad[w] + tension * (promedio - desplazamiento[w]);
                velocidad[w] += a * dt;
                desplazamiento[w] += velocidad[w] * dt;
                energia += desplazamiento[w].sqrMagnitude + velocidad[w].sqrMagnitude * 0.01f;
            }
        }

        // 2) el balon empuja la red y la red frena el balon
        bool empujando = InteractuarConBalon();

        if (registrar && activa)
        {
            for (int w = 0; w < desplazamiento.Length; w += 4)
                picoEstirado = Mathf.Max(picoEstirado, desplazamiento[w].magnitude);
        }

        if (!empujando && energia < 1e-7f && activa)
        {
            if (registrar && picoEstirado > 0.01f) Debug.Log($"[RedArco] La red se estiro hasta {picoEstirado:F2} m y quedo quieta");
            picoEstirado = 0f;
            for (int w = 0; w < desplazamiento.Length; w++) { desplazamiento[w] = Vector3.zero; velocidad[w] = Vector3.zero; }
            Escribir();
            activa = false;
        }
    }

    bool InteractuarConBalon()
    {
        if (balon == null) return false;
        Vector3 b = balon.position;

        if (!dentro)
        {
            if (caja.Contains(b)) dentro = true;
            else return false;
        }
        else if ((caja.ClosestPoint(b) - b).sqrMagnitude > 4f)
        {
            dentro = false;      // el balon volvio al punto de penal
            tocada = false;
            return false;
        }

        float R = radioBalon;
        Vector3 vb = balon.linearVelocity;
        bool empujando = false;

        int cercano = -1;
        float latCercano = float.MaxValue;

        for (int w = 0; w < restMundo.Length; w++)
        {
            Vector3 n = normalAfuera[w];
            Vector3 d = b - restMundo[w];
            float along = Vector3.Dot(d, n);
            if (along < -2f * R || along > hundimientoMaximo + 2f * R) continue;
            float lat = (d - n * along).magnitude;
            if (lat > radioInfluencia) continue;

            float prof = along + R;
            if (prof <= 0f) continue;

            if (lat < latCercano) { latCercano = lat; cercano = w; }
            if (peso[w] <= 0f) continue;

            float f = 1f - lat / radioInfluencia;
            f = f * f * (3f - 2f * f);
            float objetivo = Mathf.Min(prof, hundimientoMaximo) * f * peso[w];
            float actual = Vector3.Dot(desplazamiento[w], n);
            if (actual < objetivo)
            {
                desplazamiento[w] += n * (objetivo - actual);
                float vObjetivo = Mathf.Max(0f, Vector3.Dot(vb, n)) * f;
                float vActual = Vector3.Dot(velocidad[w], n);
                if (vActual < vObjetivo) velocidad[w] += n * (vObjetivo - vActual);
            }
            empujando = true;
        }

        if (cercano >= 0 && latCercano < R * 2.5f)
        {
            if (!tocada)
            {
                tocada = true;
                controlBalon?.Soltar();      // desde aqui manda la fisica normal
                if (registrar) Debug.Log($"[RedArco] Balon en la red a {vb.magnitude:F1} m/s (punto con peso {peso[cercano]:F2})");
            }

            Vector3 n = normalAfuera[cercano];
            float prof = Vector3.Dot(b - restMundo[cercano], n) + R;
            float p = Mathf.Max(peso[cercano], 0.2f);
            float vn = Vector3.Dot(vb, n);

            // resorte + amortiguacion: cerca del marco la red es mas dura.
            // Cuando el balon ya viene de regreso la red empuja mucho menos: una red real
            // se traga la energia del tiro en vez de devolverlo como un trampolin.
            float k = rigidezFreno * (vn > 0f ? 1f : devolucion) / p;
            Vector3 acel = -n * (k * prof) - n * (amortiguacionFreno * vn);
            balon.AddForce(acel, ForceMode.Acceleration);

            // tope: la red no se rompe
            if (prof > hundimientoMaximo * p + 0.08f && vn > 0f)
                balon.linearVelocity = vb - n * vn;
        }

        if (empujando) activa = true;
        return empujando;
    }

    void LateUpdate()
    {
        if (listo && activa) Escribir();
    }

    void Escribir()
    {
        for (int w = 0; w < verticesDe.Length; w++)
        {
            Vector3 dl = transform.InverseTransformVector(desplazamiento[w]);
            var idx = verticesDe[w];
            for (int k = 0; k < idx.Length; k++) vertsLocal[idx[k]] = restLocal[idx[k]] + dl;
        }
        malla.vertices = vertsLocal;
        malla.RecalculateNormals();
    }

    // ── preparacion de la malla ───────────────────────────────────────

    static long Clave(Vector3 p, float precision)
    {
        long x = Mathf.RoundToInt(p.x / precision), y = Mathf.RoundToInt(p.y / precision), z = Mathf.RoundToInt(p.z / precision);
        return (x * 73856093L) ^ (y * 19349663L) ^ (z * 83492791L);
    }

    /// <summary>Aristas de la malla original que van pegadas al marco: bordes abiertos y pliegues entre caras.</summary>
    List<Vector3[]> AristasDelMarco(Mesh m)
    {
        var v = m.vertices;
        var tris = m.triangles;
        var mundo = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++) mundo[i] = transform.TransformPoint(v[i]);

        // soldar por posicion
        var idDe = new Dictionary<long, int>();
        var sold = new int[v.Length];
        var posSold = new List<Vector3>();
        for (int i = 0; i < v.Length; i++)
        {
            long k = Clave(mundo[i], 0.002f);
            if (!idDe.TryGetValue(k, out int id)) { id = posSold.Count; idDe[k] = id; posSold.Add(mundo[i]); }
            sold[i] = id;
        }

        // triangulos sin repetir (una red de dos caras trae cada triangulo dos veces)
        var vistos = new HashSet<(int, int, int)>();
        var aristas = new Dictionary<(int, int), List<Vector3>>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            int a = sold[tris[t]], b = sold[tris[t + 1]], c = sold[tris[t + 2]];
            if (a == b || b == c || a == c) continue;
            var orden = new[] { a, b, c };
            System.Array.Sort(orden);
            if (!vistos.Add((orden[0], orden[1], orden[2]))) continue;

            Vector3 n = Vector3.Cross(posSold[b] - posSold[a], posSold[c] - posSold[a]).normalized;
            Agregar(aristas, a, b, n);
            Agregar(aristas, b, c, n);
            Agregar(aristas, c, a, n);
        }

        float cosPliegue = Mathf.Cos(anguloPliegue * Mathf.Deg2Rad);
        var resultado = new List<Vector3[]>();
        foreach (var par in aristas)
        {
            var ns = par.Value;
            bool marco = ns.Count == 1;
            for (int i = 0; i < ns.Count && !marco; i++)
                for (int j = i + 1; j < ns.Count && !marco; j++)
                    if (Mathf.Abs(Vector3.Dot(ns[i], ns[j])) < cosPliegue) marco = true;
            if (marco) resultado.Add(new[] { posSold[par.Key.Item1], posSold[par.Key.Item2] });
        }
        return resultado;
    }

    static void Agregar(Dictionary<(int, int), List<Vector3>> d, int a, int b, Vector3 n)
    {
        var k = a < b ? (a, b) : (b, a);
        if (!d.TryGetValue(k, out var l)) { l = new List<Vector3>(); d[k] = l; }
        l.Add(n);
    }

    /// <summary>Divide cada triangulo en 4 hasta que las aristas sean cortas.</summary>
    Mesh Subdividir(Mesh original)
    {
        var pos = new List<Vector3>(original.vertices);
        var uv = new List<Vector2>(original.uv);
        bool conUV = uv.Count == pos.Count;
        var tris = new List<int>(original.triangles);

        // solo se parten las aristas largas (y los triangulos vecinos comparten el mismo punto medio),
        // asi las caras grandes quedan finas sin multiplicar los triangulos que ya eran pequeños
        for (int pasada = 0; pasada < nivelesMaximos * 2; pasada++)
        {
            var medio = new Dictionary<(int, int), int>();
            bool Larga(int a, int b) => transform.TransformVector(pos[b] - pos[a]).magnitude > largoMaximoArista;
            int Medio(int a, int b)
            {
                var k = a < b ? (a, b) : (b, a);
                if (medio.TryGetValue(k, out int nuevo)) return nuevo;
                nuevo = pos.Count;
                pos.Add((pos[a] + pos[b]) * 0.5f);
                if (conUV) uv.Add((uv[a] + uv[b]) * 0.5f);
                medio[k] = nuevo;
                return nuevo;
            }

            // marcar aristas a partir
            var partir = new HashSet<(int, int)>();
            for (int t = 0; t < tris.Count; t += 3)
                for (int e = 0; e < 3; e++)
                {
                    int a = tris[t + e], b = tris[t + (e + 1) % 3];
                    if (Larga(a, b)) partir.Add(a < b ? (a, b) : (b, a));
                }
            if (partir.Count == 0) break;
            bool Marcada(int a, int b) => partir.Contains(a < b ? (a, b) : (b, a));

            var nuevos = new List<int>(tris.Count * 2);
            for (int t = 0; t < tris.Count; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                bool s0 = Marcada(a, b), s1 = Marcada(b, c), s2 = Marcada(c, a);
                int cuantas = (s0 ? 1 : 0) + (s1 ? 1 : 0) + (s2 ? 1 : 0);

                if (cuantas == 0) { nuevos.AddRange(new[] { a, b, c }); continue; }
                if (cuantas == 3)
                {
                    int ab = Medio(a, b), bc = Medio(b, c), ca = Medio(c, a);
                    nuevos.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
                    continue;
                }

                // rotar el triangulo (sin cambiar el sentido) para tratar un solo caso
                int x, y, z;
                if (cuantas == 1)
                {
                    if (s0) { x = a; y = b; z = c; } else if (s1) { x = b; y = c; z = a; } else { x = c; y = a; z = b; }
                    int mx = Medio(x, y);
                    nuevos.AddRange(new[] { x, mx, z, mx, y, z });
                }
                else
                {
                    // dos aristas partidas: xy y yz; la que queda entera es zx
                    if (!s2) { x = a; y = b; z = c; } else if (!s0) { x = b; y = c; z = a; } else { x = c; y = a; z = b; }
                    int m1 = Medio(x, y), m2 = Medio(y, z);
                    nuevos.AddRange(new[] { m1, y, m2, x, m1, m2, x, m2, z });
                }
            }
            tris = nuevos;
        }

        var m = new Mesh { name = original.name + " (red)", indexFormat = IndexFormat.UInt32 };
        m.SetVertices(pos);
        if (conUV) m.SetUVs(0, uv);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        var bnd = m.bounds;
        bnd.Expand(transform.InverseTransformVector(Vector3.one * hundimientoMaximo * 2f).magnitude);
        m.bounds = bnd;      // para que no desaparezca al estirarse
        return m;
    }

    void PrepararPuntos(List<Vector3[]> segmentos)
    {
        restLocal = malla.vertices;
        vertsLocal = (Vector3[])restLocal.Clone();
        var tris = malla.triangles;

        // soldar vertices con la misma posicion (costuras de UV)
        var idDe = new Dictionary<long, int>();
        var sold = new int[restLocal.Length];
        var lista = new List<List<int>>();
        var posMundo = new List<Vector3>();
        for (int i = 0; i < restLocal.Length; i++)
        {
            Vector3 pm = transform.TransformPoint(restLocal[i]);
            long k = Clave(pm, 0.002f);
            if (!idDe.TryGetValue(k, out int id))
            {
                id = lista.Count; idDe[k] = id;
                lista.Add(new List<int>());
                posMundo.Add(pm);
            }
            lista[id].Add(i);
            sold[i] = id;
        }
        int n = lista.Count;
        verticesDe = new int[n][];
        for (int w = 0; w < n; w++) verticesDe[w] = lista[w].ToArray();
        restMundo = posMundo.ToArray();

        caja = new Bounds(restMundo[0], Vector3.zero);
        foreach (var p in restMundo) caja.Encapsulate(p);
        Vector3 centro = caja.center;

        // normales hacia afuera y vecinos
        var acumulado = new Vector3[n];
        var vec = new HashSet<int>[n];
        for (int w = 0; w < n; w++) vec[w] = new HashSet<int>();
        for (int t = 0; t < tris.Length; t += 3)
        {
            int a = sold[tris[t]], b = sold[tris[t + 1]], c = sold[tris[t + 2]];
            if (a == b || b == c || a == c) continue;
            Vector3 nt = Vector3.Cross(restMundo[b] - restMundo[a], restMundo[c] - restMundo[a]).normalized;
            Vector3 ct = (restMundo[a] + restMundo[b] + restMundo[c]) / 3f;
            if (Vector3.Dot(nt, ct - centro) < 0f) nt = -nt;
            acumulado[a] += nt; acumulado[b] += nt; acumulado[c] += nt;
            vec[a].Add(b); vec[a].Add(c);
            vec[b].Add(a); vec[b].Add(c);
            vec[c].Add(a); vec[c].Add(b);
        }

        normalAfuera = new Vector3[n];
        vecinos = new int[n][];
        peso = new float[n];
        for (int w = 0; w < n; w++)
        {
            normalAfuera[w] = acumulado[w].sqrMagnitude > 1e-8f ? acumulado[w].normalized : (restMundo[w] - centro).normalized;
            vecinos[w] = new List<int>(vec[w]).ToArray();

            float dmin = float.MaxValue;
            foreach (var s in segmentos) dmin = Mathf.Min(dmin, DistanciaASegmento(restMundo[w], s[0], s[1]));
            float u = Mathf.Clamp01(dmin / Mathf.Max(0.01f, distanciaFijacion));
            peso[w] = u * u * (3f - 2f * u);
        }

        desplazamiento = new Vector3[n];
        velocidad = new Vector3[n];
    }

    static float DistanciaASegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-8f, ab.sqrMagnitude));
        return (p - (a + ab * t)).magnitude;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!listo) return;
        for (int w = 0; w < restMundo.Length; w += 3)
        {
            Gizmos.color = Color.Lerp(Color.red, Color.green, peso[w]);
            Gizmos.DrawLine(restMundo[w], restMundo[w] + normalAfuera[w] * 0.15f);
        }
    }
#endif
}
