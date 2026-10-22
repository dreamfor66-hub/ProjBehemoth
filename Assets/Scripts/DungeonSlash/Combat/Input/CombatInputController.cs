using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DungeonSlash
{
    public sealed class CombatInputController : MonoBehaviour
    {
        [SerializeField] private CombatSceneView sceneView;
        [SerializeField] private CombatController combat;
        private sealed class Contact
        {
            public Vector2 Start, Latest, AttackStart;
            public float Held;
            public bool Guard, Eligible, Charged, Capturing, Consumed;
            public int ChargeLevel = 1, Target = -1;
            public float StillSince = -1f;
            public readonly SwipeGestureAnalyzer Analyzer = new();
            public readonly TargetTraversalConfirmation Traversal = new();
        }
        private readonly Dictionary<int, Contact> contacts = new();
        private bool guardActionHeld;
        private int guardActionSerial;
        public PointerGestureState State { get; private set; }
        public float ChargeProgress { get; private set; }
        public float SecondChargeProgress { get; private set; }
        public Vector2 ChargeOrigin { get; private set; }
        public float CastingSeconds { get; private set; }
        public bool IsChargingAction => State is PointerGestureState.Charging or PointerGestureState.Charged or PointerGestureState.SecondCharging or PointerGestureState.SecondCharged;
        public bool IsGuardKeyHeld => Input.GetKey(KeyCode.Space);
        public bool IsGuardHeld => contacts.Values.Any(contact => contact.Guard && !contact.Consumed) || IsGuardKeyHeld;
        public void Configure(CombatSceneView scene, CombatController controller) { sceneView = scene; combat = controller; }
        private void Awake() => Input.simulateMouseWithTouches = false;
        private void OnDisable() => CancelAll();
        private void OnApplicationFocus(bool focused) { if (!focused) CancelAll(); }
        private void Update()
        {
            if (combat == null || combat.Player == null || !combat.IsActive || combat.InputPaused) { CancelAll(); return; }
            if (Input.touchCount > 0)
            {
                foreach (var touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began) Begin(touch.fingerId, touch.position);
                    else if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled) End(touch.fingerId, touch.position, touch.phase == TouchPhase.Canceled);
                    else Move(touch.fingerId, touch.position);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0)) Begin(-1, Input.mousePosition);
                if (Input.GetMouseButton(0)) Move(-1, Input.mousePosition);
                if (Input.GetMouseButtonUp(0)) End(-1, Input.mousePosition, false);
            }
            var held = IsGuardHeld;
            combat.Player.TrySetGuard(held);
            TrackGuardAction();
            if (held && combat.Run.Spellbook.Count > 0) CastingSeconds += Time.deltaTime; else CastingSeconds = 0;
            RefreshState();
        }
        private bool OverButton(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            return hits.Any(hit => hit.gameObject.GetComponentInParent<Button>() != null);
        }
        private void Begin(int id, Vector2 screen)
        {
            if (OverButton(screen) || !sceneView.TryScreenToLogical(screen, null, out var position)) return;
            var guard = combat.IsPointerInsidePlayerBody(position) || IsGuardKeyHeld;
            var contact = new Contact { Start = position, Latest = position, Guard = guard, Eligible = !guard && combat.Player.CanStartCharge() };
            contact.Analyzer.Reset(position, Time.unscaledTime); contacts[id] = contact;
            if (guard) combat.Player.TrySetGuard(true);
            TrackGuardAction();
        }
        private void Move(int id, Vector2 screen)
        {
            if (!contacts.TryGetValue(id, out var contact) || contact.Consumed) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneView.LogicalArea, screen, null, out var current);
            var previous = contact.Latest; contact.Latest = current; contact.Held += Time.deltaTime;
            if (contact.Guard) return;
            var player = combat.Player;
            if (contact.Capturing) { if (ConfirmTraversal(contact, previous, current)) Commit(contact); return; }
            contact.Analyzer.AddSample(current, Time.unscaledTime, player.SwipeSampleWindowSeconds);
            if (contact.Eligible)
            {
                if (contact.Held >= player.CurrentChargeDuration) contact.Charged = true;
                if (player.DoubleChargeEnabled && contact.Held >= player.CurrentChargeDuration * 2f) contact.ChargeLevel = 2;
                if (Vector2.Distance(current, contact.Start) > player.ChargeTolerance) contact.Eligible = false;
            }
            if ((!contact.Eligible || contact.Charged) && contact.Analyzer.TryGetGesture(player.MinimumSwipeDistance, player.MinimumSwipeSpeed, player.MinimumDirectionConsistency, out var gesture))
            {
                contact.Capturing = true; contact.AttackStart = gesture.TrailStart;
                if (ConfirmTraversal(contact, contact.Start, current)) Commit(contact);
            }
        }
        private void End(int id, Vector2 screen, bool canceled)
        {
            if (!contacts.TryGetValue(id, out var contact)) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneView.LogicalArea, screen, null, out var released);
            contact.Latest = released;
            if (!canceled && !contact.Consumed)
            {
                if (contact.Guard)
                {
                    var stroke = released - contact.Start;
                    if (stroke.magnitude >= combat.Player.Data.Variables.spellSwipeDistance) combat.TryCastSpell(CombatController.ResolveDirection(stroke), CastingSeconds);
                }
                else if (contact.Capturing) Commit(contact);
            }
            contacts.Remove(id);
            combat.Player.TrySetGuard(IsGuardHeld);
            TrackGuardAction(canceled);
            if (!IsGuardHeld) CastingSeconds = 0;
        }
        private void TrackGuardAction(bool canceled = false)
        {
            var held = IsGuardHeld;
            if (held && !guardActionHeld) guardActionSerial = combat.Run.ActionSerial;
            // Spells/weapons/potions already complete their action; overlapping guard contacts count once.
            if (!held && guardActionHeld && !canceled && combat.IsActive && !combat.InputPaused
                && combat.Run.Awareness.IsConcealed && guardActionSerial == combat.Run.ActionSerial)
            {
                combat.Run.StartAction(default);
                combat.Run.CompleteAction(TriggerType.OnGuardEnded, default);
            }
            guardActionHeld = held;
        }
        private bool ConfirmTraversal(Contact contact, Vector2 from, Vector2 to)
        {
            var direction = (to - contact.AttackStart).normalized;
            var crossed = combat.FarthestTraversalTarget(from, to, contact.AttackStart, direction);
            if (crossed >= 0 && crossed != contact.Target)
            { contact.Target = crossed; contact.Traversal.Reset(); }
            if (contact.Target < 0) return false;
            var moving = Vector2.Distance(from, to) > combat.Player.TraversalStationaryDistance;
            if (moving) contact.StillSince = -1f;
            else if (contact.StillSince < 0) contact.StillSince = Time.unscaledTime;
            if (!moving && contact.StillSince >= 0 && Time.unscaledTime - contact.StillSince >= combat.Player.TraversalStopConfirmSeconds) return true;
            if (!combat.TryGetTraversalTargetState(contact.Target, from, out var wasInside, out _) || !combat.TryGetTraversalTargetState(contact.Target, to, out var inside, out var outside)) return false;
            var confirmed = contact.Traversal.Observe(combat.DoesTraversalTargetTouch(contact.Target, from, to), wasInside, inside, outside, Vector2.Distance(from, to), Time.unscaledTime,
                combat.Player.TraversalExitDistance, combat.Player.TraversalStationaryDistance, combat.Player.TraversalStopConfirmSeconds);
            return confirmed && !combat.HasTraversalTargetAhead(contact.Target, to, direction);
        }
        private void Commit(Contact contact)
        {
            combat.ExecuteAttack(new AttackSegment(contact.AttackStart, contact.Latest, combat.Player.SegmentWidth), contact.Charged, contact.ChargeLevel);
            contact.Consumed = true;
        }
        private void RefreshState()
        {
            ChargeProgress = SecondChargeProgress = 0;
            var contact = contacts.Values.FirstOrDefault(item => !item.Guard && !item.Consumed);
            if (contact != null)
            {
                ChargeOrigin = contact.Latest;
                ChargeProgress = contact.Eligible || contact.Charged ? Mathf.Clamp01(contact.Held / combat.Player.CurrentChargeDuration) : 0;
                SecondChargeProgress = combat.Player.DoubleChargeEnabled ? Mathf.Clamp01(contact.Held / combat.Player.CurrentChargeDuration - 1f) : 0;
                State = contact.Capturing ? PointerGestureState.Attacking : contact.Charged ? PointerGestureState.Charged : contact.Eligible ? PointerGestureState.Charging : PointerGestureState.Pressed;
            }
            else State = IsGuardHeld ? PointerGestureState.Guarding : PointerGestureState.None;
        }
        public void CancelAll()
        {
            guardActionHeld = false;
            contacts.Clear(); State = PointerGestureState.None; CastingSeconds = ChargeProgress = SecondChargeProgress = 0;
            combat?.Player?.TrySetGuard(false);
        }
        public void ResetCasting() => CastingSeconds = 0;
    }
}
