using System.Collections.Generic;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Spawns and manages balls, hedgehogs, and flowers on the shared field.
    /// Both snakes roam the same field. Checks eating for both players.
    /// </summary>
    public class FieldManager : MonoBehaviour
    {
        [Header("Field Bounds (world space)")]
        [SerializeField] private Rect fieldBounds = new Rect(-7f, -4f, 14f, 8f);

        [Header("Prefabs")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private GameObject hedgehogPrefab;
        [SerializeField] private GameObject flowerPrefab;

        [Header("Spawn Counts")]
        [SerializeField] private int ballCount = 10;
        [SerializeField] private int hedgehogCount = 7;
        [SerializeField] private int flowerCount = 6;
        [SerializeField] private int blastBallCount = 40;

        private List<FieldObject> _balls = new List<FieldObject>();
        private List<FieldObject> _hedgehogs = new List<FieldObject>();
        private List<FieldObject> _flowers = new List<FieldObject>();
        private List<FieldObject> _blastBalls = new List<FieldObject>();

        public Rect FieldBounds => fieldBounds;

        private void Start()
        {
            SpawnField();

            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnBellyAcheStarted += HandleBellyAcheStarted;
            GameEvents.OnBellyAcheEnded += HandleBellyAcheEnded;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnBellyAcheStarted -= HandleBellyAcheStarted;
            GameEvents.OnBellyAcheEnded -= HandleBellyAcheEnded;
        }

        private void Update()
        {
            GameState state = GameManager.Instance.CurrentState;
            if (state != GameState.NormalPlay && state != GameState.BallBlast) return;

            // Check eating for BOTH snakes on the shared field
            for (int i = 0; i < 2; i++)
            {
                var snake = GameManager.Instance.GetSnake((PlayerIndex)i);
                if (snake == null || snake.IsInBellyAche) continue;
                CheckEating(snake, state);
            }
        }

        private void CheckEating(SnakeController snake, GameState state)
        {
            float eatRadius = snake.GetEatRadius();
            Vector2 snakePos = snake.transform.position;

            List<FieldObject> targets = state == GameState.BallBlast ? _blastBalls : GetEatableObjects(snake);

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (targets[i] == null || !targets[i].gameObject.activeInHierarchy) continue;
                if (targets[i].CurrentBehavior == DodgeBehavior.Dodging) continue;
                if (targets[i].CurrentBehavior == DodgeBehavior.Frozen) continue;

                float dist = Vector2.Distance(snakePos, targets[i].transform.position);
                if (dist < eatRadius)
                {
                    if (snake.TryEat(targets[i]))
                    {
                        var eaten = targets[i];
                        targets.RemoveAt(i);
                        Destroy(eaten.gameObject);
                    }
                }
            }
        }

        private List<FieldObject> GetEatableObjects(SnakeController snake)
        {
            // With synchronized equations, both players have same equation type
            if (snake.CurrentEquationType == EquationType.Addition)
                return _balls;
            else
                return _hedgehogs;
        }

        private void SpawnField()
        {
            SpawnObjects(ballPrefab, ballCount, _balls);
            SpawnObjects(hedgehogPrefab, hedgehogCount, _hedgehogs);
            SpawnObjects(flowerPrefab, flowerCount, _flowers);
        }

        private void SpawnObjects(GameObject prefab, int count, List<FieldObject> list)
        {
            if (prefab == null) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomFieldPosition();
                var go = Instantiate(prefab, pos, Quaternion.identity, transform);
                var obj = go.GetComponent<FieldObject>();
                if (obj != null)
                {
                    obj.Initialize(fieldBounds);
                    list.Add(obj);
                }
            }
        }

        private const float MinSpawnDistance = 0.8f;
        private const int MaxSpawnAttempts = 30;

        private Vector3 GetRandomFieldPosition()
        {
            for (int attempt = 0; attempt < MaxSpawnAttempts; attempt++)
            {
                float x = Random.Range(fieldBounds.xMin + 0.5f, fieldBounds.xMax - 0.5f);
                float y = Random.Range(fieldBounds.yMin + 0.5f, fieldBounds.yMax - 0.5f);
                Vector3 candidate = new Vector3(x, y, 0);

                if (!OverlapsExisting(candidate))
                    return candidate;
            }

            // Fallback if no clear spot found
            float fx = Random.Range(fieldBounds.xMin + 0.5f, fieldBounds.xMax - 0.5f);
            float fy = Random.Range(fieldBounds.yMin + 0.5f, fieldBounds.yMax - 0.5f);
            return new Vector3(fx, fy, 0);
        }

        private bool OverlapsExisting(Vector3 pos)
        {
            foreach (var obj in _balls)
                if (obj != null && Vector3.Distance(obj.transform.position, pos) < MinSpawnDistance) return true;
            foreach (var obj in _hedgehogs)
                if (obj != null && Vector3.Distance(obj.transform.position, pos) < MinSpawnDistance) return true;
            foreach (var obj in _flowers)
                if (obj != null && Vector3.Distance(obj.transform.position, pos) < MinSpawnDistance) return true;
            return false;
        }

        /// <summary>
        /// Ensures enough edible objects exist on the shared field.
        /// </summary>
        public void EnsureObjectCount(FieldObjectType type, int minCount)
        {
            List<FieldObject> list = type == FieldObjectType.Ball ? _balls : _hedgehogs;
            GameObject prefab = type == FieldObjectType.Ball ? ballPrefab : hedgehogPrefab;

            while (list.Count < minCount)
            {
                Vector3 pos = GetRandomFieldPosition();
                var go = Instantiate(prefab, pos, Quaternion.identity, transform);
                var obj = go.GetComponent<FieldObject>();
                if (obj != null)
                {
                    obj.Initialize(fieldBounds);
                    list.Add(obj);
                }
            }
        }

        // --- State Handlers ---

        private void HandleGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.PreBlast:
                    foreach (var h in _hedgehogs) h.SetHidden(true);
                    foreach (var f in _flowers) f.SetHidden(true);
                    break;

                case GameState.BallBlast:
                    SpawnBlastBalls();
                    break;

                case GameState.NormalPlay:
                    ClearBlastBalls();
                    foreach (var h in _hedgehogs) h.SetHidden(false);
                    foreach (var f in _flowers) f.SetHidden(false);
                    ReplenishField();
                    break;
            }
        }

        private void HandleBellyAcheStarted(PlayerIndex player, int overshoot)
        {
            // Freeze all objects on the shared field when any player belly aches
            foreach (var b in _balls) b.SetFrozen(true);
            foreach (var h in _hedgehogs) h.SetFrozen(true);
            foreach (var f in _flowers) f.SetFrozen(true);
        }

        private void HandleBellyAcheEnded(PlayerIndex player)
        {
            // Unfreeze — but only if no other player is still in belly ache
            var otherSnake = GameManager.Instance.GetSnake(
                player == PlayerIndex.Player1 ? PlayerIndex.Player2 : PlayerIndex.Player1);
            if (otherSnake != null && otherSnake.IsInBellyAche) return;

            foreach (var b in _balls) b.SetFrozen(false);
            foreach (var h in _hedgehogs) h.SetFrozen(false);
            foreach (var f in _flowers) f.SetFrozen(false);
        }

        private void SpawnBlastBalls()
        {
            if (ballPrefab == null) return;
            for (int i = 0; i < blastBallCount; i++)
            {
                Vector3 pos = GetRandomFieldPosition();
                var go = Instantiate(ballPrefab, pos, Quaternion.identity, transform);
                var obj = go.GetComponent<FieldObject>();
                if (obj != null)
                {
                    obj.Initialize(fieldBounds);
                    _blastBalls.Add(obj);
                }
            }
        }

        private void ClearBlastBalls()
        {
            foreach (var b in _blastBalls)
            {
                if (b != null) Destroy(b.gameObject);
            }
            _blastBalls.Clear();
        }

        private void ReplenishField()
        {
            while (_balls.Count < ballCount)
                SpawnObjects(ballPrefab, 1, _balls);
            while (_hedgehogs.Count < hedgehogCount)
                SpawnObjects(hedgehogPrefab, 1, _hedgehogs);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Vector3 center = new Vector3(fieldBounds.center.x, fieldBounds.center.y, 0);
            Vector3 size = new Vector3(fieldBounds.width, fieldBounds.height, 0);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
