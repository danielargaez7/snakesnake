using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BellyFull
{
    /// <summary>
    /// Drives the three pre-game screens:
    ///   TitleScreen      → full-screen title graphic, tap/key to advance
    ///   WaitingForPlayers→ "Place Your Tokens!" + per-player ready indicators
    ///   Countdown        → 3-2-1-GO with snake wiggle animation
    /// </summary>
    public class IntroManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject waitingPanel;
        [SerializeField] private GameObject countdownPanel;

        // ------------------------------------------------------------------
        // Title Screen
        // ------------------------------------------------------------------
        [Header("Title Screen")]
        [Tooltip("Drop your title screen graphic here as a Sprite on this Image")]
        [SerializeField] private Image titleImage;

        // ------------------------------------------------------------------
        // Waiting for Players
        // ------------------------------------------------------------------
        [Header("Waiting for Players")]
        [SerializeField] private GameObject p1ReadyGlow;   // activated when P1 token detected
        [SerializeField] private GameObject p2ReadyGlow;   // activated when P2 token detected

        // ------------------------------------------------------------------
        // Countdown
        // ------------------------------------------------------------------
        [Header("Countdown")]
        [SerializeField] private TMP_Text countdownText;

        // ------------------------------------------------------------------
        // Snake wiggle references
        // ------------------------------------------------------------------
        [Header("Snakes (wiggle during countdown)")]
        [SerializeField] private Transform snake1Transform;
        [SerializeField] private Transform snake2Transform;

        // ==================== lifecycle ====================

        private void OnEnable()  => GameEvents.OnGameStateChanged += HandleStateChanged;
        private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

        private void HandleStateChanged(GameState state)
        {
            if (titlePanel)    titlePanel.SetActive(state == GameState.TitleScreen);
            if (waitingPanel)  waitingPanel.SetActive(state == GameState.WaitingForPlayers);
            if (countdownPanel) countdownPanel.SetActive(state == GameState.Countdown);

            StopAllCoroutines();

            switch (state)
            {
                case GameState.TitleScreen:
                    StartCoroutine(TitleScreenRoutine());
                    break;
                case GameState.WaitingForPlayers:
                    StartCoroutine(WaitForPlayersRoutine());
                    break;
                case GameState.Countdown:
                    StartCoroutine(CountdownRoutine());
                    break;
            }
        }

        // ==================== Title Screen ====================

        private IEnumerator TitleScreenRoutine()
        {
            // Wait for any key (editor) or touch (device)
            while (true)
            {
                if (UnityEngine.Input.anyKeyDown) break;
                if (UnityEngine.Input.touchCount > 0) break;
                yield return null;
            }
            GameManager.Instance.BeginWaitForPlayers();
        }

        // ==================== Wait for Players ====================

        private IEnumerator WaitForPlayersRoutine()
        {
            UpdateReadyGlows(false, false);

            while (true)
            {
                bool p1 = TokenInputManager.Instance != null && TokenInputManager.Instance.Player1Active;
                bool p2 = TokenInputManager.Instance != null && TokenInputManager.Instance.Player2Active;

#if UNITY_EDITOR
                // In editor: press Space to simulate both tokens placed
                if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) { p1 = true; p2 = true; }
#endif
                UpdateReadyGlows(p1, p2);
                if (p1 && p2) break;
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);
            GameManager.Instance.BeginCountdown();
        }

        // ==================== Countdown ====================

        private IEnumerator CountdownRoutine()
        {
            // Wiggle both snakes for the duration of the countdown
            StartCoroutine(WiggleSnake(snake1Transform, 3.8f));
            StartCoroutine(WiggleSnake(snake2Transform, 3.8f));

            string[] steps     = { "3",   "2",   "1",   "GO!" };
            float[]  durations = { 0.85f, 0.85f, 0.85f, 0.6f  };

            for (int i = 0; i < steps.Length; i++)
            {
                if (countdownText) countdownText.text = steps[i];

                // Scale punch on each number
                float elapsed = 0f;
                float dur = durations[i];
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / dur;
                    if (countdownText)
                        countdownText.transform.localScale = Vector3.Lerp(Vector3.one * 1.6f, Vector3.one, t);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.2f);
            GameManager.Instance.BeginPlay();
        }

        // ==================== helpers ====================

        private IEnumerator WiggleSnake(Transform t, float duration)
        {
            if (t == null) yield break;
            float elapsed = 0f;
            float speed   = 14f;
            float amount  = 0.13f;
            Vector3 origin = t.localPosition;

            while (elapsed < duration)
            {
                t.localPosition = origin + new Vector3(Mathf.Sin(elapsed * speed) * amount, 0f, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localPosition = origin;
        }

        private void UpdateReadyGlows(bool p1, bool p2)
        {
            if (p1ReadyGlow) p1ReadyGlow.SetActive(p1);
            if (p2ReadyGlow) p2ReadyGlow.SetActive(p2);
        }
    }
}
