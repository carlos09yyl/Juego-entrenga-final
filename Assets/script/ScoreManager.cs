using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void Start()
    {
        UpdateScore();

        Invoke("ReturnGame", 3f);
    }

    void UpdateScore()
    {
        scoreText.text =
        "White Team: " + ScoreData.WhiteScore +
        "\nBlack Team: " + ScoreData.BlackScore;
    }

    void ReturnGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}