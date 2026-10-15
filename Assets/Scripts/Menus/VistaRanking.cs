using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Piezas compartidas por la pantalla final y la escena de ranking.</summary>
public static class VistaRanking
{
    static readonly Color Oro = new Color32(0xFF, 0xC5, 0x31, 0xFF);
    static readonly Color Plata = new Color32(0xC9, 0xD3, 0xE3, 0xFF);
    static readonly Color Bronce = new Color32(0xD9, 0x8A, 0x4E, 0xFF);
    static readonly Color Oscuro = new Color32(0x0A, 0x12, 0x28, 0xFF);

    /// <summary>Una fila: puesto, bandera, nombre y puntos. La del jugador va en dorado.</summary>
    public static RectTransform Fila(Transform padre, int puesto, Rivales.Fila f, Vector2 tam, string pieJugador = null)
    {
        bool yo = f.esJugador;
        var fondo = UIKit.PanelR("Fila " + puesto, padre, yo ? Oro : new Color(0.04f, 0.07f, 0.16f, 0.88f));
        var rt = fondo.rectTransform;
        rt.sizeDelta = tam;
        float alto = tam.y;
        Color texto = yo ? Oscuro : Color.white;

        Color colorPuesto = puesto == 1 ? Oro : puesto == 2 ? Plata : puesto == 3 ? Bronce : new Color(1, 1, 1, 0.12f);
        if (yo) colorPuesto = Oscuro;
        var circulo = UIKit.Img("Puesto", fondo.transform, UIKit.Circulo, colorPuesto);
        UIKit.Ubicar(circulo.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(alto - 18, alto - 18));
        bool claro = puesto <= 3 && !yo;
        var numero = UIKit.Txt("Numero", circulo.transform, puesto.ToString(), UIKit.Titulos, alto * 0.5f, yo ? Oro : claro ? Oscuro : Color.white, TextAlignmentOptions.Center);
        UIKit.Estirar(numero.rectTransform);

        var bandera = UIKit.Rect("Bandera", fondo.transform).gameObject.AddComponent<RawImage>();
        bandera.texture = Paises.Bandera(f.codigo);
        bandera.raycastTarget = false;
        float bandAlto = alto * 0.52f;
        UIKit.Ubicar(bandera.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(alto + 8, 0), new Vector2(bandAlto * 4f / 3f, bandAlto));

        float xNombre = alto + 8 + bandAlto * 4f / 3f + 16;
        string nombre = yo ? "TÚ" + (string.IsNullOrEmpty(pieJugador) ? "" : $"  <size=70%><alpha=#99>{pieJugador}") : f.nombre;
        var tNombre = UIKit.Txt("Nombre", fondo.transform, nombre, UIKit.Texto, alto * 0.44f, texto, TextAlignmentOptions.Left, 1f);
        UIKit.Ubicar(tNombre.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(xNombre, 0), new Vector2(tam.x - xNombre - 170, alto));

        var tPuntos = UIKit.Txt("Puntos", fondo.transform, f.puntos.ToString("N0"), UIKit.Titulos, alto * 0.62f, yo ? Oscuro : Oro, TextAlignmentOptions.Right, 2f);
        UIKit.Ubicar(tPuntos.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(160, alto));
        return rt;
    }

    /// <summary>Llena una columna con la tabla y la hace entrar fila por fila.</summary>
    public static List<RectTransform> Llenar(RectTransform lista, List<Rivales.Fila> filas, float altoFila, float separacion, string pieJugador = null)
    {
        foreach (Transform h in lista) Object.Destroy(h.gameObject);
        var creadas = new List<RectTransform>();
        float ancho = lista.rect.width;
        for (int i = 0; i < filas.Count; i++)
        {
            var rt = Fila(lista, i + 1, filas[i], new Vector2(ancho, altoFila), pieJugador);
            UIKit.Ubicar(rt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -i * (altoFila + separacion)), new Vector2(ancho, altoFila));
            creadas.Add(rt);
        }
        return creadas;
    }

    public static IEnumerator Entrar(RectTransform rt, Vector2 desde, float retraso, float dur = 0.4f)
    {
        var g = rt.GetComponent<CanvasGroup>();
        if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        Vector2 destino = rt.anchoredPosition;
        rt.anchoredPosition = destino + desde;
        yield return new WaitForSecondsRealtime(retraso);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f);
            rt.anchoredPosition = Vector2.Lerp(destino + desde, destino, u);
            g.alpha = u;
            yield return null;
        }
        rt.anchoredPosition = destino;
        g.alpha = 1f;
    }

    /// <summary>Fondo con la foto del estadio, tinte oscuro y zoom lento (se mueve desde Update).</summary>
    public static RectTransform Fondo(RectTransform raiz, Texture foto, float oscuridad)
    {
        var marco = UIKit.Rect("Fondo", raiz);
        UIKit.Estirar(marco);
        marco.gameObject.AddComponent<RectMask2D>();
        RectTransform capa = null;
        if (foto != null)
        {
            var img = UIKit.Rect("Foto", marco).gameObject.AddComponent<RawImage>();
            img.texture = foto;
            img.raycastTarget = false;
            var ajuste = img.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ajuste.aspectRatio = foto.width / (float)foto.height;
            capa = img.rectTransform;
        }
        var tinte = UIKit.Img("Tinte", marco, null, new Color(0.02f, 0.04f, 0.11f, oscuridad));
        UIKit.Estirar(tinte.rectTransform);
        return capa;
    }

    public static void Respirar(RectTransform capa)
    {
        if (capa == null) return;
        float t = Time.unscaledTime;
        capa.localScale = Vector3.one * (1.08f + 0.04f * Mathf.Sin(t * 0.1f));
        capa.anchoredPosition = new Vector2(Mathf.Sin(t * 0.06f) * 20f, Mathf.Cos(t * 0.08f) * 10f);
    }
}
