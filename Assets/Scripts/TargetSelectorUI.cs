using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD del juego de penales: marcador con los 5 intentos, panel para elegir a donde patear,
/// pista de controles, aviso de cada intento y el resultado grande (GOL, ATAJADA, AFUERA, AL PALO).
///
/// El HUD se arma con el menu contextual "Construir HUD" (clic derecho en el componente).
/// Queda guardado en la escena, asi se puede mover o cambiar de color desde el Inspector.
/// </summary>
public class TargetSelectorUI : MonoBehaviour
{
    public enum Resultado { Gol, Atajada, Afuera, Palo, Fallo, Barrera }

    // ── Estilo ────────────────────────────────────────────────────────
    [Header("Tipografias")]
    public TMP_FontAsset fuenteTitulos;     // Bebas Neue
    public TMP_FontAsset fuenteTexto;       // Rajdhani Bold
    public TMP_FontAsset fuenteTextoSuave;  // Rajdhani SemiBold

    [Header("Formas")]
    public Sprite spritePanel;
    public Sprite spriteBorde;
    public Sprite spriteCirculo;
    public Sprite spriteAnillo;
    public Sprite spriteBrillo;
    public Sprite spriteDegradado;

    [Header("Colores")]
    public Color colorPanel     = new Color32(0x0A, 0x12, 0x28, 0xE6);
    public Color colorAcento    = new Color32(0xFF, 0xC5, 0x31, 0xFF);
    public Color colorGol       = new Color32(0x3D, 0xFF, 0x8A, 0xFF);
    public Color colorFallo     = new Color32(0xFF, 0x4D, 0x5E, 0xFF);
    public Color colorAtajada   = new Color32(0x3D, 0xD9, 0xFF, 0xFF);
    public Color colorTextoSuave = new Color(1f, 1f, 1f, 0.6f);

    // ── Referencias (las llena "Construir HUD") ───────────────────────
    [Header("Panel de seleccion")]
    public GameObject panel;
    public Image[] cellImages;
    public Color normalColor   = new Color(1f, 1f, 1f, 0.08f);
    public Color selectedColor = new Color32(0xFF, 0xC5, 0x31, 0xF0);
    public RectTransform brilloSeleccion;

    [Header("Textos")]
    public TextMeshProUGUI selectionLabel;
    public TextMeshProUGUI hintLabel;
    public TextMeshProUGUI attemptLabel;
    public TextMeshProUGUI goalsLabel;
    [Tooltip("Ya no se usa: el aviso de cada intento sale en el banner.")]
    public TextMeshProUGUI feedbackLabel;
    public float feedbackDuration = 2.2f;

    [Header("Efecto")]
    public Image[] puntosEfecto;
    public TextMeshProUGUI textoEfecto;

    [Header("Marcador")]
    public Image[] puntosIntento;
    public Image[] anillosIntento;

    [Header("Pista de controles")]
    public CanvasGroup grupoPista;

    [Header("Banner de intento")]
    public CanvasGroup grupoBanner;
    public TextMeshProUGUI bannerTitulo;
    public TextMeshProUGUI bannerSubtitulo;

    [Header("Resultado")]
    public CanvasGroup grupoResultado;
    public RectTransform franjaResultado;
    public TextMeshProUGUI resultadoTitulo;
    public TextMeshProUGUI resultadoSombra;
    public TextMeshProUGUI resultadoSubtitulo;

    // ─────────────────────────────────────────────────────────────────

    private const string HintSelect = "<color=#FFC531>WASD</color>  apuntar      <color=#FFC531>Q / E</color>  efecto      <color=#FFC531>MANTÉN ESPACIO</color>  potencia";
    private const string HintIdle   = "Presiona  <color=#FFC531>ENTER</color>  para apuntar";

    private readonly List<Resultado> historial = new List<Resultado>();
    private int intentoActual = 1;
    private int intentosMax = 5;
    private int seleccion = 4;
    private int puntosActuales;
    private int rachaActual;
    private RectTransform insigniaRacha;
    private TextMeshProUGUI textoRacha;
    private Coroutine animRacha;

    private Coroutine animPanel, animBanner, animResultado, animPista;

    private static readonly string[] FrasesGol     = { "¡Adentro!", "¡Golazo!", "¡Imparable!", "¡A la red!" };
    private static readonly string[] FrasesAtajada = { "¡Qué manos del arquero!", "El arquero la sacó", "¡Atajadón!" };
    private static readonly string[] FrasesAfuera  = { "Se fue desviado", "No encontró el arco", "Por poquito" };
    private static readonly string[] FrasesPalo    = { "¡Se estrelló en el palo!", "La salvó el poste" };
    private static readonly string[] FrasesBarrera = { "La barrera no la dejó pasar", "Le faltó rosca", "Chocó en la barrera" };
    private static readonly string[] NombresEfecto = { "MUCHA CURVA A LA IZQUIERDA", "CURVA A LA IZQUIERDA", "SIN EFECTO", "CURVA A LA DERECHA", "MUCHA CURVA A LA DERECHA" };

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (grupoResultado != null) grupoResultado.alpha = 0f;
        if (grupoBanner != null) grupoBanner.alpha = 0f;
        ShowUI(false);
    }

    void Update()
    {
        // el punto del intento actual late suave
        if (anillosIntento == null) return;
        float pulso = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 5f);
        int i = intentoActual - 1;
        if (i >= 0 && i < anillosIntento.Length && anillosIntento[i] != null && anillosIntento[i].enabled)
            anillosIntento[i].rectTransform.localScale = Vector3.one * pulso;
    }

    // ── API que usa PenaltyTargetSelector ─────────────────────────────

    public void ShowUI(bool visible)
    {
        if (panel != null)
        {
            if (animPanel != null) StopCoroutine(animPanel);
            if (visible)
            {
                panel.SetActive(true);
                if (isActiveAndEnabled) animPanel = StartCoroutine(EntrarPanel());
            }
            else panel.SetActive(false);
        }
        CambiarPista(visible ? HintSelect : HintIdle);
    }

    public void HideAllUI()
    {
        if (panel != null) panel.SetActive(false);
        MostrarPista(false);
        if (selectionLabel != null) selectionLabel.gameObject.SetActive(false);
    }

    public void HideSelectionOnlyUI()
    {
        if (panel != null) panel.SetActive(false);
        MostrarPista(false);
    }

    public void ShowIdleHint()
    {
        CambiarPista(HintIdle);
    }

    public void UpdateSelection(int index, string targetName)
    {
        seleccion = index;
        for (int i = 0; i < cellImages.Length; i++)
        {
            if (cellImages[i] == null) continue;
            bool sel = i == index;
            cellImages[i].color = sel ? selectedColor : normalColor;
            cellImages[i].rectTransform.localScale = sel ? Vector3.one * 1.06f : Vector3.one;
        }
        if (brilloSeleccion != null && index >= 0 && index < cellImages.Length && cellImages[index] != null)
        {
            brilloSeleccion.gameObject.SetActive(true);
            brilloSeleccion.position = cellImages[index].rectTransform.position;
        }
        if (selectionLabel != null)
        {
            selectionLabel.gameObject.SetActive(true);
            selectionLabel.text = FormatTargetName(targetName).ToUpper();
        }
    }

    public void UpdateAttemptDisplay(int attemptNumber, int maxAttempts, int goals)
    {
        intentosMax = maxAttempts;
        if (attemptNumber <= 1 && goals == 0) historial.Clear();     // partida nueva

        intentoActual = Mathf.Clamp(attemptNumber, 1, maxAttempts + 1);
        if (attemptLabel != null)
            attemptLabel.text = $"{Mathf.Min(attemptNumber, maxAttempts)}<size=60%><color=#FFFFFF99>/{maxAttempts}</color></size>";
        if (goalsLabel != null)
            goalsLabel.text = puntosActuales.ToString();

        PintarPuntos();
    }

    /// <summary>Muestra los puntos en el marcador y la insignia de racha (desde 2 goles seguidos).</summary>
    public void UpdatePuntos(int puntos, int racha)
    {
        bool subioRacha = racha > rachaActual && racha >= 2;
        puntosActuales = puntos;
        rachaActual = racha;
        if (goalsLabel != null)
        {
            goalsLabel.text = puntos.ToString();
            var etiqueta = goalsLabel.transform.parent != null ? goalsLabel.transform.parent.Find("Etiqueta Goles") : null;
            var et = etiqueta != null ? etiqueta.GetComponent<TextMeshProUGUI>() : null;
            if (et != null) et.text = "PUNTOS";
        }

        AsegurarInsigniaRacha();
        if (insigniaRacha == null) return;
        insigniaRacha.gameObject.SetActive(racha >= 2);
        if (racha >= 2)
        {
            textoRacha.text = racha >= 4 ? $"<color=#FF4D5E>RACHA x{racha}</color>  ·  ARQUERO AL MÁXIMO" : $"RACHA x{racha}  ·  ARQUERO MÁS DIFÍCIL";
            if (subioRacha && isActiveAndEnabled)
            {
                if (animRacha != null) StopCoroutine(animRacha);
                animRacha = StartCoroutine(Latido(insigniaRacha));
            }
        }
    }

    private RectTransform letreroRepeticion, franjaArriba, franjaAbajo;
    private Coroutine animRepeticion;

    /// <summary>Durante la repeticion: franjas negras de cine y el letrero REPETICION con punto rojo.</summary>
    public void MostrarRepeticion(bool visible)
    {
        var raiz = transform.Find("HUD");
        if (raiz == null) raiz = transform;
        if (letreroRepeticion == null)
        {
            franjaArriba = Img("Franja Cine Arriba", raiz, null, Color.black).rectTransform;
            franjaArriba.anchorMin = new Vector2(0, 1); franjaArriba.anchorMax = new Vector2(1, 1);
            franjaArriba.pivot = new Vector2(0.5f, 1); franjaArriba.anchoredPosition = Vector2.zero;
            franjaAbajo = Img("Franja Cine Abajo", raiz, null, Color.black).rectTransform;
            franjaAbajo.anchorMin = new Vector2(0, 0); franjaAbajo.anchorMax = new Vector2(1, 0);
            franjaAbajo.pivot = new Vector2(0.5f, 0); franjaAbajo.anchoredPosition = Vector2.zero;

            letreroRepeticion = Panel("Repeticion", raiz, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -36), new Vector2(330, 70), colorPanel);
            var punto = Img("Punto", letreroRepeticion, spriteCirculo, colorFallo);
            punto.rectTransform.anchorMin = punto.rectTransform.anchorMax = new Vector2(0, 0.5f);
            punto.rectTransform.anchoredPosition = new Vector2(34, 0);
            punto.rectTransform.sizeDelta = new Vector2(22, 22);
            var txt = Texto("Texto", letreroRepeticion, "REPETICIÓN", fuenteTitulos, 48, Color.white, TextAlignmentOptions.Left);
            txt.rectTransform.anchorMin = Vector2.zero; txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(62, 0); txt.rectTransform.offsetMax = new Vector2(-10, 0);
            txt.characterSpacing = 6;
        }
        if (animRepeticion != null) StopCoroutine(animRepeticion);
        if (isActiveAndEnabled) animRepeticion = StartCoroutine(AnimarRepeticion(visible));
    }

    IEnumerator AnimarRepeticion(bool visible)
    {
        if (visible)
        {
            letreroRepeticion.gameObject.SetActive(true);
            franjaArriba.gameObject.SetActive(true);
            franjaAbajo.gameObject.SetActive(true);
            if (grupoResultado != null) grupoResultado.alpha = 0f;
        }
        float desde = franjaArriba.sizeDelta.y, hasta = visible ? 110f : 0f;
        var punto = letreroRepeticion.Find("Punto");
        for (float t = 0f; t < 0.35f || visible; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / 0.35f);
            float h = Mathf.Lerp(desde, hasta, SalidaSuave(u));
            franjaArriba.sizeDelta = new Vector2(0, h);
            franjaAbajo.sizeDelta = new Vector2(0, h);
            letreroRepeticion.localScale = Vector3.one * (visible ? 1f : 1f - u);
            if (punto != null) punto.gameObject.SetActive(Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f);
            yield return null;
        }
        if (!visible)
        {
            letreroRepeticion.gameObject.SetActive(false);
            franjaArriba.gameObject.SetActive(false);
            franjaAbajo.gameObject.SetActive(false);
            letreroRepeticion.localScale = Vector3.one;
        }
        animRepeticion = null;
    }

    /// <summary>Agrega los puntos ganados al subtitulo del resultado (ej. "+180 · ESQUINA · CON ROSCA").</summary>
    public void MostrarPuntosGanados(int ganados, string detalle)
    {
        if (resultadoSubtitulo == null) return;
        resultadoSubtitulo.text = string.IsNullOrEmpty(detalle)
            ? $"<color=#FFC531>+{ganados}</color>"
            : $"<color=#FFC531>+{ganados}</color>   {detalle}";
    }

    void AsegurarInsigniaRacha()
    {
        if (insigniaRacha != null || goalsLabel == null) return;
        var marcador = goalsLabel.transform.parent as RectTransform;
        if (marcador == null) return;
        var existente = marcador.parent != null ? marcador.parent.Find("Racha") : null;
        if (existente != null)
        {
            insigniaRacha = existente as RectTransform;
            textoRacha = existente.GetComponentInChildren<TextMeshProUGUI>(true);
            return;
        }
        insigniaRacha = Panel("Racha", marcador.parent, new Vector2(0, 1), new Vector2(0, 1),
                              marcador.anchoredPosition + new Vector2(0, -marcador.sizeDelta.y - 10f), new Vector2(460, 48),
                              new Color(colorPanel.r, colorPanel.g, colorPanel.b, 0.9f));
        var franja = Img("Franja", insigniaRacha, null, colorFallo);
        franja.rectTransform.anchorMin = new Vector2(0, 0); franja.rectTransform.anchorMax = new Vector2(0, 1);
        franja.rectTransform.pivot = new Vector2(0, 0.5f);
        franja.rectTransform.sizeDelta = new Vector2(6, -16);
        franja.rectTransform.anchoredPosition = new Vector2(14, 0);
        textoRacha = Texto("Texto Racha", insigniaRacha, "RACHA x2", fuenteTexto, 24, colorAcento, TextAlignmentOptions.Left);
        textoRacha.rectTransform.anchorMin = Vector2.zero; textoRacha.rectTransform.anchorMax = Vector2.one;
        textoRacha.rectTransform.offsetMin = new Vector2(32, 0); textoRacha.rectTransform.offsetMax = new Vector2(-12, 0);
        textoRacha.characterSpacing = 4;
        insigniaRacha.gameObject.SetActive(false);
    }

    IEnumerator Latido(RectTransform rt)
    {
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            float u = t / 0.5f;
            rt.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(u * Mathf.PI) * (1f - u));
            yield return null;
        }
        rt.localScale = Vector3.one;
        animRacha = null;
    }

    public void ShowNewAttemptFeedback(int attemptNumber, int maxAttempts)
    {
        bool ultimo = attemptNumber >= maxAttempts;
        if (bannerSubtitulo != null)
        {
            if (rachaActual >= 4) bannerSubtitulo.text = "¡EL ARQUERO ESTÁ ENCENDIDO!";
            else if (rachaActual >= 2) bannerSubtitulo.text = $"RACHA DE {rachaActual} · EL ARQUERO SE PONE SERIO";
            else bannerSubtitulo.text = ultimo ? "ÚLTIMA OPORTUNIDAD" : "SIGUIENTE TIRO";
        }
        if (bannerTitulo != null) bannerTitulo.text = ultimo ? "ÚLTIMO TIRO" : $"INTENTO {attemptNumber} <size=55%><color=#FFFFFF99>DE {maxAttempts}</color></size>";
        if (grupoBanner != null)
        {
            if (animBanner != null) StopCoroutine(animBanner);
            animBanner = StartCoroutine(AnimarBanner());
        }
        CambiarPista(HintIdle);
    }

    /// <summary>Muestra el efecto elegido: nivel de -2 (mucha curva a la izquierda) a +2.</summary>
    public void UpdateEfecto(int nivel)
    {
        nivel = Mathf.Clamp(nivel, -2, 2);
        if (puntosEfecto != null)
            for (int i = 0; i < puntosEfecto.Length; i++)
            {
                if (puntosEfecto[i] == null) continue;
                bool sel = i == nivel + 2;
                puntosEfecto[i].color = sel ? colorAcento : new Color(1f, 1f, 1f, i == 2 ? 0.35f : 0.18f);
                puntosEfecto[i].rectTransform.localScale = Vector3.one * (sel ? 1.25f : 1f);
            }
        if (textoEfecto != null) textoEfecto.text = NombresEfecto[nivel + 2];
    }

    /// <summary>Muestra el resultado grande en el centro y lo anota en el marcador.</summary>
    public void MostrarResultado(Resultado r)
    {
        historial.Add(r);
        PintarPuntos();

        string titulo; string[] frases; Color color;
        switch (r)
        {
            case Resultado.Gol:     titulo = "¡GOOOL!";   frases = FrasesGol;     color = colorAcento;  break;
            case Resultado.Atajada: titulo = "¡ATAJADA!"; frases = FrasesAtajada; color = colorAtajada; break;
            case Resultado.Palo:    titulo = "¡AL PALO!"; frases = FrasesPalo;    color = colorFallo;   break;
            case Resultado.Afuera:  titulo = "¡AFUERA!";  frases = FrasesAfuera;  color = colorFallo;   break;
            case Resultado.Barrera: titulo = "¡EN LA BARRERA!"; frases = FrasesBarrera; color = colorFallo; break;
            default:                titulo = "FALLASTE";  frases = FrasesAfuera;  color = colorFallo;   break;
        }

        if (resultadoTitulo == null || grupoResultado == null) return;
        resultadoTitulo.text = titulo;
        resultadoTitulo.color = color;
        if (resultadoSombra != null) resultadoSombra.text = titulo;
        if (resultadoSubtitulo != null) resultadoSubtitulo.text = frases[Random.Range(0, frases.Length)].ToUpper();
        if (franjaResultado != null)
        {
            // la franja queda oscura y sus bordes toman el color del resultado
            foreach (var borde in franjaResultado.GetComponentsInChildren<Image>())
                if (borde.transform != franjaResultado) borde.color = new Color(color.r, color.g, color.b, 0.8f);
        }

        if (animResultado != null) StopCoroutine(animResultado);
        animResultado = StartCoroutine(AnimarResultado(r == Resultado.Gol));
    }

    // ── Marcador ──────────────────────────────────────────────────────

    void PintarPuntos()
    {
        if (puntosIntento == null) return;
        for (int i = 0; i < puntosIntento.Length; i++)
        {
            var p = puntosIntento[i];
            var a = anillosIntento != null && i < anillosIntento.Length ? anillosIntento[i] : null;
            if (p == null) continue;
            bool visible = i < intentosMax;
            p.gameObject.SetActive(visible);
            if (!visible) continue;

            if (i < historial.Count)
            {
                p.color = historial[i] == Resultado.Gol ? colorGol : colorFallo;
                p.rectTransform.localScale = Vector3.one;
                if (a != null) a.enabled = false;
            }
            else if (i == intentoActual - 1)
            {
                p.color = new Color(1f, 1f, 1f, 0.9f);
                p.rectTransform.localScale = Vector3.one * 0.55f;
                if (a != null) { a.enabled = true; a.color = Color.white; }
            }
            else
            {
                p.color = new Color(1f, 1f, 1f, 0.16f);
                p.rectTransform.localScale = Vector3.one;
                if (a != null) a.enabled = false;
            }
        }
    }

    // ── Pista ─────────────────────────────────────────────────────────

    void CambiarPista(string texto)
    {
        if (hintLabel == null) return;
        hintLabel.text = texto;
        hintLabel.gameObject.SetActive(true);
        MostrarPista(true);
    }

    void MostrarPista(bool visible)
    {
        if (grupoPista == null) { if (hintLabel != null) hintLabel.gameObject.SetActive(visible); return; }
        if (animPista != null) StopCoroutine(animPista);
        if (!isActiveAndEnabled) { grupoPista.alpha = visible ? 1f : 0f; return; }
        animPista = StartCoroutine(Fundir(grupoPista, visible ? 1f : 0f, 0.2f));
    }

    // ── Animaciones ───────────────────────────────────────────────────

    static float SalidaSuave(float t) => 1f - Mathf.Pow(1f - t, 3f);
    static float Rebote(float t) { const float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f); }

    IEnumerator Fundir(CanvasGroup g, float hasta, float dur)
    {
        float desde = g.alpha;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            g.alpha = Mathf.Lerp(desde, hasta, t / dur);
            yield return null;
        }
        g.alpha = hasta;
    }

    IEnumerator EntrarPanel()
    {
        var rt = panel.GetComponent<RectTransform>();
        var g = panel.GetComponent<CanvasGroup>();
        Vector2 destino = rt.anchoredPosition;
        Vector2 inicio = destino + new Vector2(120f, 0f);
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            float u = Rebote(Mathf.Clamp01(t / 0.3f));
            rt.anchoredPosition = Vector2.LerpUnclamped(inicio, destino, u);
            if (g != null) g.alpha = Mathf.Clamp01(t / 0.18f);
            yield return null;
        }
        rt.anchoredPosition = destino;
        if (g != null) g.alpha = 1f;
        if (brilloSeleccion != null && seleccion < cellImages.Length && cellImages[seleccion] != null)
            brilloSeleccion.position = cellImages[seleccion].rectTransform.position;
        animPanel = null;
    }

    IEnumerator AnimarBanner()
    {
        var rt = grupoBanner.GetComponent<RectTransform>();
        Vector2 destino = rt.anchoredPosition;
        Vector2 arriba = destino + new Vector2(0f, 80f);
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float u = SalidaSuave(t / 0.35f);
            rt.anchoredPosition = Vector2.Lerp(arriba, destino, u);
            grupoBanner.alpha = u;
            yield return null;
        }
        rt.anchoredPosition = destino;
        grupoBanner.alpha = 1f;
        yield return new WaitForSecondsRealtime(feedbackDuration);
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            grupoBanner.alpha = 1f - t / 0.3f;
            rt.anchoredPosition = Vector2.Lerp(destino, arriba, SalidaSuave(t / 0.3f) * 0.5f);
            yield return null;
        }
        grupoBanner.alpha = 0f;
        rt.anchoredPosition = destino;
        animBanner = null;
    }

    IEnumerator AnimarResultado(bool gol)
    {
        var rt = resultadoTitulo.rectTransform;
        grupoResultado.alpha = 0f;
        if (franjaResultado != null) franjaResultado.localScale = new Vector3(1f, 0f, 1f);
        if (resultadoSubtitulo != null) resultadoSubtitulo.alpha = 0f;

        // entra: el texto cae desde grande con rebote y la franja se abre
        float dur = 0.45f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / dur);
            float escala = Mathf.LerpUnclamped(gol ? 2.4f : 1.8f, 1f, Rebote(u));
            rt.localScale = Vector3.one * escala;
            if (resultadoSombra != null) resultadoSombra.rectTransform.localScale = rt.localScale;
            grupoResultado.alpha = Mathf.Clamp01(t / 0.15f);
            if (franjaResultado != null) franjaResultado.localScale = new Vector3(1f, SalidaSuave(u), 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
        if (resultadoSombra != null) resultadoSombra.rectTransform.localScale = Vector3.one;
        grupoResultado.alpha = 1f;

        // subtitulo
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            if (resultadoSubtitulo != null) resultadoSubtitulo.alpha = t / 0.25f;
            yield return null;
        }
        if (resultadoSubtitulo != null) resultadoSubtitulo.alpha = 1f;

        // en el gol el texto late un par de veces
        float espera = 1.5f;
        for (float t = 0f; t < espera; t += Time.unscaledDeltaTime)
        {
            if (gol) rt.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 14f) * (1f - t / espera));
            yield return null;
        }

        // sale
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float u = t / 0.35f;
            grupoResultado.alpha = 1f - u;
            if (franjaResultado != null) franjaResultado.localScale = new Vector3(1f, 1f - SalidaSuave(u), 1f);
            yield return null;
        }
        grupoResultado.alpha = 0f;
        animResultado = null;
    }

    // ── Nombres ───────────────────────────────────────────────────────

    private string FormatTargetName(string raw) => raw switch
    {
        "UpLeft"    => "Arriba izquierda",
        "UpMiddle"  => "Arriba al centro",
        "UpRight"   => "Arriba derecha",
        "MidLeft"   => "Media altura izquierda",
        "Middle"    => "Al centro",
        "MidRight"  => "Media altura derecha",
        "Dowleft"   => "Abajo izquierda",
        "DowMiddle" => "Abajo al centro",
        "DowRight"  => "Abajo derecha",
        _           => raw
    };

    // ══════════════════════════════════════════════════════════════════
    //  Construccion del HUD (se ejecuta una vez desde el editor)
    // ══════════════════════════════════════════════════════════════════

    [ContextMenu("Construir HUD")]
    public void ConstruirHUD()
    {
        // borra todo lo que habia dentro del HUD (menos lo que no es interfaz)
        var hijos = new List<GameObject>();
        foreach (Transform h in transform) hijos.Add(h.gameObject);
        foreach (var h in hijos)
        {
            if (Application.isPlaying) Destroy(h); else DestroyImmediate(h);
        }

        normalColor = new Color(1f, 1f, 1f, 0.09f);
        selectedColor = new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.95f);

        var raiz = Rect("HUD", transform);
        Estirar(raiz);

        ConstruirMarcador(raiz);
        ConstruirPista(raiz);
        ConstruirPanelSeleccion(raiz);
        ConstruirBanner(raiz);
        ConstruirResultado(raiz);

        feedbackLabel = null;
        feedbackDuration = 2.2f;
        UpdateAttemptDisplay(1, 5, 0);
        if (grupoResultado != null) grupoResultado.alpha = 0f;
        if (grupoBanner != null) grupoBanner.alpha = 0f;
        CambiarPistaSinAnimar(HintIdle);
        if (panel != null) panel.SetActive(false);
    }

    void CambiarPistaSinAnimar(string texto)
    {
        if (hintLabel != null) hintLabel.text = texto;
        if (grupoPista != null) grupoPista.alpha = 1f;
    }

    void ConstruirMarcador(RectTransform raiz)
    {
        var m = Panel("Marcador", raiz, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -32), new Vector2(640, 118), colorPanel);
        var franja = Img("Franja", m, null, colorAcento);
        franja.rectTransform.anchorMin = new Vector2(0, 0); franja.rectTransform.anchorMax = new Vector2(0, 1);
        franja.rectTransform.pivot = new Vector2(0, 0.5f);
        franja.rectTransform.sizeDelta = new Vector2(8, -36);
        franja.rectTransform.anchoredPosition = new Vector2(16, 0);

        var et1 = Texto("Etiqueta Intento", m, "INTENTO", fuenteTextoSuave, 22, colorTextoSuave, TextAlignmentOptions.TopLeft);
        Ubicar(et1.rectTransform, new Vector2(0, 1), new Vector2(40, -16), new Vector2(160, 30));
        et1.characterSpacing = 12;
        attemptLabel = Texto("Intento", m, "1/5", fuenteTitulos, 68, Color.white, TextAlignmentOptions.TopLeft);
        Ubicar(attemptLabel.rectTransform, new Vector2(0, 1), new Vector2(38, -38), new Vector2(170, 76));

        // separadores
        for (int s = 0; s < 2; s++)
        {
            var sep = Img("Separador", m, null, new Color(1, 1, 1, 0.12f));
            Ubicar(sep.rectTransform, new Vector2(0, 0.5f), new Vector2(s == 0 ? 190 : 470, 0), new Vector2(2, 70));
        }

        // puntos de los 5 intentos
        puntosIntento = new Image[5];
        anillosIntento = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            Vector2 c = new Vector2(230 + i * 50, 0);
            var anillo = Img("Anillo " + (i + 1), m, spriteAnillo, Color.white);
            Ubicar(anillo.rectTransform, new Vector2(0, 0.5f), c, new Vector2(36, 36));
            anillo.enabled = false;
            anillosIntento[i] = anillo;
            var punto = Img("Intento " + (i + 1), m, spriteCirculo, new Color(1, 1, 1, 0.16f));
            Ubicar(punto.rectTransform, new Vector2(0, 0.5f), c, new Vector2(28, 28));
            puntosIntento[i] = punto;
        }

        var et2 = Texto("Etiqueta Goles", m, "PUNTOS", fuenteTextoSuave, 22, colorTextoSuave, TextAlignmentOptions.TopRight);
        Ubicar(et2.rectTransform, new Vector2(1, 1), new Vector2(-28, -16), new Vector2(140, 30), new Vector2(1, 1));
        et2.characterSpacing = 12;
        goalsLabel = Texto("Goles", m, "0", fuenteTitulos, 76, colorAcento, TextAlignmentOptions.TopRight);
        Ubicar(goalsLabel.rectTransform, new Vector2(1, 1), new Vector2(-26, -34), new Vector2(140, 80), new Vector2(1, 1));
    }

    void ConstruirPista(RectTransform raiz)
    {
        var p = Panel("Pista", raiz, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1200, 76), colorPanel);
        grupoPista = p.gameObject.AddComponent<CanvasGroup>();
        grupoPista.blocksRaycasts = false;
        hintLabel = Texto("Texto Pista", p, HintIdle, fuenteTextoSuave, 32, Color.white, TextAlignmentOptions.Center);
        Estirar(hintLabel.rectTransform);
        hintLabel.characterSpacing = 2;
    }

    void ConstruirPanelSeleccion(RectTransform raiz)
    {
        var p = Panel("Panel Seleccion", raiz, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 20), new Vector2(470, 600), colorPanel);
        p.gameObject.AddComponent<CanvasGroup>();
        panel = p.gameObject;

        var titulo = Texto("Titulo", p, "¿DÓNDE DISPARAS?", fuenteTitulos, 54, Color.white, TextAlignmentOptions.Center);
        Ubicar(titulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(430, 64), new Vector2(0.5f, 1));
        titulo.characterSpacing = 4;
        var linea = Img("Linea", p, null, colorAcento);
        Ubicar(linea.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -92), new Vector2(64, 4), new Vector2(0.5f, 1));

        // mini arco: palos, travesaño y una red de fondo
        var arco = Rect("Arco", p);
        Ubicar(arco, new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(392, 262), new Vector2(0.5f, 1));
        var red = Img("Red", arco, null, new Color(1, 1, 1, 0.04f));
        Estirar(red.rectTransform);
        var palo1 = Img("Palo Izquierdo", arco, null, Color.white);
        Ubicar(palo1.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, 262), new Vector2(0, 0));
        var palo2 = Img("Palo Derecho", arco, null, Color.white);
        Ubicar(palo2.rectTransform, new Vector2(1, 0), new Vector2(0, 0), new Vector2(8, 262), new Vector2(1, 0));
        var trav = Img("Travesaño", arco, null, Color.white);
        Ubicar(trav.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(392, 8), new Vector2(0.5f, 1));
        var piso = Img("Piso", p, null, new Color(1, 1, 1, 0.25f));
        Ubicar(piso.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -384), new Vector2(430, 2), new Vector2(0.5f, 1));

        var brillo = Img("Brillo Seleccion", arco, spriteBrillo, new Color(colorAcento.r, colorAcento.g, colorAcento.b, 0.55f));
        brillo.rectTransform.sizeDelta = new Vector2(200, 150);
        brilloSeleccion = brillo.rectTransform;

        cellImages = new Image[9];
        float anchoUtil = 392 - 16 - 2 * 8, altoUtil = 262 - 8 - 8 - 2 * 8;
        float w = anchoUtil / 3f, h = altoUtil / 3f;
        for (int fila = 0; fila < 3; fila++)
            for (int col = 0; col < 3; col++)
            {
                int i = fila * 3 + col;
                var celda = Img("Celda " + (i + 1), arco, spritePanel, normalColor);
                celda.type = Image.Type.Sliced;
                celda.pixelsPerUnitMultiplier = 2.2f;
                var rt = celda.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(w - 2, h - 2);
                rt.anchoredPosition = new Vector2(8 + 8 + w * col + w / 2f, -(8 + 8 + h * fila + h / 2f));
                cellImages[i] = celda;
            }

        selectionLabel = Texto("Destino", p, "AL CENTRO", fuenteTexto, 34, colorAcento, TextAlignmentOptions.Center);
        Ubicar(selectionLabel.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 138), new Vector2(430, 44), new Vector2(0.5f, 0));
        selectionLabel.characterSpacing = 6;

        // efecto: Q  o o o o o  E
        var sep = Img("Separador Efecto", p, null, new Color(1, 1, 1, 0.12f));
        Ubicar(sep.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 126), new Vector2(380, 2), new Vector2(0.5f, 0));
        var etEf = Texto("Etiqueta Efecto", p, "EFECTO", fuenteTextoSuave, 20, colorTextoSuave, TextAlignmentOptions.Center);
        Ubicar(etEf.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(300, 26), new Vector2(0.5f, 0));
        etEf.characterSpacing = 12;
        var q = Texto("Tecla Q", p, "Q", fuenteTitulos, 40, colorAcento, TextAlignmentOptions.Center);
        Ubicar(q.rectTransform, new Vector2(0.5f, 0), new Vector2(-160, 46), new Vector2(50, 46), new Vector2(0.5f, 0));
        var e = Texto("Tecla E", p, "E", fuenteTitulos, 40, colorAcento, TextAlignmentOptions.Center);
        Ubicar(e.rectTransform, new Vector2(0.5f, 0), new Vector2(160, 46), new Vector2(50, 46), new Vector2(0.5f, 0));
        puntosEfecto = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            var d = Img("Efecto " + (i - 2), p, spriteCirculo, Color.white);
            Ubicar(d.rectTransform, new Vector2(0.5f, 0), new Vector2(-100 + i * 50, 69), new Vector2(i == 2 ? 16 : 22, i == 2 ? 16 : 22));
            puntosEfecto[i] = d;
        }
        textoEfecto = Texto("Texto Efecto", p, "SIN EFECTO", fuenteTextoSuave, 20, Color.white, TextAlignmentOptions.Center);
        Ubicar(textoEfecto.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(430, 26), new Vector2(0.5f, 0));
        textoEfecto.characterSpacing = 6;
        UpdateEfecto(0);

        UpdateSelection(4, "Middle");
    }

    void ConstruirBanner(RectTransform raiz)
    {
        var b = Rect("Banner Intento", raiz);
        Ubicar(b, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(900, 170), new Vector2(0.5f, 1));
        grupoBanner = b.gameObject.AddComponent<CanvasGroup>();
        grupoBanner.blocksRaycasts = false;
        // fondo oscuro para que se lea encima de la red blanca
        var fondo = Panel("Fondo", b, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(760, 190), colorPanel);
        var acento = Img("Acento", fondo, null, colorAcento);
        Ubicar(acento.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(120, 5), new Vector2(0.5f, 0));
        bannerSubtitulo = Texto("Subtitulo", b, "SIGUIENTE TIRO", fuenteTexto, 30, colorAcento, TextAlignmentOptions.Center);
        Ubicar(bannerSubtitulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -14), new Vector2(900, 40), new Vector2(0.5f, 1));
        bannerSubtitulo.characterSpacing = 18;
        bannerTitulo = Texto("Titulo", b, "INTENTO 2", fuenteTitulos, 130, Color.white, TextAlignmentOptions.Center);
        Ubicar(bannerTitulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(900, 130), new Vector2(0.5f, 1));
        bannerTitulo.characterSpacing = 6;
    }

    void ConstruirResultado(RectTransform raiz)
    {
        var r = Rect("Resultado", raiz);
        Estirar(r);
        grupoResultado = r.gameObject.AddComponent<CanvasGroup>();
        grupoResultado.blocksRaycasts = false;

        var franja = Img("Franja", r, null, new Color(0.03f, 0.05f, 0.12f, 0.8f));
        franja.rectTransform.anchorMin = new Vector2(0, 0.5f);
        franja.rectTransform.anchorMax = new Vector2(1, 0.5f);
        franja.rectTransform.sizeDelta = new Vector2(0, 300);
        franja.rectTransform.anchoredPosition = new Vector2(0, 30);
        franjaResultado = franja.rectTransform;
        var bordeA = Img("Borde Arriba", franja.rectTransform, null, new Color(1, 1, 1, 0.25f));
        bordeA.rectTransform.anchorMin = new Vector2(0, 1); bordeA.rectTransform.anchorMax = new Vector2(1, 1);
        bordeA.rectTransform.sizeDelta = new Vector2(0, 3); bordeA.rectTransform.anchoredPosition = Vector2.zero;
        var bordeB = Img("Borde Abajo", franja.rectTransform, null, new Color(1, 1, 1, 0.25f));
        bordeB.rectTransform.anchorMin = new Vector2(0, 0); bordeB.rectTransform.anchorMax = new Vector2(1, 0);
        bordeB.rectTransform.sizeDelta = new Vector2(0, 3); bordeB.rectTransform.anchoredPosition = Vector2.zero;

        resultadoSombra = Texto("Sombra", r, "¡GOOOL!", fuenteTitulos, 250, new Color(0, 0, 0, 0.45f), TextAlignmentOptions.Center);
        Ubicar(resultadoSombra.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(8, 52), new Vector2(1600, 260));
        resultadoSombra.characterSpacing = 8;
        resultadoTitulo = Texto("Titulo", r, "¡GOOOL!", fuenteTitulos, 250, colorAcento, TextAlignmentOptions.Center);
        Ubicar(resultadoTitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1600, 260));
        resultadoTitulo.characterSpacing = 8;
        resultadoSubtitulo = Texto("Subtitulo", r, "¡IMPARABLE!", fuenteTexto, 42, Color.white, TextAlignmentOptions.Center);
        Ubicar(resultadoSubtitulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -88), new Vector2(1400, 56));
        resultadoSubtitulo.characterSpacing = 14;
    }

    // ── Ayudantes de construccion ─────────────────────────────────────

    static RectTransform Rect(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = padre.gameObject.layer;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        return rt;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static void Ubicar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam, Vector2? pivote = null)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote ?? new Vector2(0.5f, 0.5f);
        if (pivote == null && (ancla.x == 0 || ancla.x == 1) && ancla.y != 0.5f) rt.pivot = ancla;
        if (pivote == null && ancla == new Vector2(0, 0.5f)) rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    Image Img(string nombre, Transform padre, Sprite sprite, Color color)
    {
        var rt = Rect(nombre, padre);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    RectTransform Panel(string nombre, Transform padre, Vector2 ancla, Vector2 pivote, Vector2 pos, Vector2 tam, Color color)
    {
        var img = Img(nombre, padre, spritePanel, color);
        img.type = Image.Type.Sliced;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        // borde fino
        var borde = Img("Borde", rt, spriteBorde, new Color(1, 1, 1, 0.10f));
        borde.type = Image.Type.Sliced;
        Estirar(borde.rectTransform);
        return rt;
    }

    TextMeshProUGUI Texto(string nombre, Transform padre, string texto, TMP_FontAsset fuente, float tam, Color color, TextAlignmentOptions alineacion)
    {
        var rt = Rect(nombre, padre);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.alignment = alineacion;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.richText = true;
        return t;
    }
}
