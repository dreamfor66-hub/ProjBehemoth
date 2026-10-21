using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    [Serializable]
    public sealed class ExpansionChoiceButton
    {
        public Button button;
        public TextMeshProUGUI title;
        public TextMeshProUGUI description;
        public Image icon;
    }
    public sealed class PresentationChoice
    {
        public string Title, Description;
        public Sprite Icon;
        public bool Enabled = true;
        public Action Selected;
    }
    public sealed class RunExpansionView : MonoBehaviour
    {
        public TextMeshProUGUI characterStatus;
        public TextMeshProUGUI stageTitle;
        public TextMeshProUGUI companionLabel;
        public TextMeshProUGUI encounterHint;
        public Button[] spellButtons = new Button[4];
        public TextMeshProUGUI[] spellLabels = new TextMeshProUGUI[4];
        public Image[] spellFills = new Image[4];
        public GameObject modal;
        public TextMeshProUGUI modalTitle;
        public TextMeshProUGUI modalNarrative;
        public Image modalIllustration;
        public ExpansionChoiceButton[] choices;
        public Button returnButton;
        public Button previousPage, nextPage;
        public TextMeshProUGUI pageLabel;
        private PresentationChoice[] currentChoices = Array.Empty<PresentationChoice>();
        private int page;
        private CombatController combat;
        private RunController run;
        public bool IsModalOpen => modal != null && modal.activeSelf;

        public void Bind(RunController controller, CombatController battle)
        {
            run = controller; combat = battle;
            previousPage.onClick.RemoveAllListeners(); nextPage.onClick.RemoveAllListeners();
            previousPage.onClick.AddListener(() => { page--; DrawChoices(); });
            nextPage.onClick.AddListener(() => { page++; DrawChoices(); });
            for (var i = 0; i < spellButtons.Length; i++)
            { spellButtons[i].onClick.RemoveAllListeners(); spellButtons[i].interactable = false;
              foreach (var graphic in spellButtons[i].GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false; }
            returnButton.onClick.RemoveAllListeners();
            returnButton.onClick.AddListener(() => ShowModal(GameText.Get("ui.runexpansionview.c2509e30"), GameText.Get("ui.runexpansionview.db812935"), new[] {
                new PresentationChoice { Title = GameText.Get("ui.runexpansionview.7b8abde6"), Selected = HideModal },
                new PresentationChoice { Title = GameText.Get("ui.runexpansionview.db5d33bd"), Selected = RunSession.ReturnToSelection }
            }));
            HideModal(); RefreshCombat(combat);
        }
        public void RefreshCombat(CombatController battle)
        {
            if (run?.State == null) return;
            var player = run.State.Player;
            if (encounterHint != null) { encounterHint.text = battle.EncounterHint; encounterHint.gameObject.SetActive(battle.IsActive && !string.IsNullOrWhiteSpace(battle.EncounterHint)); }
            if (characterStatus != null) { characterStatus.text = player.StatusText; characterStatus.gameObject.SetActive(battle.IsActive && !string.IsNullOrEmpty(player.StatusText)); }
            if (companionLabel != null) companionLabel.text = string.Join(" · ", run.State.EventBurdens.Select(item => GameFlowController.BurdenLabel(item.Key) + GameText.Format("ui.runexpansionview.660793f8", item.Value))
                .Concat(run.State.EventGoldDebt > 0 ? new[] { GameText.Format("ui.runexpansionview.9c90367", run.State.EventGoldDebt) } : Array.Empty<string>()));
            for (var i = 0; i < 4; i++)
            {
                var spell = run.State.Slot((SwipeDirection)i);
                var arrow = MonkFormData.DirectionLabel((SwipeDirection)i);
                spellButtons[i].gameObject.SetActive(spell != null);
                if (spell == null) continue;
                var runtime = run.State.RuntimeFor(spell);
                var need = spell.castingSeconds * player.SpellCastingFor(spell);
                var readiness = runtime.Cooldown > 0 ? GameText.Format("ui.runexpansionview.98b418ae", runtime.Cooldown) : battle.IsActive && battle.CastingSeconds >= need ? GameText.Get("ui.runexpansionview.9ebf2a5b") : GameText.Format("ui.runexpansionview.9dd5ca8c", need);
                spellLabels[i].text = GameText.Format("ui.runexpansionview.31655660", arrow, spell.displayName, readiness);
                spellLabels[i].color = runtime.Cooldown > 0 ? new Color(.65f, .7f, .78f) : spell.Color;
                SetProgress(spellFills[i], runtime.Cooldown > 0 ? 1f - runtime.Cooldown / (spell.cooldownSeconds * player.SpellCooldownFor(spell)) : Mathf.Clamp01(battle.CastingSeconds / need));
                spellFills[i].color = spell.Color;
            }
        }
        public void SetStage(string name, int act) { if (stageTitle != null) stageTitle.text = GameText.Format("ui.runexpansionview.b3a6d243", act, name); }
        private static void SetProgress(Image image, float amount)
        {
            amount = Mathf.Clamp01(amount); image.fillAmount = amount; image.type = Image.Type.Simple; image.enabled = amount > 0;
            image.rectTransform.pivot = new Vector2(0, .5f); image.rectTransform.anchoredPosition = new Vector2(-55.5f, -25);
            image.rectTransform.sizeDelta = new Vector2(111f * amount, 5);
        }
        public void ShowModal(string title, string narrative, IEnumerable<PresentationChoice> options, Sprite illustration = null)
        {
            if (combat != null) combat.InputPaused = true;
            modal.SetActive(true); modal.transform.SetAsLastSibling();
            modalTitle.text = title; modalNarrative.text = narrative;
            modalNarrative.enableAutoSizing = true; modalNarrative.fontSizeMin = 18; modalNarrative.fontSizeMax = 23;
            modalIllustration.sprite = illustration; modalIllustration.enabled = illustration != null;
            currentChoices = options.ToArray(); page = 0; DrawChoices();
        }
        private void DrawChoices()
        {
            var pages = Mathf.Max(1, Mathf.CeilToInt((float)currentChoices.Length / choices.Length));
            page = Mathf.Clamp(page, 0, pages - 1);
            previousPage.gameObject.SetActive(pages > 1); nextPage.gameObject.SetActive(pages > 1);
            previousPage.interactable = page > 0; nextPage.interactable = page < pages - 1;
            pageLabel.text = pages > 1 ? GameText.Format("ui.runexpansionview.5d4342db", page + 1, pages) : "";
            var list = currentChoices.Skip(page * choices.Length).Take(choices.Length).ToArray();
            for (var i = 0; i < choices.Length; i++)
            {
                var button = choices[i]; button.button.gameObject.SetActive(i < list.Length);
                if (i >= list.Length) continue;
                var option = list[i]; button.title.text = option.Title; button.description.text = option.Description ?? "";
                button.description.enableAutoSizing = true; button.description.fontSizeMin = 16; button.description.fontSizeMax = 19;
                button.icon.sprite = option.Icon; button.icon.enabled = option.Icon != null;
                button.button.interactable = option.Enabled;
                button.button.onClick.RemoveAllListeners(); button.button.onClick.AddListener(() => { HideModal(); option.Selected?.Invoke(); });
            }
        }
        public void ShowMessage(string title, string body, Action continued) => ShowModal(title, body, new[] { new PresentationChoice { Title = GameText.Get("ui.runexpansionview.781bae3a"), Selected = continued } });
        public void HideModal() { if (modal != null) modal.SetActive(false); if (combat != null) combat.InputPaused = false; }
        public void OfferSpell(SpellData spell, Action<bool> completed)
        {
            if (run.State.RuntimeFor(spell) != null) { completed(true); return; }
            var previous = run.State.Slot(spell.defaultSlot);
            if (previous == null) { run.State.LearnSpell(spell); RefreshCombat(combat); completed(true); return; }
            ShowModal(GameText.Get("ui.runexpansionview.848d307f"), GameText.Format("ui.runexpansionview.4cf1a98", MonkFormData.DirectionLabel(spell.defaultSlot), previous.displayName, previous.description), new[] {
                new PresentationChoice { Title = spell.displayName + GameText.Get("ui.runexpansionview.cc287719"), Description = SpellDetails(spell, run.State.Player) + GameText.Get("ui.runexpansionview.903af72b"), Icon = spell.icon,
                    Selected = () => { run.State.LearnSpell(spell, true); RefreshCombat(combat); completed(true); } },
                new PresentationChoice { Title = previous.displayName + GameText.Get("ui.runexpansionview.76082537"), Description = GameText.Get("ui.runexpansionview.1ddf41c5"), Icon = previous.icon, Selected = () => completed(false) }
            });
        }
        public void ResolvePendingSpells(Action completed)
        {
            if (run.State.PendingSpellOffers.Count == 0) { completed?.Invoke(); return; }
            OfferSpell(run.State.PendingSpellOffers.Dequeue(), _ => ResolvePendingSpells(completed));
        }
        public static string SpellDetails(SpellData spell, PlayerCombatRuntime player) => GameText.Format("ui.spell.details", MonkFormData.DirectionLabel(spell.defaultSlot), spell.displayName, spell.castingSeconds * player.SpellCastingFor(spell), spell.cooldownSeconds * player.SpellCooldownFor(spell), spell.description);
    }
}
