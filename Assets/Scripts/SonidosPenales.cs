using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sonidos del juego. Se crea solo la primera vez que se usa y carga los audios
/// de Assets/Resources/Sonidos. Para cambiar un sonido basta con reemplazar el
/// archivo por otro con el mismo nombre (patada, red, gol_publico, fallo, palo, atajada,
/// silbato, lamento y ambiente, que es el murmullo del publico en bucle).
///
/// Uso: SonidosPenales.Reproducir("patada");   SonidosPenales.Ambiente(true);
/// El volumen sale de las Opciones (efectos y publico por separado).
/// </summary>
public class SonidosPenales : MonoBehaviour
{
    private static SonidosPenales instancia;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private AudioSource fuente, fuenteAmbiente;
    private float subidaAmbiente = 1f;
    private Coroutine subida;

    static SonidosPenales Instancia
    {
        get
        {
            if (instancia == null)
            {
                var go = new GameObject("Sonidos");
                instancia = go.AddComponent<SonidosPenales>();
            }
            return instancia;
        }
    }

    void Awake()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;     // se escucha igual en todo lado (sonido de interfaz / transmision)
        fuenteAmbiente = gameObject.AddComponent<AudioSource>();
        fuenteAmbiente.playOnAwake = false;
        fuenteAmbiente.loop = true;
        fuenteAmbiente.spatialBlend = 0f;
        foreach (var c in Resources.LoadAll<AudioClip>("Sonidos")) clips[c.name] = c;
        AplicarVolumen();
    }

    void OnDestroy() { if (instancia == this) instancia = null; }

    /// <summary>Reproduce un sonido por su nombre. volumen de 0 a 1.</summary>
    public static void Reproducir(string nombre, float volumen = 1f, float retraso = 0f)
    {
        var s = Instancia;
        if (!s.clips.TryGetValue(nombre, out var clip) || clip == null) return;
        // la ovacion y el lamento son del publico; lo demas son efectos
        bool publico = nombre == "gol_publico" || nombre == "lamento";
        float v = Mathf.Clamp01(volumen) * (publico ? Opciones.VolPublico : Opciones.VolEfectos);
        if (retraso <= 0f) s.fuente.PlayOneShot(clip, v);
        else s.StartCoroutine(s.Diferido(clip, v, retraso));
    }

    /// <summary>Prende o apaga el murmullo del publico (bucle).</summary>
    public static void Ambiente(bool sonar)
    {
        var s = Instancia;
        if (!s.clips.TryGetValue("ambiente", out var clip) || clip == null) return;
        if (sonar)
        {
            s.fuenteAmbiente.clip = clip;
            if (!s.fuenteAmbiente.isPlaying) s.fuenteAmbiente.Play();
        }
        else s.fuenteAmbiente.Stop();
    }

    /// <summary>El murmullo sube un rato (por ejemplo, en el gol) y vuelve solo.</summary>
    public static void SubirAmbiente(float factor, float segundos)
    {
        var s = Instancia;
        if (s.subida != null) s.StopCoroutine(s.subida);
        s.subida = s.StartCoroutine(s.Subida(factor, segundos));
    }

    public static void AplicarVolumen()
    {
        if (instancia == null) return;
        instancia.fuenteAmbiente.volume = 0.55f * Opciones.VolPublico * instancia.subidaAmbiente;
    }

    IEnumerator Subida(float factor, float segundos)
    {
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) { subidaAmbiente = Mathf.Lerp(1f, factor, t / 0.4f); AplicarVolumen(); yield return null; }
        yield return new WaitForSecondsRealtime(segundos);
        for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime) { subidaAmbiente = Mathf.Lerp(factor, 1f, t / 1.5f); AplicarVolumen(); yield return null; }
        subidaAmbiente = 1f;
        AplicarVolumen();
    }

    IEnumerator Diferido(AudioClip clip, float volumen, float retraso)
    {
        yield return new WaitForSeconds(retraso);
        fuente.PlayOneShot(clip, volumen);
    }
}
