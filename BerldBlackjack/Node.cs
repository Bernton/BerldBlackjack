using System.Text;

namespace BerldBlackjack
{
    public class Node
    {
        public static long Created { get; private set; }

        public NodeKind Kind { get; set; }

        public int[] PlayerRanks { get; }
        public int PlayerSum { get; }

        public int[] DealerRanks { get; }
        public int DealerSum { get; }

        public double Ev { get; set; } = double.MinValue;

        public (double ratio, Node child)[]? Children { get; set; } = null;


        public Node(NodeKind kind, int[] playerRanks, int[] dealerRanks)
        {
            Kind = kind;
            PlayerRanks = playerRanks.OrderBy(c => c).ToArray();
            DealerRanks = dealerRanks.OrderBy(c => c).ToArray();
            Created++;

            PlayerSum = DetermineSum(playerRanks);
            DealerSum = DetermineSum(dealerRanks);
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

            foreach (int rank in PlayerRanks)
            {
                builder.Append(Rank.ToShortString(rank));
            }

            builder.Append('-');

            foreach (int rank in DealerRanks)
            {
                builder.Append(Rank.ToShortString(rank));
            }

            builder.Append('-');
            builder.Append(Kind);

            return builder.ToString();
        }
    }
}
