namespace BerldBlackjack
{
    public static class NodeBuilder
    {
        public static List<BaseNode> Build()
        {
            List<BaseNode> baseNodes = new();

            for (int playerRankI1 = 0; playerRankI1 < Rank.Amount; playerRankI1++)
            {
                for (int playerRankI2 = playerRankI1; playerRankI2 < Rank.Amount; playerRankI2++)
                {
                    for (int dealerRankI = 0; dealerRankI < Rank.Amount; dealerRankI++)
                    {
                        int playerRank1 = Rank.ToRank(playerRankI1);
                        int playerRank2 = Rank.ToRank(playerRankI2);
                        int dealerRank = Rank.ToRank(dealerRankI);

                        int[] allRanks = new int[] { playerRank1, playerRank2, dealerRank };
                        int[] playerRanks = new int[] { playerRank1, playerRank2 };
                        int[] dealerRanks = new int[] { dealerRank };

                        double ratio = 1;
                        int[] aliveRankAmounts = NodeUtility.GetAliveRankAmounts();

                        foreach (int rank in allRanks)
                        {
                            int rankIndex = Rank.ToIndex(rank);
                            int totalAliveRankAmount = aliveRankAmounts.Sum();
                            int rankAmount = aliveRankAmounts[rankIndex];
                            ratio = ratio * rankAmount / totalAliveRankAmount;
                            aliveRankAmounts[rankIndex]--;
                        }

                        // Multiply by 2 to account for order
                        if (playerRank1 != playerRank2)
                        {
                            ratio *= 2;
                        }

                        BaseNode baseNode = new(NodeKind.PlayerDecision, playerRanks, dealerRanks, ratio);
                        CheckSetBustOrStand(baseNode);

                        baseNodes.Add(baseNode);
                    }
                }
            }

            Dictionary<string, Node> builtNodes = new();
            Stack<Node> nodesToBuild = new(baseNodes);

            while (nodesToBuild.Any())
            {
                Node node = nodesToBuild.Pop();

                (double ratio, Node)[]? children = BuildNode(node);
                node.Children = children;

                if (children is not null)
                {
                    for (int i = 0; i < children.Length; i++)
                    {
                        (double ratio, Node child) = children[i];
                        string childKey = child.ToString();

                        if (builtNodes.ContainsKey(childKey))
                        {
                            Node existingNode = builtNodes[childKey];
                            children[i] = (ratio, existingNode);
                        }
                        else
                        {
                            builtNodes.Add(childKey, child);
                            nodesToBuild.Push(child);
                        }
                    }
                }
            }

            return baseNodes;
        }

        private static void CheckSetBustOrStand(Node node)
        {
            if (node.Kind == NodeKind.PlayerDecision)
            {
                if (node.PlayerSum > 21)
                {
                    node.Kind = NodeKind.Bust;
                }

                if (node.PlayerSum == 21)
                {
                    node.Kind = NodeKind.Stand;
                }
            }

            if (node.Kind == NodeKind.DoubleStand)
            {
                if (node.PlayerSum > 21)
                {
                    node.Kind = NodeKind.DoubleBust;
                }
            }
        }

        private static (double ratio, Node)[]? BuildNode(Node node)
        {
            if (node.Kind == NodeKind.PlayerDecision)
            {
                List<(double ratio, Node)> children = new();

                Node hitChild = ConstructClone(node);
                hitChild.Kind = NodeKind.Hit;
                children.Add((1, hitChild));

                bool isStandPossible = node.PlayerSum > 11;

                if (isStandPossible)
                {
                    Node standChild = ConstructClone(node);
                    standChild.Kind = NodeKind.Stand;
                    children.Add((1, standChild));
                }

                //bool isDoublePossible =
                //    node.PlayerRanks.Length == 2 &&
                //    !node.PlayerRanks.Any(c => c == Rank.Ace) &&
                //    node.PlayerSum <= 11 && node.PlayerSum >= 9;

                //if (isDoublePossible)
                //{
                //    Node doubleChild = ConstructClone(node);
                //    doubleChild.Kind = NodeKind.Double;
                //    children.Add((1, doubleChild));
                //}

                return children.ToArray();
            }

            if (node.Kind == NodeKind.Hit || node.Kind == NodeKind.Double)
            {
                List<(double ratio, Node)> hitChildren = new();
                int[] aliveRankAmounts = NodeUtility.GetAliveRankAmounts(node);
                int totalAliveRankAmount = aliveRankAmounts.Sum();

                for (int i = 0; i < Rank.Amount; i++)
                {
                    if (aliveRankAmounts[i] == 0)
                    {
                        continue;
                    }

                    Node hitChild = CreatePlayerHitChild(node, Rank.ToRank(i));
                    double ratio = aliveRankAmounts[i] / (double)totalAliveRankAmount;
                    hitChild.Kind = node.Kind == NodeKind.Hit ? NodeKind.PlayerDecision : NodeKind.DoubleStand;
                    CheckSetBustOrStand(hitChild);
                    hitChildren.Add((ratio, hitChild));
                }

                return hitChildren.ToArray();
            }

            return null;
        }

        private static Node ConstructClone(Node node)
        {
            return new(node.Kind, node.PlayerRanks.ToArray(), node.DealerRanks.ToArray());
        }

        private static Node CreatePlayerHitChild(Node node, int rank)
        {
            int[] hitChildPlayerRanks = node.PlayerRanks.Append(rank).ToArray();
            Node hitChild = new(NodeKind.PlayerDecision, hitChildPlayerRanks, node.DealerRanks.ToArray());
            return hitChild;
        }
    }
}