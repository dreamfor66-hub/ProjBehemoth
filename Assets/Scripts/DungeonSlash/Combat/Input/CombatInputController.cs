using UnityEngine;

namespace DungeonSlash
{
    public sealed class CombatInputController : MonoBehaviour
    {
        [SerializeField] private CombatSceneView sceneView;
        [SerializeField] private CombatController combat;
        private Vector2 pressPosition;
        private Vector2 latestPosition;
        private float heldSeconds;
        private bool tracking;
        private bool chargeEligible;
        private bool chargeCompleted;
        private bool capturingSwipe;
        private bool capturedSwipeIsCharged;
        private Vector2 capturedSwipeStart;
        private Vector2 capturedSwipeEnd;
        private readonly SwipeGestureAnalyzer swipeAnalyzer = new();
        public PointerGestureState State { get; private set; }
        public float ChargeProgress { get; private set; }
        public Vector2 ChargeOrigin => latestPosition;
        public void Configure(CombatSceneView scene, CombatController controller) { sceneView = scene; combat = controller; }

        private void Update()
        {
            if (combat.Player == null)
            {
                State = PointerGestureState.None;
                tracking = false;
                capturingSwipe = false;
                ChargeProgress = 0f;
                return;
            }

            if (!combat.IsActive)
            {
                State = PointerGestureState.None;
                tracking = false;
                capturingSwipe = false;
                ChargeProgress = 0f;
                return;
            }

            if (Input.GetMouseButtonDown(0) && sceneView.TryScreenToLogical(Input.mousePosition, null, out var start))
            {
                tracking = combat.TryBeginGesture(start); pressPosition = start; latestPosition = start; heldSeconds = 0f; ChargeProgress = 0f; chargeEligible = true; chargeCompleted = false; capturingSwipe = false; swipeAnalyzer.Reset(start, Time.unscaledTime); State = tracking ? PointerGestureState.Pressed : PointerGestureState.None;
            }
            if (!tracking) return;
            if (Input.GetMouseButton(0) && sceneView.TryScreenToLogical(Input.mousePosition, null, out var current))
            {
                latestPosition = current;
                if (capturingSwipe)
                {
                    capturedSwipeEnd = current;
                    State = PointerGestureState.Attacking;
                    return;
                }

                heldSeconds += Time.unscaledDeltaTime;
                swipeAnalyzer.AddSample(current, Time.unscaledTime, combat.Player.SwipeSampleWindowSeconds);
                if (chargeEligible && Vector2.Distance(current, pressPosition) > combat.Player.ChargeTolerance)
                {
                    chargeEligible = false;
                    if (!chargeCompleted) ChargeProgress = 0f;
                }

                if (chargeEligible && !chargeCompleted)
                {
                    ChargeProgress = Mathf.Clamp01(heldSeconds / combat.Player.CurrentChargeDuration);
                    chargeCompleted = ChargeProgress >= 1f;
                }

                if ((!chargeEligible || chargeCompleted) && swipeAnalyzer.TryGetGesture(combat.Player.MinimumSwipeDistance, combat.Player.MinimumSwipeSpeed, combat.Player.MinimumDirectionConsistency, out var gesture))
                {
                    var charged = chargeCompleted;
                    if (!charged && combat.IsGuardGesture(pressPosition, current))
                    {
                        combat.TryBeginGuard(pressPosition, current);
                        State = PointerGestureState.Guarding; tracking = false;
                        return;
                    }

                    capturingSwipe = true;
                    capturedSwipeIsCharged = charged;
                    capturedSwipeStart = gesture.TrailStart;
                    capturedSwipeEnd = current;
                    State = PointerGestureState.Attacking;
                    return;
                }

                State = chargeCompleted ? PointerGestureState.Charged : chargeEligible ? PointerGestureState.Charging : PointerGestureState.Pressed;
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (sceneView.TryScreenToLogical(Input.mousePosition, null, out var releasedPosition)) latestPosition = releasedPosition;
                if (capturingSwipe)
                {
                    capturedSwipeEnd = latestPosition;
                    if (!capturedSwipeIsCharged && combat.IsGuardGesture(pressPosition, capturedSwipeEnd))
                    {
                        combat.TryBeginGuard(pressPosition, capturedSwipeEnd);
                        State = PointerGestureState.Guarding;
                    }
                    else
                    {
                        combat.ExecuteAttack(new AttackSegment(capturedSwipeStart, capturedSwipeEnd, combat.Player.SegmentWidth), capturedSwipeIsCharged);
                    }
                }
                tracking = false; State = PointerGestureState.None;
                capturingSwipe = false;
            }
        }

    }
}
