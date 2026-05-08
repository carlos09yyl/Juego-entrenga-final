using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the end-of-game overlay shown after all penalty attempts are done.
///
/// Flow:
///   1. ShowResults(goals, maxAttempts)  → shows the result panel with score summary.
///   2. After a short reveal delay       → shows the action buttons (play again / main menu).
///   3. PlayAgain()                      → hides this UI, resets everything via PenaltyTargetSelector.
///   4. GoToMainMenu()                   → loads TitleScreen scene.
///
/// Attach to the GameEndUI root GameObject.
/// Wire all references in the Inspector.
/// </summary>
public class GameEndUI : MonoBehaviour
{
    private const string TitleSceneName = "TitleScreen";

    [Header("Panels")]
    [Tooltip("Root panel — hidden at game start.")]
    public GameObject resultPanel;
    [Tooltip("Button group shown after the result reveal delay.")]
    public GameObject buttonPanel;

    [Header("Result labels")]
    public TextMeshProUGUI titleLabel;      // "¡Penales terminados!"
    public TextMeshProUGUI goalsLabel;      // "Goles: 2 / 3"
    public TextMeshProUGUI outcomeLabel;    // "¡Ganaste!" / "Casi…" / "¡Perfecto!"

    [Header("Buttons")]
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Timing")]
    [Tooltip("Seconds before the action buttons appear after the result is shown.")]
    public float buttonRevealDelay = 1.8f;

    [Header("Back-references (set by PenaltyTargetSelector)")]
    public PenaltyTargetSelector penaltySelector;

    // ── Outcome texts ─────────────────────────────────────────────────
    private const string OutcomePerfect = "¡Perfecto! Gol en todos los intentos";
    private const string OutcomeGood    = "¡Bien hecho! Anotaste {0} de {1}";
    private const string OutcomeNone    = "El portero fue imbatible…";

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Hidden at game start
        if (resultPanel != null) resultPanel.SetActive(false);

        // Wire button callbacks
        playAgainButton?.onClick.AddListener(PlayAgain);
        mainMenuButton?.onClick.AddListener(GoToMainMenu);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Displays the end-of-game result panel.
    /// Called by PenaltyTargetSelector after the last attempt resolves.
    /// </summary>
    public void ShowResults(int goalsScored, int maxAttempts)
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (buttonPanel != null) buttonPanel.SetActive(false);

        // Populate labels
        if (titleLabel != null)
            titleLabel.text = "Penales finalizados";

        if (goalsLabel != null)
            goalsLabel.text = $"Goles: {goalsScored} / {maxAttempts}";

        if (outcomeLabel != null)
            outcomeLabel.text = BuildOutcomeText(goalsScored, maxAttempts);

        // Reveal buttons after a short delay so the player can read the result first
        Invoke(nameof(RevealButtons), buttonRevealDelay);
    }

    // ── Button handlers ───────────────────────────────────────────────

    /// <summary>Resets the entire game state and hides this UI so the player can try again.</summary>
    public void PlayAgain()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        penaltySelector?.RestartGame();
    }

    /// <summary>Loads the title screen scene.</summary>
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(TitleSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void RevealButtons()
    {
        if (buttonPanel != null) buttonPanel.SetActive(true);
    }

    private string BuildOutcomeText(int goals, int max)
    {
        if (goals == max)   return OutcomePerfect;
        if (goals == 0)     return OutcomeNone;
        return string.Format(OutcomeGood, goals, max);
    }
}
