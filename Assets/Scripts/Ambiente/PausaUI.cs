using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pausa del partido (Esc o el boton de arriba a la derecha): continuar, opciones,
/// reiniciar o volver al menu. Se arma sola al iniciar con el estilo de UIKit.
/// </summary>
public class PausaUI : MonoBehaviour
{
    public static bool Pausado { get; private set; }

    public string escenaMenu = "TitleScreen";
    public TMP_FontAsset fuenteTitulos, fuenteTexto, fuenteTextoSuave;
    public Sprite spritePanel, spriteBorde, spriteCirculo, spriteBrillo;

    GameObject velo;
    Button botonContinuar, botonOpciones;
    PanelOpciones opciones;
    int cerradoEn = -1;

    void Start()
    {
        Pausado = false;
        UIKit.Usar(fuenteTitulos, fuenteTexto, fuenteTextoSuave, spritePanel, spriteBorde, spriteCirculo, spriteBrillo);
        var raiz = (RectTransform)transform;

        // boton pequeño de pausa (sirve tambien para el celular)
        var boton = UIKit.PanelR("Boton Pausa", raiz, UIKit.ColorPanel);
        boton.raycastTarget = true;
        UIKit.Ubicar(boton.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -150), new Vector2(76, 76));
        for (int i = 0; i < 2; i++)
        {
            var barra = UIKit.Img("Barra", boton.transform, null, Color.white);
            UIKit.Ubicar(barra.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -9 : 9, 0), new Vector2(9, 30));
        }
        var b = boton.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(() => Pausar(true));

        // ventana de pausa
        velo = UIKit.Img("Pausa", raiz, null, new Color(0.01f, 0.02f, 0.06f, 0.82f)).gameObject;
        velo.GetComponent<Image>().raycastTarget = true;
        UIKit.Estirar((RectTransform)velo.transform);
        var titulo = UIKit.Txt("Titulo", velo.transform, "PAUSA", UIKit.Titulos, 140, Color.white, TextAlignmentOptions.Center, 10f);
        UIKit.Ubicar(titulo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(800, 150));
        var linea = UIKit.Img("Linea", velo.transform, null, UIKit.Acento);
        UIKit.Ubicar(linea.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(100, 6));

        string[] nombres = { "CONTINUAR", "OPCIONES", "REINICIAR", "MENÚ PRINCIPAL" };
        var botones = new Button[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            botones[i] = UIKit.Boton(nombres[i], velo.transform, nombres[i], i == 0, new Vector2(480, i == 0 ? 92 : 78), i == 0 ? 56 : 46);
            UIKit.Ubicar((RectTransform)botones[i].transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80 - i * 100 - (i > 0 ? 8 : 0)), botones[i].GetComponent<RectTransform>().sizeDelta);
        }
        for (int i = 0; i < botones.Length; i++)
        {
            var n = new Navigation { mode = Navigation.Mode.Explicit };
            n.selectOnUp = botones[(i + botones.Length - 1) % botones.Length];
            n.selectOnDown = botones[(i + 1) % botones.Length];
            botones[i].navigation = n;
        }
        botonContinuar = botones[0];
        botonOpciones = botones[1];
        botones[0].onClick.AddListener(() => Pausar(false));
        botones[1].onClick.AddListener(AbrirOpciones);
        botones[2].onClick.AddListener(() => { Pausar(false); SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
        botones[3].onClick.AddListener(() => { Pausar(false); SceneManager.LoadScene(escenaMenu); });
        velo.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || opciones != null || Time.frameCount == cerradoEn) return;
        Pausar(!Pausado);
    }

    void OnDestroy()
    {
        if (Pausado) { Time.timeScale = 1f; AudioListener.pause = false; Pausado = false; }
    }

    public void Pausar(bool si)
    {
        Pausado = si;
        Time.timeScale = si ? 0f : 1f;
        AudioListener.pause = si;
        velo.SetActive(si);
        EventSystem.current?.SetSelectedGameObject(si ? botonContinuar.gameObject : null);
    }

    void AbrirOpciones()
    {
        opciones = PanelOpciones.Crear(transform, () =>
        {
            opciones = null;
            cerradoEn = Time.frameCount;
            EventSystem.current?.SetSelectedGameObject(botonOpciones.gameObject);
        });
    }
}
