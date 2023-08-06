namespace BerldBlackjack
{
    public static class Rank
    {
        public const int Amount = 10;

        public const int DeuceIndex = 0;
        public const int TrayIndex = 1;
        public const int FourIndex = 2;
        public const int FiveIndex = 3;
        public const int SixIndex = 4;
        public const int SevenIndex = 5;
        public const int EightIndex = 6;
        public const int NineIndex = 7;
        public const int TenIndex = 8;
        public const int AceIndex = 9;

        public const int Deuce = 2;
        public const int Tray = 3;
        public const int Four = 4;
        public const int Five = 5;
        public const int Six = 6;
        public const int Seven = 7;
        public const int Eight = 8;
        public const int Nine = 9;
        public const int Ten = 10;
        public const int Ace = 11;

        public static int ToRank(int index)
        {
            return index + 2;
        }

        public static int ToIndex(int rank)
        {
            return rank - 2;
        }

        public static string ToShortString(int rank)
        {
            return rank switch
            {
                Ten => "T",
                Ace => "A",
                _ => rank.ToString(),
            };
        }
    }
}