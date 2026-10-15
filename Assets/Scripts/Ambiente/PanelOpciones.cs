using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Ventana de opciones (sonido, repeticion, linea de tiro, publico). Se arma en tiempo de
/// ejecucion con el estilo de UIKit; la usan el menu principal y la pausa del juego.
/// </summary>
public class PanelOpciones : MonoBehaviour
{
    public Button botonVolver;
    public Selectable primero;
    Action alCerrar;

    /// <summary>Crea la ventana encima de todo lo que hay en <paramref name="padre"/>.</summary>
    public static PanelOpciones Crear(Transform padre, Action alCerrar)
    {
        var velo = UIKit.Img("Opciones", padre, null, new Color(0.01f, 0.02f, 0.06f, 0.9f));
        velo.raycastTarget = true;
        UIKit.Estirar(velo.rectTransform);
        var panel = velo.gameObject.AddComponent<PanelOpciones>();
        panel.alCerrar = alCerrar;

        var caja = UIKit.PanelR("Caja", velo.transform, UIKit.ColorPanel);
        UIKit.Ubicar(caja.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940, 760));
        var titulo = UIKit.Txt("Titulo", caja.transform, "OPCIONES", UIKit.Titulos, 96, Color.white, TextAlignmentOptions.Top, 8f);
        UIKit.Ubicar(titulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(800, 100));
        var linea = UIKit.Img("Linea", caja.transform, null, UIKit.Acento);
        UIKit.Ubicar(linea.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -126), new Vector2(90, 5));

        float y = -170f;
        var s1 = Deslizador(caja.transform, "EFECTOS", "Patada, red, palo, silbato", y, Opciones.VolEfectos, v => Opciones.VolEfectos = v); y -= 100;
        var s2 = Deslizador(caja.transform, "PÚBLICO", "Ambiente de la tribuna", y, Opciones.VolPublico, v => Opciones.VolPublico = v); y -= 100;
        var t1 = Interruptor(caja.transform, "REPETICIÓN DE GOLES", "Cámara lenta después de cada gol", y, () => Opciones.Repeticion, v => Opciones.Repeticion = v); y -= 100;
        var t2 = Interruptor(caja.transform, "LÍNEA DE TIRO", "Muestra por dónde va el balón", y, () => Opciones.LineaTiro, v => Opciones.LineaTiro = v); y -= 100;
        var t3 = Interruptor(caja.transform, "PÚBLICO EN TRIBUNAS", "Apágalo si el juego va lento", y, () => Opciones.PublicoVisible, v => { Opciones.PublicoVisible = v; Publico.AplicarVisible(); });

        panel.botonVolver = UIKit.Boton("Volver", caja.transform, "LISTO", true, new Vector2(320, 76), 46);
        UIKit.Ubicar((RectTransform)panel.botonVolver.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(320, 76));
        panel.botonVolver.onClick.AddListener(panel.Cerrar);

        Vertical(s1, s2, t1, t2, t3, panel.botonVolver);
        panel.primero = s1;
        return panel;
    }

    void OnEnable()
    {
        if (primero != null) EventSystem.current?.SetSelectedGameObject(primero.gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Cerrar();
    }

    public void Cerrar()
    {
        Opciones.Guardar();
        var accion = alCerrar;
        Destroy(gameObject);
        accion?.Invoke();
    }

    // ── piezas ───────────────────────────────────────────────────────

    static void Etiquetas(Transform padre, string nombre, string ayuda, float y)
    {
        var t = UIKit.Txt("Etiqueta " + nombre, padre, nombre, UIKit.Titulos, 44, Color.white, TextAlignmentOptions.TopLeft, 4f);
        UIKit.Ubicar(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, y), new Vector2(460, 50));
        var a = UIKit.Txt("Ayuda " + nombre, padre, ayuda, UIKit.TextoSuave, 22, UIKit.TextoSuaveColor, TextAlignmentOptions.TopLeft, 1f);
        UIKit.Ubicar(a.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(62, y - 48), new Vector2(460, 30));
    }

    static Slider Deslizador(Transform padre, string nombre, string ayuda, float y, float valor, Action<float> cambio)
    {
        Etiquetas(padre, nombre, ayuda, y);
        var raiz = UIKit.Rect("Deslizador " + nombre, padre);
        UIKit.Ubicar(raiz, new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-60, y - 38), new Vector2(330, 44));
        var fondo = UIKit.Img("Fondo", raiz, UIKit.Panel, new Color(1, 1, 1, 0.12f), true);
        fondo.raycastTarget = true;
        fondo.rectTransform.anchorMin = new Vector2(0, 0.5f); fondo.rectTransform.anchorMax = new Vector2(1, 0.5f);
        fondo.rectTransform.sizeDelta = new Vector2(0, 16);
        var area = UIKit.Rect("Area", raiz);
        area.anchorMin = new Vector2(0, 0.5f); area.anchorMax = new Vector2(1, 0.5f);
        area.sizeDelta = new Vector2(0, 16);
        var relleno = UIKit.Img("Relleno", area, UIKit.Panel, UIKit.Acento, true);
        relleno.rectTransform.sizeDelta = Vector2.zero;
        var areaManija = UIKit.Rect("Area Manija", raiz);
        UIKit.Estirar(areaManija);
        areaManija.offsetMin = new Vector2(14, 0); areaManija.offsetMax = new Vector2(-14, 0);
        var manija = UIKit.Img("Manija", areaManija, UIKit.Circulo, Color.white);
        manija.raycastTarget = true;
        manija.rectTransform.sizeDelta = new Vector2(36, 36);

        var s = raiz.gameObject.AddComponent<Slider>();
        s.fillRect = relleno.rectTransform;
        s.handleRect = manija.rectTransform;
        s.targetGraphic = manija;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = valor;
        var colores = s.colors;
        colores.highlightedColor = UIKit.Acento; colores.selectedColor = UIKit.Acento; colores.pressedColor = UIKit.Acento;
        s.colors = colores;
        s.onValueChanged.AddListener(v => cambio(v));
        return s;
    }

    static Button Interruptor(Transform padre, string nombre, string ayuda, float y, Func<bool> leer, Action<bool> escribir)
    {
        Etiquetas(padre, nombre, ayuda, y);
        var b = UIKit.Boton("Interruptor " + nombre, padre, "", false, new Vector2(170, 64), 38);
        UIKit.Ubicar((RectTransform)b.transform, new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-60, y - 38), new Vector2(170, 64));
        var texto = b.GetComponentInChildren<TextMeshProUGUI>();
        var fondo = b.GetComponent<Image>();
        void Pintar()
        {
            bool v = leer();
            texto.text = v ? "SÍ" : "NO";
            texto.color = v ? UIKit.Acento : new Color(1, 1, 1, 0.45f);
        }
        Pintar();
        b.onClick.AddListener(() => { escribir(!leer()); Pintar(); });
        return b;
    }

    static void Vertical(params Selectable[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            var n = new Navigation { mode = Navigation.Mode.Explicit };
            n.selectOnUp = items[(i + items.Length - 1) % items.Length];
            n.selectOnDown = items[(i + 1) % items.Length];
            items[i].navigation = n;
        }
    }
}
