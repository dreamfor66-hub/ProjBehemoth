using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class RunHudView : MonoBehaviour
    {
        [SerializeField] private Text roomInfo;
        [SerializeField] private Text runInfo;
        // Retained solely for compatibility with the minimal HUD test configuration.
        [SerializeField] private Text perkInfo;
        [SerializeField] private RectTransform perkGrid;
        [SerializeField] private RectTransform relicGrid;
        [SerializeField] private RectTransform potionGrid;
        [SerializeField] private RunHudIconView iconPrefab;
        [SerializeField] private ItemTooltipView tooltip;

        private readonly List<RunHudIconView> perkIcons = new();
        private readonly List<RunHudIconView> relicIcons = new();
        private readonly List<RunHudIconView> potionIcons = new();

        private void Awake()
        {
            CacheIcons(perkIcons, perkGrid);
            CacheIcons(relicIcons, relicGrid);
            CacheIcons(potionIcons, potionGrid);
        }

        public void Configure(Text newRoomInfo, Text newRunInfo, Text newPerkInfo)
        {
            roomInfo = newRoomInfo;
            runInfo = newRunInfo;
            perkInfo = newPerkInfo;
        }

        public void Configure(Text newRoomInfo, Text newRunInfo, RectTransform newPerkGrid, RectTransform newRelicGrid, RectTransform newPotionGrid, RunHudIconView newIconPrefab, ItemTooltipView newTooltip)
        {
            roomInfo = newRoomInfo;
            runInfo = newRunInfo;
            perkGrid = newPerkGrid;
            relicGrid = newRelicGrid;
            potionGrid = newPotionGrid;
            iconPrefab = newIconPrefab;
            tooltip = newTooltip;
        }

        public void Bind(DungeonRunState dungeon, RunState run, Action<EquipmentData> usePotion = null)
        {
            if (roomInfo != null)
            {
                roomInfo.text = string.Empty;
                roomInfo.enabled = false;
            }
            if (runInfo != null)
                runInfo.text = $"{dungeon.Floor}\uCE35  LV {run.Level}  XP {run.Experience}/{run.RequiredExperience}\nGold {run.Gold}";
            if (perkInfo != null)
            {
                perkInfo.text = string.Empty;
                perkInfo.enabled = false;
            }
            if (run == null) return;

            BindPerks(run);
            BindRelics(run);
            BindPotions(run, usePotion);
        }

        private void BindPerks(RunState run)
        {
            EnsureIconCount(perkIcons, perkGrid, run.ActivePerks.Count);
            for (var index = 0; index < perkIcons.Count; index++)
            {
                var visible = index < run.ActivePerks.Count;
                perkIcons[index].gameObject.SetActive(visible);
                if (visible) perkIcons[index].Bind(run.ActivePerks[index].Data, run.ActivePerks[index].StackCount, tooltip);
            }
        }

        private void BindRelics(RunState run)
        {
            EnsureIconCount(relicIcons, relicGrid, run.EquippedItems.Count);
            for (var index = 0; index < relicIcons.Count; index++)
            {
                var visible = index < run.EquippedItems.Count;
                relicIcons[index].gameObject.SetActive(visible);
                if (visible) relicIcons[index].Bind(run.EquippedItems[index], tooltip);
            }
        }

        private void BindPotions(RunState run, Action<EquipmentData> usePotion)
        {
            EnsureIconCount(potionIcons, potionGrid, run.PotionSlotCapacity);
            for (var index = 0; index < potionIcons.Count; index++)
            {
                potionIcons[index].gameObject.SetActive(true);
                if (index >= run.Potions.Count)
                {
                    potionIcons[index].BindEmpty(tooltip);
                    continue;
                }
                var potion = run.Potions[index];
                potionIcons[index].Bind(potion, tooltip, usePotion == null ? null : () => usePotion(potion));
            }
        }

        private void EnsureIconCount(List<RunHudIconView> icons, RectTransform parent, int requiredCount)
        {
            if (parent == null || iconPrefab == null) return;
            CacheIcons(icons, parent);
            while (icons.Count < requiredCount)
            {
                var icon = Instantiate(iconPrefab, parent);
                icon.gameObject.name = "RunHudIcon";
                icons.Add(icon);
            }
        }

        private static void CacheIcons(List<RunHudIconView> icons, RectTransform parent)
        {
            if (parent == null || icons.Count > 0) return;
            icons.AddRange(parent.GetComponentsInChildren<RunHudIconView>(true));
        }
    }
}
