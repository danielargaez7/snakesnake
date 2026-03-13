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

        public GameState CurrentState { get; private set; } = GameState.TitleScreen;
        public int[] CrownCounts { get; private set; } = new int[2];

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GameEvents.OnAllNumbersComplete += HandleAllNumbersComplete;
            GameEvents.OnBallBlastEnded     += HandleBallBlastEnded;
            SetState(GameState.TitleScreen);
        }

        private void OnDestroy()
        {
            GameEvents.OnAllNumbersComplete -= HandleAllNumbersComplete;
            GameEvents.OnBallBlastEnded     -= HandleBallBlastEnded;
        }

        // ── State transitions ───────────────────────────────────────────────

        public void BeginTitleScreen()
        {
            CrownCounts[0] = CrownCounts[1] = 0;
            foreach (var snake in snakes)
                if (snake != null) snake.ResetForRound();
            SetState(GameState.TitleScreen);
        }
        public void BeginWaitForPlayers() => SetState(GameState.WaitingForPlayers);
        public void BeginCountdown()      => SetState(GameState.Countdown);

        public void BeginPlay()
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

        // ── Event handlers ─────────────────────────────────────────────────

        private void HandleAllNumbersComplete(PlayerIndex player)
        {
            if (CurrentState != GameState.NormalPlay) return;
            SetState(GameState.PreBlast);
        }

        private void HandleBallBlastEnded(int p1Count, int p2Count)
        {
            SetState(GameState.CrownAward);

            if (p1Count > p2Count)       AwardCrown(PlayerIndex.Player1);
            else if (p2Count > p1Count)  AwardCrown(PlayerIndex.Player2);
            else                         Debug.Log("[GameManager] Tie — no crown awarded");

            // Check win
            for (int i = 0; i < 2; i++)
            {
                if (CrownCounts[i] >= crownsToWin)
                {
                    GameEvents.GameWon((PlayerIndex)i);
                    SetState(GameState.GameOver);
                    return;
                }
            }

            // Reset snakes and resume
            foreach (var snake in snakes)
                if (snake != null) snake.ResetForRound();

            Invoke(nameof(ResumePlay), 2.5f);
        }

        private void AwardCrown(PlayerIndex player)
        {
            CrownCounts[(int)player]++;
            GameEvents.CrownAwarded(player, CrownCounts[(int)player]);
            Debug.Log($"[GameManager] Crown -> {player} (total: {CrownCounts[(int)player]})");
        }

        private void ResumePlay() => SetState(GameState.NormalPlay);

        /// <summary>Called by BallBlastManager to properly update CurrentState to BallBlast.</summary>
        public void EnterBallBlastState() => SetState(GameState.BallBlast);

        // ── Accessors ──────────────────────────────────────────────────────

        public SnakeController GetSnake(PlayerIndex player) => snakes[(int)player];
        public FieldManager    GetField()                   => sharedField;
    }
}
