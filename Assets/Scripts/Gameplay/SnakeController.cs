using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Controls a single player's snake: movement tracking, belly count,
    /// eating detection, belly ache state, and ball blast glow.
    /// </summary>
    public class SnakeController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private PlayerIndex playerIndex;

        [Header("Movement")]
        [SerializeField] private float followSpeed = 12f;
        [SerializeField] private float eatRadius = 0.8f;

        [Header("Belly")]
        [SerializeField] private int maxBellyCount = 12;

        [Header("Belly Ache")]
        [SerializeField] private float bellyAcheFreezeDuration = 2.5f;
        [SerializeField] private float bellyAcheBallPopInterval = 0.4f;

        [Header("Visual References")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform bellyContainer;
        [SerializeField] private GameObject bellyBallPrefab;

        [Header("Glow (Ball Blast)")]
        [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.3f, 1f);

        public PlayerIndex Player => playerIndex;
        public int BellyCount { get; private set; }
        public int TargetNumber { get; set; }
        public EquationType CurrentEquationType { get; set; }
        public bool IsInBellyAche { get; private set; }
        public bool IsGlowing { get; private set; }
        public int BlastEatCount { get; private set; }

        private List<GameObject> _bellyBallVisuals = new List<GameObject>();
        private Color _originalColor;
        private bool _canEat = true;
        private SnakeTail _tail;

        private void Start()
        {
            BellyCount = MathSystem.Instance != null ? MathSystem.Instance.StartingBellyCount : 3;
            if (bodyRenderer != null)
                _originalColor = bodyRenderer.color;
            _tail = GetComponent<SnakeTail>();
            UpdateBellyVisuals();

            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            if (IsInBellyAche) return;
            FollowInput();
        }

        private void FollowInput()
        {
            if (TokenInputManager.Instance == null) return;

            Vector2 targetPos = TokenInputManager.Instance.GetPlayerPosition(playerIndex);
            if (!TokenInputManager.Instance.IsPlayerActive(playerIndex)) return;

            Vector3 current = transform.position;
            Vector3 target = new Vector3(targetPos.x, targetPos.y, current.z);
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

        public bool TryEat(FieldObject obj)
        {
            if (!_canEat || IsInBellyAche) return false;

            GameState state = GameManager.Instance.CurrentState;

            if (state == GameState.BallBlast)
            {
                if (obj.ObjectType == FieldObjectType.Ball)
                {
                    BlastEatCount++;
                    GameEvents.ObjectEaten(playerIndex, FieldObjectType.Ball, BellyCount);
                    return true;
                }
                return false;
            }

            if (state != GameState.NormalPlay) return false;

            // Normal play — enforce belly cap
            if (CurrentEquationType == EquationType.Addition && obj.ObjectType == FieldObjectType.Ball)
            {
                if (BellyCount >= maxBellyCount) return false; // At cap, can't eat more
                BellyCount++;
                GameEvents.ObjectEaten(playerIndex, FieldObjectType.Ball, BellyCount);
                UpdateBellyVisuals();
                CheckEquationProgress();
                return true;
            }
            else if (CurrentEquationType == EquationType.Subtraction && obj.ObjectType == FieldObjectType.Hedgehog)
            {
                if (BellyCount <= 0) return false; // Can't go below 0
                BellyCount--;
                GameEvents.ObjectEaten(playerIndex, FieldObjectType.Hedgehog, BellyCount);
                UpdateBellyVisuals();
                CheckEquationProgress();
                return true;
            }

            return false;
        }

        private void CheckEquationProgress()
        {
            if (BellyCount == TargetNumber)
            {
                GameEvents.EquationSolved(playerIndex);
            }
            else if ((CurrentEquationType == EquationType.Addition && BellyCount > TargetNumber) ||
                     (CurrentEquationType == EquationType.Subtraction && BellyCount < TargetNumber))
            {
                int overshoot = Mathf.Abs(BellyCount - TargetNumber);
                StartCoroutine(BellyAcheRoutine(overshoot));
            }
        }

        private IEnumerator BellyAcheRoutine(int overshootAmount)
        {
            IsInBellyAche = true;
            _canEat = false;
            GameEvents.BellyAcheStarted(playerIndex, overshootAmount);

            if (bodyRenderer != null)
                bodyRenderer.color = new Color(0.5f, 0.85f, 0.4f, _originalColor.a);

            yield return new WaitForSeconds(bellyAcheFreezeDuration);

            for (int i = 0; i < overshootAmount; i++)
            {
                if (CurrentEquationType == EquationType.Addition)
                    BellyCount--;
                else
                    BellyCount++;

                UpdateBellyVisuals();
                yield return new WaitForSeconds(bellyAcheBallPopInterval);
            }

            if (bodyRenderer != null)
                bodyRenderer.color = _originalColor;

            IsInBellyAche = false;
            _canEat = true;
            GameEvents.BellyAcheEnded(playerIndex);
            GameEvents.EquationSolved(playerIndex);
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
                    break;
                case GameState.NormalPlay:
                    if (IsGlowing) SetGlow(false);
                    break;
            }
        }

        /// <summary>
        /// Resets belly to a specific count. Called post-blast (reset to 3).
        /// </summary>
        public void ResetBelly(int count)
        {
            BellyCount = Mathf.Clamp(count, 0, maxBellyCount);
            BlastEatCount = 0;
            IsInBellyAche = false;
            _canEat = true;
            IsGlowing = false;
            if (bodyRenderer != null)
                bodyRenderer.color = _originalColor;
            UpdateBellyVisuals();
        }

        private void SetGlow(bool glowing)
        {
            IsGlowing = glowing;
            if (bodyRenderer != null)
                bodyRenderer.color = glowing ? glowColor : _originalColor;
        }

        private void UpdateBellyVisuals()
        {
            // Update tail segment overlays
            if (_tail != null)
                _tail.SetFilledCount(BellyCount);
        }

        private void PositionBellyBall(GameObject ball, int index)
        {
            // Arrange balls in a compact cluster inside the body
            float radius = 0.25f;
            float angle = index * 137.5f * Mathf.Deg2Rad; // golden angle for even spread
            float r = radius * Mathf.Sqrt((float)(index + 1) / maxBellyCount);
            float x = Mathf.Cos(angle) * r;
            float y = Mathf.Sin(angle) * r - 0.1f;
            ball.transform.localPosition = new Vector3(x, y, 0);
            ball.transform.localScale = Vector3.one * 0.3f;
        }

        public float GetEatRadius() => eatRadius;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, eatRadius);
        }
    }
}
