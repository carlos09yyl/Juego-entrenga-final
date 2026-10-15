using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla para elegir la seleccion: 30 banderas a la izquierda y, a la derecha,
/// el jugador girando con la camiseta y la pantaloneta del pais elegido.
/// Se arma con clic derecho > "Construir pantalla" y queda guardada en la escena.
/// </summary>
public class SeleccionPaisUI : MonoBehaviour
{
    [Header("Escenas")]
    public string escenaJuego = "GameScene";
    public string escenaMenu = "TitleScreen";

    [Header("Estilo")]
    public TMP_FontAsset fuenteTitulos, fuenteTexto, fuenteTextoSuave;
    public Sprite spritePanel, spriteBorde, spriteCirculo, spriteBrillo;

    [Header("Jugador de muestra")]
    public Transform jugador;
    public Material materialUniforme;
    public float velocidadGiro = 35f;

    [Header("Referencias (las llena Construir pantalla)")]
    public Button[] tarjetas;
    public Image[] bordesTarjeta;
    public TextMeshProUGUI nombrePais;
    public RawImage banderaGrande;
    public Image muestraCamiseta, muestraPantaloneta;
    public Button botonJugar, botonVolver;
    public RectTransform encabezado, grilla, ladoDerecho;

    private int elegido;

    void Start()
    {
        UIKit.Usar(fuenteTitulos, fuenteTexto, fuenteTextoSuave, spritePanel, spriteBorde, spriteCirculo, spriteBrillo);
        for (int i = 0; i < tarjetas.Length; i++)
        {
            int k = i;
            tarjetas[i].onClick.RemoveAllListeners();
            tarjetas[i].onClick.AddListener(() => Elegir(k, true));
            var sel = tarjetas[i].GetComponent<TarjetaPais>();
            if (sel != null) { sel.indice = k; sel.pantalla = this; }
            var bandera = tarjetas[i].transform.Find("Bandera")?.GetComponent<RawImage>();
            if (bandera != null) bandera.texture = Paises.Bandera(Paises.Lista[i].codigo);
        }
        botonJugar.onClick.RemoveAllListeners();
        botonJugar.onClick.AddListener(Jugar);
        botonVolver.onClick.RemoveAllListeners();
        botonVolver.onClick.AddListener(() => SceneManager.LoadScene(escenaMenu));

        Elegir(Paises.Elegido, false);
        EventSystem.current?.SetSelectedGameObject(tarjetas[elegido].gameObject);
        StartCoroutine(Entrada());
    }

    void Update()
    {
        if (jugador != null) jugador.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
        if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene(escenaMenu);
        if (Input.GetKeyDown(KeyCode.Space)) Jugar();
    }

    /// <summary>Cambia el pais (al pasar con el teclado o hacer clic). confirmar = clic: tambien se guarda.</summary>
    public void Elegir(int indice, bool confirmar)
    {
        elegido = Mathf.Clamp(indice, 0, Paises.Lista.Length - 1);
        var p = Paises.Lista[elegido];
        if (confirmar) Paises.Elegido = elegido;

        if (nombrePais != null) nombrePais.text = p.nombre.ToUpper();
        if (banderaGrande != null) banderaGrande.texture = Paises.Bandera(p.codigo);
        if (muestraCamiseta != null) muestraCamiseta.color = p.camiseta;
        if (muestraPantaloneta != null) muestraPantaloneta.color = p.pantaloneta;
        for (int i = 0; i < bordesTarjeta.Length; i++)
            if (bordesTarjeta[i] != null) bordesTarjeta[i].color = i == elegido ? UIKit.Acento : new Color(1, 1, 1, 0.08f);
        if (jugador != null) Paises.Vestir(jugador.gameObject, p, materialUniforme);
        if (confirmar && nombrePais != null) StartCoroutine(Latido(nombrePais.rectTransform));
    }

    void Jugar()
    {
        Paises.Elegido = elegido;
        SceneManager.LoadScene(escenaJuego);
    }

    IEnumerator Latido(RectTransform rt)
    {
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float u = t / 0.35f;
            rt.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(u * Mathf.PI));
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator Entrada()
    {
        var piezas = new List<(RectTransform rt, Vector2 desde)>
        {
            (encabezado, new Vector2(0, 60)), (grilla, new Vector2(-80, 0)), (ladoDerecho, new Vector2(80, 0))
        };
        foreach (var (rt, _) in piezas) { var g = rt.GetComponent<CanvasGroup>(); if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>(); g.alpha = 0f; }
        for (int i = 0; i < piezas.Count; i++)
        {
            var (rt, desde) = piezas[i];
            StartCoroutine(Entrar(rt, desde, 0.1f * i));
        }
        yield break;
    }

    IEnumerator Entrar(RectTransform rt, Vector2 desde, float retraso)
    {
        var g = rt.GetComponent<CanvasGroup>();
        Vector2 destino = rt.anchoredPosition;
        yield return new WaitForSecondsRealtime(retraso);
        for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.45f), 3f);
            rt.anchoredPosition = Vector2.Lerp(destino + desde, destino, u);
            g.alpha = u;
            yield return null;
        }
        rt.anchoredPosition = destino;
        g.alpha = 1f;
    }

    // ══════════════════════════════════════════════════════════════════
    //  Construccion (una vez, desde el editor)
    // ══════════════════════════════════════════════════════════════════

    [ContextMenu("Construir pantalla")]
    public void ConstruirPantalla()
    {
        UIKit.Usar(fuenteTitulos, fuenteTexto, fuenteTextoSuave, spritePanel, spriteBorde, spriteCirculo, spriteBrillo);
        var hijos = new List<GameObject>();
        foreach (Transform h in transform) hijos.Add(h.gameObject);
        foreach (var h in hijos) DestroyImmediate(h);

        var raiz = UIKit.Rect("Pantalla", transform);
        UIKit.Estirar(raiz);

        // encabezado
        encabezado = UIKit.Rect("Encabezado", raiz);
        UIKit.Ubicar(encabezado, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -60), new Vector2(1100, 170));
        var cinta = UIKit.Txt("Cinta", encabezado, "MODO TIRO LIBRE", fuenteTexto, 26, UIKit.Acento, TextAlignmentOptions.TopLeft, 18f);
        UIKit.Ubicar(cinta.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(1100, 34));
        var titulo = UIKit.Txt("Titulo", encabezado, "ELIGE TU SELECCIÓN", fuenteTitulos, 112, Color.white, TextAlignmentOptions.TopLeft, 4f);
        UIKit.Ubicar(titulo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -30), new Vector2(1100, 120));
        var sub = UIKit.Txt("Subtitulo", encabezado, "Tu jugador saldrá a la cancha con los colores de tu país", fuenteTextoSuave, 30, UIKit.TextoSuaveColor, TextAlignmentOptions.TopLeft, 2f);
        UIKit.Ubicar(sub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(4, -144), new Vector2(1100, 40));

        // grilla de banderas 6 x 5
        grilla = UIKit.Rect("Banderas", raiz);
        const int columnas = 6;
        const float ancho = 164f, alto = 132f, sep = 14f;
        int filas = Mathf.CeilToInt(Paises.Lista.Length / (float)columnas);
        UIKit.Ubicar(grilla, new Vector2(0, 1), new Vector2(0, 1), new Vector2(90, -262), new Vector2(columnas * (ancho + sep), filas * (alto + sep)));
        tarjetas = new Button[Paises.Lista.Length];
        bordesTarjeta = new Image[Paises.Lista.Length];
        for (int i = 0; i < Paises.Lista.Length; i++)
        {
            var p = Paises.Lista[i];
            var fondo = UIKit.Img("Tarjeta " + p.nombre, grilla, spritePanel, UIKit.ColorPanel, true);
            fondo.raycastTarget = true;
            UIKit.Ubicar(fondo.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f),
                new Vector2((i % columnas) * (ancho + sep) + ancho / 2f, -((i / columnas) * (alto + sep) + alto / 2f)), new Vector2(ancho, alto));
            var b = fondo.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            tarjetas[i] = b;

            var bandera = UIKit.Rect("Bandera", fondo.transform).gameObject.AddComponent<RawImage>();
            bandera.raycastTarget = false;
            bandera.texture = Paises.Bandera(p.codigo);
            UIKit.Ubicar(bandera.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(124, 82));
            var nombre = UIKit.Txt("Nombre", fondo.transform, p.nombre.ToUpper(), fuenteTexto, 19, Color.white, TextAlignmentOptions.Center, 2f);
            UIKit.Ubicar(nombre.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(ancho - 8, 28));
            nombre.enableAutoSizing = true; nombre.fontSizeMin = 13; nombre.fontSizeMax = 19;

            var borde = UIKit.Img("Borde", fondo.transform, spriteBorde, new Color(1, 1, 1, 0.08f), true);
            UIKit.Estirar(borde.rectTransform);
            bordesTarjeta[i] = borde;

            var tarjeta = fondo.gameObject.AddComponent<TarjetaPais>();
            tarjeta.indice = i;
            tarjeta.pantalla = this;
        }

        // lado derecho: nombre del pais, bandera, colores del uniforme y botones
        ladoDerecho = UIKit.Rect("Lado Derecho", raiz);
        UIKit.Ubicar(ladoDerecho, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-80, 60), new Vector2(620, 380));
        var caja = UIKit.PanelR("Caja", ladoDerecho, UIKit.ColorPanel);
        UIKit.Estirar(caja.rectTransform);
        banderaGrande = UIKit.Rect("Bandera", caja.transform).gameObject.AddComponent<RawImage>();
        banderaGrande.raycastTarget = false;
        UIKit.Ubicar(banderaGrande.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -30), new Vector2(96, 64));
        nombrePais = UIKit.Txt("Pais", caja.transform, "COLOMBIA", fuenteTitulos, 76, Color.white, TextAlignmentOptions.Left, 4f);
        UIKit.Ubicar(nombrePais.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(146, -22), new Vector2(450, 84));
        nombrePais.enableAutoSizing = true; nombrePais.fontSizeMin = 40; nombrePais.fontSizeMax = 76;

        var etCam = UIKit.Txt("Etiqueta Camiseta", caja.transform, "CAMISETA", fuenteTextoSuave, 20, UIKit.TextoSuaveColor, TextAlignmentOptions.Left, 8f);
        UIKit.Ubicar(etCam.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(76, -122), new Vector2(200, 30));
        muestraCamiseta = UIKit.Img("Color Camiseta", caja.transform, spriteCirculo, Color.yellow);
        UIKit.Ubicar(muestraCamiseta.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -118), new Vector2(36, 36));
        var etPan = UIKit.Txt("Etiqueta Pantaloneta", caja.transform, "PANTALONETA", fuenteTextoSuave, 20, UIKit.TextoSuaveColor, TextAlignmentOptions.Left, 8f);
        UIKit.Ubicar(etPan.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(356, -122), new Vector2(240, 30));
        muestraPantaloneta = UIKit.Img("Color Pantaloneta", caja.transform, spriteCirculo, Color.blue);
        UIKit.Ubicar(muestraPantaloneta.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(310, -118), new Vector2(36, 36));
        foreach (var m in new[] { muestraCamiseta, muestraPantaloneta })
        {
            var aro = UIKit.Img("Aro " + m.name, caja.transform, spriteCirculo, new Color(1, 1, 1, 0.8f));
            var rm = m.rectTransform;
            UIKit.Ubicar(aro.rectTransform, rm.anchorMin, new Vector2(0.5f, 0.5f), rm.anchoredPosition + new Vector2(18, -18), new Vector2(42, 42));
            aro.transform.SetSiblingIndex(m.transform.GetSiblingIndex());
        }

        botonJugar = UIKit.Boton("Jugar", caja.transform, "¡A JUGAR!", true, new Vector2(560, 82), 54);
        UIKit.Ubicar((RectTransform)botonJugar.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 102), new Vector2(560, 82));
        botonVolver = UIKit.Boton("Volver", caja.transform, "VOLVER", false, new Vector2(560, 62), 38);
        UIKit.Ubicar((RectTransform)botonVolver.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(560, 62));

        var ayuda = UIKit.Txt("Ayuda", raiz, "<color=#FFC531>FLECHAS</color>  ELEGIR PAÍS      <color=#FFC531>ESPACIO</color>  JUGAR      <color=#FFC531>ESC</color>  VOLVER", fuenteTextoSuave, 22, UIKit.TextoSuaveColor, TextAlignmentOptions.BottomLeft, 6f);
        UIKit.Ubicar(ayuda.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(90, 40), new Vector2(1100, 40));
    }
}
