namespace BerldBlackjack.Split
{
    public class SplitNodeBuilder
    {
        public static SplitNode Build(int splitRank)
        {
            SplitNode rootNode = new(new int[] { splitRank }, new int[] { splitRank })
            {
                FirstKind = SplitNodeKind.Hit,
                SecondKind = SplitNodeKind.Hit
            };

            Stack<SplitNode> nodesToProcess = new();
            nodesToProcess.Push(rootNode);

            while (nodesToProcess.Count > 0)
            {
                SplitNode nodeToProcess = nodesToProcess.Pop();

                List<SplitNode> children = CreateChildren(nodeToProcess);
                nodeToProcess.Children = children;

                foreach (SplitNode child in children)
                {
                    if (!child.IsFinished)
                    {
                        nodesToProcess.Push(child);
                    }
                }
            }

            return rootNode;
        }

        private static List<SplitNode> CreateChildren(SplitNode node)
        {
            List<SplitNode> children = new();

            if (node.Kind == SplitNodeKind.Decision)
            {
                SplitNode hitChild = CreateHitChild(node);
                children.Add(hitChild);
            }
            else if (node.Kind == SplitNodeKind.Hit)
            {

            }
            else if (node.Kind == SplitNodeKind.Stand)
            {

            }
            else if (node.Kind == SplitNodeKind.Double)
            {

            }
            else
            {
                throw new InvalidOperationException("Internal error.");
            }

            return children;
        }

        private static SplitNode CreateHitChild(SplitNode parent)
        {

        }

        internal static int[] GetAliveRankAmounts()
        {
            const int DeckAmount = 8;
            int[] aliveRankAmounts = new int[Rank.Amount];

            for (int i = 0; i < Rank.Amount; i++)
            {
                aliveRankAmounts[i] = Deck.GetFrequencyFromIndex(i) * DeckAmount;
            }

            return aliveRankAmounts;
        }

        internal static int[] GetAliveRankAmounts(SplitNode node)
        {
            int[] aliveRankAmounts = GetAliveRankAmounts();
            IEnumerable<int> allDeadRanks = Enumerable.Concat(node.FirstRanks, node.SecondRanks);

            foreach (int deadRank in allDeadRanks)
            {
                int rankIndex = Rank.ToIndex(deadRank);
                aliveRankAmounts[rankIndex]--;
            }

            return aliveRankAmounts;
        }
    }
}
