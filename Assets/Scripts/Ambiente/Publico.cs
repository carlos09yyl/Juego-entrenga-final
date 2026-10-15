using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Controla al publico de las tribunas: color de la camiseta de los hinchas (el del pais
/// elegido) y cuanto saltan. Al iniciar junta a todos los hinchas en pocas mallas grandes
/// (asi se dibujan rapido); en el proyecto solo se guardan los 7 modelos y los puestos.
/// </summary>
public class Publico : MonoBehaviour
{
    [Header("Hinchas (los llena la herramienta del editor)")]
    public Mesh[] modelos;
    public Material material;
    [Tooltip("x, y, z del puesto y w = hacia donde mira (grados).")]
    public Vector4[] puestos;

    [Tooltip("Lo que se mueve el publico mientras espera (0 = quietos, 1 = saltando).")]
    public float emocionBase = 0.06f;
    [Tooltip("Segundos que dura la fiesta despues de un gol.")]
    public float duracionFiesta = 4.5f;

    static Publico instancia;
    float emocion, objetivo, hastaCuando;

    static readonly int IdColor = Shader.PropertyToID("_ColorHincha");
    static readonly int IdEmocion = Shader.PropertyToID("_Emocion");

    void Awake()
    {
        instancia = this;
        Construir();
        Shader.SetGlobalColor(IdColor, Paises.Actual.camiseta);
        emocion = objetivo = emocionBase;
        Shader.SetGlobalFloat(IdEmocion, emocion);
    }

    /// <summary>Muestra u oculta las tribunas llenas segun las opciones.</summary>
    public static void AplicarVisible()
    {
        if (instancia == null) return;
        foreach (var r in instancia.GetComponentsInChildren<Renderer>(true)) r.enabled = Opciones.PublicoVisible;
    }

    void Start()
    {
        AplicarVisible();
        SonidosPenales.Ambiente(true);
    }

    void OnEnable() => PenaltyTargetSelector.TiroTerminado += Reaccionar;
    void OnDisable() => PenaltyTargetSelector.TiroTerminado -= Reaccionar;

    void Reaccionar(bool gol)
    {
        if (gol)
        {
            objetivo = 1f; hastaCuando = Time.time + duracionFiesta;
            SonidosPenales.SubirAmbiente(1.7f, 3f);
        }
        else
        {
            objetivo = 0f; hastaCuando = Time.time + 1.5f;      // se quedan quietos un momento
            SonidosPenales.Reproducir("lamento", 0.75f, 0.15f);
        }
    }

    /// <summary>Junta a todos los hinchas en mallas de hasta 60.000 vertices.</summary>
    void Construir()
    {
        if (modelos == null || modelos.Length == 0 || puestos == null || material == null) return;
        var azar = new System.Random(7);
        var colores = new Color[Paises.Lista.Length];
        for (int i = 0; i < colores.Length; i++) colores[i] = Paises.Lista[i].camiseta;

        // datos de cada modelo una sola vez
        var mv = new Vector3[modelos.Length][]; var muv = new Vector2[modelos.Length][];
        var mcol = new Color[modelos.Length][]; var mtri = new int[modelos.Length][];
        for (int i = 0; i < modelos.Length; i++)
        {
            mv[i] = modelos[i].vertices; muv[i] = modelos[i].uv; mcol[i] = modelos[i].colors; mtri[i] = modelos[i].triangles;
        }

        var v = new List<Vector3>(60000); var uv = new List<Vector2>(60000); var uv2 = new List<Vector2>(60000);
        var col = new List<Color>(60000); var tri = new List<int>(120000);
        int trozo = 0;
        void Cerrar()
        {
            if (v.Count == 0) return;
            var malla = new Mesh { name = "Tribuna " + trozo, indexFormat = IndexFormat.UInt32 };
            malla.SetVertices(v); malla.SetUVs(0, uv); malla.SetUVs(1, uv2); malla.SetColors(col); malla.SetTriangles(tri, 0);
            malla.RecalculateBounds();
            malla.bounds = new Bounds(malla.bounds.center, malla.bounds.size + Vector3.up);   // margen para los saltos
            var go = new GameObject("Tribuna " + trozo, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = malla;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            trozo++;
            v.Clear(); uv.Clear(); uv2.Clear(); col.Clear(); tri.Clear();
        }

        foreach (var p in puestos)
        {
            int k = azar.Next(modelos.Length);
            if (v.Count + mv[k].Length > 60000) Cerrar();
            float escala = 0.95f + (float)azar.NextDouble() * 0.12f;
            var matriz = Matrix4x4.TRS(new Vector3(p.x, p.y, p.z), Quaternion.Euler(0, p.w, 0), Vector3.one * escala);
            var fila = new Vector2((float)azar.NextDouble(), (float)azar.NextDouble());
            var c = colores[azar.Next(colores.Length)];
            var camiseta = new Color(c.r, c.g, c.b, 1f);
            var resto = new Color(1, 1, 1, 0);
            int base_ = v.Count;
            var vs = mv[k]; var cs = mcol[k];
            for (int i = 0; i < vs.Length; i++)
            {
                v.Add(matriz.MultiplyPoint3x4(vs[i]));
                uv.Add(muv[k][i]);
                uv2.Add(fila);
                col.Add(cs.Length > i && cs[i].a > 0.5f ? camiseta : resto);
            }
            foreach (int t in mtri[k]) tri.Add(base_ + t);
        }
        Cerrar();
    }

    void Update()
    {
        if (Time.time > hastaCuando) objetivo = emocionBase;
        emocion = Mathf.MoveTowards(emocion, objetivo, Time.deltaTime * (objetivo > emocion ? 4f : 0.6f));
        Shader.SetGlobalFloat(IdEmocion, emocion);
    }
}
