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
        public string Title, Description, Summary;
        public Sprite Icon;
        public bool Enabled = true;
        public Action Selected;
        public EquipmentData PreviewItem;
        public string Requirements;
    }
    public sealed partial class RunExpansionView : MonoBehaviour
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
        public ExpansionChoiceButton[] compactChoices;
        public ExpansionChoiceButton[] ActiveChoices => portraitEvent && portraitChoices != null && portraitChoices.Length > 0 ? portraitChoices : denseOptions ? compactChoices : choices;
        private bool denseOptions;
        public Button returnButton;
        public Button previousPage, nextPage;
        public TextMeshProUGUI pageLabel;
        public ScrollRect narrativeScroll;
        public PixelDitherTransition transition;
        public float illustratedTextY = 151, illustratedTextHeight = 128, plainTextY = 205, plainTextHeight = 236;
        [Header("Event reading layout (compact canvas)")]
        public float narrativeTop = 280;
        public float choicesBottom = -360;
        [Min(0)] public float choiceSpacing = 16, narrativeChoiceSpacing = 32;
        [Min(1)] public float illustratedNarrativeWidth = 1040, plainNarrativeWidth = 1360;
        [Min(1)] public float narrativeScrollbarWidth = 16;
        [Min(0)] public float narrativeScrollbarGap = 16;
        public bool compact;
        public ItemTooltipView tooltip;
        public SpellbookView spellbookView;
        private PresentationChoice[] currentChoices = Array.Empty<PresentationChoice>();
        private int page;
        private CombatController combat;
        private RunController run;
        private bool wasCombat;
        private float encounterHintUntil;
        public bool IsModalOpen => modal != null && modal.activeSelf || spellbookView!=null&&spellbookView.gameObject.activeSelf;
        public bool IsDecisionOpen
        {
            get
            {
                if(modal==null)return false;
                foreach(Transform panel in modal.transform.parent)if(panel.gameObject.activeInHierarchy)return true;
                return false;
            }
        }
        private void LateUpdate()
        {
            if(!compact)return;
            var show=!IsDecisionOpen;
            if(returnButton!=null)returnButton.gameObject.SetActive(show);
            if(stageTitle!=null)stageTitle.gameObject.SetActive(show && (combat==null || !combat.IsActive));
        }

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
                new PresentationChoice { Title = GameText.Get("ui.playrecord.open"), Selected = () => { HideModal(); FindFirstObjectByType<PlayRecordView>()?.Open(); } },
                new PresentationChoice { Title = GameText.Get("ui.runexpansionview.db5d33bd"), Selected = RunSession.ReturnToSelection }
            }));
            HideModal(); RefreshCombat(combat);
        }
        public void RefreshCombat(CombatController battle)
        {
            if (run?.State == null) return;
            var player = run.State.Player;
            if(battle.IsActive&&!wasCombat)encounterHintUntil=Time.unscaledTime+5;
            wasCombat=battle.IsActive;
            if (encounterHint != null) { encounterHint.text = battle.EncounterHint; encounterHint.gameObject.SetActive(!compact && battle.IsActive && !string.IsNullOrWhiteSpace(battle.EncounterHint)); }
            if (characterStatus != null) { characterStatus.text = player.StatusText; characterStatus.gameObject.SetActive(battle.IsActive && !string.IsNullOrEmpty(player.StatusText)); }
            if (companionLabel != null) companionLabel.text = string.Join(" · ", run.State.EventBurdens.Select(item => GameFlowController.BurdenLabel(item.Key) + GameText.Format("ui.runexpansionview.660793f8", item.Value))
                .Concat(run.State.EventGoldDebt > 0 ? new[] { GameText.Format("ui.runexpansionview.9c90367", run.State.EventGoldDebt) } : Array.Empty<string>()));
            for (var i = 0; i < 4; i++)
            {
                var spell = run.State.Slot((SwipeDirection)i);
                var arrow = SwipeDirectionUtility.Label((SwipeDirection)i);
                spellButtons[i].gameObject.SetActive(spell != null && battle.IsActive);
                if (spell == null) continue;
                var runtime = run.State.RuntimeFor(spell);
                var need = spell.castingSeconds * player.SpellCastingFor(spell);
                var readiness = runtime.Cooldown > 0 ? GameText.Format("ui.runexpansionview.98b418ae", runtime.Cooldown) : battle.CastingSeconds >= need ? GameText.Get("ui.runexpansionview.9ebf2a5b") : battle.CastingSeconds > 0 ? GameText.Format("ui.runexpansionview.9dd5ca8c", need) : spell.displayName;
                spellLabels[i].text = compact?arrow+(runtime.Cooldown>0?Mathf.CeilToInt(runtime.Cooldown).ToString():spell.mpCost.ToString("0")):GameText.Format("ui.spell.slotcompact", arrow, spell.mpCost, readiness);
                var icon = spellButtons[i].transform.Find("SpellIcon")?.GetComponent<Image>();
                if (icon != null) { icon.sprite = spell.icon; icon.color = Color.white; icon.enabled = spell.icon != null; }
                spellLabels[i].color = runtime.Cooldown > 0 || !player.CanPayMp(spell.mpCost) ? new Color(.48f, .46f, .54f) : spell.Color;
                SetProgress(spellFills[i], runtime.Cooldown > 0 ? 1f - runtime.Cooldown / (spell.cooldownSeconds * player.SpellCooldownFor(spell)) : Mathf.Clamp01(battle.CastingSeconds / need));
                spellFills[i].color = spell.Color;
            }
        }
        public void SetStage(string name, int act) { if (stageTitle != null) stageTitle.text = GameText.Format("ui.runexpansionview.b3a6d243", act, name); }
        private static void SetProgress(Image image, float amount)
        {
            amount=Mathf.Clamp01(amount);image.fillAmount=amount;image.type=Image.Type.Simple;image.enabled=amount>0;
            var parent=(RectTransform)image.transform.parent;var rect=image.rectTransform;
            rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.pivot=new Vector2(0,.5f);
            rect.anchoredPosition=new Vector2(-parent.rect.width*.5f+8,-parent.rect.height*.5f+8);
            rect.sizeDelta=new Vector2(Mathf.Round((parent.rect.width-16)*amount/4)*4,8);
        }
        public void ShowModal(string title, string narrative, IEnumerable<PresentationChoice> options, Sprite illustration = null, bool compactOptions = false, bool illustratedEvent = false)
        {
            RestoreStandardFraming();
            portraitEvent = compact && illustratedEvent && illustration != null && portraitChoices != null && portraitChoices.Length > 0;
            if(portraitChoices!=null)foreach(var choice in portraitChoices)choice.button.gameObject.SetActive(false);
            denseOptions=compact&&compactOptions&&compactChoices!=null&&compactChoices.Length>0;
            foreach(var choice in choices)choice.button.gameObject.SetActive(false);
            if(compactChoices!=null)foreach(var choice in compactChoices)choice.button.gameObject.SetActive(false);
            if(spellbookView!=null)spellbookView.gameObject.SetActive(false);
            if (combat != null) combat.InputPaused = true;
            modal.SetActive(true); modal.transform.parent.SetAsLastSibling(); modal.transform.SetAsLastSibling();
            modalTitle.text = title; modalNarrative.text = narrative;
            var narrativeSize=modalNarrative.GetComponent<TypographyRoleBinding>()?.designSize??44;
            modalNarrative.enableAutoSizing = !compact&&narrativeScroll == null; modalNarrative.fontSizeMin = compact?narrativeSize:18; modalNarrative.fontSizeMax = compact?narrativeSize:23;
            modalTitle.GetComponent<TypographyRoleBinding>()?.Apply();
            modalNarrative.GetComponent<TypographyRoleBinding>()?.Apply();
            var changedIllustration = modalIllustration.sprite != illustration;
            modalIllustration.sprite = illustration; modalIllustration.color = Color.white; modalIllustration.enabled = illustration != null;
            if (changedIllustration && illustration != null) transition?.Reveal();
            currentChoices = options.ToArray(); page = 0; DrawChoices();
            if (narrativeScroll != null)
            {
                narrativeScroll.StopMovement();
                narrativeScroll.verticalNormalizedPosition = 1;
            }
        }
        private void DrawChoices()
        {
            var rows=ActiveChoices;
            var pages = Mathf.Max(1, Mathf.CeilToInt((float)currentChoices.Length / rows.Length));
            page = Mathf.Clamp(page, 0, pages - 1);
            previousPage.gameObject.SetActive(pages > 1); nextPage.gameObject.SetActive(pages > 1);
            previousPage.interactable = page > 0; nextPage.interactable = page < pages - 1;
            pageLabel.text = pages > 1 ? GameText.Format("ui.runexpansionview.5d4342db", page + 1, pages) : "";
            var list = currentChoices.Skip(page * rows.Length).Take(rows.Length).ToArray();
            for (var i = 0; i < rows.Length; i++)
            {
                var button = rows[i]; button.button.gameObject.SetActive(i < list.Length);
                if (i >= list.Length) continue;
                var option = list[i]; button.title.text = "> "+option.Title; button.description.text = (string.IsNullOrWhiteSpace(option.Requirements)?"":"<color=#A49A91>"+GameText.Format("ui.feedback.condition",option.Requirements)+"</color>\n")+(option.Description ?? "");
                var preview=button.button.GetComponent<ChoicePreview>()??button.button.gameObject.AddComponent<ChoicePreview>();preview.Bind(tooltip,option.PreviewItem,option.Title,button.description.text);
                if(option.Summary!=null)button.description.text=option.Summary;
                var descriptionSize=button.description.GetComponent<TypographyRoleBinding>()?.designSize??40;
                button.description.enableAutoSizing = !compact; button.description.fontSizeMin = compact?descriptionSize:18; button.description.fontSizeMax = compact?descriptionSize:20; button.description.fontSize=compact?descriptionSize:20;
                button.title.GetComponent<TypographyRoleBinding>()?.Apply();
                button.description.GetComponent<TypographyRoleBinding>()?.Apply();
                button.icon.sprite = option.Icon; button.icon.color = Color.white; button.icon.enabled = option.Icon != null;
                button.button.interactable = option.Enabled;
                if(portraitEvent)
                {
                    button.title.color=option.Enabled?new Color32(224,212,182,255):new Color32(125,121,113,255);
                    button.description.color=option.Enabled?new Color32(201,193,173,255):new Color32(125,121,113,255);
                }
                button.button.onClick.RemoveAllListeners(); button.button.onClick.AddListener(() => { HideModal(); option.Selected?.Invoke(); });
            }
            LayoutNarrative(list.Length);
        }

        private void LayoutNarrative(int choiceCount)
        {
            if (narrativeScroll == null) return;
            var illustrated = modalIllustration.sprite != null;
            var height = illustrated ? illustratedTextHeight : plainTextHeight;
            var centerY = illustrated ? illustratedTextY : plainTextY;
            var rect = (RectTransform)narrativeScroll.transform;
            if (portraitEvent)
            {
                LayoutPortraitEvent(choiceCount, out height, out centerY);
            }
            else if (compact)
            {
                var panel=modalTitle.rectTransform.parent as RectTransform;
                var plainWidth=panel!=null?Mathf.Min(plainNarrativeWidth,panel.rect.width-80):plainNarrativeWidth;
                var width = illustrated ? Mathf.Min(illustratedNarrativeWidth,Mathf.Max(160,plainWidth-320)) : plainWidth;
                modalTitle.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,plainWidth);
                var contentTop=panel!=null?Mathf.Min(narrativeTop,panel.rect.height*.5f-112):narrativeTop;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                rect.anchoredPosition = new Vector2(illustrated ? modalTitle.rectTransform.anchoredPosition.x + (plainWidth - width) * .5f : modalTitle.rectTransform.anchoredPosition.x, rect.anchoredPosition.y);
                foreach(var row in ActiveChoices)
                {
                    var rowWidth=panel!=null?panel.rect.width-64:plainWidth;
                    ((RectTransform)row.button.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,rowWidth);
                    row.title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,rowWidth-64);
                    row.description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,rowWidth-64);
                }
                if (!denseOptions)
                {
                    // Reserve only the height that visible titles and descriptions actually need.
                    // A single Continue action should leave most of the panel available for reading.
                    var bottom = panel!=null?Mathf.Max(choicesBottom,-panel.rect.height*.5f+40):choicesBottom;
                    for (var i = choiceCount - 1; i >= 0; i--)
                    {
                        var row = choices[i];
                        row.title.GetComponent<TypographyRoleBinding>()?.Apply();
                        row.description.GetComponent<TypographyRoleBinding>()?.Apply();
                        var titleHeight = Mathf.Max(56, row.title.GetPreferredValues(row.title.text, row.title.rectTransform.rect.width, Mathf.Infinity).y);
                        var hasDescription = !string.IsNullOrWhiteSpace(row.description.text);
                        var descriptionHeight = hasDescription ? row.description.GetPreferredValues(row.description.text, row.description.rectTransform.rect.width, Mathf.Infinity).y : 0;
                        var rowHeight = Mathf.Max(80, titleHeight + (hasDescription ? 8 + descriptionHeight : 0) + 24);
                        var rowRect = (RectTransform)row.button.transform;
                        rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
                        rowRect.anchoredPosition = new Vector2(rowRect.anchoredPosition.x, bottom + rowHeight * .5f);
                        row.title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, titleHeight);
                        row.title.rectTransform.anchoredPosition = new Vector2(row.title.rectTransform.anchoredPosition.x, rowHeight * .5f - 12 - titleHeight * .5f);
                        row.description.gameObject.SetActive(hasDescription);
                        row.description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, descriptionHeight);
                        row.description.rectTransform.anchoredPosition = new Vector2(row.description.rectTransform.anchoredPosition.x, rowHeight * .5f - 12 - titleHeight - 8 - descriptionHeight * .5f);
                        bottom += rowHeight + (i > 0 ? choiceSpacing : 0);
                    }
                    height = Mathf.Max(128, contentTop - bottom - narrativeChoiceSpacing);
                    centerY = contentTop - height * .5f;
                }
            }
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, centerY);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            // Stretch both children with their parent. Fixed center offsets left the scrollbar
            // over the text when the authored narrative panel was widened.
            var viewport = narrativeScroll.viewport;
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(-narrativeScrollbarWidth - narrativeScrollbarGap, 0);
            if (narrativeScroll.verticalScrollbar != null)
            {
                var bar = (RectTransform)narrativeScroll.verticalScrollbar.transform;
                bar.anchorMin = new Vector2(1, 0); bar.anchorMax = Vector2.one;
                bar.pivot = new Vector2(1, .5f);
                bar.anchoredPosition = Vector2.zero;
                bar.sizeDelta = new Vector2(narrativeScrollbarWidth, 0);
                narrativeScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(narrativeScroll.content);
            Canvas.ForceUpdateCanvases();
        }
        public void ShowMessage(string title, string body, Action continued) => ShowModal(title, body, new[] { new PresentationChoice { Title = GameText.Get("ui.runexpansionview.781bae3a"), Selected = continued } });
        public void HideModal() { if (modal != null) modal.SetActive(false); if(spellbookView!=null)spellbookView.gameObject.SetActive(false); if (combat != null) combat.InputPaused = false; }
        public void OfferSpell(SpellData spell, Action<bool> completed, Func<bool> beforeLearning = null)
        {
            if (run.State.RuntimeFor(spell) != null) { completed(true); return; }
            var previous = run.State.Slot(spell.defaultSlot);
            if (previous == null)
            {
                if (beforeLearning != null && !beforeLearning()) { completed(false); return; }
                run.State.LearnSpell(spell); RefreshCombat(combat); completed(true); return;
            }
            ShowModal(GameText.Get("ui.spell.learn.title"), GameText.Format("ui.spell.learn.body",spell.displayName,SwipeDirectionUtility.Label(spell.defaultSlot)), new[] {
                new PresentationChoice { Title = spell.displayName + GameText.Get("ui.spell.learn.prepare"), Description = SpellDetails(spell, run.State.Player), Icon = spell.icon,
                    Selected = () => {
                        if (beforeLearning != null && !beforeLearning()) { completed(false); return; }
                        run.State.LearnSpell(spell, true); RefreshCombat(combat); completed(true);
                    } },
                new PresentationChoice { Title = previous.displayName + GameText.Get("ui.runexpansionview.76082537"), Description = GameText.Get("ui.spell.learn.store"), Icon = previous.icon, Selected = () => {
                    if(beforeLearning!=null&&!beforeLearning()){completed(false);return;}
                    run.State.RememberSpell(spell);RefreshCombat(combat);completed(true);
                } },
                new PresentationChoice {Title=GameText.Get("ui.spell.learn.decline"),Selected=()=>completed(false)}
            });
        }
        public void ResolvePendingSpells(Action completed)
        {
            if (run.State.PendingSpellOffers.Count == 0) { completed?.Invoke(); return; }
            OfferSpell(run.State.PendingSpellOffers.Dequeue(), _ => ResolvePendingSpells(completed));
        }
        public void ShowSpellbook()
        {
            if (run?.State == null || combat != null && combat.IsActive) return;
            if(spellbookView!=null){HideModal();if(combat!=null)combat.InputPaused=true;spellbookView.Open(run.State);return;}
            ShowModal(GameText.Get("ui.spellbook.title"), GameText.Get("ui.spellbook.description"), run.State.Spellbook.Select(s => new PresentationChoice
            {
                Title = SwipeDirectionUtility.Label(s.Data.defaultSlot) + " " + s.Data.displayName,
                Description = SpellDetails(s.Data, run.State.Player), Icon = s.Data.icon,
                Selected = () => { run.State.EquipKnownSpell(s.Data); RefreshCombat(combat); }
            }).Append(new PresentationChoice { Title = GameText.Get("ui.common.close"), Selected = () => { } }));
        }
        public void RefreshSpellbookHud()=>RefreshCombat(combat);
        public static string SpellDetails(SpellData spell, PlayerCombatRuntime player) => GameText.Format("ui.spell.details", SwipeDirectionUtility.Label(spell.defaultSlot), spell.displayName, spell.castingSeconds * player.SpellCastingFor(spell), spell.cooldownSeconds * player.SpellCooldownFor(spell), spell.description) + GameText.Format("ui.mana.cost", spell.mpCost);
    }
}
