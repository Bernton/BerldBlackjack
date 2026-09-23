namespace BerldBlackjack
{
    // Adds a Split child to every pair.
    //
    // Each split hand is its own tree that knows its own cards, the dealer's up card and the pair cards
    // that started the other hands. By linearity of expectation the split is worth the sum of its hands,
    // so the hands never have to be evaluated together. Without resplitting this is exact: the cards the
    // other hand draws change the dealer's cards, but not their distribution on average.
    //
    // Resplitting is handled by working out how many hands the split ends with: every time a hand draws
    // another pair card it becomes a new hand, until the maximum is reached. Each hand is then valued for
    // the final number of hands, which is the only approximation.
    public static class SplitEvaluator
    {
        public static void Add(List<BaseNode> baseNodes)
        {
            List<SplitCase> splitCases = baseNodes
                .Where(c => c.PlayerRanks.Length == 2 && c.PlayerRanks[0] == c.PlayerRanks[1])
                .Where(c => GetMaxHandAmount(c.PlayerRanks[0]) >= 2)
                .Select(c => new SplitCase(c))
                .ToList();

            NodeBuilder.BuildTree(splitCases.SelectMany(c => c.GetSplitHands()));

            foreach (SplitCase splitCase in splitCases)
            {
                BaseNode pairNode = splitCase.PairNode;

                Node splitNode = new(NodeKind.Split, pairNode.PlayerRanks.ToArray(), pairNode.DealerRanks.ToArray())
                {
                    Ev = splitCase.Evaluate()
                };

                pairNode.Children = (pairNode.Children ?? Array.Empty<(double, Node)>()).Append((1, splitNode)).ToArray();

                if (splitNode.Ev > pairNode.Ev)
                {
                    pairNode.Ev = splitNode.Ev;
                }
            }
        }

        private static int GetMaxHandAmount(int splitRank)
        {
            return splitRank == Rank.Ace && !Rules.IsResplitAcesAllowed ? Math.Min(2, Rules.MaxSplitHands) : Rules.MaxSplitHands;
        }

        private class SplitCase
        {
            public BaseNode PairNode { get; }

            private readonly int _splitRank;
            private readonly int _dealerRank;
            private readonly int _maxHandAmount;

            // Deck without the dealer's up card
            private readonly int _pairAmount;
            private readonly int _totalAmount;

            // Indexed by hand amount: the split hands for a deck missing that many pair cards
            private readonly List<(double ratio, Node splitHand)>[] _nonPairHands;
            private readonly Node?[] _pairHands;

            public SplitCase(BaseNode pairNode)
            {
                PairNode = pairNode;
                _splitRank = pairNode.PlayerRanks[0];
                _dealerRank = pairNode.DealerRanks[0];
                _maxHandAmount = GetMaxHandAmount(_splitRank);

                int[] aliveRankAmounts = NodeUtility.GetAliveRankAmounts();
                aliveRankAmounts[Rank.ToIndex(_dealerRank)]--;
                _pairAmount = aliveRankAmounts[Rank.ToIndex(_splitRank)];
                _totalAmount = aliveRankAmounts.Sum();

                _nonPairHands = new List<(double ratio, Node splitHand)>[_maxHandAmount + 1];
                _pairHands = new Node?[_maxHandAmount + 1];

                for (int handAmount = 2; handAmount <= Math.Min(_maxHandAmount, _pairAmount); handAmount++)
                {
                    _nonPairHands[handAmount] = new();

                    int[] handAliveRankAmounts = NodeUtility.GetAliveRankAmounts(CreateSplitHand(0, handAmount));
                    int handTotalAmount = handAliveRankAmounts.Sum();

                    for (int i = 0; i < Rank.Amount; i++)
                    {
                        int secondRank = Rank.ToRank(i);
                        int secondAmount = handAliveRankAmounts[i];

                        if (secondAmount == 0)
                        {
                            continue;
                        }

                        if (secondRank != _splitRank)
                        {
                            _nonPairHands[handAmount].Add((secondAmount / (double)handTotalAmount, CreateSplitHand(secondRank, handAmount)));
                        }
                        else if (handAmount == _maxHandAmount)
                        {
                            // A pair card that can no longer be resplit is played as it is
                            _pairHands[handAmount] = CreateSplitHand(secondRank, handAmount);
                        }
                    }
                }
            }

            public IEnumerable<Node> GetSplitHands()
            {
                return _nonPairHands
                    .Where(c => c is not null)
                    .SelectMany(c => c.Select(d => d.splitHand))
                    .Concat(_pairHands.OfType<Node>());
            }

            public double Evaluate()
            {
                // Two hands, each missing its second card; the two pair cards are out of the deck
                return EvaluateHands(2, 2, 2, 2, 0);
            }

            private double EvaluateHands(int pendingAmount, int handAmount, int pairOutAmount, int cardOutAmount, int pairHandAmount)
            {
                if (pendingAmount == 0)
                {
                    double ev = (handAmount - pairHandAmount) * EvaluateNonPairHand(handAmount);

                    if (pairHandAmount > 0)
                    {
                        Node pairHand = _pairHands[handAmount] ?? throw new InvalidOperationException("Internal error.");
                        ev += pairHandAmount * NodeEvaluator.EvaluateNode(pairHand);
                    }

                    return ev;
                }

                double pairChance = (_pairAmount - pairOutAmount) / (double)(_totalAmount - cardOutAmount);
                double childrenEv = (1 - pairChance) * EvaluateHands(pendingAmount - 1, handAmount, pairOutAmount, cardOutAmount + 1, pairHandAmount);

                if (pairChance > 0)
                {
                    if (handAmount < _maxHandAmount)
                    {
                        // Resplit: the pair card starts a new hand, and the current hand still needs its second card
                        childrenEv += pairChance * EvaluateHands(pendingAmount + 1, handAmount + 1, pairOutAmount + 1, cardOutAmount + 1, pairHandAmount);
                    }
                    else
                    {
                        childrenEv += pairChance * EvaluateHands(pendingAmount - 1, handAmount, pairOutAmount + 1, cardOutAmount + 1, pairHandAmount + 1);
                    }
                }

                return childrenEv;
            }

            private double EvaluateNonPairHand(int handAmount)
            {
                List<(double ratio, Node splitHand)> nonPairHands = _nonPairHands[handAmount];
                double evSum = nonPairHands.Sum(c => c.ratio * NodeEvaluator.EvaluateNode(c.splitHand));
                double ratioSum = nonPairHands.Sum(c => c.ratio);
                return evSum / ratioSum;
            }

            // A split hand of the pair card and the second card; 0 as second rank creates the hand before its second card
            private Node CreateSplitHand(int secondRank, int handAmount)
            {
                int[] playerRanks = secondRank == 0 ? new int[] { _splitRank } : new int[] { _splitRank, secondRank };
                int[] removedRanks = Enumerable.Repeat(_splitRank, handAmount - 1).ToArray();

                Node splitHand = new(NodeKind.PlayerDecision, playerRanks, new int[] { _dealerRank }, _splitRank, removedRanks);
                NodeBuilder.CheckSetBustOrStand(splitHand);
                return splitHand;
            }
        }
    }
}
