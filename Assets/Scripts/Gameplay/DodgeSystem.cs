using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Controls the Symmetric Dodge System for the shared field.
    /// Every frame, reads the current equation type from GameManager
    /// and enforces the correct dodge/idle behavior on all field objects.
    /// Addition: balls idle (edible), hedgehogs dodge (uncatchable)
    /// Subtraction: hedgehogs idle (edible), balls dodge (uncatchable)
    /// </summary>
    [RequireComponent(typeof(FieldManager))]
    public class DodgeSystem : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (GameManager.Instance == null) return;

            var state = GameManager.Instance.CurrentState;
            if (state == GameState.BallBlast || state == GameState.PreBlast)
                return;
            if (state != GameState.NormalPlay)
                return;

            // Check what each player currently needs — players may have different equation types
            // Don't skip belly-ache snakes; their objects should stay Idle so they don't scatter
            bool anyNeedsBalls = false;
            bool anyNeedsHedgehogs = false;
            for (int i = 0; i < 2; i++)
            {
                var snake = GameManager.Instance.GetSnake((PlayerIndex)i);
                if (snake == null) continue;
                if (snake.CurrentEquationType == EquationType.Addition)
                    anyNeedsBalls = true;
                else
                    anyNeedsHedgehogs = true;
            }

            var objects = GetComponentsInChildren<FieldObject>(true);

            foreach (var obj in objects)
            {
                if (obj.CurrentBehavior == DodgeBehavior.Frozen ||
                    obj.CurrentBehavior == DodgeBehavior.Hidden)
                    continue;

                if (obj.ObjectType == FieldObjectType.Ball)
                    obj.CurrentBehavior = anyNeedsBalls ? DodgeBehavior.Idle : DodgeBehavior.Dodging;
                else if (obj.ObjectType == FieldObjectType.Hedgehog)
                    obj.CurrentBehavior = anyNeedsHedgehogs ? DodgeBehavior.Idle : DodgeBehavior.Dodging;
            }
        }

        /// <summary>
        /// Kept for compatibility with MathSystem calls, but no longer needed
        /// since LateUpdate enforces behavior every frame.
        /// </summary>
        public void ApplyDodgeBehavior(EquationType equationType) { }
    }
}
