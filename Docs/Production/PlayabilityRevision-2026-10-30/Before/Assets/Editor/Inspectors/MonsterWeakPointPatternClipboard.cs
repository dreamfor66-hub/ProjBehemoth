using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DungeonSlash.EditorTools
{
    internal static class MonsterWeakPointPatternClipboard
    {
        [Serializable] sealed class Pattern
        {
            public string format = "behemoth.weak-point-pattern";
            public int version = 1;
            public List<WeakPointDefinition> points = new();
        }
        internal static WeakPointDefinition Clone(WeakPointDefinition point) => new()
        { id = point.id, localPosition = point.localPosition, hitRadius = point.hitRadius, requiredChargeHits = point.requiredChargeHits, optional = point.optional };
        internal static void Copy(MonsterAttackData attack) =>
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(new Pattern { points = attack.chargeWeakPoints.Select(Clone).ToList() });
        internal static bool TryRead(out List<WeakPointDefinition> points)
        {
            points = null;
            try
            {
                var text = EditorGUIUtility.systemCopyBuffer;
                if (string.IsNullOrEmpty(text) || !text.Contains("behemoth.weak-point-pattern")) return false;
                var value = JsonUtility.FromJson<Pattern>(text);
                if (value == null || value.format != "behemoth.weak-point-pattern" || value.version != 1 || value.points == null
                    || value.points.Any(p => p == null || !Finite(p.localPosition.x) || !Finite(p.localPosition.y)
                        || !Finite(p.hitRadius) || p.hitRadius < 1 || p.requiredChargeHits < 1)) return false;
                points = value.points.Select(Clone).ToList(); return true;
            }
            catch (ArgumentException) { return false; }
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Paste(MonsterAttackData attack)
        {
            if (attack == null || !TryRead(out var points)) return false;
            Undo.RegisterCompleteObjectUndo(attack, "Paste weak-point pattern");
            attack.chargeWeakPoints = points;
            EditorUtility.SetDirty(attack); if (EditorUtility.IsPersistent(attack)) AssetDatabase.SaveAssetIfDirty(attack); return true;
        }
    }
}
