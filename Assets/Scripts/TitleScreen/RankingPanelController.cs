using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TitleScreen
{
    /// <summary>
    /// Populates the arcade-style ranking panel with persisted entries.
    /// </summary>
    public class RankingPanelController : MonoBehaviour
    {
        [Header("Row Template")]
        [SerializeField] private GameObject rankingRowPrefab;
        [SerializeField] private Transform rowContainer;

        [Header("Empty State")]
        [SerializeField] private GameObject emptyLabel;

        /// <summary>Clears and repopulates the ranking list from saved data.</summary>
        public void RefreshRanking()
        {
            foreach (Transform child in rowContainer)
                Destroy(child.gameObject);

            List<RankingEntry> entries = RankingManager.GetRanking();

            bool hasEntries = entries.Count > 0;
            if (emptyLabel != null) emptyLabel.SetActive(!hasEntries);

            for (int i = 0; i < entries.Count; i++)
            {
                GameObject row = Instantiate(rankingRowPrefab, rowContainer);
                RankingRow rowScript = row.GetComponent<RankingRow>();
                rowScript?.SetData(i + 1, entries[i].playerName, entries[i].goals);
            }
        }
    }
}
