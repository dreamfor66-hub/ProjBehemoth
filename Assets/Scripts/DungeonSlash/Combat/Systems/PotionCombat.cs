using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class CombatController
    {
        public bool CanThrowPotion => IsActive && !isFinishing && Player != null && Player.IsAlive && activeMonsters.Any(m => m.Runtime.IsAlive);

        public bool PotionTargetAt(Vector2 screen, out MonsterRuntime target, out Vector2 position)
        {
            target = null; position = default;
            if (!CanThrowPotion) return false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(sceneView.LogicalArea, screen, sceneView.UiCamera, out var local);
            var monster = activeMonsters.Where(m => m.Runtime.IsAlive && Vector2.Distance(local, m.Position) <= m.Runtime.Data.bodyHitRadius * m.VisualScale * (m.View.presentation != null && m.View.presentation.arcade ? m.View.arcadeArtScale : 1))
                .OrderBy(m => Vector2.Distance(local, m.Position)).FirstOrDefault();
            if (monster == null) return false;
            target = monster.Runtime;
            position = RectTransformUtility.WorldToScreenPoint(sceneView.UiCamera, sceneView.LogicalArea.TransformPoint(monster.Position));
            return true;
        }

        public bool ThrowPotion(EquipmentData potion, MonsterRuntime target)
        {
            if (!CanThrowPotion || !activeMonsters.Any(m => m.Runtime == target && m.Runtime.IsAlive) || !run.TryThrowPotion(potion, target)) return false;
            ResolveQueuedAbilities(0);
            foreach (var monster in activeMonsters) { monster.Runtime.Buffs.UpdateState(0); monster.View.Bind(monster.Runtime); }
            RefreshHud();
            if (activeMonsters.All(m => !m.Runtime.IsAlive)) Finish(true);
            return true;
        }
    }
}
