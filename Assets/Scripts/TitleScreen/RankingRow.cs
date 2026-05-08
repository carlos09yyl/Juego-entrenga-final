using TMPro;
using UnityEngine;

namespace TitleScreen
{
    /// <summary>
    /// Represents a single row in the arcade ranking table.
    /// </summary>
    public class RankingRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI goalsText;

        /// <summary>Fills the row with ranking data.</summary>
        public void SetData(int rank, string playerName, int goals)
        {
            if (rankText != null)  rankText.text  = rank.ToString("D2");
            if (nameText != null)  nameText.text  = playerName.ToUpper();
            if (goalsText != null) goalsText.text = goals.ToString("D3");
        }
    }
}
