using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BellyFull
{
    /// <summary>
    /// HUD controller for one player on the shared screen.
    /// P1 displays top-left, P2 displays top-right.
    /// Shows equation (e.g. "3+2=2"), crowns, and blast UI.
    /// One instance per player.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private PlayerIndex playerIndex;

        [Header("Equation Display")]
        [SerializeField] private TextMeshProUGUI equationText;

        [Header("Crown Display")]
        [SerializeField] private GameObject[] crownIcons = new GameObject[3];

        [Header("Ball Blast UI")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI blastEatCountText;
        [SerializeField] private GameObject blastOverlay;

        [Header("Game Over")]
        [SerializeField] private GameObject winBanner;

        // Tracked equation state
        private int _startBelly;
        private int _target;
        private string _operator;
        private int _operand;
        private int _currentBelly;
        private bool _celebrating;

        private static readonly string[] CelebrationWords = { "YES!", "NICE!", "WOO!", "YAY!", "COOL!" };

        private void Start()
        {
            if (blastOverlay != null) blastOverlay.SetActive(false);
            if (winBanner != null) winBanner.SetActive(false);
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            foreach (var c in crownIcons)
                if (c != null) c.SetActive(false);

            GameEvents.OnEquationGenerated += HandleEquationGenerated;
            GameEvents.OnObjectEaten += HandleObjectEaten;
            GameEvents.OnCrownAwarded += HandleCrownAwarded;
            GameEvents.OnBallBlastCountdown += HandleBlastCountdown;
            GameEvents.OnBallBlastStarted += HandleBlastStarted;
            GameEvents.OnBallBlastEnded += HandleBlastEnded;
            GameEvents.OnGameWon += HandleGameWon;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnEquationGenerated -= HandleEquationGenerated;
            GameEvents.OnObjectEaten -= HandleObjectEaten;
            GameEvents.OnCrownAwarded -= HandleCrownAwarded;
            GameEvents.OnBallBlastCountdown -= HandleBlastCountdown;
            GameEvents.OnBallBlastStarted -= HandleBlastStarted;
            GameEvents.OnBallBlastEnded -= HandleBlastEnded;
            GameEvents.OnGameWon -= HandleGameWon;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            if (BallBlastManager.Instance != null && BallBlastManager.Instance.IsBlasting)
            {
                var snake = GameManager.Instance.GetSnake(playerIndex);
                if (snake != null && blastEatCountText != null)
                {
                    blastEatCountText.text = snake.BlastEatCount.ToString();
                }
            }
        }

        private void HandleEquationGenerated(PlayerIndex player, int current, int target, EquationType type)
        {
            if (player != playerIndex) return;

            _startBelly = current;
            _target = target;
            _currentBelly = current;
            _operand = Mathf.Abs(target - current);
            _operator = type == EquationType.Addition ? "+" : "-";

            if (!_celebrating)
                UpdateEquationDisplay();
        }

        private void HandleObjectEaten(PlayerIndex player, FieldObjectType type, int bellyCount)
        {
            if (player != playerIndex) return;
            _currentBelly = bellyCount;

            if (_celebrating) return;

            UpdateEquationDisplay();

            // Check if just solved
            if (_currentBelly == _target)
            {
                StartCoroutine(SolveCelebration());
            }
        }

        private void UpdateEquationDisplay()
        {
            if (equationText == null) return;

            bool correct = _currentBelly == _target;
            string answerColor = correct ? "#00CC00" : "#CC0000";

            equationText.text = $"{_startBelly}{_operator}{_operand}=<color={answerColor}><size=150%>{_currentBelly}</size></color>";
        }

        private IEnumerator SolveCelebration()
        {
            _celebrating = true;

            if (equationText == null) { _celebrating = false; yield break; }

            var rt = equationText.GetComponent<RectTransform>();
            Vector3 originalScale = rt != null ? rt.localScale : Vector3.one;

            // Flash the correct answer green
            equationText.text = $"<color=#00CC00><size=150%>{_startBelly}{_operator}{_operand}={_target}</size></color>";

            // Scale punch up
            if (rt != null)
            {
                float elapsed = 0f;
                float punchDuration = 0.2f;
                while (elapsed < punchDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / punchDuration;
                    float scale = 1f + 0.4f * Mathf.Sin(t * Mathf.PI);
                    rt.localScale = originalScale * scale;
                    yield return null;
                }
            }

            // Show fun word
            string word = CelebrationWords[Random.Range(0, CelebrationWords.Length)];
            equationText.text = $"<color=#FFD700><size=200%>{word}</size></color>";

            // Bounce the word
            if (rt != null)
            {
                float elapsed = 0f;
                float bounceDuration = 0.5f;
                while (elapsed < bounceDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / bounceDuration;
                    float scale = 1f + 0.2f * Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);
                    rt.localScale = originalScale * scale;
                    yield return null;
                }
            }

            // Brief pause on the word
            yield return new WaitForSeconds(0.3f);

            // Restore scale and show new equation
            if (rt != null) rt.localScale = originalScale;
            _celebrating = false;
            UpdateEquationDisplay();
        }

        private void HandleCrownAwarded(PlayerIndex player, int totalCrowns)
        {
            if (player != playerIndex) return;
            for (int i = 0; i < crownIcons.Length && i < totalCrowns; i++)
            {
                if (crownIcons[i] != null)
                    crownIcons[i].SetActive(true);
            }
        }

        private void HandleBlastCountdown()
        {
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                StartCoroutine(CountdownRoutine());
            }
        }

        private IEnumerator CountdownRoutine()
        {
            countdownText.text = "3";
            yield return new WaitForSeconds(1f);
            countdownText.text = "2";
            yield return new WaitForSeconds(1f);
            countdownText.text = "1";
            yield return new WaitForSeconds(1f);
            countdownText.gameObject.SetActive(false);
        }

        private void HandleBlastStarted()
        {
            if (blastOverlay != null) blastOverlay.SetActive(true);
            if (blastEatCountText != null)
            {
                blastEatCountText.gameObject.SetActive(true);
                blastEatCountText.text = "0";
            }
        }

        private void HandleBlastEnded(int p1Count, int p2Count)
        {
            if (blastOverlay != null) blastOverlay.SetActive(false);
            if (blastEatCountText != null) blastEatCountText.gameObject.SetActive(false);
        }

        private void HandleGameWon(PlayerIndex winner)
        {
            if (winner == playerIndex && winBanner != null)
                winBanner.SetActive(true);
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.NormalPlay)
            {
                if (blastOverlay != null) blastOverlay.SetActive(false);
            }
        }
    }
}
