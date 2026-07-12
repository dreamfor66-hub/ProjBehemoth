using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash { [CreateAssetMenu(menuName = "Dungeon Slash/Charge Pattern Data")] public sealed class ChargePatternData : ScriptableObject { public string displayName = "Exposed Core"; [Min(.1f)] public float timeLimit = 2.5f; public List<WeakPointDefinition> weakPoints = new(); } }
