using UnityEngine;

/// <summary>
/// Celebracion del gol: dos cañones de confeti salen desde los palos y una explosion
/// de chispas doradas sale desde la red. Todo se arma por codigo al iniciar, no hay que
/// crear nada en la escena. Reemplaza al sistema de particulas viejo.
/// </summary>
public class CelebracionGol : MonoBehaviour
{
    [Header("Arco (lo llena el PenaltyTargetSelector)")]
    public Vector3 bocaMin;
    public Vector3 bocaMax;
    [Tooltip("Material base de particulas (URP). Se usa una copia.")]
    public Material materialBase;

    [Header("Confeti")]
    public int confetiPorLado = 140;
    public Color[] colores =
    {
        new Color32(0xFF, 0xC5, 0x31, 0xFF),   // dorado
        new Color32(0xFF, 0xFF, 0xFF, 0xFF),   // blanco
        new Color32(0xE8, 0x2E, 0x3C, 0xFF),   // rojo
        new Color32(0x3D, 0xD9, 0xFF, 0xFF),   // celeste
        new Color32(0x3D, 0xFF, 0x8A, 0xFF),   // verde
    };

    private ParticleSystem confetiIzq, confetiDer, chispas;

    void Start()
    {
        if (materialBase == null) return;

        // confeti: cuadritos planos que giran en 3D (dos caras visibles, sin textura)
        var matConfeti = new Material(materialBase);
        matConfeti.mainTexture = Texture2D.whiteTexture;
        if (matConfeti.HasProperty("_BaseMap")) matConfeti.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (matConfeti.HasProperty("_Cull")) matConfeti.SetFloat("_Cull", 0f);

        Vector3 izq = new Vector3(bocaMin.x + 0.3f, bocaMin.y + 0.2f, bocaMin.z - 0.3f);
        Vector3 der = new Vector3(bocaMax.x - 0.3f, bocaMin.y + 0.2f, bocaMin.z - 0.3f);
        confetiIzq = CrearConfeti("Confeti Izquierda", izq, new Vector3(0.35f, 1f, -0.35f), matConfeti);
        confetiDer = CrearConfeti("Confeti Derecha", der, new Vector3(-0.35f, 1f, -0.35f), matConfeti);

        Vector3 centro = new Vector3((bocaMin.x + bocaMax.x) * 0.5f, (bocaMin.y + bocaMax.y) * 0.45f, bocaMin.z + 0.4f);
        chispas = CrearChispas("Chispas Gol", centro, new Material(materialBase));
    }

    /// <summary>Dispara la celebracion.</summary>
    public void Lanzar()
    {
        confetiIzq?.Play(true);
        confetiDer?.Play(true);
        chispas?.Play(true);
    }

    // ─────────────────────────────────────────────────────────────────

    ParticleSystem Base(string nombre, Vector3 pos, Quaternion rot)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.SetPositionAndRotation(pos, rot);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var em = ps.emission;
        em.rateOverTime = 0f;
        return ps;
    }

    ParticleSystem CrearConfeti(string nombre, Vector3 pos, Vector3 direccion, Material mat)
    {
        var ps = Base(nombre, pos, Quaternion.LookRotation(direccion.normalized));
        var main = ps.main;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.2f, 4.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 16f);
        main.gravityModifier = 0.3f;
        main.maxParticles = 600;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.07f, 0.11f);
        main.startSizeZ = 1f;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var grad = new Gradient();
        var keys = new GradientColorKey[Mathf.Min(colores.Length, 8)];
        for (int i = 0; i < keys.Length; i++) keys[i] = new GradientColorKey(colores[i], keys.Length == 1 ? 0f : i / (keys.Length - 1f));
        grad.mode = GradientMode.Fixed;
        grad.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        main.startColor = new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor };

        // dos tandas: un cañonazo fuerte y otro mas suave
        var em = ps.emission;
        em.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)confetiPorLado),
            new ParticleSystem.Burst(0.25f, (short)(confetiPorLado / 2)),
        });

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 22f;
        forma.radius = 0.15f;

        // el aire lo frena y lo hace flotar como papelitos
        var lim = ps.limitVelocityOverLifetime;
        lim.enabled = true;
        lim.limit = 1.6f;
        lim.dampen = 0.12f;

        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 1.4f;
        ruido.frequency = 0.6f;
        ruido.scrollSpeed = 0.4f;

        var giro = ps.rotationOverLifetime;
        giro.enabled = true;
        giro.separateAxes = true;
        giro.x = new ParticleSystem.MinMaxCurve(-8f, 8f);
        giro.y = new ParticleSystem.MinMaxCurve(-8f, 8f);
        giro.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        // se desvanece al final
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var desvanecer = new Gradient();
        desvanecer.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                           new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = desvanecer;

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Mesh;
        r.mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        r.alignment = ParticleSystemRenderSpace.World;
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return ps;
    }

    ParticleSystem CrearChispas(string nombre, Vector3 pos, Material mat)
    {
        var ps = Base(nombre, pos, Quaternion.identity);
        var main = ps.main;
        main.duration = 0.5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.gravityModifier = 0.35f;
        main.maxParticles = 300;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.35f), Color.white);

        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 160) });

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Hemisphere;
        forma.radius = 0.4f;
        forma.rotation = new Vector3(-90f, 0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.1f), 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.06f;
        r.lengthScale = 1.5f;
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return ps;
    }
}
