using UnityEngine;
using UnityEngine.SceneManagement;

public class MissTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            ScoreData.BlackScore++; // punto para el arquero
            SceneManager.LoadScene("ScoreScene");
        }
    }
}