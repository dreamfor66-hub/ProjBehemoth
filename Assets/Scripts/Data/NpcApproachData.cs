using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/NPC Rear Approach")]
    public sealed class NpcApproachData : ScriptableObject
    {
        public string titleKey;
        public string narrativeKey;
        [Serializable] public sealed class EventResponse
        {
            public RandomEventData story;
            public string narrativeKey;
            [Tooltip("Page -1 selects the event's root options.")] public int page = -1;
            [Min(0)] public int option;
        }
        [Tooltip("Reference the actual terminal hostile choice. Different encounters with the same person may have different responses.")]
        public List<EventResponse> eventResponses = new();
        [Tooltip("A non-random, authored response event for a service NPC. Its terminal option uses the same cost/effect/aftermath system as every other encounter.")]
        public RandomEventData standaloneResponse;
        [Tooltip("For a subject with a real authored combat form rather than an event choice.")]
        public EncounterData encounter;
        [Header("Concealed identity (optional)")]
        public EncounterData concealedEncounter;
        [Range(0, 1)] public float concealedChance;
        [Range(1, 3)] public int concealedMinAct = 2;
        public string combatAnnouncementKey;
        public string victoryTextKey;
        public RoomObjectData aftermath;
        public bool removeAfterCombat = true;
        public EncounterData CombatIdentity(int seed, int roomId, int act)
        {
            if (concealedEncounter != null && act >= concealedMinAct && new System.Random(unchecked(seed + roomId * 1619 + act * 7919)).NextDouble() < concealedChance)
                return concealedEncounter;
            return encounter;
        }
    }
}
