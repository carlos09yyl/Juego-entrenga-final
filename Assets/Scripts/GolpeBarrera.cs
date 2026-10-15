using UnityEngine;

/// <summary>Avisa a la barrera cuando el balon le pega a un jugador.</summary>
public class GolpeBarrera : MonoBehaviour
{
    public Barrera barrera;

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag("Ball")) barrera?.Golpe();
    }
}
