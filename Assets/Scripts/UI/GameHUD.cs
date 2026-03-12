using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BellyFull
{
    /// <summary>
    /// Per-player HUD.
    /// Hole overlays are created as children of the NumberImage at the correct
    /// sprite-UV positions so each hole has an exact world-space delivery target.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        // ── Static registry ───────────────────────────────────────────────
        public static GameHUD Get(PlayerIndex player) => _registry[(int)player];
        private static readonly GameHUD[] _registry = new GameHUD[2];

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] private PlayerIndex playerIndex;

        [Header("Number Display")]
        [SerializeField] private Sprite[] numberSprites;
        [SerializeField] private Image numberImage;

        [Header("Hole Indicators (shown below the number)")]
        [SerializeField] private Transform holesContainer;
        [SerializeField] private float holeIndicatorRadius = 32f;
        [SerializeField] private Color holeEmptyColor  = new Color(0.9f, 0.85f, 0.8f, 0.55f);
        [SerializeField] private Color holeFilledColor = new Color(0.2f, 0.9f, 0.2f, 0.95f);

        [Header("Completed Numbers Row")]
        [SerializeField] private Transform completedRowContainer;

        [Header("Crowns")]
        [SerializeField] private GameObject[] crownIcons = new GameObject[3];

        [Header("Ball Blast")]
        [SerializeField] private GameObject blastOverlay;
        [SerializeField] private TextMeshProUGUI blastEatCountText;
        [SerializeField] private TextMeshProUGUI blastTimerText;

        [Header("Game Over")]
        [SerializeField] private GameObject winBanner;

        // ── Runtime state ─────────────────────────────────────────────────
        private readonly List<Image> _holeOverlays = new List<Image>();
        private bool[] _holesFilledMask;
        private int    _currentNumber;

        // ── Unity lifecycle ───────────────────────────────────────────────

        private void Awake()
        {
            _registry[(int)playerIndex] = this;
        }

        private void OnDestroy()
        {
            if (_registry[(int)playerIndex] == this) _registry[(int)playerIndex] = null;
        }

        private void Start()
        {
            InitHUD();

            GameEvents.OnNumberAdvanced   += HandleNumberAdvanced;
            GameEvents.OnHoleDelivered    += HandleHoleDelivered;
            GameEvents.OnNumberCompleted  += HandleNumberCompleted;
            GameEvents.OnCrownAwarded     += HandleCrownAwarded;
            GameEvents.OnBallBlastStarted += HandleBlastStarted;
            GameEvents.OnBallBlastEnded   += HandleBlastEnded;
            GameEvents.OnGameWon          += HandleGameWon;
            GameEvents.OnGameStateChanged += HandleStateChanged;
        }

        private void Update()
        {
            if (BallBlastManager.Instance != null && BallBlastManager.Instance.IsBlasting)
            {
                var snake = GameManager.Instance?.GetSnake(playerIndex);
                if (snake != null && blastEatCountText != null)
                    blastEatCountText.text = snake.BlastEatCount.ToString();

                if (blastTimerText != null)
                    blastTimerText.text = Mathf.CeilToInt(BallBlastManager.Instance.BlastTimeRemaining).ToString();
            }
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Returns the index of the nearest unfilled hole within world radius,
        /// or -1 if none found / overlays not ready.
        /// </summary>
        public int GetNearestUnfilledHoleIndex(Vector3 worldPos, float radius)
        {
            int   best     = -1;
            float bestDist = radius;
            for (int i = 0; i < _holeOverlays.Count; i++)
            {
                if (_holeOverlays[i] == null) continue;
                if (_holesFilledMask != null && i < _holesFilledMask.Length && _holesFilledMask[i]) continue;
                float dist = Vector2.Distance(worldPos, OverlayToWorld(_holeOverlays[i].rectTransform));
                if (dist < bestDist) { bestDist = dist; best = i; }
            }
            return best;
        }

        // ── Init ──────────────────────────────────────────────────────────

        private void InitHUD()
        {
            if (blastOverlay != null) blastOverlay.SetActive(false);
            if (winBanner    != null) winBanner.SetActive(false);
            foreach (var c in crownIcons)
                if (c != null) c.SetActive(false);

            RefreshNumber(1, 0);
        }

        // ── Number / hole display ─────────────────────────────────────────

        private void RefreshNumber(int number, int holesFilled)
        {
            _currentNumber  = number;
            _holesFilledMask = new bool[number];

            // Swap sprite
            if (numberImage != null && numberSprites != null &&
                number >= 1 && number <= numberSprites.Length &&
                numberSprites[number - 1] != null)
            {
                numberImage.sprite = numberSprites[number - 1];
                numberImage.gameObject.SetActive(true);
            }

            // Rebuild hole overlays
            ClearHoleOverlays();
            StartCoroutine(BuildHoleOverlays(number, holesFilled));
        }

        private void ClearHoleOverlays()
        {
            foreach (var img in _holeOverlays)
                if (img != null) Destroy(img.gameObject);
            _holeOverlays.Clear();
        }

        private IEnumerator BuildHoleOverlays(int number, int prefilledCount)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            if (numberImage == null || numberImage.sprite == null) yield break;

            var uvs = (number >= 1 && number < HolePositionData.HoleUVs.Length)
                      ? HolePositionData.HoleUVs[number] : null;
            if (uvs == null) yield break;

            var   rt      = numberImage.rectTransform;
            float rectW   = rt.rect.width;
            float rectH   = rt.rect.height;
            float spriteW = numberImage.sprite.rect.width;
            float spriteH = numberImage.sprite.rect.height;
            float scale   = Mathf.Min(rectW / spriteW, rectH / spriteH);
            float rendW   = spriteW * scale;
            float rendH   = spriteH * scale;
            float xOff    = (rectW - rendW) * 0.5f;
            float yOff    = (rectH - rendH) * 0.5f;
            float d       = holeIndicatorRadius * 2f;

            for (int i = 0; i < uvs.Length; i++)
            {
                var go = new GameObject($"HoleSlot_{i}", typeof(RectTransform));
                go.transform.SetParent(rt, false);

                var holeRT       = go.GetComponent<RectTransform>();
                holeRT.anchorMin = holeRT.anchorMax = new Vector2(0.5f, 0.5f);
                holeRT.pivot     = new Vector2(0.5f, 0.5f);
                holeRT.sizeDelta = new Vector2(d, d);

                float localX = xOff + uvs[i].x * rendW;
                float localY = yOff + (1f - uvs[i].y) * rendH;
                holeRT.anchoredPosition = new Vector2(localX - rectW * 0.5f, localY - rectH * 0.5f);

                var img   = go.AddComponent<Image>();
                img.color = i < prefilledCount ? holeFilledColor : holeEmptyColor;
                _holeOverlays.Add(img);
            }
        }

        // ── Hole fill ─────────────────────────────────────────────────────

        private void FillHoleOverlay(int index)
        {
            if (index < 0 || index >= _holeOverlays.Count) return;
            if (_holeOverlays[index] == null) return;
            _holeOverlays[index].color = holeFilledColor;
            StartCoroutine(PopScale(_holeOverlays[index].transform));
        }

        private IEnumerator PopScale(Transform t)
        {
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float s = 1f + 0.5f * Mathf.Sin(elapsed / 0.25f * Mathf.PI);
                t.localScale = Vector3.one * s;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        // ── World position helper ─────────────────────────────────────────

        private static Vector3 OverlayToWorld(RectTransform rt)
        {
            // Screen Space Overlay: RectTransform.position is in screen pixels
            Vector3 sp    = rt.position;
            float   depth = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
            world.z = 0f;
            return world;
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void HandleNumberAdvanced(PlayerIndex player, int newNumber)
        {
            if (player != playerIndex) return;
            RefreshNumber(newNumber, 0);
        }

        private void HandleHoleDelivered(PlayerIndex player, int holeIndex, int filled, int total)
        {
            if (player != playerIndex) return;
            if (_holesFilledMask != null && holeIndex < _holesFilledMask.Length)
                _holesFilledMask[holeIndex] = true;
            FillHoleOverlay(holeIndex);
        }

        private void HandleNumberCompleted(PlayerIndex player, int number)
        {
            if (player != playerIndex) return;
            StartCoroutine(NumberCompleteCelebration());
            AddCompletedBadge(number);
        }

        private void AddCompletedBadge(int number)
        {
            if (completedRowContainer == null) return;
            if (numberSprites == null || number < 1 || number > numberSprites.Length) return;

            var go = new GameObject($"Badge_{number}", typeof(RectTransform));
            go.transform.SetParent(completedRowContainer, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(42f, 56f);

            var img = go.AddComponent<Image>();
            img.sprite = numberSprites[number - 1];
            img.preserveAspect = true;

            StartCoroutine(PopInBadge(go.transform));
        }

        private IEnumerator PopInBadge(Transform t)
        {
            t.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < 0.35f)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / 0.35f;
                // EaseOutBack
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float s = 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
                t.localScale = Vector3.one * Mathf.Max(0f, s);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private IEnumerator FadeOutCompletedRow()
        {
            if (completedRowContainer == null) yield break;
            var cg = completedRowContainer.GetComponent<CanvasGroup>();
            if (cg == null) cg = completedRowContainer.gameObject.AddComponent<CanvasGroup>();
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - t / 0.4f;
                yield return null;
            }
            cg.alpha = 0f;
            foreach (Transform child in completedRowContainer)
                Destroy(child.gameObject);
        }

        private IEnumerator NumberCompleteCelebration()
        {
            if (numberImage == null) yield break;

            Vector3 orig = numberImage.transform.localScale;
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float s = 1f + 0.35f * Mathf.Sin(elapsed / 0.5f * Mathf.PI);
                numberImage.transform.localScale = orig * s;
                yield return null;
            }
            numberImage.transform.localScale = orig;
        }

        private void HandleCrownAwarded(PlayerIndex player, int total)
        {
            if (player != playerIndex) return;
            for (int i = 0; i < crownIcons.Length && i < total; i++)
                if (crownIcons[i] != null) crownIcons[i].SetActive(true);
        }

        private void HandleBlastStarted()
        {
            if (blastOverlay != null) blastOverlay.SetActive(true);
            if (blastEatCountText != null) { blastEatCountText.gameObject.SetActive(true); blastEatCountText.text = "0"; }
        }

        private void HandleBlastEnded(int p1, int p2)
        {
            if (blastOverlay      != null) blastOverlay.SetActive(false);
            if (blastEatCountText != null) blastEatCountText.gameObject.SetActive(false);
        }

        private void HandleGameWon(PlayerIndex winner)
        {
            if (winner == playerIndex && winBanner != null) winBanner.SetActive(true);
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.PreBlast)
            {
                // Hide number and holes — BlastUI takes over centre display
                if (numberImage != null) numberImage.gameObject.SetActive(false);
                ClearHoleOverlays();
                StartCoroutine(FadeOutCompletedRow());
                return;
            }

            if (state != GameState.NormalPlay) return;
            if (blastOverlay != null) blastOverlay.SetActive(false);

            // Reset completed row alpha and clear badges for new round
            if (completedRowContainer != null)
            {
                var cg = completedRowContainer.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
                foreach (Transform child in completedRowContainer)
                    Destroy(child.gameObject);
            }

            int crowns = GameManager.Instance != null ? GameManager.Instance.CrownCounts[(int)playerIndex] : 0;
            foreach (var c in crownIcons) if (c != null) c.SetActive(false);
            for (int i = 0; i < crownIcons.Length && i < crowns; i++)
                if (crownIcons[i] != null) crownIcons[i].SetActive(true);
            RefreshNumber(1, 0);
        }
    }
}
