using UnityEngine;

namespace BellyFull
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Win Condition")]
        [SerializeField] private int crownsToWin = 3;

        [Header("References")]
        [SerializeField] private SnakeController[] snakes = new SnakeController[2];
        [SerializeField] private FieldManager sharedField;

        [Header("Post-Blast Reset")]
        [SerializeField] private int postBlastBellyCount = 3;

        public GameState CurrentState { get; private set; } = GameState.WaitingForPlayers;
        public int[] CrownCounts { get; private set; } = new int[2];

        // Synchronized equation type — both players always on same operation
        public EquationType CurrentEquationType { get; private set; } = EquationType.Addition;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SubscribeEvents();
            StartGame();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            GameEvents.OnEquationSolved += HandleEquationSolved;
            GameEvents.OnBellyAcheStarted += HandleBellyAcheStarted;
            GameEvents.OnBellyAcheEnded += HandleBellyAcheEnded;
            GameEvents.OnEnergyBarFull += HandleEnergyBarFull;
            GameEvents.OnBallBlastEnded += HandleBallBlastEnded;
        }

        private void UnsubscribeEvents()
        {
            GameEvents.OnEquationSolved -= HandleEquationSolved;
            GameEvents.OnBellyAcheStarted -= HandleBellyAcheStarted;
            GameEvents.OnBellyAcheEnded -= HandleBellyAcheEnded;
            GameEvents.OnEnergyBarFull -= HandleEnergyBarFull;
            GameEvents.OnBallBlastEnded -= HandleBallBlastEnded;
        }

        public void StartGame()
        {
            CrownCounts[0] = 0;
            CrownCounts[1] = 0;
            SetState(GameState.NormalPlay);
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            GameEvents.GameStateChanged(newState);
            Debug.Log($"[GameManager] State -> {newState}");
        }

        /// <summary>
        /// Sets the synchronized equation type for both players.
        /// Called by MathSystem when generating new equations.
        /// </summary>
        public void SetEquationType(EquationType type)
        {
            CurrentEquationType = type;
        }

        private void HandleEquationSolved(PlayerIndex player)
        {
            // Energy bar boost handled by EnergyBarManager
        }

        private void HandleBellyAcheStarted(PlayerIndex player, int overshoot)
        {
            // Per-player state — FieldManager freezes objects around that snake
        }

        private void HandleBellyAcheEnded(PlayerIndex player)
        {
        }

        private void HandleEnergyBarFull()
        {
            if (CurrentState != GameState.NormalPlay) return;
            SetState(GameState.PreBlast);
        }

        private void HandleBallBlastEnded(int p1Count, int p2Count)
        {
            SetState(GameState.CrownAward);

            // Award crown (tie = no crown)
            if (p1Count > p2Count)
                AwardCrown(PlayerIndex.Player1);
            else if (p2Count > p1Count)
                AwardCrown(PlayerIndex.Player2);
            else
                Debug.Log("[GameManager] Tie — no crown awarded");

            // Check win condition
            for (int i = 0; i < 2; i++)
            {
                if (CrownCounts[i] >= crownsToWin)
                {
                    GameEvents.GameWon((PlayerIndex)i);
                    SetState(GameState.GameOver);
                    return;
                }
            }

            // Reset bellies to post-blast count and resume
            foreach (var snake in snakes)
            {
                if (snake != null)
                    snake.ResetBelly(postBlastBellyCount);
            }

            Invoke(nameof(ResumeNormalPlay), 2.5f);
        }

        private void AwardCrown(PlayerIndex player)
        {
            CrownCounts[(int)player]++;
            GameEvents.CrownAwarded(player, CrownCounts[(int)player]);
            Debug.Log($"[GameManager] Crown -> {player} (total: {CrownCounts[(int)player]})");
        }

        private void ResumeNormalPlay()
        {
            SetState(GameState.NormalPlay);
        }

        public SnakeController GetSnake(PlayerIndex player) => snakes[(int)player];
        public FieldManager GetField() => sharedField;
    }
}
