using System;

namespace BellyFull
{
    public static class GameEvents
    {
        // Game state
        public static event Action<GameState> OnGameStateChanged;
        public static void GameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

        // Catch-carry-drop
        public static event Action<PlayerIndex> OnHedgehogCaught;           // snake picked up a hedgehog
        public static void HedgehogCaught(PlayerIndex p) => OnHedgehogCaught?.Invoke(p);

        public static event Action<PlayerIndex, int, int, int> OnHoleDelivered;  // player, holeIndex, holesFilled, holesTotal
        public static void HoleDelivered(PlayerIndex p, int holeIdx, int filled, int total) => OnHoleDelivered?.Invoke(p, holeIdx, filled, total);

        public static event Action<PlayerIndex, int> OnNumberCompleted;     // player, completedNumber (1-10)
        public static void NumberCompleted(PlayerIndex p, int number) => OnNumberCompleted?.Invoke(p, number);

        public static event Action<PlayerIndex, int> OnNumberAdvanced;      // player, newNumber (2-10)
        public static void NumberAdvanced(PlayerIndex p, int number) => OnNumberAdvanced?.Invoke(p, number);

        public static event Action<PlayerIndex> OnAllNumbersComplete;        // player finished 1-10
        public static void AllNumbersComplete(PlayerIndex p) => OnAllNumbersComplete?.Invoke(p);

        // Ball Blast
        public static event Action OnBallBlastCountdown;
        public static void BallBlastCountdown() => OnBallBlastCountdown?.Invoke();

        public static event Action OnBallBlastStarted;
        public static void BallBlastStarted() => OnBallBlastStarted?.Invoke();

        public static event Action<int, int> OnBallBlastEnded;              // p1 count, p2 count
        public static void BallBlastEnded(int p1, int p2) => OnBallBlastEnded?.Invoke(p1, p2);

        // Ball eaten during blast
        public static event Action<PlayerIndex> OnBlastBallEaten;
        public static void BlastBallEaten(PlayerIndex p) => OnBlastBallEaten?.Invoke(p);

        // Crowns / Win
        public static event Action<PlayerIndex, int> OnCrownAwarded;        // player, total crowns
        public static void CrownAwarded(PlayerIndex p, int total) => OnCrownAwarded?.Invoke(p, total);

        public static event Action<PlayerIndex> OnGameWon;
        public static void GameWon(PlayerIndex p) => OnGameWon?.Invoke(p);
    }
}
