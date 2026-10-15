using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Tarjeta de una bandera: al pasar con el mouse o el teclado cambia el pais y crece un poco.</summary>
public class TarjetaPais : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int indice;
    public SeleccionPaisUI pantalla;

    private bool activa;
    private float valor;

    public void OnSelect(BaseEventData e) { activa = true; pantalla?.Elegir(indice, false); }
    public void OnDeselect(BaseEventData e) { activa = false; }
    public void OnPointerEnter(PointerEventData e) { EventSystem.current?.SetSelectedGameObject(gameObject); }
    public void OnPointerExit(PointerEventData e) { }

    void Update()
    {
        valor = Mathf.MoveTowards(valor, activa ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        float u = valor * valor * (3f - 2f * valor);
        transform.localScale = Vector3.one * (1f + 0.08f * u);
    }
}
