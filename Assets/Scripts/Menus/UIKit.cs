using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Piezas de interfaz con el estilo del juego (paneles azul oscuro, dorado, Bebas Neue y Rajdhani).
/// Las usan el menu principal y la seleccion de pais para verse igual que el HUD.
/// </summary>
public static class UIKit
{
    public static TMP_FontAsset Titulos, Texto, TextoSuave;
    public static Sprite Panel, Borde, Circulo, Brillo;

    public static readonly Color ColorPanel = new Color32(0x0A, 0x12, 0x28, 0xE6);
    public static readonly Color Acento = new Color32(0xFF, 0xC5, 0x31, 0xFF);
    public static readonly Color TextoSuaveColor = new Color(1f, 1f, 1f, 0.6f);
    public static readonly Color Rojo = new Color32(0xFF, 0x4D, 0x5E, 0xFF);

    public static void Usar(TMP_FontAsset titulos, TMP_FontAsset texto, TMP_FontAsset suave, Sprite panel, Sprite borde, Sprite circulo, Sprite brillo)
    {
        Titulos = titulos; Texto = texto; TextoSuave = suave;
        Panel = panel; Borde = borde; Circulo = circulo; Brillo = brillo;
    }

    public static RectTransform Rect(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = padre.gameObject.layer;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        return rt;
    }

    public static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    public static void Ubicar(RectTransform rt, Vector2 ancla, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    public static Image Img(string nombre, Transform padre, Sprite sprite, Color color, bool rebanada = false)
    {
        var rt = Rect(nombre, padre);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (rebanada && sprite != null) img.type = Image.Type.Sliced;
        return img;
    }

    public static Image PanelR(string nombre, Transform padre, Color color, bool borde = true)
    {
        var img = Img(nombre, padre, Panel, color, true);
        if (borde && Borde != null)
        {
            var b = Img("Borde", img.transform, Borde, new Color(1, 1, 1, 0.10f), true);
            Estirar(b.rectTransform);
        }
        return img;
    }

    public static TextMeshProUGUI Txt(string nombre, Transform padre, string texto, TMP_FontAsset fuente, float tam, Color color, TextAlignmentOptions alineacion, float espaciado = 0f)
    {
        var rt = Rect(nombre, padre);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.alignment = alineacion;
        t.characterSpacing = espaciado;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.richText = true;
        return t;
    }

    /// <summary>Boton grande del menu. principal = dorado con texto oscuro.</summary>
    public static Button Boton(string nombre, Transform padre, string texto, bool principal, Vector2 tam, float tamTexto = 50f)
    {
        var fondo = Img(nombre, padre, Panel, principal ? Acento : ColorPanel, true);
        fondo.raycastTarget = true;
        fondo.rectTransform.sizeDelta = tam;
        var boton = fondo.gameObject.AddComponent<Button>();
        boton.transition = Selectable.Transition.None;

        var borde = Img("Borde", fondo.transform, Borde, new Color(1, 1, 1, principal ? 0.35f : 0.14f), true);
        Estirar(borde.rectTransform);

        var barra = Img("Barra", fondo.transform, null, principal ? new Color(0.04f, 0.07f, 0.16f, 1f) : Acento);
        barra.rectTransform.anchorMin = new Vector2(0, 0.5f); barra.rectTransform.anchorMax = new Vector2(0, 0.5f);
        barra.rectTransform.pivot = new Vector2(0, 0.5f);
        barra.rectTransform.anchoredPosition = new Vector2(18, 0);
        barra.rectTransform.sizeDelta = new Vector2(0, tam.y * 0.45f);

        var t = Txt("Texto", fondo.transform, texto, Titulos, tamTexto, principal ? new Color32(0x0A, 0x12, 0x28, 0xFF) : Color.white, TextAlignmentOptions.Center, 6f);
        Estirar(t.rectTransform);

        var anim = fondo.gameObject.AddComponent<BotonMenu>();
        anim.principal = principal;
        anim.barra = barra.rectTransform;
        anim.texto = t;
        anim.fondo = fondo;
        return boton;
    }
}

