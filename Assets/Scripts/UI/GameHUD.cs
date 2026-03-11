using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BellyFull
{
    /// <summary>
    /// HUD controller for one player on the shared screen.
    /// P1 displays top-left, P2 displays top-right.
    /// Shows belly count, equation, energy bar, crowns, and blast UI.
    /// One instance per player.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private PlayerIndex playerIndex;

        [Header("Belly Display")]
        [SerializeField] private TextMeshProUGUI bellyCountText;

        [Header("Equation Display")]
        [SerializeField] private TextMeshProUGUI equationText;

        [Header("Energy Bar")]
        [SerializeField] private Image energyBarFill;
        [SerializeField] private Image energyBarGlow;

        [Header("Crown Display")]
        [SerializeField] private GameObject[] crownIcons = new GameObject[3];

        [Header("Ball Blast UI")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI blastEatCountText;
        [SerializeField] private GameObject blastOverlay;

        [Header("Game Over")]
        [SerializeField] private GameObject winBanner;

        private void Start()
        {
            // Hide optional elements
            if (blastOverlay != null) blastOverlay.SetActive(false);
            if (winBanner != null) winBanner.SetActive(false);
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            foreach (var c in crownIcons)
                if (c != null) c.SetActive(false);

            GameEvents.OnEquationGenerated += HandleEquationGenerated;
            GameEvents.OnObjectEaten += HandleObjectEaten;
            GameEvents.OnEnergyBarChanged += HandleEnergyBarChanged;
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
            GameEvents.OnEnergyBarChanged -= HandleEnergyBarChanged;
            GameEvents.OnCrownAwarded -= HandleCrownAwarded;
            GameEvents.OnBallBlastCountdown -= HandleBlastCountdown;
            GameEvents.OnBallBlastStarted -= HandleBlastStarted;
            GameEvents.OnBallBlastEnded -= HandleBlastEnded;
            GameEvents.OnGameWon -= HandleGameWon;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            // Update blast eat count in real time during Ball Blast
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

            if (bellyCountText != null)
                bellyCountText.text = current.ToString();

            if (equationText != null)
            {
                int operand = Mathf.Abs(target - current);
                string op = type == EquationType.Addition ? "+" : "-";
                equationText.text = $"{op}{operand}";
            }
        }

        private void HandleObjectEaten(PlayerIndex player, FieldObjectType type, int bellyCount)
        {
            if (player != playerIndex) return;
            if (bellyCountText != null)
                bellyCountText.text = bellyCount.ToString();
        }

        private void HandleEnergyBarChanged(PlayerIndex player, float fill)
        {
            if (player != playerIndex) return;
            if (energyBarFill != null)
                energyBarFill.fillAmount = fill;

            // Pulse glow as bar approaches full
            if (energyBarGlow != null)
            {
                float glowAlpha = fill > 0.7f ? Mathf.PingPong(Time.time * 3f, 1f) * (fill - 0.7f) / 0.3f : 0f;
                var c = energyBarGlow.color;
                c.a = glowAlpha;
                energyBarGlow.color = c;
            }
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

        private System.Collections.IEnumerator CountdownRoutine()
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
