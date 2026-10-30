using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class RunHudView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI roomInfo;
        [SerializeField] private TextMeshProUGUI runInfo;
        [SerializeField] private Image characterPortrait;
        [SerializeField] private TextMeshProUGUI characterName;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image healthFill;
        [SerializeField] private TextMeshProUGUI experienceText;
        [SerializeField] private Image experienceFill;
        [SerializeField] private TextMeshProUGUI goldText;
        // Retained solely for compatibility with the minimal HUD test configuration.
        [SerializeField] private TextMeshProUGUI perkInfo;
        [SerializeField] private RectTransform perkGrid;
        [SerializeField] private RectTransform relicGrid;
        [SerializeField] private RectTransform potionGrid;
        [SerializeField] private RunHudIconView iconPrefab;
        [SerializeField] private ItemTooltipView tooltip;

        private readonly List<RunHudIconView> perkIcons = new();
        private readonly List<RunHudIconView> relicIcons = new();
        private readonly List<RunHudIconView> potionIcons = new();
        private RunState boundRun;

        // Damage, healing and maximum HP can change between navigation HUD refreshes.
        private EquipmentData[] shownPotions = Array.Empty<EquipmentData>();
        private void LateUpdate()
        {
            RefreshCharacterSummary();
            if (boundRun != null && potionGrid != null && !shownPotions.SequenceEqual(boundRun.Potions)) BindPotions(boundRun, null, null);
        }

        public void ConfigureCharacterSummary(Image portrait, TextMeshProUGUI nameLabel, TextMeshProUGUI hpLabel,
            Image hpFill, TextMeshProUGUI xpLabel, Image xpFill, TextMeshProUGUI goldLabel)
        {
            characterPortrait = portrait; characterName = nameLabel; healthText = hpLabel;
            healthFill = hpFill; experienceText = xpLabel; experienceFill = xpFill; goldText = goldLabel;
        }

        private void Awake()
        {
            CacheIcons(perkIcons, perkGrid);
            CacheIcons(relicIcons, relicGrid);
            CacheIcons(potionIcons, potionGrid);
        }

        public void Configure(TextMeshProUGUI newRoomInfo, TextMeshProUGUI newRunInfo, TextMeshProUGUI newPerkInfo)
        {
            roomInfo = newRoomInfo;
            runInfo = newRunInfo;
            perkInfo = newPerkInfo;
        }

        public void Configure(TextMeshProUGUI newRoomInfo, TextMeshProUGUI newRunInfo, RectTransform newPerkGrid, RectTransform newRelicGrid, RectTransform newPotionGrid, RunHudIconView newIconPrefab, ItemTooltipView newTooltip)
        {
            roomInfo = newRoomInfo;
            runInfo = newRunInfo;
            perkGrid = newPerkGrid;
            relicGrid = newRelicGrid;
            potionGrid = newPotionGrid;
            iconPrefab = newIconPrefab;
            tooltip = newTooltip;
        }

        public void Bind(DungeonRunState dungeon, RunState run, Action<EquipmentData> usePotion = null, Action<PerkData> removePerk = null, Action<EquipmentData> removeEquipment = null)
        {
            if (roomInfo != null)
            {
                roomInfo.text = string.Empty;
                roomInfo.enabled = false;
            }
            boundRun = run;
            if (runInfo != null)
            {
                runInfo.text = string.Empty;
                runInfo.enabled = false;
            }
            if (perkInfo != null)
            {
                perkInfo.text = string.Empty;
                perkInfo.enabled = false;
            }
            if (run == null) return;

            RefreshCharacterSummary();

            BindPerks(run, removePerk);
            BindRelics(run, removeEquipment);
            BindPotions(run, usePotion, removeEquipment);
        }

        private void RefreshCharacterSummary()
        {
            if (boundRun?.Player == null) return;
            var player = boundRun.Player;
            if (characterPortrait != null)
            {
                characterPortrait.sprite = player.Data.portrait;
                characterPortrait.color = Color.white;
                characterPortrait.enabled = player.Data.portrait != null;
                characterPortrait.preserveAspect = true;
            }
            if (characterName != null) { characterName.text = player.Data.displayName; characterName.color = player.Data.characterColor; }
            if (healthText != null) healthText.text = GameText.Format("ui.combathudview.dfe9f229", player.CurrentHp, player.MaxHp);
            SetSummaryGauge(healthFill, player.MaxHp <= 0 ? 0 : player.CurrentHp / player.MaxHp);
            if (experienceText != null) experienceText.text = GameText.Format("ui.hud.experience", boundRun.Level, boundRun.Experience, boundRun.RequiredExperience);
            SetSummaryGauge(experienceFill, boundRun.RequiredExperience <= 0 ? 0 : (float)boundRun.Experience / boundRun.RequiredExperience);
            if (goldText != null) goldText.text = GameText.Format("ui.hud.gold", boundRun.Gold);
        }

        private static void SetSummaryGauge(Image fill, float ratio)
        {
            if (fill == null) return;
            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            fill.type = Image.Type.Simple;
        }

        private void BindPerks(RunState run, Action<PerkData> removePerk)
        {
            EnsureIconCount(perkIcons, perkGrid, run.ActivePerks.Count);
            for (var index = 0; index < perkIcons.Count; index++)
            {
                var visible = index < run.ActivePerks.Count;
                perkIcons[index].gameObject.SetActive(visible);
                if (visible)
                {
                    var perk = run.ActivePerks[index].Data;
                    perkIcons[index].Bind(perk, run.ActivePerks[index].StackCount, tooltip, removePerk == null ? null : () => removePerk(perk));
                }
            }
        }

        private void BindRelics(RunState run, Action<EquipmentData> removeEquipment)
        {
            EnsureIconCount(relicIcons, relicGrid, run.EquippedItems.Count);
            for (var index = 0; index < relicIcons.Count; index++)
            {
                var visible = index < run.EquippedItems.Count;
                relicIcons[index].gameObject.SetActive(visible);
                if (visible)
                {
                    var item = run.EquippedItems[index];
                    relicIcons[index].Bind(item, tooltip, null, removeEquipment == null ? null : () => removeEquipment(item));
                }
            }
        }

        private void BindPotions(RunState run, Action<EquipmentData> usePotion, Action<EquipmentData> removeEquipment)
        {
            shownPotions = run.Potions.ToArray();
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
                var anchor = (RectTransform)potionIcons[index].transform;
                var actions = GetComponentInParent<Canvas>()?.GetComponentInChildren<PotionActionView>(true);
                potionIcons[index].Bind(potion, tooltip, actions == null ? null : () => actions.Open(potion, anchor));
            }
        }

        private void EnsureIconCount(List<RunHudIconView> icons, RectTransform parent, int requiredCount)
        {
            if (parent == null || iconPrefab == null) return;
            var pool=parent.GetComponent<RunHudIconPool>();
            if(pool==null)
            {
                pool=parent.gameObject.AddComponent<RunHudIconPool>();pool.content=parent;pool.prefab=iconPrefab;
            }
            pool.SetCount(requiredCount);icons.Clear();icons.AddRange(pool.Active);
        }

        private static void CacheIcons(List<RunHudIconView> icons, RectTransform parent)
        {
            if (parent == null || icons.Count > 0) return;
            icons.AddRange(parent.GetComponentsInChildren<RunHudIconView>(true));
        }
    }
}
