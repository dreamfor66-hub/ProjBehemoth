#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    // Called by the single, scoped world review in the user's existing Editor.
    // Does not change the live run or start another suite/Editor instance.
    public static class MapFatigueLiveChecks
    {
        public static void CheckCurrentFloor(RunState run, DungeonGraph graph, Action<bool, string> check)
        {
            var live = run.Fatigue;
            var settings = live.Settings;
            var expected = FatigueFloorBudget.For(graph, settings.costPerStep);
            check(live.Capacity == expected.Capacity && live.Capacity >= 100 && live.Capacity % 10 == 0,
                "actual floor receives map-derived fatigue capacity " + live.Capacity);
            check(expected.CoverageRoute.Distinct().Count() == graph.Rooms.Count,
                "capacity route includes every actual room, including optional branches");
            check(live.Value <= live.Capacity && live.Value > 0, "live floor has a valid remaining fatigue amount");
            var fatigue = new FatigueRuntime();
            fatigue.BeginFloor(settings, graph);
            for (var i = 0; i < expected.CoverageSteps; i++) fatigue.Step();
            check(!fatigue.IsDead && fatigue.Value >= settings.costPerStep,
                "one complete finite exploration retains at least one movement cost without camp/potion");
            check(Mathf.Approximately(fatigue.AttackSpeedMultiplier, Mathf.Clamp(settings.attackSpeed.Evaluate(fatigue.Percent), .1f, 1))
                && Mathf.Approximately(fatigue.FinalDamageMultiplier, Mathf.Clamp01(settings.finalDamage.Evaluate(fatigue.Percent))),
                "actual Unity fatigue curves use remaining percentage on this floor");
            check(fatigue.Camp() && fatigue.Value == fatigue.Capacity && !fatigue.Camp(),
                "one full camp restores the actual maximum and cannot repeat");
            while (fatigue.Percent > settings.shortRestCeiling * .5f) fatigue.Step();
            check(fatigue.ShortRest() && Mathf.Approximately(fatigue.Percent, settings.shortRestCeiling),
                "brief rest restores the authored percentage rather than a fixed60");
            fatigue.Recover(100000);
            check(fatigue.Value == fatigue.Capacity, "recovery clamps at the actual floor maximum");
            fatigue.Step();
            check(fatigue.Value == fatigue.Capacity - settings.costPerStep && fatigue.GraceRemaining == 0,
                "first movement after recovery immediately spends the authored cost");
            while (!fatigue.IsDead) fatigue.Step();
            check(fatigue.Value == 0 && fatigue.IsDead, "exhaustion still reaches exactly0 and ends survival");
            var text = GameText.Format("ui.fatigue.detail", Mathf.CeilToInt(live.Value), "여유", "0", "0", live.Capacity);
            check(text.Contains("/" + live.Capacity) && !text.Contains("{4}"), "localized fatigue tooltip shows the actual denominator");
        }
    }
}
#endif
