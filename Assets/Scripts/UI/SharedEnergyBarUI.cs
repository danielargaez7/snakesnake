using UnityEngine;
using UnityEngine.UI;

namespace BellyFull
{
    /// <summary>
    /// Shared energy bar displayed at top-center of screen.
    /// Frame sprite sits on top, fill sprite underneath shows progress.
    /// </summary>
    public class SharedEnergyBarUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image frameImage;

        private void Start()
        {
            GameEvents.OnEnergyBarChanged += HandleEnergyBarChanged;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;

            if (fillImage != null)
                fillImage.fillAmount = 0f;
        }

        private void OnDestroy()
        {
            GameEvents.OnEnergyBarChanged -= HandleEnergyBarChanged;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleEnergyBarChanged(float fill)
        {
            if (fillImage != null)
                fillImage.fillAmount = fill;

            // Pulse the fill color as it approaches full
            if (fillImage != null && fill > 0.7f)
            {
                float pulse = Mathf.PingPong(Time.time * 3f, 1f) * (fill - 0.7f) / 0.3f;
                fillImage.color = Color.Lerp(new Color(0.2f, 0.8f, 0.2f), Color.yellow, pulse);
            }
            else if (fillImage != null)
            {
                fillImage.color = new Color(0.2f, 0.8f, 0.2f); // green
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            // Show/hide based on game state
            gameObject.SetActive(state == GameState.NormalPlay || state == GameState.WaitingForPlayers);
        }
    }
}
