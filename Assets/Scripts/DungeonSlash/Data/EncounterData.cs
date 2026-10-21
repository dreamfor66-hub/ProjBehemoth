using System;
using System.Collections.Generic;
using UnityEngine;
namespace DungeonSlash
{
    [Serializable]
    public sealed class EncounterMember
    {
        public MonsterData monster;
        public Vector2 offset;
        [Range(.25f, 1f)] public float scale = .75f;
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
