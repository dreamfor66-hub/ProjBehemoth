using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Dungeon Map Style")]
    public sealed class DungeonMapStyle : ScriptableObject
    {
        [Tooltip("Encounter categories drawn as large rooms. Other encounters occupy small corridor cells; this never changes their rewards or movement cost.")]
        public List<RoomEncounterType> largeRoomTypes = new()
        {
            RoomEncounterType.Start, RoomEncounterType.Boss, RoomEncounterType.Elite,
            RoomEncounterType.Chest, RoomEncounterType.MajorReward, RoomEncounterType.Shop,
            RoomEncounterType.Camp, RoomEncounterType.Goddess, RoomEncounterType.Event
        };
        [Min(60)] public float cellSpacing = 84;
        [Min(32)] public float roomSize = 50;
        [Min(16)] public float corridorEventSize = 24;
        [Min(4)] public float corridorTileSize = 10;
        [Min(2)] public float corridorTileGap = 4;
        public bool IsLargeRoom(DungeonRoom room) => room != null && largeRoomTypes.Contains(room.Type);
        public float SizeFor(DungeonRoom room) => IsLargeRoom(room) ? roomSize : corridorEventSize;
    }
}
