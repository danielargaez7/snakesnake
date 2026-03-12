using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Tracks each player's progress through numbers 1-10.
    /// Each number N has N individually-targetable holes.
    /// </summary>
    public class NumberManager : MonoBehaviour
    {
        public static NumberManager Instance { get; private set; }

        private int[]    _currentNumber  = { 1, 1 };
        private int[]    _holesFilled    = { 0, 0 };
        private bool[][] _holesFilledSet = { null, null };

        public const int MaxNumber = 5;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            InitHoleState(0);
            InitHoleState(1);
            GameEvents.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.NormalPlay)
                ResetForRound();
        }

        private void InitHoleState(int playerIdx)
        {
            _holesFilledSet[playerIdx] = new bool[_currentNumber[playerIdx]];
        }

        // ── Delivery ──────────────────────────────────────────────────────

        /// <summary>Fill a specific hole by index. Returns false if already filled.</summary>
        public bool ReceiveDeliveryToHole(PlayerIndex player, int holeIndex)
        {
            int idx = (int)player;
            if (_holesFilledSet[idx] == null || holeIndex < 0 || holeIndex >= _holesFilledSet[idx].Length)
                return false;
            if (_holesFilledSet[idx][holeIndex]) return false; // already filled

            _holesFilledSet[idx][holeIndex] = true;
            _holesFilled[idx]++;

            int total = _currentNumber[idx];
            GameEvents.HoleDelivered(player, holeIndex, _holesFilled[idx], total);
            Debug.Log($"[NumberManager] {player}: hole[{holeIndex}] {_holesFilled[idx]}/{total} (number {_currentNumber[idx]})");

            if (_holesFilled[idx] >= total)
            {
                int completed = _currentNumber[idx];
                GameEvents.NumberCompleted(player, completed);
                Debug.Log($"[NumberManager] {player}: number {completed} complete!");

                if (completed >= MaxNumber)
                {
                    GameEvents.AllNumbersComplete(player);
                }
                else
                {
                    _currentNumber[idx]++;
                    _holesFilled[idx] = 0;
                    InitHoleState(idx);
                    GameEvents.NumberAdvanced(player, _currentNumber[idx]);
                }
            }
            return true;
        }

        /// <summary>Is a specific hole already filled?</summary>
        public bool IsHoleFilled(PlayerIndex player, int holeIndex)
        {
            int idx = (int)player;
            if (_holesFilledSet[idx] == null || holeIndex < 0 || holeIndex >= _holesFilledSet[idx].Length)
                return true; // treat out-of-range as filled
            return _holesFilledSet[idx][holeIndex];
        }

        public int  GetCurrentNumber(PlayerIndex player) => _currentNumber[(int)player];
        public int  GetHolesFilled(PlayerIndex player)   => _holesFilled[(int)player];
        public int  GetPlayerGap()       => Mathf.Abs(_currentNumber[0] - _currentNumber[1]);
        public PlayerIndex? GetLeadingPlayer()
        {
            if (_currentNumber[0] > _currentNumber[1]) return PlayerIndex.Player1;
            if (_currentNumber[1] > _currentNumber[0]) return PlayerIndex.Player2;
            return null;
        }

        public void ResetForRound()
        {
            _currentNumber[0] = _currentNumber[1] = 1;
            _holesFilled[0]   = _holesFilled[1]   = 0;
            InitHoleState(0);
            InitHoleState(1);
        }
    }
}
