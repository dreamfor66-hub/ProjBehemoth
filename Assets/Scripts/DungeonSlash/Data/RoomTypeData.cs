using UnityEngine;
namespace DungeonSlash { [CreateAssetMenu(menuName = "Dungeon Slash/Room Type Data")] public sealed class RoomTypeData : ScriptableObject { public RoomEncounterType encounterType; public string displayName; public Color mapColor = Color.white; public bool revealAtStart; } }
