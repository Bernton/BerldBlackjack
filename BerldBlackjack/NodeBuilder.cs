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
                    existingNode.Ev = double.MinValue;
                }

                SetPlayerEv(playerNode, dealerNode, existingNodes);

                StringBuilder builder = new();

                foreach (int rank in playerNode.Ranks)
                {
                    builder.Append(Rank.ToShortString(rank));
                }

                builder.Append('-');
                builder.Append(Rank.ToShortString(dealerNode.Ranks.First()));
                builder.Append($": {playerNode.Ev,10:N5}");

                double situationOdds = GetOdds(Enumerable.Concat(playerNode.Ranks, dealerNode.Ranks), null);

                // Compensate odds for sorting
                if (playerNode.Ranks[0] != playerNode.Ranks[1])
                {
                    situationOdds *= 2;
                }

                builder.Append($" {situationOdds * 100,10:N5}%");

                totalEv += playerNode.Ev * situationOdds;
                totalOdds += situationOdds;

                Console.WriteLine(builder.ToString());
            }

            Console.WriteLine();
            Console.WriteLine($"Total EV: {totalEv}");
            Console.WriteLine($"Total Odds: {totalOdds}");

            return rootNode;
        }

        private static double GetEv(int playerSum, int[] ranks, Node dealerNode, Node[] existingNodes)
        {
            int dealerRank = dealerNode.Ranks.First();
            ranks = ranks.OrderBy(c => c).ToArray();

            string key = GetRanksKey(dealerRank, ranks);
            double[] sumOdds;

            if (_dealerSumOdds.ContainsKey(key))
            {
                sumOdds = _dealerSumOdds[key];
            }
            else
            {
                SetOdds(existingNodes, ranks);

                sumOdds = CountDealerSum(dealerNode, new double[7]);
                double totalSumOdds = sumOdds.Sum();

                for (int i = 0; i < sumOdds.Length; i++)
                {
                    sumOdds[i] /= totalSumOdds;
                }

                _dealerSumOdds.Add(key, sumOdds);
            }

            double ev = GetEvFromResult(playerSum, ranks.Length, sumOdds);
            return ev;
        }

        private static void SetPlayerEv(Node node, Node dealerNode, Node[] existingNodes)
        {
            if (node.Ev != double.MinValue)
            {
                return;
            }

            if (node.Children is null)
            {
                if (node.Kind == NodeKind.Stand)
                {
                    node.Ev = GetEv(node.Sum, node.Ranks, dealerNode, existingNodes);
                }
                else if (node.Kind == NodeKind.Bust)
                {
                    node.Ev = -1;
                }
            }
            else
            {
                double hitEv = 0;

                foreach (Node child in node.Children)
                {
                    SetPlayerEv(child, dealerNode, existingNodes);

                    double childHitEv = child.Ev * child.Odds / node.Odds;

                    if (!double.IsNaN(childHitEv))
                    {
                        hitEv += childHitEv;
                    }
                }

                node.HitEv = hitEv;
                node.StandEv = GetEv(node.Sum, node.Ranks, dealerNode, existingNodes);
                node.Ev = Math.Max(node.HitEv, node.StandEv);
            }
        }

        private static readonly Dictionary<string, double[]> _dealerSumOdds = new();

        private static double GetEvFromResult(int playerSum, int rankAmount, double[] sumOdds)
        {
            bool isPlayerBlackjack = playerSum == 21 && rankAmount == 2;
            double totalEv = 0;

            if (isPlayerBlackjack)
            {
                for (int i = 0; i < 6; i++)
                {
                    totalEv += sumOdds[i];
                }

                totalEv *= 1.5;
            }
            else
            {
                for (int i = 0; i < 7; i++)
                {
                    bool isDealerBlackjack = i == 7;
                    bool isDealerBust = i == 0;
                    int dealerSum = i + 16;
                    double iSumOdds = sumOdds[i];

                    if (isDealerBust || playerSum > dealerSum)
                    {
                        totalEv += iSumOdds;
                    }
                    else if (isDealerBlackjack || dealerSum > playerSum)
                    {
                        totalEv -= iSumOdds;
                    }
                }
            }

            return totalEv;
        }

        private static string GetRanksKey(int dealerRank, int[] ranks)
        {
            StringBuilder builder = new();

            builder.Append(Rank.ToShortString(dealerRank));
            builder.Append('-');

            for (int i = 0; i < ranks.Length; i++)
            {
                builder.Append(Rank.ToShortString(ranks[i]));
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

        private static void SetOdds(IEnumerable<Node> allNodes, IEnumerable<int> deadPlayerRanks)
        {
            foreach (Node node in allNodes)
            {
                node.Odds = GetOdds(node.Ranks, deadPlayerRanks);
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
