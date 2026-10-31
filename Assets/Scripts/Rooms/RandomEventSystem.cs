using System;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public static class RandomEventSystem
    {
        private const string HistoryKey = "Behemoth.EventHistory.v1";
        public static RandomEventData Draw(GameContent content, RunState run, int act, int room, int exits = -1)
        {
            var history = PlayerPrefs.GetString(HistoryKey, "").Split('|');
            var random = new System.Random(unchecked(run.Seed + act * 3571 + room * 1013));
            var candidates = content.events.Where(item => item != null && !item.excludedFromRandomPool && act >= item.minAct && act <= item.maxAct && !run.SeenEvents.Contains(item.id)
                && (exits < 0 || item.roomLayout != EventRoomLayout.Alcove || exits == 1)
                && (string.IsNullOrEmpty(item.requiredFlag) || run.EventFlags.Contains(item.requiredFlag))
                && (act > 1 || run.NormalCombatsInAct >= item.minNormalCombatsInAct)
                && item.options.Any(option => UnavailableReason(option, run) == null)
                && (string.IsNullOrEmpty(item.forbiddenFlag) || !run.EventFlags.Contains(item.forbiddenFlag))).ToArray();
            if (candidates.Length == 0) return null;
            var scored = candidates.Select(item => new { item, score = -Math.Log(Math.Max(.00001, random.NextDouble())) / (item.weight * (history.Contains(item.id) ? .2 : 1)) }).OrderBy(pair => pair.score).First().item;
            run.SeenEvents.Add(scored.id);
            PlayerPrefs.SetString(HistoryKey, string.Join("|", history.Where(id => !string.IsNullOrEmpty(id) && id != scored.id).Append(scored.id).TakeLast(24)));
            return scored;
        }
        public static string UnavailableReason(EventOption option, RunState run)
        {
            if (option.supplyCost > run.SupplyCount(option.supply)) return GameText.Get("ui.event.supplymissing");
            if (option.requirements.Any(r => !r.Matches(run))) return GameText.Get("ui.event.notready");
            if (run.Gold < option.goldCost) return GameText.Get("ui.randomeventsystem.b7b7a578");
            if (run.Player.CurrentHp <= option.hpCost) return GameText.Get("ui.randomeventsystem.d3c41de9");
            if (run.Player.MaxHp - option.maxHpCost < 20f) return GameText.Get("ui.randomeventsystem.470f2a23");
            if (option.consumeHealingPotion && !run.Potions.Any(RunState.IsHealingPotion)) return GameText.Get("ui.randomeventsystem.5844d85e");
            if (option.requiredRelic != null && !run.EquippedItems.Contains(option.requiredRelic)) return GameText.Get("ui.randomeventsystem.94f41882");
            if (option.effects.Any(effect => effect.kind == EventEffectKind.SpellChoice || effect.kind == EventEffectKind.Relic && effect.relic?.grantedSpell != null) && !run.MagicIntroduced) return GameText.Get("ui.randomeventsystem.94f41882");
            if (!string.IsNullOrEmpty(option.requiredPerk) && !run.ActivePerks.Any(perk => perk.Data.id == option.requiredPerk)) return GameText.Get("ui.randomeventsystem.ad3c2ff");
            if (!string.IsNullOrEmpty(option.requiredFlag) && !run.EventFlags.Contains(option.requiredFlag)) return GameText.Get("ui.randomeventsystem.f87e5029");
            if (option.effects.Any(effect => effect.kind == EventEffectKind.ExclusiveChoice) && run.ExclusiveCount >= RunState.ExclusiveLimit) return GameText.Get("ui.randomeventsystem.209c6ec6");
            if (option.effects.Any(effect => effect.kind == EventEffectKind.SacrificeRelic) && !run.EquippedItems.Any(item => item.scope == RelicScope.Shared && item.grantedSpell == null)) return GameText.Get("ui.randomeventsystem.2273fe56");
            if (option.effects.Any(effect => effect.kind == EventEffectKind.RemovePerk) && run.ActivePerks.Count == 0) return GameText.Get("ui.randomeventsystem.77f5a29c");
            if (option.effects.Any(effect => effect.kind == EventEffectKind.UpgradePerk) && !run.ActivePerks.Any(perk => perk.CanStack)) return GameText.Get("ui.randomeventsystem.11d7e512");
            if (option.effects.Any(effect => effect.kind == EventEffectKind.Relic && effect.relic != null && run.EquippedItems.Contains(effect.relic))) return GameText.Get("ui.randomeventsystem.7baef27d");
            return null;
        }
        public static bool Pay(EventOption option, RunState run)
        {
            if (UnavailableReason(option, run) != null) return false;
            if (!run.Pay(option.goldCost, option.hpCost, option.maxHpCost)) return false;
            if (!run.SpendSupply(option.supply, option.supplyCost)) return false;
            if (option.consumeHealingPotion)
            {
                var potion = run.Potions.First(RunState.IsHealingPotion);
                run.TryRemoveEquipment(potion);
            }
            return true;
        }
    }
}
