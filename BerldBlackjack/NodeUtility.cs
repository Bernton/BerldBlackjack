namespace BerldBlackjack
{
    internal static class NodeUtility
    {
        internal static int DeckAmount { get; set; } = 8;
        internal static bool IsBasicStrategy { get; set; } = false;

        internal static int[] GetAliveRankAmounts()
        {
            int[] aliveRankAmounts = new int[Rank.Amount];

            for (int i = 0; i < Rank.Amount; i++)
            {
                aliveRankAmounts[i] = Deck.GetFrequencyFromIndex(i) * DeckAmount;
            }

            return aliveRankAmounts;
        }

        internal static int[] GetAliveRankAmounts(Node node)
        {
            int[] aliveRankAmounts = GetAliveRankAmounts();
            int[] allRanks;

            if (IsBasicStrategy)
            {
                allRanks = new int[] { node.DealerRanks[0] };
            }
            else
            {
                allRanks = Enumerable.Concat(node.PlayerRanks, node.DealerRanks).ToArray();
            }

            foreach (int rank in allRanks)
            {
                int rankIndex = Rank.ToIndex(rank);
                aliveRankAmounts[rankIndex]--;
            }

            return aliveRankAmounts;
        }
    }
}
