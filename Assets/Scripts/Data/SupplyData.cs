using System;
using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Supply")]
    public sealed class SupplyData : LocalizedData
    {
        public string displayName;
        public Sprite icon;
    }
    [Serializable] public sealed class StartingSupply { public SupplyData supply; [Min(0)] public int count; }
}
