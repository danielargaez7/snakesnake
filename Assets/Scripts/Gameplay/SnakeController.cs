using System.Collections;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Controls one player's snake: token-following movement, catch hedgehog,
    /// carry visual (ball shown at snake tip), deliver to number zone, Ball Blast glow.
    /// </summary>
    public class SnakeController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private PlayerIndex playerIndex;

        [Header("Movement")]
        [SerializeField] private float followSpeed = 12f;
        [SerializeField] private float catchRadius = 0.7f;

        [Header("Carry Visual")]
        [Tooltip("Small circle/ball GameObject that appears under the snake tip when carrying")]
        [SerializeField] private GameObject carriedBallVisual;
        [Tooltip("Local offset from snake center to show carried ball (tip of snout)")]
        [SerializeField] private Vector3 carryOffset = new Vector3(0f, 0.45f, 0f);

        [Header("Glow (Ball Blast)")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.3f, 1f);

        public PlayerIndex Player    => playerIndex;
        public bool IsCarrying       { get; private set; }
        public bool IsGlowing        { get; private set; }
        public int  BlastEatCount    { get; private set; }
        public bool IsMoving         { get; private set; }

        private Color _originalColor;
        private SnakeTail _tail;
        private Vector3 _prevPosition;

        private void Start()
        {
            _prevPosition = transform.position;
            if (bodyRenderer != null)
                _originalColor = bodyRenderer.color;
            _tail = GetComponent<SnakeTail>();

            if (carriedBallVisual != null)
                carriedBallVisual.SetActive(false);

            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            Vector3 before = transform.position;
            FollowInput();
            IsMoving = (transform.position - before).sqrMagnitude > 0.00001f;
            _prevPosition = transform.position;

            // Keep carried ball at snake tip in world space
            if (IsCarrying && carriedBallVisual != null)
            {
                // carryOffset is in local space; transform it to world
                carriedBallVisual.transform.position =
                    transform.position + transform.rotation * carryOffset;
                carriedBallVisual.transform.rotation = Quaternion.identity;
            }
        }

        private void FollowInput()
        {
            if (TokenInputManager.Instance == null) return;
            if (!TokenInputManager.Instance.IsPlayerActive(playerIndex)) return;

            Vector2 targetPos = TokenInputManager.Instance.GetPlayerPosition(playerIndex);
            Vector3 current = transform.position;
            Vector3 target  = new Vector3(targetPos.x, targetPos.y, current.z);

            transform.position = Vector3.Lerp(current, target, followSpeed * Time.deltaTime);

            // Face movement direction
            Vector3 diff = target - current;
            if (diff.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Quaternion.Euler(0, 0, angle), 8f * Time.deltaTime);
            }
        }

        /// <summary>Called by FieldManager when snake reaches an uncaught hedgehog.</summary>
        public void CatchHedgehog()
        {
            if (IsCarrying) return;
            IsCarrying = true;
            if (carriedBallVisual != null)
                carriedBallVisual.SetActive(true);
            GameEvents.HedgehogCaught(playerIndex);
        }

        /// <summary>Called by FieldManager when carrying snake reaches a specific hole.</summary>
        public void DeliverBallToHole(int holeIndex)
        {
            if (!IsCarrying) return;
            IsCarrying = false;
            if (carriedBallVisual != null)
                carriedBallVisual.SetActive(false);
            NumberManager.Instance?.ReceiveDeliveryToHole(playerIndex, holeIndex);
        }

        /// <summary>Called by FieldManager during Ball Blast to eat a blast ball.</summary>
        public void EatBlastBall()
        {
            BlastEatCount++;
            GameEvents.BlastBallEaten(playerIndex);
        }

        public void ResetForRound()
        {
            IsCarrying = false;
            BlastEatCount = 0;
            if (carriedBallVisual != null)
                carriedBallVisual.SetActive(false);
            if (bodyRenderer != null)
                bodyRenderer.color = _originalColor;
        }

        private void HandleGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.BallBlast:
                    SetGlow(true);
                    BlastEatCount = 0;
                    break;
                case GameState.CrownAward:
                    SetGlow(false);
                    // Drop any carried ball on blast end
                    IsCarrying = false;
                    if (carriedBallVisual != null) carriedBallVisual.SetActive(false);
                    break;
                case GameState.NormalPlay:
                    SetGlow(false);
                    IsCarrying = false;
                    if (carriedBallVisual != null) carriedBallVisual.SetActive(false);
                    break;
            }
        }

        private void SetGlow(bool on)
        {
            IsGlowing = on;
            if (bodyRenderer != null)
                bodyRenderer.color = on ? glowColor : _originalColor;
        }

        public float GetCatchRadius() => catchRadius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, catchRadius);
            // Show carry position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + transform.rotation * carryOffset, 0.2f);
        }
    }
}
