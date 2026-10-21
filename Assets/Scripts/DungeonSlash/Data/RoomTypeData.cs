using UnityEngine;
namespace DungeonSlash { [CreateAssetMenu(menuName = "Dungeon Slash/Room Type Data")] public sealed class RoomTypeData : LocalizedData { public RoomEncounterType encounterType; public string displayName; public Color mapColor = Color.white; public bool revealAtStart; } }
