using UnityEngine;

namespace BellyFull
{
    /// <summary>
    /// Controls the Symmetric Dodge System for the shared field.
    /// With synchronized equations, ALL objects of the wrong type dodge ALL snakes.
    /// Attach to the same GameObject as FieldManager.
    /// </summary>
    [RequireComponent(typeof(FieldManager))]
    public class DodgeSystem : MonoBehaviour
    {
        private void Start()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.NormalPlay)
            {
                // Apply dodge based on the synchronized equation type
                ApplyDodgeBehavior(GameManager.Instance.CurrentEquationType);
            }
            else if (state == GameState.BallBlast || state == GameState.PreBlast)
            {
                // During blast: no dodge — hedgehogs hidden, only balls on field
                SetAllBehavior(FieldObjectType.Ball, DodgeBehavior.Idle);
            }
        }

        /// <summary>
        /// Called when the synchronized equation type changes.
        /// Both players share the same operation type, so dodge is global.
        /// Addition: balls idle (edible), hedgehogs dodge (uncatchable)
        /// Subtraction: hedgehogs idle (edible), balls dodge (uncatchable)
        /// </summary>
        public void ApplyDodgeBehavior(EquationType equationType)
        {
            if (equationType == EquationType.Addition)
            {
                SetAllBehavior(FieldObjectType.Ball, DodgeBehavior.Idle);
                SetAllBehavior(FieldObjectType.Hedgehog, DodgeBehavior.Dodging);
            }
            else
            {
                SetAllBehavior(FieldObjectType.Ball, DodgeBehavior.Dodging);
                SetAllBehavior(FieldObjectType.Hedgehog, DodgeBehavior.Idle);
            }

            SetAllBehavior(FieldObjectType.Flower, DodgeBehavior.Idle);
        }

        private void SetAllBehavior(FieldObjectType type, DodgeBehavior behavior)
        {
            var objects = GetComponentsInChildren<FieldObject>(true);
            foreach (var obj in objects)
            {
                if (obj.ObjectType == type)
                    obj.CurrentBehavior = behavior;
            }
        }
    }
}
