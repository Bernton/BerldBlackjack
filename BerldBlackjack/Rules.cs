namespace BerldBlackjack
{
    public static class Rules
    {
        public static int DeckAmount { get; set; } = 6;

        public static bool IsDoubleAnyTwoAllowed { get; set; } = true;
        public static bool IsDoubleAfterSplitAllowed { get; set; } = true;

        // 2 = split once, 4 = resplit up to four hands
        public static int MaxSplitHands { get; set; } = 4;
        public static bool IsResplitAcesAllowed { get; set; } = false;
    }
}
