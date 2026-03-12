using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BellyFull
{
    /// <summary>
    /// Controls all Ball Blitz overlay UI:
    ///   PreBlast  → #5 sprite scales in at top-center
    ///   BallBlast → countdown 5→1 in center during last 5s of blast
    ///   BlastEnded → "GREAT JOB!" + confetti
    /// </summary>
    public class BlastUI : MonoBehaviour
    {
        public static BlastUI Instance { get; private set; }

        [Header("Center Number Display (shows #5 during pre-blast)")]
        [SerializeField] private Image centerNumberImage;
        [SerializeField] private Sprite[] numberSprites;

        [Header("Countdown (shown during blast)")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Great Job")]
        [SerializeField] private TextMeshProUGUI greatJobText;

        [Header("Confetti")]
        [SerializeField] private RectTransform confettiContainer;
        [SerializeField] private int confettiCount = 70;

        private bool _countdownStarted;

        private static readonly Color[] ConfettiColors =
        {
            new Color(1f,  0.2f, 0.2f),
            new Color(0.2f, 0.85f, 0.2f),
            new Color(0.2f, 0.45f, 1f),
            new Color(1f,  0.9f, 0.1f),
            new Color(1f,  0.4f, 0.85f),
            new Color(0.4f, 0.95f, 1f),
            new Color(1f,  0.6f, 0.1f),
        };

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            HideAll();
            GameEvents.OnGameStateChanged += HandleStateChanged;
            GameEvents.OnBallBlastEnded   += HandleBlastEnded;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleStateChanged;
            GameEvents.OnBallBlastEnded   -= HandleBlastEnded;
        }

        private void Update()
        {
            // Trigger countdown during last 5 seconds of the blast
            if (!_countdownStarted &&
                BallBlastManager.Instance != null &&
                BallBlastManager.Instance.IsBlasting &&
                BallBlastManager.Instance.BlastTimeRemaining <= 5f)
            {
                _countdownStarted = true;
                StartCoroutine(CountdownSequence());
            }
        }

        // ── State events ──────────────────────────────────────────────────────

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.PreBlast)
            {
                // Show #5 sprite centre-top as transition into blast
                StartCoroutine(ShowNumberFive());
            }
            else if (state == GameState.BallBlast)
            {
                // Hide the #5 display; countdown will start via Update() when ≤5s remain
                if (centerNumberImage != null) centerNumberImage.gameObject.SetActive(false);
                _countdownStarted = false;
            }
            else if (state == GameState.NormalPlay)
            {
                StopAllCoroutines();
                _countdownStarted = false;
                HideAll();
                ClearConfetti();
            }
        }

        private void HandleBlastEnded(int p1, int p2)
        {
            StopAllCoroutines();
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            StartCoroutine(GreatJobAndConfetti());
        }

        // ── Pre-blast: #5 flies to centre-top ────────────────────────────────

        private IEnumerator ShowNumberFive()
        {
            if (centerNumberImage == null) yield break;
            if (numberSprites == null || numberSprites.Length < 5 || numberSprites[4] == null) yield break;

            centerNumberImage.sprite = numberSprites[4];
            centerNumberImage.gameObject.SetActive(true);
            centerNumberImage.transform.localScale = Vector3.zero;

            float t = 0f;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float p = t / 0.55f;
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float s = 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
                centerNumberImage.transform.localScale = Vector3.one * Mathf.Max(0f, s);
                yield return null;
            }
            centerNumberImage.transform.localScale = Vector3.one;
            // Stays visible until BallBlast state (HandleStateChanged hides it)
        }

        // ── Blast countdown 5→1 ───────────────────────────────────────────────

        private IEnumerator CountdownSequence()
        {
            if (countdownText == null) yield break;

            countdownText.gameObject.SetActive(true);

            for (int i = 5; i >= 1; i--)
            {
                countdownText.text = i.ToString();
                countdownText.color = CountdownColor(i);
                countdownText.transform.localScale = Vector3.one * 1.6f;

                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(1.6f, 0.85f, Mathf.SmoothStep(0f, 1f, t));
                    countdownText.transform.localScale = Vector3.one * s;
                    yield return null;
                }
            }

            countdownText.gameObject.SetActive(false);
        }

        // ── Great Job + confetti ──────────────────────────────────────────────

        private IEnumerator GreatJobAndConfetti()
        {
            if (greatJobText == null) yield break;

            greatJobText.gameObject.SetActive(true);
            greatJobText.transform.localScale = Vector3.zero;

            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                float p = t / 0.45f;
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float s = 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
                greatJobText.transform.localScale = Vector3.one * Mathf.Max(0f, s);
                yield return null;
            }
            greatJobText.transform.localScale = Vector3.one;

            SpawnConfetti();
            yield return new WaitForSeconds(1.5f);

            greatJobText.gameObject.SetActive(false);
            ClearConfetti();
        }

        // ── Confetti ──────────────────────────────────────────────────────────

        private void SpawnConfetti()
        {
            if (confettiContainer == null) return;
            for (int i = 0; i < confettiCount; i++)
            {
                var go = new GameObject("Confetti", typeof(RectTransform));
                go.transform.SetParent(confettiContainer, false);

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(Random.Range(9f, 18f), Random.Range(9f, 18f));
                rt.anchoredPosition = new Vector2(Random.Range(-920f, 920f), Random.Range(350f, 580f));
                rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

                var img = go.AddComponent<Image>();
                img.color = ConfettiColors[Random.Range(0, ConfettiColors.Length)];

                StartCoroutine(AnimateConfetti(rt));
            }
        }

        private IEnumerator AnimateConfetti(RectTransform rt)
        {
            if (rt == null) yield break;
            float fallSpeed = Random.Range(280f, 560f);
            float sway      = Random.Range(60f, 160f);
            float swayFreq  = Random.Range(1f, 3f);
            float startX    = rt.anchoredPosition.x;
            float duration  = Random.Range(1.4f, 2.6f);
            float elapsed   = 0f;

            while (elapsed < duration && rt != null)
            {
                elapsed += Time.deltaTime;
                rt.anchoredPosition = new Vector2(
                    startX + Mathf.Sin(elapsed * swayFreq * Mathf.PI * 2f) * sway,
                    rt.anchoredPosition.y - fallSpeed * Time.deltaTime);
                rt.Rotate(0f, 0f, 200f * Time.deltaTime);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        private void ClearConfetti()
        {
            if (confettiContainer == null) return;
            foreach (Transform child in confettiContainer)
                Destroy(child.gameObject);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void HideAll()
        {
            if (centerNumberImage != null) centerNumberImage.gameObject.SetActive(false);
            if (countdownText     != null) countdownText.gameObject.SetActive(false);
            if (greatJobText      != null) greatJobText.gameObject.SetActive(false);
        }

        private static Color CountdownColor(int i) => i switch
        {
            5 => new Color(0.3f, 0.9f, 0.3f),
            4 => new Color(0.5f, 0.85f, 1f),
            3 => new Color(1f, 0.9f, 0.2f),
            2 => new Color(1f, 0.5f, 0.1f),
            _ => new Color(1f, 0.2f, 0.2f),
        };
    }
}
