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

        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject rankingPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Sub-controllers")]
        [SerializeField] private RankingPanelController rankingPanelController;

        private void Start()
        {
            ShowMainMenu();
        }

        /// <summary>Shows the main menu and hides all other panels.</summary>
        public void ShowMainMenu()
        {
            SetPanelsActive(main: true, ranking: false, credits: false);
        }

        /// <summary>Loads the game scene.</summary>
        public void OnNewGame()
        {
            SceneManager.LoadScene(GameSceneName);
        }

        /// <summary>Opens the ranking panel.</summary>
        public void OnRanking()
        {
            SetPanelsActive(main: false, ranking: true, credits: false);
            rankingPanelController?.RefreshRanking();
        }

        /// <summary>Opens the credits panel.</summary>
        public void OnCredits()
        {
            SetPanelsActive(main: false, ranking: false, credits: true);
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

        private void SetPanelsActive(bool main, bool ranking, bool credits)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(main);
            if (rankingPanel != null)  rankingPanel.SetActive(ranking);
            if (creditsPanel != null)  creditsPanel.SetActive(credits);
        }
    }
}
