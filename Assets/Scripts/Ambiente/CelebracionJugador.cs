using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// El pateador celebra el gol: se voltea hacia la camara y da unos saltos con los brazos arriba.
/// Usa la animacion de salto de los Mini Simple Characters sin tocar su Animator Controller.
/// </summary>
public class CelebracionJugador : MonoBehaviour
{
    public AnimationClip clipSalto;
    public int saltos = 3;
    public float alturaSalto = 0.32f;
    public float duracionSalto = 0.5f;

    Animator anim;
    Coroutine rutina;
    PlayableGraph grafo;

    void Awake() => anim = GetComponentInChildren<Animator>();
    void OnEnable() => PenaltyTargetSelector.TiroTerminado += AlTerminar;
    void OnDisable()
    {
        PenaltyTargetSelector.TiroTerminado -= AlTerminar;
        Soltar();
    }

    void AlTerminar(bool gol)
    {
        if (!gol || anim == null || clipSalto == null) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Celebrar());
    }

    IEnumerator Celebrar()
    {
        yield return new WaitForSeconds(0.35f);
        Vector3 inicio = transform.position;
        Quaternion giroInicial = transform.rotation;

        // se da la vuelta hacia la tribuna de atras (de frente a quien juega)
        Quaternion giroFinal = giroInicial * Quaternion.Euler(0f, 180f, 0f);

        Soltar();
        grafo = PlayableGraph.Create("Celebracion");
        var salida = AnimationPlayableOutput.Create(grafo, "Salida", anim);
        var clip = AnimationClipPlayable.Create(grafo, clipSalto);
        clip.SetApplyFootIK(false);
        salida.SetSourcePlayable(clip);
        grafo.Play();

        for (int i = 0; i < saltos; i++)
        {
            clip.SetTime(clipSalto.length * 0.2f);
            clip.SetSpeed(clipSalto.length * 0.65f / duracionSalto);
            for (float t = 0f; t < duracionSalto; t += Time.deltaTime)
            {
                float u = t / duracionSalto;
                if (i == 0) transform.rotation = Quaternion.Slerp(giroInicial, giroFinal, Mathf.Clamp01(u * 2f));
                transform.position = inicio + Vector3.up * alturaSalto * Mathf.Sin(u * Mathf.PI);
                yield return null;
            }
        }
        transform.position = inicio;
        yield return new WaitForSeconds(0.3f);
        Soltar();
        rutina = null;
    }

    /// <summary>Devuelve el control al Animator normal (lo llama tambien el reinicio del tiro).</summary>
    public void Soltar()
    {
        if (grafo.IsValid()) grafo.Destroy();
    }
}
