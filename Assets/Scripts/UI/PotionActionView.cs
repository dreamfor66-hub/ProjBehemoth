using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    // One shared surface for quick-bar and inventory potions. Hover descriptions stay in ItemTooltipView.
    public sealed class PotionActionView : MonoBehaviour, IPointerClickHandler, IPointerMoveHandler
    {
        public RunController run;
        public CombatController combat;
        public ArcadeHudController drawers;
        public RunExpansionView expansion;
        public ItemTooltipView tooltip;
        public RectTransform menu;
        public Button drink, throwButton, discard, cancel;
        public TextMeshProUGUI heading, prompt;
        public Image bottle, targetRing;
        public bool IsOpen => gameObject.activeSelf;
        public bool IsTargeting { get; private set; }
        public EquipmentData Selected => potion;
        EquipmentData potion;
        bool flying;
        Vector2 discardPosition;
        Camera Camera { get { var canvas=GetComponentInParent<Canvas>(); return canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera; } }
        RectTransform Root => (RectTransform)transform;

        void Awake()
        {
            discardPosition=((RectTransform)discard.transform).anchoredPosition;
            drink.onClick.AddListener(Drink);
            throwButton.onClick.AddListener(BeginThrow);
            discard.onClick.AddListener(Discard);
            cancel.onClick.AddListener(Close);
        }
        public void Open(EquipmentData item, RectTransform anchor)
        {
            if (run.State == null || item == null || !run.State.Potions.Contains(item) || expansion.IsDecisionOpen) return;
            StopAllCoroutines();
            potion = item; flying = false; IsTargeting = false; cancel.interactable = true;
            gameObject.SetActive(true); transform.SetAsLastSibling(); combat.PotionInputBlocked = true;
            tooltip.Hide(); menu.gameObject.SetActive(true); cancel.gameObject.SetActive(false);
            bottle.enabled = targetRing.enabled = false; prompt.gameObject.SetActive(false);
            heading.text = item.displayName;
            drink.gameObject.SetActive(item.HasPotionAction(false));throwButton.gameObject.SetActive(item.HasPotionAction(true));
            ((RectTransform)discard.transform).anchoredPosition=!item.HasPotionAction(false)&&!item.HasPotionAction(true)?((RectTransform)drink.transform).anchoredPosition:discardPosition;
            drink.interactable = run.State.CanUsePotion(item, false);
            throwButton.interactable = combat.CanThrowPotion && run.State.CanUsePotion(item, true);
            var corners = new Vector3[4]; anchor.GetWorldCorners(corners);
            var p = (Vector2)Root.InverseTransformPoint(corners[0]);
            menu.anchoredPosition = new Vector2(Mathf.Clamp(p.x + menu.rect.width * .5f, Root.rect.xMin + menu.rect.width * .5f + 8, Root.rect.xMax - menu.rect.width * .5f - 8),
                Mathf.Clamp(p.y - menu.rect.height * .5f - 12, Root.rect.yMin + menu.rect.height * .5f + 8, Root.rect.yMax - menu.rect.height * .5f - 8));
        }
        public void Drink()
        {
            if (flying || potion == null || !run.TryUsePotion(potion)) return;
            Close();
        }
        public void Discard()
        {
            if (flying || potion == null || !run.TryRemoveEquipment(potion)) return;
            Close();
        }
        public void BeginThrow()
        {
            if (flying || !combat.CanThrowPotion || !run.State.CanUsePotion(potion, true)) return;
            drawers.Close(); combat.PotionInputBlocked = true;
            IsTargeting = true; menu.gameObject.SetActive(false); cancel.gameObject.SetActive(true);
            prompt.text = GameText.Get("ui.potion.target"); prompt.gameObject.SetActive(true);
            bottle.sprite = potion.icon; bottle.enabled = true; MoveAim(Input.mousePosition);
        }
        public void Close()
        {
            StopAllCoroutines(); IsTargeting = flying = false; potion = null;
            if (combat != null) combat.PotionInputBlocked = false;
            gameObject.SetActive(false);
        }
        void OnDisable() { if (combat != null) combat.PotionInputBlocked = false; }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)) { Close(); return; }
            if (run.State == null || !run.State.Player.IsAlive || potion == null || !run.State.Potions.Contains(potion) || expansion.IsDecisionOpen || IsTargeting && !combat.CanThrowPotion) Close();
        }
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || flying) return;
            if (!IsTargeting) { Close(); return; }
            if (combat.PotionTargetAt(e.position, out var target, out var destination)) StartCoroutine(Flight(target, destination));
        }
        public void OnPointerMove(PointerEventData e) { if (IsTargeting && !flying) MoveAim(e.position); }
        void MoveAim(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, Camera, out var p);
            bottle.rectTransform.anchoredPosition = new Vector2(Mathf.Round(p.x / 4) * 4, Mathf.Round(p.y / 4) * 4) + new Vector2(24, -24);
            var valid = combat.PotionTargetAt(screen, out _, out var center);
            targetRing.enabled = valid;
            if (valid) { RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, center, Camera, out p); targetRing.rectTransform.anchoredPosition = p; }
        }
        IEnumerator Flight(MonsterRuntime target, Vector2 destination)
        {
            flying = true; targetRing.enabled = false; cancel.interactable = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, destination, Camera, out var end);
            var start = new Vector2(0, Root.rect.yMin + 100);
            for (float t = 0; t < .22f; t += Time.unscaledDeltaTime)
            {
                var point = Vector2.Lerp(start, end, t / .22f) + Vector2.up * (Mathf.Sin(t / .22f * Mathf.PI) * 120);
                bottle.rectTransform.anchoredPosition = new Vector2(Mathf.Round(point.x / 4) * 4, Mathf.Round(point.y / 4) * 4);
                yield return null;
            }
            combat.ThrowPotion(potion, target); cancel.interactable = true; Close();
        }
    }
}
