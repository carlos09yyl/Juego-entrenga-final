using UnityEngine;

/// <summary>Le pone al pateador los colores del pais elegido en la pantalla de seleccion.</summary>
public class UniformeJugador : MonoBehaviour
{
    [Tooltip("Material de color liso (Unlit/Color) que se copia para la camiseta y la pantaloneta.")]
    public Material materialBase;

    void Start()
    {
        Paises.Vestir(gameObject, Paises.Actual, materialBase);
    }
}
