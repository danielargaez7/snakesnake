using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Manages a single shared energy bar for both players.
    /// Passive fill + active boost on equation solve from either player.
    /// When the bar fills, triggers Ball Blast.
    /// </summary>
    public class EnergyBarManager : MonoBehaviour
    {
        public static EnergyBarManager Instance { get; private set; }

        [Header("Fill Rates")]
        [SerializeField] private float passiveFillRate = 0.01f;   // Per second (~100s to fill passively)
        [SerializeField] private float solveBoostAmount = 0.12f;  // ~10-15% per solve

        private float _fillAmount;
        private bool _active;

        public float FillAmount => _fillAmount;

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

            _fillAmount += passiveFillRate * Time.deltaTime;
            _fillAmount = Mathf.Clamp01(_fillAmount);
            GameEvents.EnergyBarChanged(_fillAmount);

            if (_fillAmount >= 1f)
            {
                _active = false;
                GameEvents.EnergyBarFull();
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.NormalPlay)
            {
                _active = true;
                if (_fillAmount > 0.5f)
                    ResetBar();
            }
            else
            {
                _active = false;
            }
        }

        private void HandleEquationSolved(PlayerIndex player)
        {
            if (!_active) return;
            _fillAmount += solveBoostAmount;
            _fillAmount = Mathf.Clamp01(_fillAmount);
            GameEvents.EnergyBarChanged(_fillAmount);

            if (_fillAmount >= 1f)
            {
                _active = false;
                GameEvents.EnergyBarFull();
            }
        }

        public void ResetBar()
        {
            _fillAmount = 0f;
            GameEvents.EnergyBarChanged(0f);
        }
    }
}
