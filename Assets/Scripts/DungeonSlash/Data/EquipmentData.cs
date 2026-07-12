using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash { [CreateAssetMenu(menuName = "Dungeon Slash/Equipment Data")] public sealed class EquipmentData : ScriptableObject { public string id; public string displayName; [TextArea] public string description; [Min(0)] public int price = 20; [Min(1)] public int maxPurchaseCount = 1; public List<StatModifier> modifiers = new(); } }
