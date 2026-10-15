using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>Animacion de los botones: crecen un poco, aparece una barra dorada y el fondo se ilumina.</summary>
public class BotonMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public bool principal;
    public RectTransform barra;
    public TextMeshProUGUI texto;
    public Image fondo;

    private bool encima, seleccionado;
    private float valor;

    public void OnPointerEnter(PointerEventData e) { encima = true; EventSystem.current?.SetSelectedGameObject(gameObject); }
    public void OnPointerExit(PointerEventData e) { encima = false; }
    public void OnSelect(BaseEventData e) { seleccionado = true; }
    public void OnDeselect(BaseEventData e) { seleccionado = false; }

    void Update()
    {
        float objetivo = encima || seleccionado ? 1f : 0f;
        valor = Mathf.MoveTowards(valor, objetivo, Time.unscaledDeltaTime * 7f);
        float u = valor * valor * (3f - 2f * valor);
        transform.localScale = Vector3.one * (1f + 0.05f * u);
        if (barra != null) barra.sizeDelta = new Vector2(Mathf.Lerp(0f, 8f, u), barra.sizeDelta.y);
        if (fondo != null)
        {
            Color baseC = principal ? UIKit.Acento : UIKit.ColorPanel;
            Color luz = principal ? new Color(1f, 0.85f, 0.45f, 1f) : new Color(0.12f, 0.2f, 0.42f, 0.95f);
            fondo.color = Color.Lerp(baseC, luz, u);
        }
    }
}
