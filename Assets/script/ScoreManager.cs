using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the ScoreScene UI: shows final results and waits for the player
/// to choose "Play Again" or "Main Menu". Never auto-returns to GameScene.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    private const string GameSceneName  = "GameScene";
    private const string TitleSceneName = "TitleScreen";

    [Header("Labels")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI outcomeText;

    [Header("Buttons")]
    public Button playAgainButton;
    public Button mainMenuButton;

    void Start()
    {
        UpdateScore();

        playAgainButton?.onClick.AddListener(PlayAgain);
        mainMenuButton?.onClick.AddListener(GoToMainMenu);
    }

    void UpdateScore()
    {
        int goals    = ScoreData.WhiteScore;
        int attempts = ScoreData.WhiteScore + ScoreData.BlackScore;

        if (scoreText != null)
            scoreText.text = $"Goles: {goals} / {attempts}";

        if (outcomeText != null)
        {
            if (goals == attempts)
                outcomeText.text = "¡Perfecto! Gol en cada intento";
            else if (goals == 0)
                outcomeText.text = "El portero fue imbatible…";
            else
                outcomeText.text = $"¡Bien hecho! Anotaste {goals} de {attempts}";
        }
    }

    /// <summary>Reloads the game scene to start a fresh penalty session.</summary>
    public void PlayAgain()
    {
        ScoreData.WhiteScore = 0;
        ScoreData.BlackScore = 0;
        SceneManager.LoadScene(GameSceneName);
    }

    /// <summary>Returns to the title screen.</summary>
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(TitleSceneName);
    }
}
