using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    public enum EventDiscovery { Automatic, Visible, Concealed }

    [Serializable]
    public sealed class RoomObjectBinding
    {
        public RoomEncounterType encounter;
        public RoomObjectData artwork;
    }

    [CreateAssetMenu(menuName = "Dungeon Slash/Exploration Settings")]
    public sealed class ExplorationSettings : ScriptableObject
    {
        [Header("Optional annexes; never replace the base floor's encounters")]
        [Range(0, 1)] public float annexChance = .78f;
        [Range(0, 1)] public float secondAnnexChance = .23f;
        [Range(0, 1)] public float concealedEntranceChance = .62f;
        [Range(0, 1)] public float lockedEntranceChance = .48f;
        [Range(0, 1)] public float annexEventChance = .46f;
        [Range(0, 1)] public float annexCombatChance = .025f;
        [Min(1)] public int maximumKeyDistance = 4;
        [Min(1), Tooltip("Repeated investigation of a real wall before its secret entrance opens.")]
        public int secretWallSearches = 5;
        [Header("World artwork")]
        public List<RoomObjectBinding> roomObjects = new();
        public RoomObjectData key, mechanism, lockedDoor, fallbackStory;
        public RoomObjectData hiddenWall;
        public RoomObjectData eliteEntrance, bossEntrance;
        public List<RoomObjectData> concealedEntranceClues = new();
        public RoomObjectData For(RoomEncounterType type) => roomObjects.Find(b => b.encounter == type)?.artwork;
    }
}
