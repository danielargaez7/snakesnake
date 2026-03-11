using System.Collections;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Controls the Ball Blast sequence: pre-blast (hedgehogs hide),
    /// countdown, 8-second frenzy, eat counting, crown award, and post-blast reset.
    /// </summary>
    public class BallBlastManager : MonoBehaviour
    {
        public static BallBlastManager Instance { get; private set; }

        [Header("Timing")]
        [SerializeField] private float preBlastDuration = 2f;    // Hedgehogs hiding
        [SerializeField] private float countdownDuration = 3f;    // 3-2-1
        [SerializeField] private float blastDuration = 8f;        // The frenzy

        public float BlastTimeRemaining { get; private set; }
        public bool IsBlasting { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.PreBlast)
            {
                StartCoroutine(BlastSequence());
            }
        }

        private IEnumerator BlastSequence()
        {
            // Phase 1: Pre-blast — hedgehogs hide (handled by FieldManager via state change)
            Debug.Log("[BallBlast] Pre-blast: hedgehogs hiding...");
            yield return new WaitForSeconds(preBlastDuration);

            // Phase 2: Countdown 3-2-1
            GameEvents.BallBlastCountdown();
            Debug.Log("[BallBlast] 3... 2... 1...");
            yield return new WaitForSeconds(countdownDuration);

            // Phase 3: BLAST!
            GameEvents.GameStateChanged(GameState.BallBlast);
            GameEvents.BallBlastStarted();
            IsBlasting = true;
            BlastTimeRemaining = blastDuration;
            Debug.Log("[BallBlast] BALL BLAST!");

            // Reset snake blast counters
            var s1 = GameManager.Instance.GetSnake(PlayerIndex.Player1);
            var s2 = GameManager.Instance.GetSnake(PlayerIndex.Player2);

            // Count down blast timer
            while (BlastTimeRemaining > 0)
            {
                BlastTimeRemaining -= Time.deltaTime;
                yield return null;
            }

            IsBlasting = false;
            BlastTimeRemaining = 0;

            // Get eat counts
            int p1Count = s1 != null ? s1.BlastEatCount : 0;
            int p2Count = s2 != null ? s2.BlastEatCount : 0;

            Debug.Log($"[BallBlast] Over! P1: {p1Count}, P2: {p2Count}");
            GameEvents.BallBlastEnded(p1Count, p2Count);

            // GameManager handles crown award and state transition from here
        }
    }
}
