using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pantalla al terminar los 5 tiros: titulo segun el resultado, puntos que suben,
/// record, como fue cada tiro, puesto en el ranking de rivales y botones para seguir.
/// Se arma con clic derecho > "Construir pantalla".
/// </summary>
public class PantallaFinalUI : MonoBehaviour
{
    [Header("Escenas")]
    public string escenaJuego = "GameScene";
    public string escenaPais = "SeleccionPais";
    public string escenaMenu = "TitleScreen";

    [Header("Estilo")]
    public TMP_FontAsset fuenteTitulos, fuenteTexto, fuenteTextoSuave;
    public Sprite spritePanel, spriteBorde, spriteCirculo, spriteBrillo;
    public Texture fondo;

    [Header("Referencias (las llena Construir pantalla)")]
    public RectTransform capaFondo, columnaIzquierda, columnaDerecha, listaRanking;
    public TextMeshProUGUI titulo, subtitulo, textoPuntos, textoMejor, textoPuesto, textoPais, textoGoles;
    public RawImage banderaPais;
    public RectTransform insigniaRecord, filaTiros;
    public Button botonOtraVez, botonPais, botonMenu;

    static readonly Color Verde = new Color32(0x2E, 0xD5, 0x73, 0xFF);

    void Start()
    {
        UIKit.Usar(fuenteTitulos, fuenteTexto, fuenteTextoSuave, spritePanel, spriteBorde, spriteCirculo, spriteBrillo);
        Conectar(botonOtraVez, () => SceneManager.LoadScene(escenaJuego));
        Conectar(botonPais, () => SceneManager.LoadScene(escenaPais));
        Conectar(botonMenu, () => SceneManager.LoadScene(escenaMenu));

        int goles = ScoreData.WhiteScore;
        int intentos = Mathf.Max(ScoreData.Jugadas.Count, ScoreData.WhiteScore + ScoreData.BlackScore, 5);
        int puntos = ScoreData.Puntos;
        bool record = puntos > 0 && puntos > ScoreData.MejorAlEmpezar;
        var pais = Paises.Actual;

        titulo.text = goles == intentos ? "¡PERFECTO!" : goles >= 4 ? "¡PARTIDAZO!" : goles >= 2 ? "¡BUEN PARTIDO!" : goles == 1 ? "¡AL MENOS UNO!" : "EL ARQUERO GANÓ";
        subtitulo.text = goles == intentos ? "Gol en todos los tiros. Nadie te para." :
                         goles == 0 ? "Hoy no entró ninguno. La revancha te espera." :
                         $"Anotaste {goles} de {intentos} tiros libres.";
        textoPais.text = pais.nombre.ToUpper();
        banderaPais.texture = Paises.Bandera(pais.codigo);
        textoGoles.text = $"GOLES  <color=#FFC531>{goles}/{intentos}</color>";
        textoMejor.text = $"MEJOR  <color=#FFC531>{Mathf.Max(ScoreData.MejorPuntaje, puntos):N0}</color>";
        textoPuntos.text = "0";
        insigniaRecord.gameObject.SetActive(false);
        textoMejor.gameObject.SetActive(!record);

        // como fue cada tiro
        for (int i = 0; i < filaTiros.childCount; i++)
        {
            var chip = filaTiros.GetChild(i);
            string jugada = i < ScoreData.Jugadas.Count ? ScoreData.Jugadas[i] : (i < goles ? "Gol" : "Fallo");
            bool gol = jugada == "Gol";
            chip.GetComponent<Image>().color = gol ? Verde : new Color(1f, 0.3f, 0.37f, 0.9f);
            var etiqueta = chip.Find("Etiqueta")?.GetComponent<TextMeshProUGUI>();
            if (etiqueta != null) etiqueta.text = Etiqueta(jugada);
            chip.gameObject.SetActive(i < intentos);
        }

        // ranking con los rivales de esta visita
        var tabla = Rivales.Tabla(puntos, pais, out int posicion);
        textoPuesto.text = $"PUESTO <color=#FFC531>#{posicion + 1}</color> DE {tabla.Count}";
        var filas = VistaRanking.Llenar(listaRanking, tabla, 56f, 8f, pais.nombre.ToUpper());

        StartCoroutine(Secuencia(puntos, record, filas, posicion));
    }

    static string Etiqueta(string jugada)
    {
        switch (jugada)
        {
            case "Gol": return "GOL";
            case "Atajada": return "ATAJADA";
            case "Afuera": return "AFUERA";
            case "Palo": return "PALO";
            case "Barrera": return "BARRERA";
            default: return "FALLO";
        }
    }

    static void Conectar(Button b, UnityEngine.Events.UnityAction a)
    {
        if (b == null) return;
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(a);
    }

    void Update()
    {
        VistaRanking.Respirar(capaFondo);
        if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene(escenaMenu);
    }

    IEnumerator Secuencia(int puntos, bool record, List<RectTransform> filas, int posicion)
    {
        StartCoroutine(VistaRanking.Entrar(columnaIzquierda, new Vector2(-90, 0), 0f, 0.5f));
        StartCoroutine(VistaRanking.Entrar(columnaDerecha, new Vector2(90, 0), 0.15f, 0.5f));
        for (int i = 0; i < filas.Count; i++)
            if (i != posicion) StartCoroutine(VistaRanking.Entrar(filas[i], new Vector2(40, 0), 0.4f + 0.05f * i));
        // los tiros aparecen uno por uno
        for (int i = 0; i < filaTiros.childCount; i++)
            StartCoroutine(Saltar((RectTransform)filaTiros.GetChild(i), 0.5f + 0.12f * i));

        // los puntos suben
        yield return new WaitForSecondsRealtime(0.7f);
        float dur = Mathf.Lerp(0.6f, 1.6f, Mathf.Clamp01(puntos / 1500f));
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float u = 1f - Mathf.Pow(1f - t / dur, 3f);
            textoPuntos.text = Mathf.RoundToInt(puntos * u).ToString("N0");
            yield return null;
        }
        textoPuntos.text = puntos.ToString("N0");
        StartCoroutine(Latido(textoPuntos.rectTransform, 0.18f));

        if (record)
        {
            insigniaRecord.gameObject.SetActive(true);
            StartCoroutine(Saltar(insigniaRecord, 0f));
            SonidosPenales.Reproducir("gol_publico");
        }

        // la fila del jugador entra al final, desde lejos, para que se note su puesto
        if (posicion >= 0 && posicion < filas.Count)
        {
            yield return VistaRanking.Entrar(filas[posicion], new Vector2(260, 0), 0.1f, 0.55f);
            StartCoroutine(Latido(filas[posicion], 0.06f));
        }

        EventSystem.current?.SetSelectedGameObject(botonOtraVez.gameObject);
        while (record && insigniaRecord != null)
        {
            insigniaRecord.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 5f));
            yield return null;
        }
    }

    IEnumerator Saltar(RectTransform rt, float retraso)
    {
        rt.localScale = Vector3.zero;
        yield return new WaitForSecondsRealtime(retraso);
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float u = t / 0.35f;
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float e = 1f + c3 * Mathf.Pow(u - 1f, 3f) + c1 * Mathf.Pow(u - 1f, 2f);
            rt.localScale = Vector3.one * e;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator Latido(RectTransform rt, float fuerza)
    {
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            rt.localScale = Vector3.one * (1f + fuerza * Mathf.Sin(t / 0.3f * Mathf.PI));
            yield return null;
        }
        rt.localScale = Vector3.one;
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
        capaFondo = VistaRanking.Fondo(raiz, fondo, 0.84f);

        // ── columna izquierda: resultado ──
        columnaIzquierda = UIKit.Rect("Resultado", raiz);
        UIKit.Ubicar(columnaIzquierda, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(900, 900));
        var izq = columnaIzquierda;

        var cinta = UIKit.Txt("Cinta", izq, "PARTIDO TERMINADO", fuenteTexto, 28, UIKit.Acento, TextAlignmentOptions.TopLeft, 18f);
        UIKit.Ubicar(cinta.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(900, 36));
        titulo = UIKit.Txt("Titulo", izq, "¡BUEN PARTIDO!", fuenteTitulos, 150, Color.white, TextAlignmentOptions.TopLeft, 4f);
        UIKit.Ubicar(titulo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-4, -30), new Vector2(900, 150));
        titulo.enableAutoSizing = true; titulo.fontSizeMin = 90; titulo.fontSizeMax = 150;
        subtitulo = UIKit.Txt("Subtitulo", izq, "Anotaste 3 de 5 tiros libres.", fuenteTextoSuave, 32, UIKit.TextoSuaveColor, TextAlignmentOptions.TopLeft, 1f);
        UIKit.Ubicar(subtitulo.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -182), new Vector2(900, 44));

        // pais
        banderaPais = UIKit.Rect("Bandera", izq).gameObject.AddComponent<RawImage>();
        banderaPais.raycastTarget = false;
        UIKit.Ubicar(banderaPais.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -250), new Vector2(60, 45));
        textoPais = UIKit.Txt("Pais", izq, "COLOMBIA", fuenteTexto, 34, Color.white, TextAlignmentOptions.Left, 6f);
        UIKit.Ubicar(textoPais.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(76, -248), new Vector2(500, 48));

        // puntos
        var caja = UIKit.PanelR("Caja Puntos", izq, UIKit.ColorPanel);
        UIKit.Ubicar(caja.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -320), new Vector2(840, 250));
        var etPuntos = UIKit.Txt("Etiqueta", caja.transform, "PUNTOS", fuenteTextoSuave, 26, UIKit.TextoSuaveColor, TextAlignmentOptions.TopLeft, 14f);
        UIKit.Ubicar(etPuntos.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -26), new Vector2(400, 34));
        textoPuntos = UIKit.Txt("Puntos", caja.transform, "0", fuenteTitulos, 170, UIKit.Acento, TextAlignmentOptions.Left, 4f);
        UIKit.Ubicar(textoPuntos.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(32, 12), new Vector2(460, 190));
        textoGoles = UIKit.Txt("Goles", caja.transform, "GOLES 3/5", fuenteTitulos, 46, Color.white, TextAlignmentOptions.Right, 4f);
        UIKit.Ubicar(textoGoles.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -40), new Vector2(330, 56));
        textoMejor = UIKit.Txt("Mejor", caja.transform, "MEJOR 0", fuenteTitulos, 46, Color.white, TextAlignmentOptions.Right, 4f);
        UIKit.Ubicar(textoMejor.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 40), new Vector2(330, 56));

        var record = UIKit.PanelR("Nuevo Record", caja.transform, UIKit.Rojo, false);
        insigniaRecord = record.rectTransform;
        UIKit.Ubicar(insigniaRecord, new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-190, 66), new Vector2(300, 64));
        var tRecord = UIKit.Txt("Texto", record.transform, "¡NUEVO RÉCORD!", fuenteTitulos, 44, Color.white, TextAlignmentOptions.Center, 4f);
        UIKit.Estirar(tRecord.rectTransform);
        insigniaRecord.localRotation = Quaternion.Euler(0, 0, 4f);

        // como fue cada tiro
        var etTiros = UIKit.Txt("Etiqueta Tiros", izq, "TUS TIROS", fuenteTextoSuave, 24, UIKit.TextoSuaveColor, TextAlignmentOptions.TopLeft, 14f);
        UIKit.Ubicar(etTiros.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -600), new Vector2(400, 32));
        filaTiros = UIKit.Rect("Tiros", izq);
        UIKit.Ubicar(filaTiros, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -640), new Vector2(840, 110));
        for (int i = 0; i < 5; i++)
        {
            var chip = UIKit.Img("Tiro " + (i + 1), filaTiros, spritePanel, Color.gray, true);
            UIKit.Ubicar(chip.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(76 + i * 172, 0), new Vector2(152, 104));
            var num = UIKit.Txt("Numero", chip.transform, (i + 1).ToString(), fuenteTextoSuave, 20, new Color(1, 1, 1, 0.75f), TextAlignmentOptions.TopLeft, 0f);
            UIKit.Ubicar(num.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(12, -6), new Vector2(40, 26));
            var et = UIKit.Txt("Etiqueta", chip.transform, "GOL", fuenteTitulos, 44, Color.white, TextAlignmentOptions.Center, 3f);
            UIKit.Ubicar(et.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(140, 56));
            et.enableAutoSizing = true; et.fontSizeMin = 24; et.fontSizeMax = 44;
        }

        // botones
        botonOtraVez = UIKit.Boton("Otra Vez", izq, "JUGAR DE NUEVO", true, new Vector2(400, 84), 46);
        UIKit.Ubicar((RectTransform)botonOtraVez.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(400, 84));
        botonPais = UIKit.Boton("Cambiar Pais", izq, "CAMBIAR PAÍS", false, new Vector2(240, 84), 36);
        UIKit.Ubicar((RectTransform)botonPais.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(420, 0), new Vector2(240, 84));
        botonMenu = UIKit.Boton("Menu", izq, "MENÚ", false, new Vector2(160, 84), 36);
        UIKit.Ubicar((RectTransform)botonMenu.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(680, 0), new Vector2(160, 84));
        Horizontal(botonOtraVez, botonPais, botonMenu);

        // ── columna derecha: ranking ──
        columnaDerecha = UIKit.Rect("Ranking", raiz);
        UIKit.Ubicar(columnaDerecha, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-100, 0), new Vector2(760, 900));
        var panel = UIKit.PanelR("Panel", columnaDerecha, new Color(0.03f, 0.06f, 0.15f, 0.82f));
        UIKit.Estirar(panel.rectTransform);
        var tRank = UIKit.Txt("Titulo", panel.transform, "RANKING", fuenteTitulos, 72, Color.white, TextAlignmentOptions.TopLeft, 6f);
        UIKit.Ubicar(tRank.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(36, -26), new Vector2(400, 80));
        var tRiv = UIKit.Txt("Subtitulo", panel.transform, "RIVALES", fuenteTextoSuave, 24, UIKit.Acento, TextAlignmentOptions.TopLeft, 14f);
        UIKit.Ubicar(tRiv.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -104), new Vector2(300, 30));
        textoPuesto = UIKit.Txt("Puesto", panel.transform, "PUESTO #1 DE 10", fuenteTitulos, 44, Color.white, TextAlignmentOptions.TopRight, 3f);
        UIKit.Ubicar(textoPuesto.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -44), new Vector2(380, 56));
        listaRanking = UIKit.Rect("Lista", panel.transform);
        listaRanking.anchorMin = new Vector2(0, 0); listaRanking.anchorMax = new Vector2(1, 1);
        listaRanking.offsetMin = new Vector2(30, 40); listaRanking.offsetMax = new Vector2(-30, -150);
    }

    static void Horizontal(params Button[] botones)
    {
        for (int i = 0; i < botones.Length; i++)
        {
            var n = new Navigation { mode = Navigation.Mode.Explicit };
            n.selectOnLeft = botones[(i + botones.Length - 1) % botones.Length];
            n.selectOnRight = botones[(i + 1) % botones.Length];
            botones[i].navigation = n;
        }
    }
}
