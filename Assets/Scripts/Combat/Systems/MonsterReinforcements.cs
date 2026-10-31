using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class CombatController
    {
        readonly Dictionary<MonsterRuntime, float> reinforcementTimers = new();
        void TickReinforcements(float deltaTime)
        {
            foreach (var leader in activeMonsters.ToArray())
            {
                var runtime = leader.Runtime; var data = runtime.Data;
                if (!runtime.IsAlive || data.reinforcementInterval <= 0 || data.supportRoster == null || runtime.IsUnaware) continue;
                var missing = data.supportRoster.Where(s => s != null).GroupBy(s => s)
                    .FirstOrDefault(slots => activeMonsters.Count(m => m.Runtime.IsAlive && m.Runtime.Data == slots.Key) < slots.Count());
                if (missing == null) { reinforcementTimers[runtime] = data.reinforcementInterval; continue; }
                if (runtime.IsStunned) continue;
                var remaining = reinforcementTimers.TryGetValue(runtime, out var timer) ? timer : data.reinforcementInterval;
                remaining -= deltaTime; reinforcementTimers[runtime] = remaining;
                if (remaining > 0) continue;
                reinforcementTimers[runtime] = data.reinforcementInterval;
                var corpse = activeMonsters.FirstOrDefault(m => !m.Runtime.IsAlive && m.Runtime.Data == missing.Key);
                Vector2 placement;
                if (corpse != null)
                {
                    placement = corpse.BasePosition + Vector2.up * corpse.Runtime.Data.FootprintY * corpse.VisualScale;
                    ClearWeakPointViews(corpse, true);
                    if (corpse.View != null) Destroy(corpse.View.gameObject);
                    activeMonsters.Remove(corpse);
                }
                else
                {
                    var slot = data.supportRoster.IndexOf(missing.Key) + 1;
                    placement = sceneView.GetMonsterFormationPosition(slot, data.supportRoster.Count + 1)
                        + Vector2.up * missing.Key.FormationFootprintOffset;
                }
                SpawnMonster(missing.Key, placement, .45f);
                // No catch-up loop: even a long frame restores at most one actor.
                return;
            }
        }
    }
}
