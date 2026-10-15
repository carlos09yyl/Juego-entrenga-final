using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu de inicio: logo, botones (Jugar, Ranking, Creditos, Salir), mejor puntaje y panel de creditos.
/// Se arma con clic derecho en el componente > "Construir menu" y queda guardado en la escena.
/// Los textos de los creditos se editan en el Inspector (formato: "ROL|Nombre").
/// </summary>
public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Escenas")]
    public string escenaJugar = "SeleccionPais";
    public string escenaRanking = "Ranking";

    [Header("Estilo")]
    public TMP_FontAsset fuenteTitulos, fuenteTexto, fuenteTextoSuave;
    public Sprite spritePanel, spriteBorde, spriteCirculo, spriteBrillo, spriteDegradado;

    [Header("Creditos (ROL|Nombre)")]
    public string[] creditos =
    {
        "DISEÑO Y DESARROLLO|Carlos Adel Beleño Durán",
        "ASESORÍA · DISEÑO DE VIDEOJUEGOS 3|Profesora (nombre por confirmar)",
        "VERSIÓN ORIGINAL · TRABAJO DE CLASE|Carlos Andrés Junieles",
        "ESTADIO|Football Mini Stadium · SkillMasters (Unity Asset Store)",
        "PERSONAJES|Mini Simple Characters (Unity Asset Store)",
        "ANIMACIONES|Mixamo · Adobe",
        "BALONES|Sport Balls · Saritasa (Unity Asset Store)",
        "TIPOGRAFÍAS|Bebas Neue y Rajdhani (SIL Open Font License)",
        "BANDERAS|flag-icons (licencia MIT)",
    };

    [Header("Referencias (las llena Construir menu)")]
    public RectTransform logo;
    public RectTransform[] botones;
    public GameObject panelCreditos;
    public TextMeshProUGUI textoMejorPuntaje;
    [Tooltip("Tarjeta de abajo a la izquierda con tu record; al tocarla abre el ranking.")]
    public Button tarjetaRecord;
    public TextMeshProUGUI etiquetaRecord, pieRecord;
    public RawImage banderaRecord;

    [Header("Fondo")]
    public Texture fondo;
    public RectTransform capaFondo;
    public Button botonJugar, botonVolverCreditos;

    void Start()
    {
        UIKit.Usar(fuenteTitulos, fuenteTexto, fuenteTextoSuave, spritePanel, spriteBorde, spriteCirculo, spriteBrillo);
        if (panelCreditos != null) panelCreditos.SetActive(false);
        MostrarRecord();
        Conectar();
        StartCoroutine(Entrada());
    }

    /// <summary>Tu record guardado, con la bandera de tu seleccion y el puesto en el ranking.</summary>
    void MostrarRecord()
    {
        int mejor = ScoreData.MejorPuntaje;
        var pais = Paises.Actual;
        if (banderaRecord != null) banderaRecord.texture = Paises.Bandera(pais.codigo);
        if (mejor <= 0)
        {
            if (etiquetaRecord != null) etiquetaRecord.text = "TU RÉCORD";
            if (textoMejorPuntaje != null) textoMejorPuntaje.text = "¡HAZ EL PRIMERO!";
            if (pieRecord != null) pieRecord.text = "Juega un partido para entrar al ranking";
            return;
        }
        if (etiquetaRecord != null) etiquetaRecord.text = "TU RÉCORD  ·  " + pais.nombre.ToUpper();
        if (pieRecord != null) pieRecord.text = "Toca aquí para ver el <color=#FFC531>ranking</color>";
        if (textoMejorPuntaje != null) StartCoroutine(Contar(textoMejorPuntaje, mejor));
    }

    IEnumerator Contar(TextMeshProUGUI t, int valor)
    {
        t.text = "0";
        yield return new WaitForSecondsRealtime(0.6f);
        for (float x = 0f; x < 0.9f; x += Time.unscaledDeltaTime)
        {
            t.text = Mathf.RoundToInt(valor * (1f - Mathf.Pow(1f - x / 0.9f, 3f))).ToString("N0");
            yield return null;
        }
        t.text = valor.ToString("N0");
    }

    void Conectar()
    {
        if (botones == null || botones.Length < 5) return;
        Accion(botones[0], () => SceneManager.LoadScene(escenaJugar));
        Accion(botones[1], () => SceneManager.LoadScene(escenaRanking));
        Accion(botones[2], AbrirOpciones);
        Accion(botones[3], () => MostrarCreditos(true));
        Accion(botones[4], Salir);
        if (tarjetaRecord != null) { tarjetaRecord.onClick.RemoveAllListeners(); tarjetaRecord.onClick.AddListener(() => SceneManager.LoadScene(escenaRanking)); }
        if (botonVolverCreditos != null) { botonVolverCreditos.onClick.RemoveAllListeners(); botonVolverCreditos.onClick.AddListener(() => MostrarCreditos(false)); }
    }

    static void Accion(RectTransform rt, UnityEngine.Events.UnityAction a)
    {
        var b = rt != null ? rt.GetComponent<Button>() : null;
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(a);
    }

    void Update()
    {
        if (capaFondo != null)
        {
            float t = Time.unscaledTime;
            capaFondo.localScale = Vector3.one * (1.06f + 0.04f * Mathf.Sin(t * 0.12f));
            capaFondo.anchoredPosition = new Vector2(Mathf.Sin(t * 0.07f) * 18f, Mathf.Cos(t * 0.09f) * 8f);
        }
        bool atras = Input.GetKeyDown(KeyCode.Escape);
        if (atras && panelCreditos != null && panelCreditos.activeSelf) MostrarCreditos(false);

        // si nada esta seleccionado y se usa el teclado, selecciona el primer boton
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null &&
            (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S)))
        {
            var primero = panelCreditos != null && panelCreditos.activeSelf ? botonVolverCreditos?.gameObject : botones[0].gameObject;
            EventSystem.current.SetSelectedGameObject(primero);
        }
    }

    public void MostrarCreditos(bool visible)
    {
        if (panelCreditos == null) return;
        panelCreditos.SetActive(visible);
        if (visible) StartCoroutine(Aparecer(panelCreditos.GetComponent<CanvasGroup>(), 0.25f));
        EventSystem.current?.SetSelectedGameObject(visible ? botonVolverCreditos?.gameObject : botones[3].gameObject);
    }

    void AbrirOpciones()
    {
        PanelOpciones.Crear(transform, () => EventSystem.current?.SetSelectedGameObject(botones[2].gameObject));
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── animaciones ───────────────────────────────────────────────────

    static float Rebote(float t) { const float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f); }

    IEnumerator Aparecer(CanvasGroup g, float dur)
    {
        if (g == null) yield break;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime) { g.alpha = t / dur; yield return null; }
        g.alpha = 1f;
    }

    IEnumerator Entrada()
    {
        var items = new List<RectTransform>();
        if (logo != null) items.Add(logo);
        if (botones != null) items.AddRange(botones);
        var destinos = new Vector2[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            destinos[i] = items[i].anchoredPosition;
            var g = items[i].GetComponent<CanvasGroup>();
            if (g == null) g = items[i].gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
        }
        for (int i = 0; i < items.Count; i++)
            StartCoroutine(EntrarUno(items[i], destinos[i], i == 0 ? new Vector2(0, 60) : new Vector2(0, -40), 0.12f * i));
        yield return new WaitForSecondsRealtime(0.12f * items.Count + 0.4f);
        if (EventSystem.current != null && botones != null && botones.Length > 0)
            EventSystem.current.SetSelectedGameObject(botones[0].gameObject);
    }

    IEnumerator EntrarUno(RectTransform rt, Vector2 destino, Vector2 desde, float retraso)
    {
        var g = rt.GetComponent<CanvasGroup>();
        yield return new WaitForSecondsRealtime(retraso);
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            float u = Mathf.Clamp01(t / 0.5f);
            rt.anchoredPosition = Vector2.LerpUnclamped(destino + desde, destino, Rebote(u));
            g.alpha = Mathf.Clamp01(u * 2f);
            yield return null;
        }
        rt.anchoredPosition = destino;
        g.alpha = 1f;
    }

    // ══════════════════════════════════════════════════════════════════
    //  Construccion (una vez, desde el editor)
    // ══════════════════════════════════════════════════════════════════

    [ContextMenu("Construir menu")]
    public void ConstruirMenu()
    {
        UIKit.Usar(fuenteTitulos, fuenteTexto, fuenteTextoSuave, spritePanel, spriteBorde, spriteCirculo, spriteBrillo);
        var hijos = new List<GameObject>();
        foreach (Transform h in transform) hijos.Add(h.gameObject);
        foreach (var h in hijos) DestroyImmediate(h);

        var raiz = UIKit.Rect("Menu", transform);
        UIKit.Estirar(raiz);

        // foto del estadio de fondo, con un zoom lento
        capaFondo = null;
        if (fondo != null)
        {
            var marco = UIKit.Rect("Fondo", raiz);
            UIKit.Estirar(marco);
            marco.gameObject.AddComponent<RectMask2D>();
            var foto = UIKit.Rect("Foto", marco).gameObject.AddComponent<RawImage>();
            foto.texture = fondo;
            foto.raycastTarget = false;
            foto.color = new Color(0.78f, 0.82f, 0.95f, 1f);
            var ajuste = foto.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ajuste.aspectRatio = fondo.width / (float)fondo.height;
            capaFondo = foto.rectTransform;
            var tinte = UIKit.Img("Tinte", marco, null, new Color(0.03f, 0.06f, 0.16f, 0.35f));
            UIKit.Estirar(tinte.rectTransform);
        }

        // oscurecer abajo y arriba para que se lea sobre la ilustracion
        var sombraAbajo = UIKit.Img("Sombra Abajo", raiz, spriteDegradado, new Color(0.02f, 0.04f, 0.1f, 0.92f));
        sombraAbajo.rectTransform.anchorMin = new Vector2(0, 0); sombraAbajo.rectTransform.anchorMax = new Vector2(1, 0.62f);
        sombraAbajo.rectTransform.offsetMin = Vector2.zero; sombraAbajo.rectTransform.offsetMax = Vector2.zero;
        var sombraArriba = UIKit.Img("Sombra Arriba", raiz, spriteDegradado, new Color(0.02f, 0.04f, 0.1f, 0.7f));
        sombraArriba.rectTransform.anchorMin = new Vector2(0, 0.7f); sombraArriba.rectTransform.anchorMax = new Vector2(1, 1);
        sombraArriba.rectTransform.offsetMin = Vector2.zero; sombraArriba.rectTransform.offsetMax = Vector2.zero;
        sombraArriba.rectTransform.localScale = new Vector3(1, -1, 1);      // volteado: opaco arriba

        // logo
        logo = UIKit.Rect("Logo", raiz);
        UIKit.Ubicar(logo, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1200, 330));
        var brillo = UIKit.Img("Brillo", logo, spriteBrillo, new Color(1f, 0.77f, 0.19f, 0.18f));
        UIKit.Ubicar(brillo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1100, 420));
        var cinta = UIKit.Txt("Cinta", logo, "FÚTBOL  ·  TIRO LIBRE", fuenteTexto, 34, UIKit.Acento, TextAlignmentOptions.Center, 22f);
        UIKit.Ubicar(cinta.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(1200, 44));
        var sombraTitulo = UIKit.Txt("Sombra Titulo", logo, "THE FINAL <color=#00000000>GOAL</color>", fuenteTitulos, 200, new Color(0, 0, 0, 0.45f), TextAlignmentOptions.Center, 6f);
        UIKit.Ubicar(sombraTitulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(8, -58), new Vector2(1200, 220));
        var titulo = UIKit.Txt("Titulo", logo, "THE FINAL <color=#FFC531>GOAL</color>", fuenteTitulos, 200, Color.white, TextAlignmentOptions.Center, 6f);
        UIKit.Ubicar(titulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(1200, 220));
        var linea = UIKit.Img("Linea", logo, null, UIKit.Acento);
        UIKit.Ubicar(linea.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -282), new Vector2(120, 6));

        // botones
        string[] nombres = { "JUGAR", "RANKING", "OPCIONES", "CRÉDITOS", "SALIR" };
        botones = new RectTransform[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            var b = UIKit.Boton("Boton " + nombres[i], raiz, nombres[i], i == 0, new Vector2(i == 0 ? 520 : 460, i == 0 ? 96 : 74), i == 0 ? 60 : 46);
            var rt = (RectTransform)b.transform;
            UIKit.Ubicar(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, i == 0 ? 500 : 400 - (i - 1) * 84), rt.sizeDelta);
            botones[i] = rt;
        }
        botonJugar = botones[0].GetComponent<Button>();
        // navegacion vertical explicita
        for (int i = 0; i < botones.Length; i++)
        {
            var b = botones[i].GetComponent<Button>();
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = botones[(i + botones.Length - 1) % botones.Length].GetComponent<Button>();
            nav.selectOnDown = botones[(i + 1) % botones.Length].GetComponent<Button>();
            b.navigation = nav;
        }

        // mejor puntaje y pie
        // tarjeta del record (se llena al iniciar; al tocarla abre el ranking)
        var cajaRecord = UIKit.PanelR("Tu Record", raiz, UIKit.ColorPanel);
        cajaRecord.raycastTarget = true;
        UIKit.Ubicar(cajaRecord.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 36), new Vector2(470, 150));
        var acento = UIKit.Img("Acento", cajaRecord.transform, null, UIKit.Acento);
        UIKit.Ubicar(acento.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(6, 104));
        banderaRecord = UIKit.Rect("Bandera", cajaRecord.transform).gameObject.AddComponent<RawImage>();
        banderaRecord.raycastTarget = false;
        UIKit.Ubicar(banderaRecord.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -22), new Vector2(40, 30));
        etiquetaRecord = UIKit.Txt("Etiqueta", cajaRecord.transform, "TU RÉCORD", fuenteTextoSuave, 22, UIKit.TextoSuaveColor, TextAlignmentOptions.TopLeft, 12f);
        UIKit.Ubicar(etiquetaRecord.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -24), new Vector2(360, 30));
        textoMejorPuntaje = UIKit.Txt("Puntos", cajaRecord.transform, "—", fuenteTitulos, 60, UIKit.Acento, TextAlignmentOptions.TopLeft, 3f);
        UIKit.Ubicar(textoMejorPuntaje.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -56), new Vector2(420, 60));
        textoMejorPuntaje.enableAutoSizing = true; textoMejorPuntaje.fontSizeMin = 34; textoMejorPuntaje.fontSizeMax = 60;
        pieRecord = UIKit.Txt("Pie", cajaRecord.transform, "Juega un partido para entrar al ranking", fuenteTextoSuave, 20, Color.white, TextAlignmentOptions.BottomLeft, 1f);
        UIKit.Ubicar(pieRecord.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(28, 14), new Vector2(430, 26));
        tarjetaRecord = cajaRecord.gameObject.AddComponent<Button>();
        tarjetaRecord.transition = Selectable.Transition.ColorTint;
        tarjetaRecord.targetGraphic = cajaRecord;
        var colores = tarjetaRecord.colors;
        colores.highlightedColor = new Color(1.3f, 1.3f, 1.5f, 1f); colores.selectedColor = colores.highlightedColor;
        tarjetaRecord.colors = colores;
        tarjetaRecord.navigation = new Navigation { mode = Navigation.Mode.None };
        var pie = UIKit.Txt("Pie", raiz, "DISEÑO DIGITAL  ·  2026", fuenteTextoSuave, 22, UIKit.TextoSuaveColor, TextAlignmentOptions.BottomRight, 10f);
        UIKit.Ubicar(pie.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(600, 40));
        var ayuda = UIKit.Txt("Ayuda", raiz, "<color=#FFC531>W / S</color>  ELEGIR      <color=#FFC531>ENTER</color>  ACEPTAR", fuenteTextoSuave, 22, UIKit.TextoSuaveColor, TextAlignmentOptions.Bottom, 6f);
        UIKit.Ubicar(ayuda.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(800, 40));

        ConstruirCreditos(raiz);
    }

    void ConstruirCreditos(RectTransform raiz)
    {
        var velo = UIKit.Img("Creditos", raiz, null, new Color(0.01f, 0.02f, 0.06f, 0.88f));
        velo.raycastTarget = true;
        UIKit.Estirar(velo.rectTransform);
        velo.gameObject.AddComponent<CanvasGroup>();
        panelCreditos = velo.gameObject;

        // el autor arriba y centrado; el resto en dos columnas
        int filas = Mathf.CeilToInt(Mathf.Max(0, creditos.Length - 1) / 2f);
        float alto = 150 + 96 + filas * 82 + 130;
        var caja = UIKit.PanelR("Caja", velo.transform, UIKit.ColorPanel);
        UIKit.Ubicar(caja.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1060, alto));
        var titulo = UIKit.Txt("Titulo", caja.transform, "CRÉDITOS", fuenteTitulos, 96, Color.white, TextAlignmentOptions.Center, 8f);
        UIKit.Ubicar(titulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(900, 100));
        var linea = UIKit.Img("Linea", caja.transform, null, UIKit.Acento);
        UIKit.Ubicar(linea.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -126), new Vector2(90, 5));

        for (int i = 0; i < creditos.Length; i++)
        {
            string[] partes = creditos[i].Split('|');
            bool autor = i == 0;
            int k = i - 1;
            float x = autor ? 0f : (k % 2 == 0 ? -255f : 255f);
            float y = autor ? -150f : -150f - 96f - (k / 2) * 82f;
            float ancho = autor ? 900f : 490f;
            var rol = UIKit.Txt("Rol " + (i + 1), caja.transform, partes[0], fuenteTextoSuave, 20, UIKit.Acento, TextAlignmentOptions.Center, 8f);
            UIKit.Ubicar(rol.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(x, y), new Vector2(ancho, 26));
            var nombre = UIKit.Txt("Nombre " + (i + 1), caja.transform, partes.Length > 1 ? partes[1] : "", autor ? fuenteTitulos : fuenteTexto, autor ? 54 : 30, Color.white, TextAlignmentOptions.Center, autor ? 4f : 1f);
            UIKit.Ubicar(nombre.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(x, y - 26), new Vector2(ancho, autor ? 60 : 40));
            nombre.enableAutoSizing = true; nombre.fontSizeMin = 18; nombre.fontSizeMax = autor ? 54 : 30;
        }

        botonVolverCreditos = UIKit.Boton("Volver", caja.transform, "VOLVER", false, new Vector2(320, 70), 42);
        UIKit.Ubicar((RectTransform)botonVolverCreditos.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(320, 70));
        panelCreditos.SetActive(false);
    }
}
