using UnityEngine;
using UnityEngine.SceneManagement;

namespace TitleScreen
{
    /// <summary>
    /// Controls the main menu panel and navigates between subpanels.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private const string GameSceneName = "GameScene";
        private const string RankingSceneName = "Ranking";
        private const string CreditsSceneName = "Credits";

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;

        private void Start()
        {
            ShowMainMenu();
        }

        /// <summary>Shows the main menu.</summary>
        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        /// <summary>Loads the game scene to start a new game.</summary>
        public void OnNewGame()
        {
            SceneManager.LoadScene(GameSceneName);
        }

        /// <summary>Loads the ranking scene.</summary>
        public void OnRanking()
        {
            SceneManager.LoadScene(RankingSceneName);
        }

        /// <summary>Loads the credits scene.</summary>
        public void OnCredits()
        {
            SceneManager.LoadScene(CreditsSceneName);
        }

        /// <summary>Quits the application.</summary>
        public void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
