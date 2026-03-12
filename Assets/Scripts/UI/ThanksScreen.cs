using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BellyFull
{
    /// <summary>
    /// Full-screen "Thanks for Playing" slide shown after GameOver.
    /// Stays active (hidden via CanvasGroup) so it can receive the GameOver event.
    /// Drop your image sprite onto the Image component in the Inspector.
    /// </summary>
    public class ThanksScreen : MonoBehaviour
    {
        [SerializeField] private Image slideImage;
        [SerializeField] private float delayAfterWin = 2.5f;
        [SerializeField] private float fadeDuration  = 0.8f;
        [SerializeField] private float holdDuration  = 15f;

        private CanvasGroup _cg;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            Hide();
        }

        private void Start()
        {
            GameEvents.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
                StartCoroutine(ShowSlide());
        }

        private IEnumerator ShowSlide()
        {
            yield return new WaitForSeconds(delayAfterWin);

            Show();

            // Fade in
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _cg.alpha = t / fadeDuration;
                yield return null;
            }
            _cg.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(holdDuration);

            // Fade out
            t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                _cg.alpha = 1f - t / fadeDuration;
                yield return null;
            }

            Hide();

            // Return to title / front page
            GameManager.Instance?.BeginWaitForPlayers();
        }

        private void Show()
        {
            _cg.alpha          = 0f;
            _cg.blocksRaycasts = true;
            _cg.interactable   = true;
        }

        private void Hide()
        {
            _cg.alpha          = 0f;
            _cg.blocksRaycasts = false;
            _cg.interactable   = false;
        }
    }
}
