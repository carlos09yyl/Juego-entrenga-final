using System.Collections.Generic;
using UnityEngine;

namespace TitleScreen
{
    /// <summary>
    /// Manages the persisted arcade-style ranking list using PlayerPrefs.
    /// </summary>
    public static class RankingManager
    {
        private const string RankingKey = "Ranking";
        private const int MaxEntries = 10;

        /// <summary>Returns the sorted ranking list, best first.</summary>
        public static List<RankingEntry> GetRanking()
        {
            string json = PlayerPrefs.GetString(RankingKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new List<RankingEntry>();

            RankingList wrapper = JsonUtility.FromJson<RankingList>(json);
            return wrapper?.entries ?? new List<RankingEntry>();
        }

        /// <summary>Adds a new entry, keeps the top MaxEntries sorted by goals descending.</summary>
        public static void AddEntry(string playerName, int goals)
        {
            List<RankingEntry> entries = GetRanking();
            entries.Add(new RankingEntry(playerName, goals));
            entries.Sort((a, b) => b.goals.CompareTo(a.goals));

            if (entries.Count > MaxEntries)
                entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

            RankingList wrapper = new RankingList { entries = entries };
            PlayerPrefs.SetString(RankingKey, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        /// <summary>Removes all ranking entries.</summary>
        public static void ClearRanking()
        {
            PlayerPrefs.DeleteKey(RankingKey);
            PlayerPrefs.Save();
        }

        [System.Serializable]
        private class RankingList
        {
            public List<RankingEntry> entries;
        }
    }
}
