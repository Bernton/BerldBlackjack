namespace BerldBlackjack
{
    public class NodeEvaluator
    {
        private static readonly Dictionary<string, double> _dealerResultMemoization = new();


        public static void Evaluate(IEnumerable<Node> nodesToEvaluate)
        {
            foreach (Node nodeToEvaluate in nodesToEvaluate)
            {
                EvaluateNode(nodeToEvaluate);
                Console.WriteLine($"{nodeToEvaluate}: {nodeToEvaluate.Ev}");
            }
        }

        internal static double EvaluateNode(Node node)
        {
            if (node.Ev != double.MinValue)
            {
                return node.Ev;
            }

            if (node.Children is null)
            {
                if (node.Kind == NodeKind.Bust)
                {
                    node.Ev = -1;
                }
                else if (node.Kind == NodeKind.DoubleBust)
                {
                    node.Ev = -2;
                }
                else if (node.Kind == NodeKind.Stand)
                {
                    node.Ev = EvaluateDealerResult(node);
                }
                else if (node.Kind == NodeKind.DoubleStand)
                {
                    node.Ev = EvaluateDealerResult(node) * 2;
                }
                else
                {
                    throw new InvalidOperationException("Internal error.");
                }
            }
            else
            {
                if (node.Kind == NodeKind.PlayerDecision)
                {
                    double childrenEvMax = double.MinValue;

                    foreach ((double _, Node child) in node.Children)
                    {
                        double childEv = EvaluateNode(child);

                        if (childEv > childrenEvMax)
                        {
                            childrenEvMax = childEv;
                        }
                    }

                    node.Ev = childrenEvMax;
                }
                else
                {
                    double childrenEvSum = 0;

                    foreach ((double ratio, Node child) in node.Children)
                    {
                        double childEv = EvaluateNode(child);
                        childrenEvSum += childEv * ratio;
                    }

                    node.Ev = childrenEvSum;
                }
            }

            return node.Ev;
        }

        private static double EvaluateDealerResult(Node node)
        {
            string memoizationKey = node.GetStateKey();

            if (_dealerResultMemoization.ContainsKey(memoizationKey))
            {
                return _dealerResultMemoization[memoizationKey];
            }

            if (node.DealerRanks.Length >= 2)
            {
                bool playerHasBlackjack = node.IsBlackjack;
                bool dealerHasBlackjack = node.DealerSum == 21 && node.DealerRanks.Length == 2;

                if (playerHasBlackjack)
                {
                    if (dealerHasBlackjack)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1.5;
                    }
                }
                else if (dealerHasBlackjack)
                {
                    return -1;
                }

                if (node.DealerSum > 21)
                {
                    return 1;
                }
                else if (node.DealerSum > 16)
                {
                    return EvFromDealerResult(node.PlayerSum, node.DealerSum);
                }
            }

            List<(double ratio, Node child)> hitChildren = new();
            int[] aliveRankAmounts = NodeUtility.GetAliveRankAmounts(node);
            int deckAmount = aliveRankAmounts.Sum();

            for (int i = 0; i < Rank.Amount; i++)
            {
                if (aliveRankAmounts[i] == 0)
                {
                    continue;
                }

                Node hitChild = CreateDealerHitChild(node, Rank.ToRank(i));
                double ratio = aliveRankAmounts[i] / (double)deckAmount;
                hitChildren.Add((ratio, hitChild));
            }

            double childrenEv = 0;

            foreach ((double ratio, Node hitChild) in hitChildren)
            {
                childrenEv += ratio * EvaluateDealerResult(hitChild);
            }

            _dealerResultMemoization.Add(memoizationKey, childrenEv);
            return childrenEv;
        }

        private static Node CreateDealerHitChild(Node node, int rank)
        {
            int[] hitChildDealerRanks = node.DealerRanks.Append(rank).ToArray();
            Node hitChild = new(node.Kind, node.PlayerRanks.ToArray(), hitChildDealerRanks, node.SplitRank, node.RemovedRanks);
            return hitChild;
        }

        private static double EvFromDealerResult(int playerSum, int dealerSum)
        {
            return Math.Sign(playerSum - dealerSum);
        }
    }
}
