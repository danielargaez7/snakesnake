using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// A hedgehog or blast ball on the shared field.
    /// Hedgehogs wander randomly; flee from snakes (extended flee radius for leading player).
    /// </summary>
    public class FieldObject : MonoBehaviour
    {
        [SerializeField] private FieldObjectType objectType;

        [Header("Wander")]
        [SerializeField] private float wanderSpeed = 2.4f;
        [SerializeField] private float wanderTargetChangeInterval = 4.5f;

        [Header("Flee")]
        [SerializeField] private float fleeSpeed = 5.5f;
        [SerializeField] private float fleeRadius = 2.2f;
        [SerializeField] private float rubberBandFleeRadius = 3.8f; // for leading player

        [Header("Separation")]
        [SerializeField] private float separationRadius = 0.6f;
        [SerializeField] private float separationForce = 2.5f;

        public FieldObjectType ObjectType   => objectType;
        public HedgehogBehavior CurrentBehavior { get; set; } = HedgehogBehavior.Wandering;

        private Vector2 _wanderTarget;
        private float   _wanderTimer;
        private Vector2 _velocity;
        private Rect    _fieldBounds;
        private SpriteRenderer _renderer;
        private Animator _animator;

        private static readonly List<FieldObject> _allObjects = new List<FieldObject>();

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _allObjects.Add(this);
        }

        private void OnDestroy()
        {
            _allObjects.Remove(this);
        }

        public void Initialize(Rect fieldBounds)
        {
            _fieldBounds = fieldBounds;
            PickNewWanderTarget();
        }

        private void Update()
        {
            if (CurrentBehavior == HedgehogBehavior.Hidden) return;
            if (objectType == FieldObjectType.Ball) return; // blast balls are static

            UpdateMovement();
            ApplySeparation();
            KeepInBounds();

            // Flip sprite to face movement direction (side-view sprite)
            bool moving = _velocity.sqrMagnitude > 0.05f;
            if (_animator != null)
                _animator.SetBool("isMoving", moving);

            if (moving && Mathf.Abs(_velocity.x) > 0.05f && _renderer != null)
                _renderer.flipX = _velocity.x < 0;
        }

        private void UpdateMovement()
        {
            // Check if any snake is close enough to flee from
            Vector2 fleeDir = GetFleeDirection();

            if (fleeDir.sqrMagnitude > 0.001f)
            {
                // Add organic randomness to flee
                Vector2 perp = new Vector2(-fleeDir.y, fleeDir.x);
                fleeDir += perp * (Mathf.PerlinNoise(Time.time * 3f + GetInstanceID(), 0) - 0.5f) * 0.4f;
                fleeDir.Normalize();

                _velocity = Vector2.Lerp(_velocity, fleeDir * fleeSpeed, 7f * Time.deltaTime);
            }
            else
            {
                // Normal wander
                _wanderTimer -= Time.deltaTime;
                if (_wanderTimer <= 0f ||
                    Vector2.Distance(transform.position, _wanderTarget) < 0.4f)
                {
                    PickNewWanderTarget();
                }

                Vector2 toTarget = (_wanderTarget - (Vector2)transform.position).normalized;
                _velocity = Vector2.Lerp(_velocity, toTarget * wanderSpeed, 3f * Time.deltaTime);
            }

            transform.position += (Vector3)(_velocity * Time.deltaTime);
        }

        private Vector2 GetFleeDirection()
        {
            if (GameManager.Instance == null) return Vector2.zero;

            Vector2 myPos = transform.position;
            Vector2 totalFlee = Vector2.zero;

            for (int i = 0; i < 2; i++)
            {
                var snake = GameManager.Instance.GetSnake((PlayerIndex)i);
                if (snake == null || snake.IsCarrying) continue; // already carrying, ignore

                float radius = GetFleeRadiusFor((PlayerIndex)i);
                float dist = Vector2.Distance(myPos, snake.transform.position);

                if (dist < radius)
                {
                    Vector2 away = (myPos - (Vector2)snake.transform.position).normalized;
                    float weight = 1f - (dist / radius);
                    totalFlee += away * weight;
                }
            }

            return totalFlee;
        }

        private float GetFleeRadiusFor(PlayerIndex player)
        {
            if (NumberManager.Instance == null) return fleeRadius;

            var leader = NumberManager.Instance.GetLeadingPlayer();
            if (leader == player && NumberManager.Instance.GetPlayerGap() >= 2)
                return rubberBandFleeRadius;

            return fleeRadius;
        }

        private void PickNewWanderTarget()
        {
            // Bias toward the opposite side of the field so hedgehogs spread out
            float cx = _fieldBounds.center.x;
            float cy = _fieldBounds.center.y;
            Vector2 myPos = transform.position;

            float x = myPos.x < cx
                ? Random.Range(cx * 0.1f, _fieldBounds.xMax - 0.5f)
                : Random.Range(_fieldBounds.xMin + 0.5f, cx * 0.1f);

            float y = myPos.y < cy
                ? Random.Range(cy * 0.1f, _fieldBounds.yMax - 0.5f)
                : Random.Range(_fieldBounds.yMin + 0.5f, cy * 0.1f);

            _wanderTarget = new Vector2(x, y);
            _wanderTimer = Random.Range(wanderTargetChangeInterval * 1.0f,
                                        wanderTargetChangeInterval * 2.0f);
        }

        private void ApplySeparation()
        {
            Vector2 myPos = transform.position;
            Vector2 push = Vector2.zero;

            foreach (var other in _allObjects)
            {
                if (other == this || other == null) continue;
                float dist = Vector2.Distance(myPos, other.transform.position);
                if (dist < separationRadius && dist > 0.01f)
                {
                    float strength = 1f - (dist / separationRadius);
                    push += (myPos - (Vector2)other.transform.position).normalized * strength;
                }
            }

            if (push.sqrMagnitude > 0.001f)
                transform.position += (Vector3)(push.normalized * separationForce * Time.deltaTime);
        }

        private void KeepInBounds()
        {
            Vector3 p = transform.position;
            if (p.x < _fieldBounds.xMin) { p.x = _fieldBounds.xMin; _velocity.x =  Mathf.Abs(_velocity.x) * 0.8f; }
            if (p.x > _fieldBounds.xMax) { p.x = _fieldBounds.xMax; _velocity.x = -Mathf.Abs(_velocity.x) * 0.8f; }
            if (p.y < _fieldBounds.yMin) { p.y = _fieldBounds.yMin; _velocity.y =  Mathf.Abs(_velocity.y) * 0.8f; }
            if (p.y > _fieldBounds.yMax) { p.y = _fieldBounds.yMax; _velocity.y = -Mathf.Abs(_velocity.y) * 0.8f; }
            transform.position = p;
        }

        public void SetHidden(bool hidden)
        {
            CurrentBehavior = hidden ? HedgehogBehavior.Hidden : HedgehogBehavior.Wandering;
            if (_renderer != null) _renderer.enabled = !hidden;
        }

        /// <summary>Scale-in from zero with EaseOutBack — used when bubbles are "thrown" onto screen.</summary>
        public void ScaleIn(float delay = 0f)
        {
            transform.localScale = Vector3.zero;
            StartCoroutine(ScaleInCoroutine(delay));
        }

        private IEnumerator ScaleInCoroutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            float elapsed = 0f;
            const float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / duration;
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float s = 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
                transform.localScale = Vector3.one * Mathf.Max(0f, s);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        /// <summary>Bubble pop animation — brief expand then shrink to zero, then destroys.</summary>
        public void Pop()
        {
            if (CurrentBehavior == HedgehogBehavior.Hidden) return;
            CurrentBehavior = HedgehogBehavior.Hidden;
            StartCoroutine(PopCoroutine());
        }

        private IEnumerator PopCoroutine()
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Expand to 1.6x in first 40%, shrink to 0 in remaining 60%
                float s = t < 0.4f
                    ? Mathf.Lerp(1f, 1.6f, t / 0.4f)
                    : Mathf.Lerp(1.6f, 0f, (t - 0.4f) / 0.6f);
                transform.localScale = startScale * s;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
