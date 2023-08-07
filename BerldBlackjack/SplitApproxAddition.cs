//using System.Diagnostics;

//namespace BerldBlackjack
//{
//    public static class SplitApproxAddition
//    {
//        public static void Add(List<BaseNode> baseNodes)
//        {
//            for (int i = 0; i < baseNodes.Count; i++)
//            {
//                BaseNode baseNode = baseNodes[i];

//                if (baseNode.Children is null) continue;

//                int splitRank = baseNode.PlayerRanks[0];

//                if (baseNode.PlayerRanks[1] == splitRank)
//                {
//                    Node splitNode = new(NodeKind.Split, baseNode.PlayerRanks.ToArray(), baseNode.DealerRanks.ToArray());

//                    List<BaseNode> splitRankNodes = baseNodes
//                        .Where(c =>
//                            c.PlayerRanks.Contains(splitRank) &&
//                            c.DealerRanks[0] == baseNode.DealerRanks[0])
//                        .ToList();

//                    List<(double ratio, BaseNode node)> splitRankNodeRatios = new();

//                    int[] aliveRankAmounts = NodeUtility.GetAliveRankAmounts(splitNode);
//                    int totalAliveRankAmount = aliveRankAmounts.Sum();

//                    foreach (BaseNode splitRankNode in splitRankNodes)
//                    {
//                        bool isPair = splitRankNode.PlayerRanks[0] == splitRankNode.PlayerRanks[1];
//                        int otherRank = isPair ? splitRank : splitRankNode.PlayerRanks.First(c => c != splitRank);
//                        int otherRankIndex = Rank.ToIndex(otherRank);
//                        int rankAmount = aliveRankAmounts[otherRankIndex];
//                        double ratio = rankAmount / (double)totalAliveRankAmount;
//                        splitRankNodeRatios.Add((ratio, splitRankNode));
//                    }

//                    double evSum = splitRankNodeRatios.Sum(c => c.ratio * c.node.Ev);

//                    if (splitRank == Rank.Ace)
//                    {
//                        evSum = splitRankNodeRatios.Sum(c => FindEquivalentEv(c.node, baseNode, baseNodes, c.ratio));
//                    }

//                    double ratioSum = splitRankNodeRatios.Sum(c => c.ratio);
//                    double averageEv = evSum / ratioSum;

//                    splitNode.Ev = averageEv * 2;

//                    baseNode.Children = baseNode.Children.Append((1, splitNode)).ToArray();

//                    if (splitNode.Ev > baseNode.Ev)
//                    {
//                        baseNode.Ev = splitNode.Ev;
//                    }
//                }
//            }
//        }

//        private static double FindEquivalentEv(BaseNode splitRankNode, BaseNode baseNode, List<BaseNode> baseNodes, double ratio)
//        {
//            double similarChildEv;

//            if (splitRankNode.Children is null)
//            {
//                Node baseNode9A = baseNodes.First(c =>
//                {
//                    return
//                        c.PlayerRanks[0] == Rank.Nine &&
//                        c.PlayerRanks[1] == Rank.Ace &&
//                        c.DealerRanks[0] == baseNode.DealerRanks[0] &&
//                        c.Children is not null;
//                });

//                Debug.Assert(baseNode9A.Children is not null);
//                Node baseNode9AHit = baseNode9A.Children.First(c => c.child.Kind == NodeKind.Hit).child;

//                Debug.Assert(baseNode9AHit.Children is not null);
//                Node baseNodeA9A = baseNode9AHit.Children.First(c => c.child.PlayerSum == 21).child;

//                similarChildEv = baseNodeA9A.Ev;
//            }
//            else
//            {
//                Node splitRankNodeHit = splitRankNode.Children.First(c => c.child.Kind == NodeKind.Hit).child;

//                Debug.Assert(splitRankNodeHit.Children is not null);

//                IEnumerable<(double ratio, Node child)> standChildren = splitRankNodeHit.Children.Select(c =>
//                {
//                    if (c.child.Children is null)
//                    {
//                        return c;
//                    }
//                    else
//                    {
//                        (double _, Node standChild) = c.child.Children.First(c => c.child.Kind == NodeKind.Stand);
//                        return (c.ratio, standChild);
//                    }
//                });

//                double ev = standChildren.Sum(c => c.ratio * c.child.Ev);

//                //Node doubleChild = GetDoubleChild(splitRankNode);
//                //similarChildEv = doubleChild.Ev / 2;

//                similarChildEv = ev;
//            }

//            return similarChildEv * ratio;
//        }

//        private static Node GetDoubleChild(Node node)
//        {
//            Debug.Assert(node.Children is not null);
//            return node.Children.First(c => c.child.Kind == NodeKind.Double).child;
//        }
//    }
//}
