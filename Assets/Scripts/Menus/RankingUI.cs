using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Escena de ranking: 10 puestos con rivales simulados (cambian en cada visita)
/// y el jugador con su mejor puntaje y la bandera de su seleccion.
/// Se arma con clic derecho > "Construir pantalla".
/// </summary>
public class RankingUI : MonoBehaviour
{
    [Header("Escenas")]
    public string escenaJugar = "SeleccionPais";
    public string escenaMenu = "TitleScreen";

    [Header("Estilo")]
    public TMP_FontAsset fuenteTitulos, fuenteTexto, fuenteTextoSuave;
    public Sprite spritePanel, spriteBorde, spriteCirculo, spriteBrillo;
    public Texture fondo;

    [Header("Referencias (las llena Construir pantalla)")]
    public RectTransform capaFondo, encabezado, panel, lista;
    public TextMeshProUGUI textoPuesto;
    public Button botonJugar, botonVolver;

    void Start()
    {
        UIKit.Usar(fuenteTitulos, fuenteTexto, fuenteTextoSuave, spritePanel, spriteBorde, spriteCirculo, spriteBrillo);
        botonJugar.onClick.RemoveAllListeners();
        botonJugar.onClick.AddListener(() => SceneManager.LoadScene(escenaJugar));
        botonVolver.onClick.RemoveAllListeners();
        botonVolver.onClick.AddListener(() => SceneManager.LoadScene(escenaMenu));

        var pais = Paises.Actual;
        int mejor = ScoreData.MejorPuntaje;
        var tabla = Rivales.Tabla(mejor, pais, out int posicion);
        textoPuesto.text = mejor > 0
            ? $"TU MEJOR PUESTO  <color=#FFC531>#{posicion + 1}</color>"
            : "JUEGA UN PARTIDO PARA ENTRAR AL RANKING";
        var filas = VistaRanking.Llenar(lista, tabla, 60f, 8f, pais.nombre.ToUpper());
        StartCoroutine(Entrada(filas, posicion));
    }

    void Update()
    {
        VistaRanking.Respirar(capaFondo);
        if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene(escenaMenu);
    }

    IEnumerator Entrada(List<RectTransform> filas, int posicion)
    {
        StartCoroutine(VistaRanking.Entrar(encabezado, new Vector2(0, 50), 0f, 0.45f));
        StartCoroutine(VistaRanking.Entrar(panel, new Vector2(0, -50), 0.1f, 0.45f));
        for (int i = 0; i < filas.Count; i++)
            StartCoroutine(VistaRanking.Entrar(filas[i], i == posicion ? new Vector2(260, 0) : new Vector2(40, 0), 0.3f + 0.06f * i + (i == posicion ? 0.5f : 0f)));
        yield return new WaitForSecondsRealtime(0.6f);
        EventSystem.current?.SetSelectedGameObject(botonJugar.gameObject);
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
        capaFondo = VistaRanking.Fondo(raiz, fondo, 0.86f);

        encabezado = UIKit.Rect("Encabezado", raiz);
        UIKit.Ubicar(encabezado, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1100, 170));
        var cinta = UIKit.Txt("Cinta", encabezado, "TABLA DE RIVALES", fuenteTexto, 26, UIKit.Acento, TextAlignmentOptions.Top, 18f);
        UIKit.Ubicar(cinta.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(1100, 34));
        var titulo = UIKit.Txt("Titulo", encabezado, "RANKING", fuenteTitulos, 120, Color.white, TextAlignmentOptions.Top, 8f);
        UIKit.Ubicar(titulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(1100, 124));
        textoPuesto = UIKit.Txt("Puesto", encabezado, "TU MEJOR PUESTO #1", fuenteTitulos, 40, Color.white, TextAlignmentOptions.Top, 4f);
        UIKit.Ubicar(textoPuesto.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1100, 48));

        panel = UIKit.Rect("Panel", raiz);
        UIKit.Ubicar(panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -250), new Vector2(1000, 760));
        var fondoPanel = UIKit.PanelR("Fondo", panel, new Color(0.03f, 0.06f, 0.15f, 0.82f));
        UIKit.Estirar(fondoPanel.rectTransform);
        lista = UIKit.Rect("Lista", panel);
        lista.anchorMin = Vector2.zero; lista.anchorMax = Vector2.one;
        lista.offsetMin = new Vector2(30, 30); lista.offsetMax = new Vector2(-30, -30);

        botonJugar = UIKit.Boton("Jugar", raiz, "JUGAR", true, new Vector2(320, 80), 46);
        UIKit.Ubicar((RectTransform)botonJugar.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-90, 50), new Vector2(320, 80));
        botonVolver = UIKit.Boton("Volver", raiz, "VOLVER", false, new Vector2(260, 80), 40);
        UIKit.Ubicar((RectTransform)botonVolver.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(90, 50), new Vector2(260, 80));

        var nota = UIKit.Txt("Nota", raiz, "Ranking local con rivales simulados · cambian en cada visita", fuenteTextoSuave, 22, UIKit.TextoSuaveColor, TextAlignmentOptions.Bottom, 2f);
        UIKit.Ubicar(nota.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(1000, 30));
    }
}
