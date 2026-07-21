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
        private bool secondChargeCompleted;
        private bool capturingSwipe;
        private bool capturedSwipeIsCharged;
        private int capturedSwipeChargeLevel;
        private Vector2 capturedSwipeStart;
        private Vector2 capturedSwipeEnd;
        private readonly SwipeGestureAnalyzer swipeAnalyzer = new();
        private readonly TargetTraversalConfirmation traversalConfirmation = new();
        private readonly GuardRestConfirmation guardRestConfirmation = new();
        private int traversalTargetIndex = -1;
        private bool guardEntryStartedOutsideBody;
        public PointerGestureState State { get; private set; }
        public float ChargeProgress { get; private set; }
        public float SecondChargeProgress { get; private set; }
        public Vector2 ChargeOrigin => latestPosition;
        public bool IsChargingAction => State is PointerGestureState.Charging or PointerGestureState.Charged or PointerGestureState.SecondCharging or PointerGestureState.SecondCharged;
        public bool IsGuardKeyHeld => Input.GetKey(KeyCode.Space);
        public void Configure(CombatSceneView scene, CombatController controller) { sceneView = scene; combat = controller; }

        private void Update()
        {
            if (combat.Player == null)
            {
                State = PointerGestureState.None;
                tracking = false;
                capturingSwipe = false;
                ResetTraversal();
                ResetGuardRest();
                guardEntryStartedOutsideBody = false;
                ChargeProgress = 0f;
                SecondChargeProgress = 0f;
                return;
            }

            if (!combat.IsActive)
            {
                State = PointerGestureState.None;
                tracking = false;
                capturingSwipe = false;
                ResetTraversal();
                ResetGuardRest();
                guardEntryStartedOutsideBody = false;
                ChargeProgress = 0f;
                SecondChargeProgress = 0f;
                return;
            }

            if (Input.GetMouseButtonDown(0) && sceneView.TryScreenToLogical(Input.mousePosition, null, out var start))
            {
                tracking = combat.TryBeginGesture(start); pressPosition = start; latestPosition = start; heldSeconds = 0f; ChargeProgress = 0f; SecondChargeProgress = 0f;
                // A press that began during recovery cannot become a charge later just by holding it.
                chargeEligible = combat.Player.CanStartCharge();
                chargeCompleted = false; secondChargeCompleted = false; capturingSwipe = false; ResetTraversal(); ResetGuardRest(); guardEntryStartedOutsideBody = tracking && !combat.IsPointerInsidePlayerBody(start); swipeAnalyzer.Reset(start, Time.unscaledTime); State = tracking ? PointerGestureState.Pressed : PointerGestureState.None;
            }
            if (!tracking) return;
            if (Input.GetMouseButton(0) && sceneView.TryScreenToLogical(Input.mousePosition, null, out var current))
            {
                var previousPosition = latestPosition;
                latestPosition = current;
                if (capturingSwipe)
                {
                    var previous = capturedSwipeEnd;
                    capturedSwipeEnd = current;
                    if (TryConfirmTraversal(previous, current))
                    {
                        CommitTraversalAttack();
                        return;
                    }
                    if (TryBeginBodyEntryGuard(current))
                    {
                        State = PointerGestureState.Guarding;
                        tracking = false;
                        capturingSwipe = false;
                        return;
                    }
                    State = PointerGestureState.Attacking;
                    return;
                }

                heldSeconds += Time.unscaledDeltaTime;
                swipeAnalyzer.AddSample(current, Time.unscaledTime, combat.Player.SwipeSampleWindowSeconds);
                if (chargeEligible && Vector2.Distance(current, pressPosition) > combat.Player.ChargeTolerance)
                {
                    chargeEligible = false;
                    if (!chargeCompleted) ChargeProgress = 0f;
                    if (!secondChargeCompleted) SecondChargeProgress = 0f;
                }

                if (chargeEligible && !chargeCompleted)
                {
                    ChargeProgress = Mathf.Clamp01(heldSeconds / combat.Player.CurrentChargeDuration);
                    chargeCompleted = ChargeProgress >= 1f;
                }
                if (chargeEligible && chargeCompleted && combat.Player.DoubleChargeEnabled && !secondChargeCompleted)
                {
                    SecondChargeProgress = Mathf.Clamp01((heldSeconds - combat.Player.CurrentChargeDuration) / combat.Player.CurrentChargeDuration);
                    secondChargeCompleted = SecondChargeProgress >= 1f;
                }

                if (TryBeginBodyEntryGuard(current))
                {
                    State = PointerGestureState.Guarding;
                    tracking = false;
                    return;
                }

                if (TryBeginRestGuard(previousPosition, current))
                {
                    State = PointerGestureState.Guarding;
                    tracking = false;
                    return;
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
                    capturedSwipeChargeLevel = secondChargeCompleted ? 2 : 1;
                    // Keep the original gesture-derived start for the final release attack.
                    capturedSwipeStart = gesture.TrailStart;
                    capturedSwipeEnd = current;
                    ResetTraversal();
                    // Traversal confirmation sees the entire input from its real press point,
                    // including a press that began inside the target before the flick was recognized.
                    if (TryConfirmTraversal(pressPosition, capturedSwipeEnd))
                    {
                        CommitTraversalAttack();
                        return;
                    }
                    State = PointerGestureState.Attacking;
                    return;
                }

                State = !chargeEligible ? PointerGestureState.Pressed : !chargeCompleted ? PointerGestureState.Charging : combat.Player.DoubleChargeEnabled ? secondChargeCompleted ? PointerGestureState.SecondCharged : PointerGestureState.SecondCharging : PointerGestureState.Charged;
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (sceneView.TryScreenToLogical(Input.mousePosition, null, out var releasedPosition)) latestPosition = releasedPosition;
                if (capturingSwipe)
                {
                    capturedSwipeEnd = latestPosition;
                    if (TryBeginBodyEntryGuard(capturedSwipeEnd))
                    {
                        State = PointerGestureState.Guarding;
                    }
                    else if (!capturedSwipeIsCharged && combat.IsGuardGesture(pressPosition, capturedSwipeEnd))
                    {
                        combat.TryBeginGuard(pressPosition, capturedSwipeEnd);
                        State = PointerGestureState.Guarding;
                    }
                    else
                    {
                        combat.ExecuteAttack(new AttackSegment(capturedSwipeStart, capturedSwipeEnd, combat.Player.SegmentWidth), capturedSwipeIsCharged, capturedSwipeChargeLevel);
                    }
                }
                tracking = false; State = PointerGestureState.None; ChargeProgress = 0f; SecondChargeProgress = 0f;
                capturingSwipe = false;
                ResetTraversal();
                ResetGuardRest();
                guardEntryStartedOutsideBody = false;
            }
        }

        private bool TryConfirmTraversal(Vector2 from, Vector2 to)
        {
            if (traversalTargetIndex < 0 && !combat.TryFindTraversalTarget(from, to, out traversalTargetIndex))
                return false;
            if (!combat.TryGetTraversalTargetState(traversalTargetIndex, from, out var wasInsideTarget, out _) ||
                !combat.TryGetTraversalTargetState(traversalTargetIndex, to, out var isInsideTarget, out var outsideDistance))
                return false;

            var touchesTarget = combat.DoesTraversalTargetTouch(traversalTargetIndex, from, to);
            return traversalConfirmation.Observe(
                touchesTarget,
                wasInsideTarget,
                isInsideTarget,
                outsideDistance,
                Vector2.Distance(from, to),
                Time.unscaledTime,
                combat.Player.TraversalExitDistance,
                combat.Player.TraversalStationaryDistance,
                combat.Player.TraversalStopConfirmSeconds);
        }

        private void CommitTraversalAttack()
        {
            combat.ExecuteAttack(new AttackSegment(capturedSwipeStart, capturedSwipeEnd, combat.Player.SegmentWidth), capturedSwipeIsCharged, capturedSwipeChargeLevel);
            tracking = false;
            capturingSwipe = false;
            State = PointerGestureState.None;
            ChargeProgress = 0f;
            SecondChargeProgress = 0f;
            ResetTraversal();
        }

        private void ResetTraversal()
        {
            traversalTargetIndex = -1;
            traversalConfirmation.Reset();
        }

        private bool TryBeginRestGuard(Vector2 previousPosition, Vector2 currentPosition)
        {
            var isCandidate = combat.IsRestGuardCandidate(pressPosition, currentPosition);
            var pathTouchesGuardZone = combat.DoesPathTouchPlayerGuardZone(previousPosition, currentPosition);
            if (!guardRestConfirmation.Observe(
                    pathTouchesGuardZone,
                    isCandidate,
                    Vector2.Distance(previousPosition, currentPosition),
                    Time.unscaledTime,
                    combat.Player.GuardRestStationaryDistance,
                    combat.Player.GuardRestConfirmSeconds))
                return false;

            if (!combat.TryBeginRestGuard(pressPosition, currentPosition))
                return false;

            ResetGuardRest();
            return true;
        }

        private bool TryBeginBodyEntryGuard(Vector2 currentPosition)
        {
            if (!guardEntryStartedOutsideBody || !combat.TryBeginBodyEntryGuard(pressPosition, currentPosition)) return false;
            guardEntryStartedOutsideBody = false;
            ResetGuardRest();
            return true;
        }

        private void ResetGuardRest() => guardRestConfirmation.Reset();

    }
}
