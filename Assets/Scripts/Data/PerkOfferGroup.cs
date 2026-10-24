using UnityEngine;

namespace DungeonSlash
{
    [CreateAssetMenu(menuName = "Dungeon Slash/Perk Offer Group")]
    public sealed class PerkOfferGroup : ScriptableObject
    {
        [Min(0), Tooltip("Combined offer weight for all eligible members. An ungrouped perk has weight 1. At most one member appears in an offer.")]
        public float weight = 1;
    }
}
