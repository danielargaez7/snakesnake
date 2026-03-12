using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Manages the shared field: hedgehog spawning/catching, delivery zone detection,
    /// and Ball Blast ball spawning/eating.
    /// </summary>
    public class FieldManager : MonoBehaviour
    {
        [Header("Field Bounds (world space)")]
        [SerializeField] private Rect fieldBounds = new Rect(-7f, -4f, 14f, 8f);

        [Header("Prefabs")]
        [SerializeField] private GameObject hedgehogPrefab;
        [SerializeField] private GameObject ballPrefab;     // blast balls only

        [Header("Counts")]
        [SerializeField] private int hedgehogCount = 8;
        [SerializeField] private int blastBallCount = 80;

        [Header("Delivery Zones")]
        [Tooltip("World-space position of Player 1's number display (top-left)")]
        [SerializeField] private Transform p1DeliveryZone;
        [Tooltip("World-space position of Player 2's number display (top-right)")]
        [SerializeField] private Transform p2DeliveryZone;
        [SerializeField] private float deliveryRadius = 1.5f;

        public Rect FieldBounds => fieldBounds;

        private List<FieldObject> _hedgehogs  = new List<FieldObject>();
        private List<FieldObject> _blastBalls = new List<FieldObject>();

        private void Start()
        {
            SpawnHedgehogs(hedgehogCount);
            GameEvents.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            GameState state = GameManager.Instance.CurrentState;

            if (state == GameState.NormalPlay)
            {
                for (int i = 0; i < 2; i++)
                {
                    var snake = GameManager.Instance.GetSnake((PlayerIndex)i);
                    if (snake == null) continue;

                    if (!snake.IsCarrying)
                        TryCatch(snake);
                    else
                        TryDeliver(snake);
                }
                ReplenishHedgehogs();
            }
            else if (state == GameState.BallBlast)
            {
                for (int i = 0; i < 2; i++)
                {
                    var snake = GameManager.Instance.GetSnake((PlayerIndex)i);
                    if (snake != null) TryEatBlastBall(snake);
                }
            }
        }

        // ── Catch ──────────────────────────────────────────────────────────

        private void TryCatch(SnakeController snake)
        {
            float radius = snake.GetCatchRadius();
            Vector2 snakePos = snake.transform.position;

            for (int i = _hedgehogs.Count - 1; i >= 0; i--)
            {
                if (_hedgehogs[i] == null) { _hedgehogs.RemoveAt(i); continue; }
                if (_hedgehogs[i].CurrentBehavior == HedgehogBehavior.Hidden) continue;

                float dist = Vector2.Distance(snakePos, _hedgehogs[i].transform.position);
                if (dist < radius)
                {
                    Destroy(_hedgehogs[i].gameObject);
                    _hedgehogs.RemoveAt(i);
                    snake.CatchHedgehog();
                    return; // one at a time
                }
            }
        }

        // ── Deliver ────────────────────────────────────────────────────────

        private void TryDeliver(SnakeController snake)
        {
            Vector2 snakePos = snake.transform.position;

            var hud = GameHUD.Get(snake.Player);
            if (hud != null)
            {
                int holeIdx = hud.GetNearestUnfilledHoleIndex(snakePos, deliveryRadius);
                if (holeIdx >= 0)
                {
                    snake.DeliverBallToHole(holeIdx);
                    return;
                }
                return; // HUD ready but no hole in range — don't fall through
            }

            // Fallback: fixed delivery zone (no HUD available)
            Transform zone = snake.Player == PlayerIndex.Player1 ? p1DeliveryZone : p2DeliveryZone;
            Vector2 zonePos = zone != null ? (Vector2)zone.position
                : (snake.Player == PlayerIndex.Player1 ? new Vector2(-3.5f, 3.8f) : new Vector2(3.5f, 3.8f));
            if (Vector2.Distance(snakePos, zonePos) < deliveryRadius)
                snake.DeliverBallToHole(0);
        }

        // ── Ball Blast ─────────────────────────────────────────────────────

        private void TryEatBlastBall(SnakeController snake)
        {
            if (!snake.IsMoving) return; // stationary snake can't pop bubbles

            float radius = snake.GetCatchRadius();
            Vector2 snakePos = snake.transform.position;

            for (int i = _blastBalls.Count - 1; i >= 0; i--)
            {
                if (_blastBalls[i] == null) { _blastBalls.RemoveAt(i); continue; }
                float dist = Vector2.Distance(snakePos, _blastBalls[i].transform.position);
                if (dist < radius)
                {
                    _blastBalls[i].Pop(); // bubble pop animation then self-destroys
                    _blastBalls.RemoveAt(i);
                    snake.EatBlastBall();
                    return;
                }
            }
        }

        // ── Spawning ───────────────────────────────────────────────────────

        private void SpawnHedgehogs(int count)
        {
            if (hedgehogPrefab == null) return;
            for (int i = 0; i < count; i++)
            {
                var go  = Instantiate(hedgehogPrefab, GetRandomFieldPos(), Quaternion.identity, transform);
                var obj = go.GetComponent<FieldObject>();
                if (obj != null)
                {
                    obj.Initialize(fieldBounds);
                    _hedgehogs.Add(obj);
                }
            }
        }

        private void ReplenishHedgehogs()
        {
            // Clean up destroyed refs
            _hedgehogs.RemoveAll(h => h == null);

            while (_hedgehogs.Count < hedgehogCount)
                SpawnHedgehogs(1);
        }

        private void SpawnBlastBalls() => StartCoroutine(SpawnBlastBallsStaggered());

        private IEnumerator SpawnBlastBallsStaggered()
        {
            if (ballPrefab == null) yield break;
            for (int i = 0; i < blastBallCount; i++)
            {
                var go  = Instantiate(ballPrefab, GetRandomFieldPos(), Quaternion.identity, transform);
                var obj = go.GetComponent<FieldObject>();
                if (obj != null)
                {
                    obj.Initialize(fieldBounds);
                    obj.ScaleIn(Random.Range(0f, 0.08f)); // tiny stagger = "thrown" feel
                    _blastBalls.Add(obj);
                }
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.55f, 0.92f, 1f, 0.72f);

                if (i % 4 == 0) yield return null; // yield every 4 to avoid one big frame spike
            }
        }

        private void ClearBlastBalls()
        {
            foreach (var b in _blastBalls)
                if (b != null) Destroy(b.gameObject);
            _blastBalls.Clear();
        }

        private void ClearAllHedgehogs()
        {
            foreach (var h in _hedgehogs)
                if (h != null) Destroy(h.gameObject);
            _hedgehogs.Clear();
        }

        // ── State ──────────────────────────────────────────────────────────

        private void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.PreBlast:
                    ClearAllHedgehogs();
                    break;

                case GameState.BallBlast:
                    SpawnBlastBalls();
                    break;

                case GameState.NormalPlay:
                    ClearBlastBalls();
                    SpawnHedgehogs(hedgehogCount);
                    break;
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private Vector3 GetRandomFieldPos()
        {
            // Keep away from delivery zones (top strip)
            float x = Random.Range(fieldBounds.xMin + 0.5f, fieldBounds.xMax - 0.5f);
            float y = Random.Range(fieldBounds.yMin + 0.5f, fieldBounds.yMax - 2.5f); // avoid top
            return new Vector3(x, y, 0);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireCube(
                new Vector3(fieldBounds.center.x, fieldBounds.center.y, 0),
                new Vector3(fieldBounds.width, fieldBounds.height, 0));

            // Delivery zones
            Gizmos.color = Color.cyan;
            Vector3 p1 = p1DeliveryZone != null ? p1DeliveryZone.position : new Vector3(-3.5f, 3.8f, 0);
            Vector3 p2 = p2DeliveryZone != null ? p2DeliveryZone.position : new Vector3( 3.5f, 3.8f, 0);
            Gizmos.DrawWireSphere(p1, deliveryRadius);
            Gizmos.DrawWireSphere(p2, deliveryRadius);
        }
    }
}
