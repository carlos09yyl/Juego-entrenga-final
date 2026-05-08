using System;

namespace TitleScreen
{
    [Serializable]
    public class RankingEntry
    {
        public string playerName;
        public int goals;

        public RankingEntry(string playerName, int goals)
        {
            this.playerName = playerName;
            this.goals = goals;
        }
    }
}
