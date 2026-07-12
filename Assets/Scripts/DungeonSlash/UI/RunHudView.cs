using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class RunHudView : MonoBehaviour
    {
        [SerializeField] private Text roomInfo;
        [SerializeField] private Text runInfo;
        [SerializeField] private Text perkInfo;

        public void Configure(Text newRoomInfo, Text newRunInfo, Text newPerkInfo)
        {
            roomInfo = newRoomInfo;
            runInfo = newRunInfo;
            perkInfo = newPerkInfo;
        }

        public void Bind(DungeonRunState dungeon, RunState run)
        {
            if (roomInfo != null)
            {
                roomInfo.text = string.Empty;
                roomInfo.enabled = false;
            }
            if (runInfo != null)
                runInfo.text = $"LV {run.Level}  XP {run.Experience}/{run.RequiredExperience}\nGold {run.Gold}";
            if (perkInfo != null)
                perkInfo.text = run.ActivePerks.Count == 0 ? "Perks: none" : "Perks: " + string.Join(", ", run.ActivePerks.Select(perk => $"{perk.Data.displayName} {perk.StackCount}"));
        }
    }
}
