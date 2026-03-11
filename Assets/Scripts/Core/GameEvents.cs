using System;
using UnityEngine;

namespace BellyFull
{
    public static class GameEvents
    {
        // Game state
        public static event Action<GameState> OnGameStateChanged;
        public static void GameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

        // Math / Equations
        public static event Action<PlayerIndex, int, int, EquationType> OnEquationGenerated; // player, current, target, type
        public static void EquationGenerated(PlayerIndex p, int current, int target, EquationType type)
            => OnEquationGenerated?.Invoke(p, current, target, type);

        public static event Action<PlayerIndex> OnEquationSolved;
        public static void EquationSolved(PlayerIndex p) => OnEquationSolved?.Invoke(p);

        // Eating
        public static event Action<PlayerIndex, FieldObjectType, int> OnObjectEaten; // player, type, new belly count
        public static void ObjectEaten(PlayerIndex p, FieldObjectType type, int bellyCount)
            => OnObjectEaten?.Invoke(p, type, bellyCount);

        // Belly Ache
        public static event Action<PlayerIndex, int> OnBellyAcheStarted; // player, overshoot amount
        public static void BellyAcheStarted(PlayerIndex p, int overshoot) => OnBellyAcheStarted?.Invoke(p, overshoot);

        public static event Action<PlayerIndex> OnBellyAcheEnded;
        public static void BellyAcheEnded(PlayerIndex p) => OnBellyAcheEnded?.Invoke(p);

        // Energy Bar (shared between both players)
        public static event Action<float> OnEnergyBarChanged; // fill 0-1
        public static void EnergyBarChanged(float fill) => OnEnergyBarChanged?.Invoke(fill);

        public static event Action OnEnergyBarFull;
        public static void EnergyBarFull() => OnEnergyBarFull?.Invoke();

        // Ball Blast
        public static event Action OnBallBlastCountdown;
        public static void BallBlastCountdown() => OnBallBlastCountdown?.Invoke();

        public static event Action OnBallBlastStarted;
        public static void BallBlastStarted() => OnBallBlastStarted?.Invoke();

        public static event Action<int, int> OnBallBlastEnded; // p1 count, p2 count
        public static void BallBlastEnded(int p1Count, int p2Count) => OnBallBlastEnded?.Invoke(p1Count, p2Count);

        // Crowns
        public static event Action<PlayerIndex, int> OnCrownAwarded; // player, total crowns
        public static void CrownAwarded(PlayerIndex p, int total) => OnCrownAwarded?.Invoke(p, total);

        public static event Action<PlayerIndex> OnGameWon;
        public static void GameWon(PlayerIndex p) => OnGameWon?.Invoke(p);

        // Dodge interactions (for data logging)
        public static event Action<PlayerIndex, FieldObjectType> OnDodgeAttempt;
        public static void DodgeAttempt(PlayerIndex p, FieldObjectType type) => OnDodgeAttempt?.Invoke(p, type);
    }
}
