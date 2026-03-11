using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Manages energy bar fill state for each player.
    /// Passive fill + active boost on equation solve.
    /// When either bar fills, triggers Ball Blast for both players.
    /// </summary>
    public class EnergyBarManager : MonoBehaviour
    {
        public static EnergyBarManager Instance { get; private set; }

        [Header("Fill Rates")]
        [SerializeField] private float passiveFillRate = 0.02f;   // Per second (very slow)
        [SerializeField] private float solveBoostAmount = 0.12f;  // ~10-15% per solve

        private float[] _fillAmounts = new float[2];
        private bool _active;

        public float GetFillAmount(PlayerIndex player) => _fillAmounts[(int)player];

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnEquationSolved += HandleEquationSolved;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnEquationSolved -= HandleEquationSolved;
        }

        private void Update()
        {
            if (!_active) return;

            // Passive fill for both players
            for (int i = 0; i < 2; i++)
            {
                _fillAmounts[i] += passiveFillRate * Time.deltaTime;
                _fillAmounts[i] = Mathf.Clamp01(_fillAmounts[i]);
                GameEvents.EnergyBarChanged((PlayerIndex)i, _fillAmounts[i]);

                if (_fillAmounts[i] >= 1f)
                {
                    _active = false;
                    GameEvents.EnergyBarFull((PlayerIndex)i);
                    return;
                }
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.NormalPlay)
            {
                _active = true;
            }
            else
            {
                _active = false;
            }

            if (state == GameState.NormalPlay && _fillAmounts[0] > 0.5f)
            {
                // Post-blast reset
                ResetBars();
            }
        }

        private void HandleEquationSolved(PlayerIndex player)
        {
            if (!_active) return;
            _fillAmounts[(int)player] += solveBoostAmount;
            _fillAmounts[(int)player] = Mathf.Clamp01(_fillAmounts[(int)player]);
            GameEvents.EnergyBarChanged(player, _fillAmounts[(int)player]);

            if (_fillAmounts[(int)player] >= 1f)
            {
                _active = false;
                GameEvents.EnergyBarFull(player);
            }
        }

        public void ResetBars()
        {
            _fillAmounts[0] = 0f;
            _fillAmounts[1] = 0f;
            GameEvents.EnergyBarChanged(PlayerIndex.Player1, 0f);
            GameEvents.EnergyBarChanged(PlayerIndex.Player2, 0f);
        }
    }
}
