using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de potencia del tiro. Se arma sola dentro del Canvas al iniciar
/// (no hay que crear nada a mano). La zona clara marca la potencia ideal.
/// </summary>
public class BarraPotencia : MonoBehaviour
{
    [Header("Donde se dibuja")]
    [Tooltip("Canvas donde se crea la barra. Si esta vacio usa el primero que encuentre.")]
    public Canvas canvas;
    public Vector2 posicion = new Vector2(0f, 200f);   // desde el centro de abajo
    public Vector2 tamano = new Vector2(440f, 26f);

    [Header("Colores")]
    public Color colorFondo = new Color(0.04f, 0.07f, 0.2f, 0.92f);
    public Color colorBorde = new Color(0.35f, 0.8f, 1f, 1f);
    public Color colorZona = new Color(1f, 1f, 1f, 0.22f);
    public Gradient colorRelleno;

    [Header("Estilo")]
    public TMP_FontAsset fuente;
    public Sprite spritePanel;

    private RectTransform raiz;
    private Image relleno;
    private RectTransform zona;
    private CanvasGroup grupo;
    private TextMeshProUGUI etiqueta;
    private Coroutine ocultar;

    void Reset()
    {
        colorRelleno = GradienteBase();
    }

    void Awake()
    {
        if (colorRelleno == null || colorRelleno.colorKeys.Length < 2) colorRelleno = GradienteBase();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null) Construir();
    }

    static Gradient GradienteBase()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.3f, 0.85f, 1f), 0f),
                new GradientColorKey(new Color(0.35f, 1f, 0.45f), 0.6f),
                new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.82f),
                new GradientColorKey(new Color(1f, 0.25f, 0.2f), 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    void Construir()
    {
        Sprite caja = spritePanel;

        var go = new GameObject("Barra de Potencia", typeof(RectTransform), typeof(CanvasGroup));
        raiz = go.GetComponent<RectTransform>();
        raiz.SetParent(canvas.transform, false);
        raiz.anchorMin = raiz.anchorMax = new Vector2(0.5f, 0f);
        raiz.pivot = new Vector2(0.5f, 0f);
        raiz.anchoredPosition = posicion;
        raiz.sizeDelta = tamano;
        grupo = go.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;

        // borde y fondo
        var borde = Hijo("Borde", raiz, caja, colorBorde);
        Estirar(borde.rectTransform, -3f);
        var fondo = Hijo("Fondo", raiz, caja, colorFondo);
        Estirar(fondo.rectTransform, 0f);

        // zona ideal
        var z = Hijo("Zona Ideal", raiz, null, colorZona);
        zona = z.rectTransform;
        zona.anchorMin = new Vector2(0.62f, 0f);
        zona.anchorMax = new Vector2(0.82f, 1f);
        zona.offsetMin = new Vector2(0f, 3f);
        zona.offsetMax = new Vector2(0f, -3f);

        // relleno
        relleno = Hijo("Relleno", raiz, null, Color.white);
        Estirar(relleno.rectTransform, 4f);
        relleno.type = Image.Type.Filled;
        relleno.fillMethod = Image.FillMethod.Horizontal;
        relleno.fillOrigin = 0;
        relleno.fillAmount = 0f;
        relleno.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);

        // texto
        var tgo = new GameObject("Etiqueta", typeof(RectTransform));
        var trt = tgo.GetComponent<RectTransform>();
        trt.SetParent(raiz, false);
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 0f);
        trt.anchoredPosition = new Vector2(0f, 6f);
        trt.sizeDelta = new Vector2(0f, 30f);
        etiqueta = tgo.AddComponent<TextMeshProUGUI>();
        if (fuente != null) etiqueta.font = fuente;
        etiqueta.text = "POTENCIA";
        etiqueta.fontSize = 26f;
        etiqueta.fontStyle = FontStyles.Bold;
        etiqueta.characterSpacing = 8f;
        etiqueta.alignment = TextAlignmentOptions.Center;
        etiqueta.color = Color.white;
    }

    static Image Hijo(string nombre, Transform padre, Sprite sprite, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        if (sprite != null) img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Estirar(RectTransform rt, float margen)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    public void Mostrar(bool visible)
    {
        if (raiz == null) return;
        if (ocultar != null) { StopCoroutine(ocultar); ocultar = null; }
        grupo.alpha = visible ? 1f : 0f;
        raiz.localScale = Vector3.one;
        if (visible) { relleno.fillAmount = 0f; etiqueta.text = "POTENCIA"; }
    }

    public void Actualizar(float p, float zonaMin, float zonaMax)
    {
        if (raiz == null) return;
        zona.anchorMin = new Vector2(zonaMin, 0f);
        zona.anchorMax = new Vector2(zonaMax, 1f);
        relleno.fillAmount = p;
        relleno.color = colorRelleno.Evaluate(p);
        bool ideal = p >= zonaMin && p <= zonaMax;
        raiz.localScale = Vector3.one * (ideal ? 1.04f : 1f);
    }

    /// <summary>Muestra el resultado un momento y se oculta.</summary>
    public void Soltar(float p)
    {
        if (raiz == null) return;
        float zMin = zona.anchorMin.x, zMax = zona.anchorMax.x;
        etiqueta.text = p >= zMin && p <= zMax ? "¡PERFECTO!" : p > zMax ? "DEMASIADO FUERTE" : "FLOJO";
        if (ocultar != null) StopCoroutine(ocultar);
        ocultar = StartCoroutine(Ocultar());
    }

    IEnumerator Ocultar()
    {
        yield return new WaitForSeconds(0.7f);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            grupo.alpha = 1f - t / 0.3f;
            yield return null;
        }
        grupo.alpha = 0f;
        ocultar = null;
    }
}
