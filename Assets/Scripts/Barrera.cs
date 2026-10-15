using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Barrera del tiro libre: dos jugadores quietos (en idle) que en cada intento se paran
/// al azar en 2 de 3 posiciones (izquierda, centro, derecha). Las posiciones estan dentro
/// de una caja que, vista desde el balon, va de palo a palo.
/// Los jugadores se crean solos al iniciar, con los mismos colores del arquero.
/// </summary>
public class Barrera : MonoBehaviour
{
    [Header("Jugadores")]
    public GameObject prefabJugador;
    public RuntimeAnimatorController controlador;
    [Tooltip("Se copian sus materiales para que la barrera vista igual que el arquero.")]
    public Transform copiarColoresDe;
    public float escala = 1.25f;

    [Header("Posiciones (las llena la herramienta del editor)")]
    [Tooltip("Izquierda, centro y derecha vistas desde el balon, en el mundo.")]
    public Vector3[] posiciones = new Vector3[3];
    [Tooltip("Metros que se corre el arquero hacia el lado libre.")]
    public float corrimientoArquero = 1.1f;

    [Header("Salto (como en el futbol real, saltan cuando se patea)")]
    [Tooltip("Animacion de salto de los Mini Simple Characters.")]
    public AnimationClip clipSalto;
    [Tooltip("Metros que se elevan segun el nivel del arquero (racha 0..4). Siempre menos que el arquero.")]
    public float[] alturaPorNivel = { 0.15f, 0.2f, 0.26f, 0.32f, 0.38f };
    [Tooltip("Segundos en el aire.")]
    public float duracionSalto = 0.62f;

    /// <summary>Se dispara cuando el balon pega en un jugador de la barrera.</summary>
    public event Action OnGolpe;

    /// <summary>Indice de la posicion que quedo libre (0 izquierda, 1 centro, 2 derecha).</summary>
    public int Libre { get; private set; } = 1;

    private readonly GameObject[] jugadores = new GameObject[2];
    private readonly Vector3[] base_ = new Vector3[2];
    private readonly Coroutine[] saltos = new Coroutine[2];

    void Awake()
    {
        if (prefabJugador == null || posiciones == null || posiciones.Length < 3) return;
        for (int i = 0; i < 2; i++) jugadores[i] = CrearJugador(i);
        Reubicar();
    }

    GameObject CrearJugador(int i)
    {
        var go = Instantiate(prefabJugador, transform);
        go.name = "Jugador Barrera " + (i + 1);
        go.transform.localScale = Vector3.one * escala;
        go.transform.rotation = transform.rotation;

        var anim = go.GetComponent<Animator>();
        if (anim != null)
        {
            anim.runtimeAnimatorController = controlador;
            anim.applyRootMotion = false;
            // la patada del controlador (estado "Kick") se reemplaza por el salto, solo para la barrera
            if (clipSalto != null && controlador != null)
            {
                var cambio = new AnimatorOverrideController(controlador);
                var lista = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
                cambio.GetOverrides(lista);
                for (int k = 0; k < lista.Count; k++)
                    if (lista[k].Key != null && !lista[k].Key.name.ToLower().Contains("idle"))
                        lista[k] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(lista[k].Key, clipSalto);
                cambio.ApplyOverrides(lista);
                anim.runtimeAnimatorController = cambio;
            }
        }

        // mismos colores que el arquero
        if (copiarColoresDe != null)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var modelo = Buscar(copiarColoresDe, r.name);
                if (modelo == null) continue;
                var rm = modelo.GetComponent<Renderer>();
                if (rm != null) r.sharedMaterials = rm.sharedMaterials;
            }
        }

        // cuerpo solido para que el balon rebote
        var cap = go.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 0.78f, 0f);
        cap.height = 1.56f;
        cap.radius = 0.36f;
        var golpe = go.AddComponent<GolpeBarrera>();
        golpe.barrera = this;
        return go;
    }

    static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    /// <summary>Elige al azar la posicion libre y para a los dos jugadores en las otras dos.</summary>
    public int Reubicar()
    {
        Libre = UnityEngine.Random.Range(0, 3);
        int j = 0;
        for (int s = 0; s < 3 && j < 2; s++)
        {
            if (s == Libre || jugadores[j] == null) continue;
            if (saltos[j] != null) { StopCoroutine(saltos[j]); saltos[j] = null; }
            base_[j] = posiciones[s];
            jugadores[j].transform.position = posiciones[s];
            var anim = jugadores[j].GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
                anim.Play(0, 0, UnityEngine.Random.value);   // que no respiren al mismo tiempo
            j++;
        }
        return Libre;
    }

    /// <summary>Hacia donde (X del mundo) se corre el arquero para cubrir el hueco.</summary>
    public float DesplazamientoArquero()
    {
        if (Libre == 1) return 0f;
        float centro = posiciones[1].x;
        return Mathf.Sign(posiciones[Libre].x - centro) * corrimientoArquero;
    }

    internal void Golpe() => OnGolpe?.Invoke();

    /// <summary>Los dos jugadores saltan al mismo tiempo que se patea. nivel = racha del jugador (0..4).</summary>
    public void Saltar(int nivel)
    {
        float altura = alturaPorNivel != null && alturaPorNivel.Length > 0
            ? alturaPorNivel[Mathf.Clamp(nivel, 0, alturaPorNivel.Length - 1)] : 0.3f;
        for (int j = 0; j < 2; j++)
        {
            if (jugadores[j] == null) continue;
            if (saltos[j] != null) StopCoroutine(saltos[j]);
            // cada uno con una pizca de diferencia para que no parezcan robots
            saltos[j] = StartCoroutine(SaltoUno(j, altura * UnityEngine.Random.Range(0.9f, 1.05f), UnityEngine.Random.Range(0f, 0.05f)));
        }
    }

    IEnumerator SaltoUno(int j, float altura, float retraso)
    {
        var go = jugadores[j];
        var anim = go.GetComponent<Animator>();
        if (retraso > 0f) yield return new WaitForSeconds(retraso);

        if (anim != null && clipSalto != null)
        {
            anim.speed = clipSalto.length * 0.62f / duracionSalto;
            anim.Play("Kick", 0, 0.22f);       // arranca justo en el impulso del salto
        }

        for (float t = 0f; t < duracionSalto; t += Time.deltaTime)
        {
            float u = t / duracionSalto;
            go.transform.position = base_[j] + Vector3.up * altura * Mathf.Sin(u * Mathf.PI);
            yield return null;
        }
        go.transform.position = base_[j];

        if (anim != null)
        {
            anim.speed = 1f;
            anim.CrossFade("Idle", 0.2f, 0, UnityEngine.Random.value);
        }
        saltos[j] = null;
    }
}
