using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>F1-only prototype test panel. It intentionally remains outside normal run progression.</summary>
    public sealed partial class DebugCheatOverlay : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private List<Button> monsterButtons = new();
        [SerializeField] private List<Button> perkButtons = new();
        [SerializeField] private List<Button> equipmentButtons = new();
        [SerializeField] private Button startBattleButton;
        [SerializeField] private Button infiniteBattleButton;
        [SerializeField] private Button addPerkButton;
        [SerializeField] private Button addEquipmentButton;
        [SerializeField] private TextMeshProUGUI infiniteBattleLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [Header("Image catalog (saved editor layout)")]
        public ScrollRect catalogScroll;
        public DebugCatalogTile tilePrefab;
        public Button[] categoryButtons;
        public Button closeButton;
        public Button grantButton;
        public TextMeshProUGUI grantLabel, selectionLabel, catalogLabel;
        public ItemTooltipView catalogTooltip;
        public RectTransform tooltipAnchor;
        public List<MonsterData> monsterCatalog = new();
        private readonly List<DebugCatalogTile> tiles = new();
        private readonly List<UnityEngine.Object> visibleEntries = new();
        private int category;

        private readonly List<MonsterData> monsters = new();
        private readonly List<PerkData> perks = new();
        private readonly List<EquipmentData> equipment = new();
        private RunController run;
        private CombatController combat;
        private MonsterData selectedMonster;
        private PerkData selectedPerk;
        private EquipmentData selectedEquipment;
        private bool infiniteBattle;

        public bool IsOpen => panel != null && panel.activeSelf;
        public event Action<MonsterData> BattleRequested;
        public event Action InventoryChanged;
        public event Action VisibilityChanged;

        public void Configure(GameObject newPanel, IEnumerable<Button> newMonsterButtons, IEnumerable<Button> newPerkButtons, IEnumerable<Button> newEquipmentButtons, Button newStartBattleButton, Button newInfiniteBattleButton, TextMeshProUGUI newInfiniteBattleLabel, Button newAddPerkButton, Button newAddEquipmentButton, TextMeshProUGUI newStatusLabel)
        {
            panel = newPanel;
            monsterButtons = new List<Button>(newMonsterButtons ?? Array.Empty<Button>());
            perkButtons = new List<Button>(newPerkButtons ?? Array.Empty<Button>());
            equipmentButtons = new List<Button>(newEquipmentButtons ?? Array.Empty<Button>());
            startBattleButton = newStartBattleButton;
            infiniteBattleButton = newInfiniteBattleButton;
            infiniteBattleLabel = newInfiniteBattleLabel;
            addPerkButton = newAddPerkButton;
            addEquipmentButton = newAddEquipmentButton;
            statusLabel = newStatusLabel;

            WireButtons();
            panel?.SetActive(false);
            UpdateInfiniteBattleLabel();
        }

        private void Awake()
        {
            // Configure is called while the prototype scene is generated. Runtime
            // UnityEvent listeners added there are not serialized into the scene,
            // so wire the actual scene buttons again after deserialization.
            WireButtons();
            UpdateInfiniteBattleLabel();
        }

        private void WireButtons()
        {
            WireChapterButtons();
            WireButton(startBattleButton, RequestBattle);
            WireButton(infiniteBattleButton, ToggleInfiniteBattle);
            WireButton(addPerkButton, AddSelectedPerk);
            WireButton(addEquipmentButton, AddSelectedEquipment);
            WireButton(closeButton, () => SetOpen(false));
            WireButton(grantButton, () => { if (category == 1) AddSelectedPerk(); else AddSelectedEquipment(); });
            if (categoryButtons != null)
                for (var i = 0; i < categoryButtons.Length; i++)
                {
                    var tab = i;
                    categoryButtons[i].onClick.RemoveAllListeners();
                    categoryButtons[i].onClick.AddListener(() => SelectCategory(tab));
                }
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        public void Bind(RunController newRun, CombatController newCombat)
        {
            run = newRun;
            combat = newCombat;
            PopulateLists();
            combat?.SetInfiniteBattle(infiniteBattle);
        }

        private void Update()
        {
            // The project enables both input backends.  The legacy query alone
            // intermittently misses function keys in the Game view, so use the
            // active Input System device first and retain the legacy path as a
            // compatibility fallback.
            var pressedInInputSystem = Keyboard.current?.f1Key.wasPressedThisFrame == true;
            var pressedInLegacyInput = Input.GetKeyDown(KeyCode.F1);
            if (pressedInInputSystem || pressedInLegacyInput)
                TogglePanel();
        }

        /// <summary>Exposed for deterministic tests and optional debug UI hooks.</summary>
        public void TogglePanel()
        {
            chapterFlow?.EndPlayRecordInspection();
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (panel == null) return;
            catalogTooltip?.Hide();
            panel.SetActive(open);
            if (open)
            {
                // Modal panels can raise their owning layer. F1 must also raise its owning
                // branch so chapter travel remains reachable from tutorials/events/results.
                var branch = panel.transform;
                branch.SetAsLastSibling();
                while (branch.parent != null && branch.parent.GetComponent<Canvas>() == null)
                { branch = branch.parent; branch.SetAsLastSibling(); }
            }
            VisibilityChanged?.Invoke();
            if (open)
            {
                PopulateLists();
                SetStatus(GameText.Get(category == 4 ? "ui.debugchapter.hint" : catalogScroll != null ? "ui.debugcatalog.hint" : "ui.debugcheatoverlay.f94d1cf7"));
            }
        }

        private void PopulateLists()
        {
            monsters.Clear();
            if (monsterCatalog.Count > 0) monsters.AddRange(monsterCatalog.FindAll(m => m != null));
            else if (combat != null) monsters.AddRange(combat.GetDebugMonsterCatalog());
            perks.Clear();
            if (run?.Perks != null)
            {
                // PerkSystem owns the source list; the serialized run controller list is exposed through this helper.
                perks.AddRange(run.AvailablePerks);
            }
            equipment.Clear();
            if (run != null) equipment.AddRange(run.Equipment);

            if (!monsters.Contains(selectedMonster)) selectedMonster = monsters.Count > 0 ? monsters[0] : null;
            if (!perks.Contains(selectedPerk)) selectedPerk = perks.Count > 0 ? perks[0] : null;
            if (!equipment.Contains(selectedEquipment)) selectedEquipment = equipment.Count > 0 ? equipment[0] : null;
            if (!roomObjectCatalog.Contains(selectedRoomObject)) selectedRoomObject = roomObjectCatalog.Count > 0 ? roomObjectCatalog[0] : null;
            if (catalogScroll != null) { RefreshCatalog(); return; }
            BindMonsterButtons();
            BindPerkButtons();
            BindEquipmentButtons();
        }

        public void SelectCategory(int tab)
        {
            if (category == tab) return;
            category = Mathf.Clamp(tab, 0, categoryButtons != null ? categoryButtons.Length - 1 : 3);
            catalogTooltip?.Hide();
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            RefreshCatalog();
            catalogScroll.StopMovement();
            catalogScroll.verticalNormalizedPosition = 1;
            SetStatus(GameText.Get(category == 4 ? "ui.debugchapter.hint" : "ui.debugcatalog.hint"));
        }

        private void RefreshCatalog()
        {
            if (catalogScroll == null || tilePrefab == null) return;
            if (tiles.Count == 0) tiles.AddRange(catalogScroll.content.GetComponentsInChildren<DebugCatalogTile>(true));
            visibleEntries.Clear();
            if (category == 0) visibleEntries.AddRange(monsters);
            else if (category == 1) visibleEntries.AddRange(perks);
            else if (category == 4) visibleEntries.AddRange(roomObjectCatalog.FindAll(a => a != null));
            else foreach (var item in equipment) if (item.IsPotion == (category == 3)) visibleEntries.Add(item);
            if (category is 2 or 3 && (selectedEquipment == null || selectedEquipment.IsPotion != (category == 3)))
                selectedEquipment = visibleEntries.Count > 0 ? (EquipmentData)visibleEntries[0] : null;
            while (tiles.Count < visibleEntries.Count) tiles.Add(Instantiate(tilePrefab, catalogScroll.content));
            for (var i = 0; i < tiles.Count; i++)
            {
                tiles[i].gameObject.SetActive(i < visibleEntries.Count);
                if (i >= visibleEntries.Count) continue;
                var entry = visibleEntries[i];
                Sprite icon; string title, description;
                if (entry is MonsterData monster)
                {
                    icon = monster.portrait; title = monster.displayName;
                    description = GameText.Format("ui.debugcatalog.monsterstats", monster.maxHp, monster.normalAttackInterval, monster.chargeInterval)
                        + "\n\n" + monster.encounterHint;
                    foreach (var attack in monster.attacks)
                        if (attack != null && !string.IsNullOrEmpty(attack.displayName)) description += "\n• " + attack.displayName;
                }
                else if (entry is PerkData perk) { icon = perk.icon; title = perk.displayName; description = perk.description; }
                else if (entry is RoomObjectData art) { icon = art.front; title = GameText.Get(art.labelKey); description = GameText.Get("ui.debugchapter.objecthelp"); }
                else { var item = (EquipmentData)entry; icon = item.icon; title = item.displayName; description = item.description; }
                tiles[i].Bind(icon, title, description, IsSelected(entry), catalogTooltip, tooltipAnchor, () => SelectEntry(entry));
            }
            for (var i = 0; i < categoryButtons.Length; i++)
                categoryButtons[i].image.color = i == category ? new Color32(114, 83, 40, 255) : new Color32(37, 39, 44, 255);
            catalogLabel.text = category == 4 ? GameText.Format("ui.debugchapter.count", visibleEntries.Count) : GameText.Format("ui.debugcatalog.count", GameText.Get("ui.debugcatalog.tab" + category), visibleEntries.Count);
            startBattleButton.gameObject.SetActive(category == 0);
            infiniteBattleButton.gameObject.SetActive(category == 0);
            grantButton.gameObject.SetActive(category is 1 or 2 or 3);
            grantLabel.text = category is 1 or 2 or 3 ? GameText.Get("ui.debugcatalog.grant" + category) : "";
            chapterTools?.SetActive(category == 4);
            catalogHoverHint?.SetActive(category != 4);
            UpdateSelection();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(catalogScroll.content);
        }

        private bool IsSelected(UnityEngine.Object entry) => entry != null && (entry == selectedMonster || entry == selectedPerk || entry == selectedEquipment || entry == selectedRoomObject);

        private void SelectEntry(UnityEngine.Object entry)
        {
            if (entry is MonsterData monster) selectedMonster = monster;
            else if (entry is PerkData perk) selectedPerk = perk;
            else if (entry is RoomObjectData art) selectedRoomObject = art;
            else selectedEquipment = (EquipmentData)entry;
            for (var i = 0; i < visibleEntries.Count; i++) tiles[i].SetSelected(IsSelected(visibleEntries[i]));
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            var title = category == 4 ? selectedRoomObject != null ? GameText.Get(selectedRoomObject.labelKey) : null : category == 0 ? selectedMonster?.displayName : category == 1 ? selectedPerk?.displayName : selectedEquipment?.displayName;
            var valid = category == 4 ? selectedRoomObject != null : category == 0 ? selectedMonster != null : category == 1 ? selectedPerk != null : selectedEquipment != null && selectedEquipment.IsPotion == (category == 3);
            selectionLabel.text = valid ? GameText.Format("ui.debugcatalog.selected", title) : GameText.Get("ui.debugcatalog.choose");
            startBattleButton.interactable = category == 0 && valid;
            grantButton.interactable = category is 1 or 2 or 3 && valid;
            RefreshChapterTools();
        }

        private void BindMonsterButtons()
        {
            for (var index = 0; index < monsterButtons.Count; index++)
            {
                var button = monsterButtons[index];
                var visible = index < monsters.Count;
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                var monster = monsters[index];
                SetButton(button, monster.displayName, monster == selectedMonster, () => { selectedMonster = monster; BindMonsterButtons(); });
            }
        }

        private void BindPerkButtons()
        {
            for (var index = 0; index < perkButtons.Count; index++)
            {
                var button = perkButtons[index];
                var visible = index < perks.Count;
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                var perk = perks[index];
                SetButton(button, perk.displayName, perk == selectedPerk, () => { selectedPerk = perk; BindPerkButtons(); });
            }
        }

        private void BindEquipmentButtons()
        {
            for (var index = 0; index < equipmentButtons.Count; index++)
            {
                var button = equipmentButtons[index];
                var visible = index < equipment.Count;
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                var item = equipment[index];
                var prefix = item.IsPotion ? GameText.Get("ui.debugcheatoverlay.33c2f52b") : GameText.Get("ui.debugcheatoverlay.3439cc1b");
                SetButton(button, prefix + item.displayName, item == selectedEquipment, () => { selectedEquipment = item; BindEquipmentButtons(); });
            }
        }

        private static void SetButton(Button button, string label, bool selected, Action clicked)
        {
            var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) text.text = label;
            if (button.image != null) button.image.color = selected ? new Color(.78f, .56f, .16f) : new Color(.16f, .27f, .42f);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked());
        }

        private void RequestBattle()
        {
            if (selectedMonster == null) { SetStatus(GameText.Get("ui.debugcheatoverlay.9b49ea72")); return; }
            SetOpen(false);
            BattleRequested?.Invoke(selectedMonster);
        }

        private void ToggleInfiniteBattle()
        {
            infiniteBattle = !infiniteBattle;
            combat?.SetInfiniteBattle(infiniteBattle);
            UpdateInfiniteBattleLabel();
            SetStatus(infiniteBattle ? GameText.Get("ui.debugcheatoverlay.36cd1624") : GameText.Get("ui.debugcheatoverlay.84d3cfc5"));
        }

        private void AddSelectedPerk()
        {
            if (selectedPerk == null) { SetStatus(GameText.Get("ui.debugcheatoverlay.6ee59f11")); return; }
            if (!run.TryGrantPerk(selectedPerk)) { SetStatus(GameText.Get("ui.debugcheatoverlay.4f7a5151")); return; }
            SetStatus(GameText.Format("ui.debugcheatoverlay.3599f487", selectedPerk.displayName));
            InventoryChanged?.Invoke();
        }

        private void AddSelectedEquipment()
        {
            if (selectedEquipment == null) { SetStatus(GameText.Get("ui.debugcheatoverlay.9c2a558d")); return; }
            if (!run.TryGrantEquipment(selectedEquipment)) { SetStatus(selectedEquipment.IsPotion ? GameText.Get("ui.debugcheatoverlay.eed3920e") : GameText.Get("ui.debugcheatoverlay.6d1a009f")); return; }
            SetStatus(GameText.Format("ui.debugcheatoverlay.3599f487", selectedEquipment.displayName));
            InventoryChanged?.Invoke();
        }

        private void UpdateInfiniteBattleLabel()
        {
            if (infiniteBattleLabel != null)
                infiniteBattleLabel.text = infiniteBattle ? GameText.Get("ui.debugcheatoverlay.4b9195f8") : GameText.Get("ui.debugcheatoverlay.841ae6d2");
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
        }
    }
}
