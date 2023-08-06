namespace BerldBlackjack
{
    public static class Deck
    {
        public const int Amount = 52;

        public const int TenFrequency = 16;
        public const int DefaultFrequency = 4;


        public static int GetFrequencyFromIndex(int index)
        {
            return index switch
            {
                Rank.TenIndex => TenFrequency,
                _ => DefaultFrequency
            };
        }
    }
}
