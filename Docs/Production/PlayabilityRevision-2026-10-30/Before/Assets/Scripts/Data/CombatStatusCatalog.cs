using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Status Catalog")]
    public sealed class CombatStatusCatalog : ScriptableObject
    {
        public BuffData poison, burn, slow, lightningRod;
        private static CombatStatusCatalog cached;
        public static CombatStatusCatalog Instance => cached != null ? cached : cached = Resources.Load<CombatStatusCatalog>("CombatStatusCatalog");
        public BuffData Get(StatusKind kind) => kind switch { StatusKind.Poison => poison, StatusKind.Burn => burn, StatusKind.Slow => slow, _ => lightningRod };
    }
}
