using System.Text;

namespace BerldBlackjack.Split
{
    public class SplitNode
    {
        public static long Created { get; private set; }

        public bool IsFirstFinished => IsFinished(FirstKind);
        public bool IsFinished => IsFirstFinished && IsFinished(SecondKind);

        public int[] Ranks => IsFirstFinished ? SecondRanks : FirstRanks;
        public int Sum => IsFirstFinished ? SecondSum : FirstSum;
        public SplitNodeKind Kind => IsFirstFinished ? SecondKind : FirstKind;

        public int[] FirstRanks { get; }
        public int FirstSum { get; }
        public SplitNodeKind FirstKind { get; set; }

        public int[] SecondRanks { get; }
        public int SecondSum { get; }
        public SplitNodeKind SecondKind { get; set; }

        public double Odds { get; set; }

        public List<SplitNode> Children { get; set; } = new();

        public SplitNode(int[] firstRanks, int[] secondRanks)
        {
            FirstRanks = firstRanks.OrderBy(c => c).ToArray();
            SecondRanks = secondRanks.OrderBy(c => c).ToArray();
            FirstSum = DetermineSum(firstRanks);
            SecondSum = DetermineSum(secondRanks);
            Created++;
        }

        public static int DetermineSum(int[] ranks)
        {
            int[] noneAceRanks = ranks.Where(c => c != Rank.Ace).ToArray();
            int noneAceSum = noneAceRanks.Sum();
            int aceCount = ranks.Length - noneAceRanks.Length;

            if (aceCount == 0)
            {
                return noneAceSum;
            }
            else
            {
                int highAceSum = Rank.Ace + (aceCount - 1) + noneAceSum;

                if (highAceSum <= 21)
                {
                    return highAceSum;
                }
                else
                {
                    int lowAceSum = aceCount + noneAceSum;
                    return lowAceSum;
                }
            }
        }

        private static bool IsFinished(SplitNode node)
        {
            return
                IsFinished(node.FirstKind) &&
                IsFinished(node.SecondKind);
        }

        private static bool IsFinished(SplitNodeKind kind)
        {
            return
                kind == SplitNodeKind.Stand ||
                kind == SplitNodeKind.Bust ||
                kind == SplitNodeKind.DoubleStand ||
                kind == SplitNodeKind.DoubleBust;
        }

        public override string ToString()
        {
            StringBuilder builder = new();

            foreach (int rank in FirstRanks)
            {
                builder.Append(Rank.ToShortString(rank));
            }

            builder.Append('-');
            builder.Append(FirstKind);

            builder.Append(" | ");

            foreach (int rank in SecondRanks)
            {
                builder.Append(Rank.ToShortString(rank));
            }

            builder.Append('-');
            builder.Append(SecondKind);

            return builder.ToString();
        }
    }
}
