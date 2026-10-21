using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public sealed partial class GameFlowController
    {
        [SerializeField] private GameContent content;
        [SerializeField] private RunExpansionView expansionView;
        private Action eventCombatCompleted;
        private RandomEventData activeEvent;
        private void BindExpansion() { eventCombatCompleted = null; activeEvent = null; expansionView?.Bind(runController, combatController); }
        private void ShowStartExplanation()
        {
            if (expansionView == null) return;
            var data = runController.State.Player.Data;
            expansionView.ShowMessage(data.displayName + GameText.Get("ui.gameflowexpansion.ca3d25b5"), data.description + GameText.Get("ui.gameflowexpansion.34facc8c") + (runController.State.Spellbook.Count > 0 ? GameText.Get("ui.gameflowexpansion.a8b9dc2c") : "") + GameText.Get("ui.gameflowexpansion.a63f7436"), () => RefreshHud());
        }
        private void ShowRandomEvent(DungeonRoom room)
        {
            State = GameFlowState.RoomEvent;
            if (content == null || expansionView == null) { room.IsCleared = true; ShowNavigation(); return; }
            activeEvent = RandomEventSystem.Draw(content, runController.State, dungeonController.Floor, room.RoomId);
            if (activeEvent == null)
            {
                room.IsCleared = true;
                expansionView.ShowMessage(GameText.Get("ui.gameflowexpansion.44d9a674"), GameText.Get("ui.gameflowexpansion.2d68940e"), AfterRoomResolution); return;
            }
            var current = activeEvent;
            var options = current.options.Select(option =>
            {
                var unavailable = RandomEventSystem.UnavailableReason(option, runController.State);
                var costs = new List<string>();
                if (option.goldCost > 0) costs.Add(GameText.Format("ui.gameflowexpansion.77b4b9fc", option.goldCost));
                if (option.hpCost > 0) costs.Add(GameText.Format("ui.gameflowexpansion.bd8c797", option.hpCost));
                if (option.maxHpCost > 0) costs.Add(GameText.Format("ui.gameflowexpansion.5b6889a1", option.maxHpCost));
                if (option.consumeHealingPotion) costs.Add(GameText.Get("ui.gameflowexpansion.ffbb9952"));
                return new PresentationChoice {
                    Title = option.title,
                    Description = option.description + (costs.Count > 0 ? "\n" + string.Join(" · ", costs) : "") + (unavailable != null ? "\n[" + unavailable + "]" : ""),
                    Enabled = unavailable == null,
                    Selected = () =>
                    {
                        if (!RandomEventSystem.Pay(option, runController.State)) { ShowRandomEvent(room); return; }
                        // Resolve once, including when this choice opens a reward selector or a battle.
                        room.IsCleared = true;
                        var messages = new List<string> { GameText.Get("ui.gameflowexpansion.5cf1ad0d") + option.title };
                        if (costs.Count > 0) messages.Add(string.Join(" · ", costs));
                        ApplyEventEffects(current, option.effects, 0, messages);
                    }
                };
            }).ToArray();
            expansionView.ShowModal(current.displayName, current.narrative, options, current.illustration);
        }
        private void ApplyEventEffects(RandomEventData story, IReadOnlyList<EventEffect> effects, int index, List<string> messages)
        {
            RefreshHud();
            if (index >= effects.Count)
            { expansionView.ShowMessage(story.displayName, string.Join("\n", messages), AfterRoomResolution); return; }
            var effect = effects[index]; var run = runController.State;
            Action next = () => ApplyEventEffects(story, effects, index + 1, messages);
            switch (effect.kind)
            {
                case EventEffectKind.Potion:
                    var foundPotion = runController.GetPotionDrop(run.Seed, run.SeenEvents.Count);
                    if (foundPotion != null && runController.TryGrantEquipment(foundPotion)) messages.Add(foundPotion.displayName);
                    else { run.Player.Heal(8); messages.Add(GameText.Format("ui.gameflowexpansion.84ab5d91", 8)); }
                    break;
                case EventEffectKind.Burden:
                    run.AddBurden(effect.flag, Mathf.RoundToInt(effect.amount)); messages.Add(BurdenLabel(effect.flag) + GameText.Format("ui.gameflowexpansion.52949f16", effect.amount)); break;
                case EventEffectKind.GoldDebt:
                    run.EventGoldDebt += Mathf.RoundToInt(effect.amount); messages.Add(GameText.Format("ui.gameflowexpansion.20ed3cd1", effect.amount)); break;
                case EventEffectKind.Gold:
                    run.GainGold(Mathf.RoundToInt(effect.amount)); messages.Add(GameText.Format("ui.gameflowexpansion.faa291fc", effect.amount)); break;
                case EventEffectKind.Heal:
                    var before = run.Player.CurrentHp; run.Player.Heal(effect.amount); messages.Add(GameText.Format("ui.gameflowexpansion.84ab5d91", run.Player.CurrentHp - before)); break;
                case EventEffectKind.Stat:
                    run.Player.ApplyModifiers(new[] { new StatModifier { kind = effect.stat, value = effect.amount } }); messages.Add(GameText.Format("ui.gameflowexpansion.9a1c4500", StatName(effect.stat), effect.amount)); break;
                case EventEffectKind.Relic:
                    if (runController.TryGrantRelic(effect.relic)) messages.Add(GameText.Get("ui.gameflowexpansion.9f2ed0e0") + effect.relic.displayName + "\n" + effect.relic.description);
                    expansionView.ResolvePendingSpells(next); return;
                case EventEffectKind.SetFlag:
                    run.EventFlags.Add(effect.flag); if (!string.IsNullOrEmpty(effect.flag)) messages.Add(GameText.Get("ui.gameflowexpansion.75c6600c")); break;
                case EventEffectKind.RemoveFlag:
                    run.EventFlags.Remove(effect.flag); break;
                case EventEffectKind.SharedRelicChoice:
                case EventEffectKind.ExclusiveChoice:
                    var relics = effect.kind == EventEffectKind.ExclusiveChoice ? runController.GetExclusiveChoices(run.Seed + run.SeenEvents.Count * 577)
                        : runController.GetRelicChoices(run.Seed, run.SeenEvents.Count * 997);
                    if (relics.Count == 0) { run.GainGold(20); messages.Add(GameText.Get("ui.gameflowexpansion.8dabb184")); break; }
                    expansionView.ShowModal(effect.kind == EventEffectKind.ExclusiveChoice ? GameText.Get("ui.gameflowexpansion.d1e98751") : GameText.Get("ui.gameflowexpansion.d1e21774"), GameText.Get("ui.gameflowexpansion.7bf1bd68"), relics.Select(relic => new PresentationChoice {
                        Title = relic.displayName, Description = relic.description, Icon = relic.icon,
                        Selected = () => { if (runController.TryGrantRelic(relic)) messages.Add(GameText.Get("ui.gameflowexpansion.9f2ed0e0") + relic.displayName); expansionView.ResolvePendingSpells(next); }
                    })); return;
                case EventEffectKind.PerkChoice:
                    var perks = runController.Perks.GetChoices(run, run.Seed + run.SeenEvents.Count * 997);
                    if (perks.Count == 0) { run.GainGold(15); messages.Add(GameText.Get("ui.gameflowexpansion.8b1abf43")); break; }
                    expansionView.ShowModal(GameText.Get("ui.gameflowexpansion.66154851"), GameText.Get("ui.gameflowexpansion.62bbe05c"), perks.Select(perk => new PresentationChoice {
                        Title = perk.displayName, Description = perk.description, Icon = perk.icon,
                        Selected = () => { runController.TryGrantPerk(perk); messages.Add(GameText.Get("ui.gameflowexpansion.ef9eaadd") + perk.displayName); next(); }
                    })); return;
                case EventEffectKind.SpellChoice:
                    var spells = content.spells.Where(spell => run.MagicIntroduced && spell != null && run.RuntimeFor(spell) == null).OrderBy(spell => StableHash(spell.id, run.Seed + run.SeenEvents.Count)).Take(3).ToArray();
                    if (spells.Length == 0) { run.GainGold(20); messages.Add(GameText.Get("ui.gameflowexpansion.7b666ce8")); break; }
                    expansionView.ShowModal(GameText.Get("ui.gameflowexpansion.805b2755"), GameText.Get("ui.gameflowexpansion.7c97cdb8"), spells.Select(spell => new PresentationChoice {
                        Title = MonkFormData.DirectionLabel(spell.defaultSlot) + " " + spell.displayName, Description = RunExpansionView.SpellDetails(spell, run.Player), Icon = spell.icon,
                        Selected = () => expansionView.OfferSpell(spell, accepted => { messages.Add(accepted ? GameText.Get("ui.gameflowexpansion.d76dc5b8") + spell.displayName : GameText.Get("ui.gameflowexpansion.ef14f1f9")); next(); })
                    })); return;
                case EventEffectKind.Fight:
                    if (effect.encounter == null) { messages.Add(GameText.Get("ui.gameflowexpansion.ad4ca858")); break; }
                    eventCombatCompleted = next;
                    combatController.PrepareFixedEncounter(run, effect.encounter, dungeonController.Floor);
                    State = GameFlowState.Combat;
                    expansionView.ShowMessage(effect.encounter.displayName, effect.encounter.hint, () => { combatController.BeginPreparedCombat(); }); return;
                case EventEffectKind.SacrificeRelic:
                    var owned = run.EquippedItems.Where(item => item.scope == RelicScope.Shared && item.grantedSpell == null).ToArray();
                    expansionView.ShowModal(GameText.Get("ui.gameflowexpansion.bd5f93d"), GameText.Get("ui.gameflowexpansion.26125a92"), owned.Select(item => new PresentationChoice {
                        Title = item.displayName, Description = item.description, Icon = item.icon,
                        Selected = () => { runController.TryRemoveEquipment(item); messages.Add(GameText.Get("ui.gameflowexpansion.274cc0ec") + item.displayName); next(); }
                    })); return;
                case EventEffectKind.RemovePerk:
                case EventEffectKind.UpgradePerk:
                    var candidates = run.ActivePerks.Where(perk => effect.kind == EventEffectKind.RemovePerk || perk.CanStack).ToArray();
                    expansionView.ShowModal(effect.kind == EventEffectKind.RemovePerk ? GameText.Get("ui.gameflowexpansion.32ce9f51") : GameText.Get("ui.gameflowexpansion.a76f5823"), GameText.Get("ui.gameflowexpansion.c4b91a69"), candidates.Select(perk => new PresentationChoice {
                        Title = perk.Data.displayName, Description = perk.Data.description, Icon = perk.Data.icon,
                        Selected = () => { if (effect.kind == EventEffectKind.RemovePerk) runController.TryRemovePerk(perk.Data); else runController.TryGrantPerk(perk.Data); messages.Add((effect.kind == EventEffectKind.RemovePerk ? GameText.Get("ui.gameflowexpansion.61725663") : GameText.Get("ui.gameflowexpansion.62b8ab8c")) + perk.Data.displayName); next(); }
                    })); return;
            }
            next();
        }
        private bool HandleEventCombat(CombatResult result)
        {
            var run = runController.State;
            run.CompleteEventConsequences();
            run.EmitEvent(TriggerType.OnCombatWon, default);
            if (eventCombatCompleted == null) return false;
            var completed = eventCombatCompleted; eventCombatCompleted = null;
            GrantAndShowReward(runController.GetCombatReward(result.Monsters, false), completed); return true;
        }
        private static int StableHash(string text, int seed)
        { unchecked { var hash = seed; foreach (var c in text ?? "") hash = hash * 31 + c; return hash & int.MaxValue; } }
        private static string StatName(ModifierKind kind) => kind switch {
            ModifierKind.MaxHp => GameText.Get("ui.gameflowexpansion.dc3590e0"), ModifierKind.NormalDamage => GameText.Get("ui.gameflowexpansion.e1fc90f9"), ModifierKind.ChargeDamage => GameText.Get("ui.gameflowexpansion.9baea365"),
            ModifierKind.AttackCooldownMultiplier => GameText.Get("ui.gameflowexpansion.d7e402c1"), ModifierKind.ChargeDurationMultiplier => GameText.Get("ui.gameflowexpansion.5570f829"), ModifierKind.DamageReduction => GameText.Get("ui.gameflowexpansion.546de53b"), ModifierKind.ShieldMax => GameText.Get("ui.stat.shieldmax"), _ => kind.ToString()
        };
        internal static string BurdenLabel(string kind) => kind switch { "Fatigue" => GameText.Get("ui.gameflowexpansion.1a4697c5"), "CrackedGuard" => GameText.Get("ui.gameflowexpansion.3851cc8c"), "BluntEdge" => GameText.Get("ui.gameflowexpansion.5ee9b14f"), _ => kind };
    }
}
