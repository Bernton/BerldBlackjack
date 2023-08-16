using System.Text;

namespace BerldBlackjack
{
    public class Node
    {
        public static long Created { get; private set; }

        public NodeKind Kind { get; set; }

        public int[] Ranks { get; }
        public int Sum { get; }

        public double Odds { get; set; }

        public double Ev { get; set; } = double.MinValue;
        public double StandEv { get; set; } = double.MinValue;
        public double DoubleEv { get; set; } = double.MinValue;
        public double SplitEv { get; set; } = double.MinValue;
        public double HitEv { get; set; } = double.MinValue;

        public Node[]? Children { get; set; } = null;

        public Node(int[] playerRanks)
        {
            Ranks = playerRanks.OrderBy(c => c).ToArray();
            Sum = DetermineSum(playerRanks);

            if (Sum > 21)
            {
                Kind = NodeKind.Bust;
            }
            else if (Sum == 21)
            {
                Kind = NodeKind.Stand;
            }
            else
            {
                Kind = NodeKind.Branch;
            }

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

        public override string ToString()
        {
            StringBuilder builder = new();

            foreach (int rank in Ranks)
            {
                builder.Append(Rank.ToShortString(rank));
            }

            builder.Append('-');
            builder.Append(Kind);

            return builder.ToString();
        }
    }
}
