using System.Collections.Generic;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Base class for all field objects (balls, hedgehogs, flowers).
    /// Handles dodge behavior, movement, and lifecycle.
    /// On a shared field, dodging objects flee from BOTH snakes.
    /// </summary>
    public class FieldObject : MonoBehaviour
    {
        [SerializeField] private FieldObjectType objectType;
        [SerializeField] private float dodgeSpeed = 6f;
        [SerializeField] private float dodgeDetectRadius = 2.5f;

        public FieldObjectType ObjectType => objectType;
        public DodgeBehavior CurrentBehavior { get; set; } = DodgeBehavior.Idle;

        [SerializeField] private float separationRadius = 0.7f;
        [SerializeField] private float separationForce = 3f;

        private Vector2 _velocity;
        private Rect _fieldBounds;
        private SpriteRenderer _renderer;

        private static readonly List<FieldObject> _allObjects = new List<FieldObject>();

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _allObjects.Add(this);
        }

        private void OnDestroy()
        {
            _allObjects.Remove(this);
        }

        public void Initialize(Rect fieldBounds)
        {
            _fieldBounds = fieldBounds;
        }

        private void Update()
        {
            switch (CurrentBehavior)
            {
                case DodgeBehavior.Dodging:
                    UpdateDodge();
                    break;
                case DodgeBehavior.Idle:
                    UpdateIdle();
                    break;
                case DodgeBehavior.Frozen:
                case DodgeBehavior.Hidden:
                    break;
            }

            ApplySeparation();
            KeepInBounds();
        }

        private void ApplySeparation()
        {
            Vector2 myPos = transform.position;
            Vector2 pushDir = Vector2.zero;

            for (int i = 0; i < _allObjects.Count; i++)
            {
                var other = _allObjects[i];
                if (other == this || other == null) continue;

                Vector2 otherPos = other.transform.position;
                float dist = Vector2.Distance(myPos, otherPos);

                if (dist < separationRadius && dist > 0.01f)
                {
                    Vector2 away = (myPos - otherPos).normalized;
                    float strength = 1f - (dist / separationRadius);
                    pushDir += away * strength;
                }
            }

            if (pushDir.sqrMagnitude > 0.001f)
            {
                transform.position += (Vector3)(pushDir.normalized * separationForce * Time.deltaTime);
            }
        }

        private void UpdateDodge()
        {
            if (GameManager.Instance == null) return;

            // Only flee from snakes that would want to eat this object type
            // Balls flee from Addition snakes, hedgehogs flee from Subtraction snakes
            Vector2 myPos = transform.position;
            Vector2 totalFleeDir = Vector2.zero;
            bool anyClose = false;

            for (int i = 0; i < 2; i++)
            {
                SnakeController snake = GameManager.Instance.GetSnake((PlayerIndex)i);
                if (snake == null) continue;

                // Skip snakes that don't eat this object type
                bool wouldEat = (objectType == FieldObjectType.Ball && snake.CurrentEquationType == EquationType.Addition)
                             || (objectType == FieldObjectType.Hedgehog && snake.CurrentEquationType == EquationType.Subtraction);
                if (!wouldEat) continue;

                Vector2 snakePos = snake.transform.position;
                float dist = Vector2.Distance(myPos, snakePos);

                if (dist < dodgeDetectRadius)
                {
                    Vector2 fleeDir = (myPos - snakePos).normalized;
                    // Weight by proximity — closer snake has more influence
                    float weight = 1f - (dist / dodgeDetectRadius);
                    totalFleeDir += fleeDir * weight;
                    anyClose = true;

                    // Log dodge attempt for data tracking
                    if (dist < snake.GetEatRadius() * 1.5f)
                    {
                        GameEvents.DodgeAttempt(snake.Player, objectType);
                    }
                }
            }

            if (anyClose)
            {
                // Add perpendicular randomness for organic feel
                totalFleeDir.Normalize();
                Vector2 perp = new Vector2(-totalFleeDir.y, totalFleeDir.x);
                totalFleeDir += perp * (Mathf.PerlinNoise(Time.time * 3f + GetInstanceID(), 0) - 0.5f) * 0.5f;
                totalFleeDir.Normalize();

                _velocity = Vector2.Lerp(_velocity, totalFleeDir * dodgeSpeed, 6f * Time.deltaTime);
            }
            else
            {
                _velocity *= 0.95f;
            }

            transform.position += (Vector3)(_velocity * Time.deltaTime);
        }

        private void UpdateIdle()
        {
            if (objectType == FieldObjectType.Flower)
            {
                float sway = Mathf.Sin(Time.time * 2f + GetInstanceID()) * 0.02f;
                transform.position += new Vector3(sway * Time.deltaTime, 0, 0);
            }
            else
            {
                float bob = Mathf.Sin(Time.time * 1.5f + GetInstanceID() * 0.7f) * 0.01f;
                transform.position += new Vector3(0, bob * Time.deltaTime, 0);
            }

            _velocity = Vector2.zero;
        }

        private void KeepInBounds()
        {
            Vector3 pos = transform.position;

            if (pos.x < _fieldBounds.xMin) { pos.x = _fieldBounds.xMin; _velocity.x = Mathf.Abs(_velocity.x) * 0.8f; }
            if (pos.x > _fieldBounds.xMax) { pos.x = _fieldBounds.xMax; _velocity.x = -Mathf.Abs(_velocity.x) * 0.8f; }
            if (pos.y < _fieldBounds.yMin) { pos.y = _fieldBounds.yMin; _velocity.y = Mathf.Abs(_velocity.y) * 0.8f; }
            if (pos.y > _fieldBounds.yMax) { pos.y = _fieldBounds.yMax; _velocity.y = -Mathf.Abs(_velocity.y) * 0.8f; }

            transform.position = pos;
        }

        public void SetHidden(bool hidden)
        {
            CurrentBehavior = hidden ? DodgeBehavior.Hidden : DodgeBehavior.Idle;
            if (_renderer != null) _renderer.enabled = !hidden;
            gameObject.SetActive(!hidden);
        }

        public void SetFrozen(bool frozen)
        {
            CurrentBehavior = frozen ? DodgeBehavior.Frozen : DodgeBehavior.Idle;
        }
    }
}
