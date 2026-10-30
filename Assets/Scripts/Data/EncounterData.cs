using System;
using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    [Serializable]
    public sealed class EncounterMember
    {
        public MonsterData monster;
        [Tooltip("Placement of the body hitbox bottom relative to MonsterSpawnRoot. Raise Y for flying creatures; scale is applied to hitbox height automatically.")]
        public Vector2 offset;
        [Min(0)] public float actionDelay;
    }
    [CreateAssetMenu(menuName = "Dungeon Slash/Encounter")]
    public sealed class EncounterData : LocalizedData
    {
        public string id;
        public string displayName;
        [TextArea] public string hint;
        public RoomEncounterType category;
        public List<EncounterMember> members = new();
    }
}
