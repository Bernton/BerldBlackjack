using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace BerldBlackjack
{
    public static class NodeBuilder
    {
        public static Node Build()
        {
            Node rootNode = new(Array.Empty<int>());

            Dictionary<string, Node> existingNodeMap = new();
            Stack<Node> nodesToBuild = new();

            existingNodeMap.Add(rootNode.ToString(), rootNode);
            nodesToBuild.Push(rootNode);

            while (nodesToBuild.Any())
            {
                Node node = nodesToBuild.Pop();

                Node[] children = CreateHitChildren(node);
                node.Children = children;

                for (int i = 0; i < children.Length; i++)
                {
                    Node child = children[i];
                    string childKey = child.ToString();

                    if (existingNodeMap.ContainsKey(childKey))
                    {
                        Node existingNode = existingNodeMap[childKey];
                        children[i] = existingNode;
                    }
                    else
                    {
                        if (child.Kind == NodeKind.Branch)
                        {
                            nodesToBuild.Push(child);
                        }

                        existingNodeMap.Add(childKey, child);
                    }
                }
            }

            Debug.Assert(rootNode.Children is not null);

            Node[] existingNodes = existingNodeMap.Values.ToArray();

            Node[] startingPlayerNodes = existingNodes.Where(c => c.Ranks.Length == 2).ToArray();
            Node[] startingDealerNodes = existingNodes.Where(c => c.Ranks.Length == 1).ToArray();

            List<(Node playerNode, Node dealerNode)> startingSituations = new();

            foreach (Node playerNode in startingPlayerNodes)
            {
                foreach (Node dealerNode in startingDealerNodes)
                {
                    startingSituations.Add((playerNode, dealerNode));
                }
            }

            double totalEv = 0;
            double totalOdds = 0;

            foreach (var (playerNode, dealerNode) in startingSituations)
            {
                foreach (Node existingNode in existingNodes)
                {
                    existingNode.HitEv = double.MinValue;
                    existingNode.StandEv = double.MinValue;
                    existingNode.DoubleEv = double.MinValue;
                    existingNode.SplitEv = double.MinValue;
                    existingNode.Ev = double.MinValue;
                }

                SetPlayerNodeEv(playerNode, rootNode, dealerNode.Ranks.First(), existingNodes, null);

                double playerHitEv = playerNode.HitEv;
                double playerStandEv = playerNode.StandEv;
                double playerDoubleEv = playerNode.DoubleEv;
                double playerEv = playerNode.Ev;

                bool isSplitPossible = playerNode.Ranks[0] == playerNode.Ranks[1];
                double playerSplitEv = double.MinValue;

                if (isSplitPossible)
                {
                    foreach (Node existingNode in existingNodes)
                    {
                        existingNode.HitEv = double.MinValue;
                        existingNode.StandEv = double.MinValue;
                        existingNode.DoubleEv = double.MinValue;
                        existingNode.SplitEv = double.MinValue;
                        existingNode.Ev = double.MinValue;
                    }

                    int splitRank = playerNode.Ranks[0];
                    int splitRankIndex = Rank.ToIndex(splitRank);

                    Node splitNode = rootNode.Children[splitRankIndex];

                    IEnumerable<int> deadRanks = new int[] { splitRank };

                    SetPlayerNodeEv(splitNode, rootNode, dealerNode.Ranks.First(), existingNodes, deadRanks);

                    double firstSplitEv = splitNode.Ev;

                    // Not allowed to hit further
                    if (splitRank == Rank.Ace)
                    {
                        Debug.Assert(splitNode.Children is not null);

                        double aceSplitEv = 0;

                        for (int rankIndex = 0; rankIndex < Rank.Amount; rankIndex++)
                        {
                            Node splitNodeChild = splitNode.Children[rankIndex];
                            IEnumerable<int> allDeadRanks = Enumerable.Concat(splitNode.Ranks, deadRanks);

                            int rank = Rank.ToRank(rankIndex);
                            double childRatio = GetOdds(new int[] { rank }, allDeadRanks);

                            aceSplitEv += splitNodeChild.StandEv * childRatio;
                        }

                        firstSplitEv = aceSplitEv;
                    }

                    // Approx
                    playerSplitEv = firstSplitEv * 2;

                    if (playerSplitEv > playerEv)
                    {
                        playerEv = playerSplitEv;
                    }
                }

                StringBuilder builder = new();

                foreach (int rank in playerNode.Ranks)
                {
                    builder.Append(Rank.ToShortString(rank));
                }

                builder.Append('-');
                builder.Append(Rank.ToShortString(dealerNode.Ranks.First()));
                builder.Append($": {playerEv,10:N5}");

                double situationOdds = GetOdds(Enumerable.Concat(playerNode.Ranks, dealerNode.Ranks), null);

                // Compensate odds for sorting
                if (playerNode.Ranks[0] != playerNode.Ranks[1])
                {
                    situationOdds *= 2;
                }

                builder.Append($" {situationOdds * 100,10:N5}%");

                string decision;

                if (playerHitEv == playerEv)
                {
                    decision = "Hit";
                }
                else if (playerStandEv == playerEv)
                {
                    decision = "Stand";
                }
                else if (playerDoubleEv == playerEv)
                {
                    decision = "Double";
                }
                else if (playerSplitEv == playerEv)
                {
                    decision = "Split";
                }
                else
                {
                    decision = "Blackjack";
                }

                builder.Append($" -> {decision}");

                totalEv += playerEv * situationOdds;
                totalOdds += situationOdds;

                Console.WriteLine(builder.ToString());
            }

            Console.WriteLine();
            Console.WriteLine($"Total EV: {totalEv}");
            Console.WriteLine($"Total Odds: {totalOdds}");

            return rootNode;
        }

        private static double GetEv(Node playerNode, Node rootNode, int dealerRank, Node[] existingNodes, IEnumerable<int>? deadRanks, double multiplier)
        {
            Debug.Assert(rootNode.Children is not null);

            int playerSum = playerNode.Sum;
            int[] playerRanks = playerNode.Ranks.OrderBy(c => c).ToArray();

            if (deadRanks is not null)
            {
                deadRanks = deadRanks.OrderBy(c => c);
            }

            string currentKey = GetDealerSumOddsKey(playerRanks, dealerRank, deadRanks);

            if (!_dealerSumOdds.ContainsKey(currentKey))
            {
                SetOdds(existingNodes, playerRanks, deadRanks);

                for (int rankIndex = 0; rankIndex < Rank.Amount; rankIndex++)
                {
                    Node dealerNode = rootNode.Children[rankIndex];

                    double[] sumOdds = CountDealerSum(dealerNode, new double[7]);
                    double totalSumOdds = sumOdds.Sum();

                    for (int i = 0; i < sumOdds.Length; i++)
                    {
                        sumOdds[i] /= totalSumOdds;
                    }

                    string key = GetDealerSumOddsKey(playerRanks, dealerNode.Ranks.First(), deadRanks);
                    _dealerSumOdds.Add(key, sumOdds);
                }
            }

            double[] currentSumOdds = _dealerSumOdds[currentKey];
            double ev = GetEvFromResult(playerSum, playerRanks.Length, currentSumOdds, multiplier);
            return ev;
        }

        private static void SetPlayerNodeEv(Node playerNode, Node rootNode, int dealerRank, Node[] existingNodes, IEnumerable<int>? deadRanks)
        {
            if (playerNode.Ev != double.MinValue)
            {
                return;
            }

            if (playerNode.Children is null)
            {
                if (playerNode.Kind == NodeKind.Stand)
                {
                    playerNode.Ev = GetEv(playerNode, rootNode, dealerRank, existingNodes, deadRanks, 1);
                }
                else if (playerNode.Kind == NodeKind.Bust)
                {
                    playerNode.Ev = -1;
                }
                else
                {
                    throw new InvalidOperationException("Internal error.");
                }
            }
            else
            {
                double hitEv = 0;

                for (int i = 0; i < playerNode.Children.Length; i++)
                {
                    Node child = playerNode.Children[i];
                    SetPlayerNodeEv(child, rootNode, dealerRank, existingNodes, deadRanks);

                    IEnumerable<int> allDeadRanks = deadRanks is null ? playerNode.Ranks : Enumerable.Concat(playerNode.Ranks, deadRanks);

                    int rank = Rank.ToRank(i);
                    double childRatio = GetOdds(new int[] { rank }, allDeadRanks);
                    double childHitEv = child.Ev * childRatio;
                    hitEv += childHitEv;
                }

                playerNode.HitEv = hitEv;
                playerNode.StandEv = GetEv(playerNode, rootNode, dealerRank, existingNodes, deadRanks, 1);
                playerNode.Ev = Math.Max(playerNode.HitEv, playerNode.StandEv);

                bool isDoublePossible = playerNode.Ranks.Length == 2 && playerNode.Sum != 21;

                if (isDoublePossible)
                {
                    double doubleEv = 0;

                    for (int i = 0; i < playerNode.Children.Length; i++)
                    {
                        Node child = playerNode.Children[i];
                        double childDoubleEv = GetChildDoubleEv(child, rootNode, dealerRank, existingNodes, deadRanks);

                        int rank = Rank.ToRank(i);
                        double childRatio = GetOdds(new int[] { rank }, playerNode.Ranks);
                        double childHitEv = childDoubleEv * childRatio;
                        doubleEv += childHitEv;
                    }

                    playerNode.DoubleEv = doubleEv;

                    if (playerNode.DoubleEv > playerNode.Ev)
                    {
                        playerNode.Ev = playerNode.DoubleEv;
                    }
                }
            }
        }

        private static double GetChildDoubleEv(Node playerNode, Node rootNode, int dealerRank, Node[] existingNodes, IEnumerable<int>? deadRanks)
        {
            if (playerNode.Kind == NodeKind.Bust)
            {
                return -2;
            }

            return GetEv(playerNode, rootNode, dealerRank, existingNodes, deadRanks, 2);
        }

        private static readonly Dictionary<string, double[]> _dealerSumOdds = new();

        private static double GetEvFromResult(int playerSum, int rankAmount, double[] sumOdds, double multiplier)
        {
            double totalEv = 0;
            bool isPlayerBlackjack = playerSum == 21 && rankAmount == 2;

            if (isPlayerBlackjack)
            {
                for (int i = 0; i < 6; i++)
                {
                    totalEv += sumOdds[i] * multiplier;
                }

                totalEv *= 1.5;
            }
            else
            {
                // Dealer bust
                totalEv += sumOdds[0] * multiplier;

                for (int i = 1; i < 7; i++)
                {
                    double sumOddEntry = sumOdds[i];
                    int dealerSum = i + 16;

                    if (playerSum > dealerSum)
                    {
                        totalEv += sumOddEntry * multiplier;
                    }
                    else if (dealerSum > playerSum)
                    {
                        totalEv -= sumOddEntry * multiplier;
                    }
                }
            }

            return totalEv;
        }

        private static string GetDealerSumOddsKey(int[] ranks, int dealerRank, IEnumerable<int>? deadRanks)
        {
            StringBuilder builder = new();

            builder.Append(Rank.ToShortString(dealerRank));
            builder.Append('-');

            IEnumerable<int> allDeadRanks;

            if (deadRanks is not null)
            {
                allDeadRanks = Enumerable.Concat(ranks, deadRanks).OrderBy(c => c);
            }
            else
            {
                allDeadRanks = ranks;
            }

            foreach (int deadRank in allDeadRanks)
            {
                builder.Append(Rank.ToShortString(deadRank));
            }

            return builder.ToString();
        }

        private static double GetOdds(IEnumerable<int> ranks, IEnumerable<int>? deadRanks)
        {
            const int DeckAmount = 8;

            int[] rankAmounts = new int[Rank.Amount];
            int totalRankAmount = 0;

            for (int i = 0; i < Rank.Amount; i++)
            {
                int frequency = i == Rank.TenIndex ? 16 : 4;
                int rankAmount = frequency * DeckAmount;
                rankAmounts[i] = rankAmount;
                totalRankAmount += rankAmount;
            }

            if (deadRanks is not null)
            {
                foreach (int deadRank in deadRanks)
                {
                    int rankIndex = Rank.ToIndex(deadRank);
                    rankAmounts[rankIndex]--;

                    if (rankAmounts[rankIndex] < 0)
                    {
                        return 0;
                    }

                    totalRankAmount--;
                }
            }

            double odds = 1;

            foreach (int rank in ranks)
            {
                int rankIndex = Rank.ToIndex(rank);
                int rankAmount = rankAmounts[rankIndex];

                odds *= rankAmount / (double)totalRankAmount;

                rankAmounts[rankIndex]--;

                if (rankAmounts[rankIndex] < 0)
                {
                    return 0;
                }

                totalRankAmount--;
            }

            return odds;
        }

        private static void SetOdds(IEnumerable<Node> allNodes, IEnumerable<int> deadPlayerRanks, IEnumerable<int>? otherDeadCards)
        {
            IEnumerable<int> deadCards = otherDeadCards is null ?
                deadPlayerRanks :
                Enumerable.Concat(deadPlayerRanks, otherDeadCards).OrderBy(c => c);

            foreach (Node node in allNodes)
            {
                node.Odds = GetOdds(node.Ranks, deadCards);
            }
        }

        private static double[] CountDealerSum(Node node, double[] dealerSumOdds)
        {
            if (node.Sum > 16)
            {
                // Index of 0 stands for bust
                int index = 0;

                if (node.Sum == 21 && node.Ranks.Length == 2)
                {
                    // Dealer Blackjack
                    index = 6;
                }
                else if (node.Sum <= 21)
                {
                    index = node.Sum - 16;
                }

                dealerSumOdds[index] += node.Odds;
            }
            else
            {
                Debug.Assert(node.Children is not null);

                foreach (Node child in node.Children)
                {
                    CountDealerSum(child, dealerSumOdds);
                }
            }

            return dealerSumOdds;
        }

        private static Node[] CreateHitChildren(Node node)
        {
            Node[] hitChildren = new Node[Rank.Amount];

            for (int rankIndex = 0; rankIndex < Rank.Amount; rankIndex++)
            {
                int rank = Rank.ToRank(rankIndex);

                Node hitChild = CreatePlayerHitChild(node, rank);
                hitChildren[rankIndex] = hitChild;
            }

            return hitChildren;
        }

        private static Node CreatePlayerHitChild(Node node, int rank)
        {
            int[] hitChildPlayerRanks = node.Ranks.Append(rank).ToArray();
            Node hitChild = new(hitChildPlayerRanks);
            return hitChild;
        }
    }
}
