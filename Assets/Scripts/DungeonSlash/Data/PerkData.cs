using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash { [CreateAssetMenu(menuName = "Dungeon Slash/Perk Data")] public sealed class PerkData : ScriptableObject { public string id; public string displayName; [TextArea] public string description; [Min(1)] public int maxStacks = 1; public List<StatModifier> modifiers = new(); } }
