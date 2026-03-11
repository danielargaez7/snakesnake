using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Generates equations with SYNCHRONIZED equation types — both players
    /// always have the same operation (both addition or both subtraction).
    /// Each player gets their own target number based on their belly state.
    /// </summary>
    public class MathSystem : MonoBehaviour
    {
        public static MathSystem Instance { get; private set; }

        [Header("Difficulty")]
        [SerializeField] private DifficultyTier currentTier = DifficultyTier.Tier1;

        [Header("Settings")]
        [SerializeField] private int startingBellyCount = 3;

        // Per-player equation state
        private int[] _currentBelly = new int[2];
        private int[] _targetNumber = new int[2];

        // Synchronized equation type for both players
        private EquationType _currentSyncedType = EquationType.Addition;
        private int _equationsSinceSwitch;
        private int _switchAfterCount = 3; // Switch operation type every ~3 equations

        // Tracking for tier advancement
        private int _totalEquations;
        private int _correctEquations;

        public DifficultyTier CurrentTier => currentTier;
        public int StartingBellyCount => startingBellyCount;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnEquationSolved += HandleEquationSolved;
            GameEvents.OnObjectEaten += HandleObjectEaten;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnEquationSolved -= HandleEquationSolved;
            GameEvents.OnObjectEaten -= HandleObjectEaten;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.NormalPlay)
            {
                // Pick the synchronized equation type
                _currentSyncedType = ChooseSyncedEquationType();
                _equationsSinceSwitch = 0;
                GameManager.Instance.SetEquationType(_currentSyncedType);

                // Generate equations for both players
                for (int i = 0; i < 2; i++)
                {
                    var snake = GameManager.Instance.GetSnake((PlayerIndex)i);
                    _currentBelly[i] = snake != null ? snake.BellyCount : startingBellyCount;
                    GenerateEquation((PlayerIndex)i);
                }

                // Apply dodge behavior for the new equation type
                var field = GameManager.Instance.GetField();
                if (field != null)
                {
                    var dodge = field.GetComponent<DodgeSystem>();
                    if (dodge != null)
                        dodge.ApplyDodgeBehavior(_currentSyncedType);
                }
            }
        }

        private void HandleEquationSolved(PlayerIndex player)
        {
            int idx = (int)player;
            _correctEquations++;
            _totalEquations++;

            _currentBelly[idx] = _targetNumber[idx];
            _equationsSinceSwitch++;

            // Check if it's time to switch the synchronized equation type
            if (ShouldSwitchType())
            {
                _currentSyncedType = _currentSyncedType == EquationType.Addition
                    ? EquationType.Subtraction
                    : EquationType.Addition;
                _equationsSinceSwitch = 0;
                GameManager.Instance.SetEquationType(_currentSyncedType);

                // Update dodge behavior for new type
                var field = GameManager.Instance.GetField();
                if (field != null)
                {
                    var dodge = field.GetComponent<DodgeSystem>();
                    if (dodge != null)
                        dodge.ApplyDodgeBehavior(_currentSyncedType);
                }

                // Regenerate equation for the OTHER player too (new type)
                PlayerIndex other = player == PlayerIndex.Player1 ? PlayerIndex.Player2 : PlayerIndex.Player1;
                GenerateEquation(other);
            }

            GenerateEquation(player);
        }

        private void HandleObjectEaten(PlayerIndex player, FieldObjectType type, int newBellyCount)
        {
            _currentBelly[(int)player] = newBellyCount;
        }

        private bool ShouldSwitchType()
        {
            if (currentTier == DifficultyTier.Tier1)
            {
                // Tier 1: addition only, but force subtraction if any player near cap
                for (int i = 0; i < 2; i++)
                {
                    if (_currentBelly[i] >= GetMaxNumber() - 1)
                        return true;
                }
                return false;
            }

            // Tier 2+: switch after ~3 equations solved (by either player)
            if (_equationsSinceSwitch >= _switchAfterCount)
                return true;

            // Force switch if any player is near bounds
            for (int i = 0; i < 2; i++)
            {
                if (_currentSyncedType == EquationType.Addition && _currentBelly[i] >= GetMaxNumber() - 1)
                    return true;
                if (_currentSyncedType == EquationType.Subtraction && _currentBelly[i] <= 1)
                    return true;
            }

            return false;
        }

        public void GenerateEquation(PlayerIndex player)
        {
            int idx = (int)player;
            int belly = _currentBelly[idx];

            int operand;
            int target;

            // Generate valid equation using the synchronized type
            int maxAttempts = 50;
            do
            {
                operand = ChooseOperand();
                target = _currentSyncedType == EquationType.Addition ? belly + operand : belly - operand;
                maxAttempts--;
            }
            while ((target < 0 || target > GetMaxNumber()) && maxAttempts > 0);

            if (maxAttempts <= 0)
            {
                // Fallback: move by 1 in the current direction
                operand = 1;
                target = _currentSyncedType == EquationType.Addition ? belly + 1 : belly - 1;
                target = Mathf.Clamp(target, 0, GetMaxNumber());
            }

            _targetNumber[idx] = target;

            // Update the snake
            var snake = GameManager.Instance.GetSnake(player);
            if (snake != null)
            {
                snake.TargetNumber = target;
                snake.CurrentEquationType = _currentSyncedType;
            }

            // Ensure enough objects on shared field
            var field = GameManager.Instance.GetField();
            if (field != null)
            {
                int needed = Mathf.Abs(target - belly) + 3;
                if (_currentSyncedType == EquationType.Addition)
                    field.EnsureObjectCount(FieldObjectType.Ball, needed);
                else
                    field.EnsureObjectCount(FieldObjectType.Hedgehog, needed);
            }

            GameEvents.EquationGenerated(player, belly, target, _currentSyncedType);
            Debug.Log($"[MathSystem] {player}: {belly} {(_currentSyncedType == EquationType.Addition ? "+" : "-")}{operand} = {target}");
        }

        private EquationType ChooseSyncedEquationType()
        {
            if (currentTier == DifficultyTier.Tier1)
                return EquationType.Addition;

            // Start with addition, alternate from there
            return EquationType.Addition;
        }

        private int ChooseOperand()
        {
            switch (currentTier)
            {
                case DifficultyTier.Tier1: return Random.Range(1, 3);  // 1 or 2
                case DifficultyTier.Tier2: return Random.Range(1, 4);  // 1-3
                case DifficultyTier.Tier3: return Random.Range(1, 5);  // 1-4
                default: return 1;
            }
        }

        private int GetMaxNumber()
        {
            switch (currentTier)
            {
                case DifficultyTier.Tier1: return 5;
                case DifficultyTier.Tier2: return 8;
                case DifficultyTier.Tier3: return 12;
                default: return 5;
            }
        }

        public void EvaluateTierAdvancement()
        {
            if (_totalEquations == 0) return;
            float accuracy = (float)_correctEquations / _totalEquations;

            if (accuracy >= 0.8f && currentTier < DifficultyTier.Tier3)
            {
                currentTier++;
                Debug.Log($"[MathSystem] Tier advanced to {currentTier}!");
            }
        }

        public int GetCurrentBelly(PlayerIndex player) => _currentBelly[(int)player];
        public int GetTargetNumber(PlayerIndex player) => _targetNumber[(int)player];
        public EquationType GetCurrentEquationType() => _currentSyncedType;
        public float GetAccuracy() => _totalEquations > 0 ? (float)_correctEquations / _totalEquations : 0f;
    }
}
