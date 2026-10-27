using UnityEngine;
using UnityEngine.Serialization;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Dungeon Generation Settings")]
    public sealed class DungeonGenerationSettings : ScriptableObject
    {
        [Min(5)] public int minRooms = 20;
        [Min(5)] public int maxRooms = 26;
        [Min(1)] public int minimumBossDistance = 7;
        [FormerlySerializedAs("minimumMajorRewardDistance")] [Min(1)] public int minimumChestDistance = 4;
        [Min(1)] public int minimumRewardRooms = 2;
        [Min(1)] public int minimumFountainRooms = 1;
        [Min(1)] public int minimumShopRooms = 1;
        [Min(1)] public int minimumGoddessRooms = 1;
        [Min(0)] public int eliteRoomCount = 1;
        [FormerlySerializedAs("majorRewardRoomCount")] [Min(1)] public int chestRoomCount = 1;
        [Range(0f, 1f)] public float loopChance = .18f;
        [Range(0f, 1f)] public float combatRoomRatioMin = .35f;
        [Range(0f, 1f)] public float combatRoomRatioMax = .55f;
        [Min(1)] public int maxGenerationAttempts = 60;
        [Range(0, 8)] public int eventRoomCount = 3;
        [Header("Act one pacing")]
        [Min(0), Tooltip("Opening corridor of guaranteed normal combats, before the first branching decision.")]
        public int firstActOpeningCombats = 3;
        [Min(1)] public int firstActServiceDistance = 4;
        [Min(1)] public int firstActCampDistance = 7;
        [Range(2, 4)] public int firstActMaximumExits = 3;
        [Min(0)] public int firstActMaximumJunctions = 3;
        [Range(0, 1)] public float firstActLoopChance = .06f;
    }
}
