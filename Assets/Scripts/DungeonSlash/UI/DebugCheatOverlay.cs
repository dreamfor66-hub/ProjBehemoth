using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>F1-only prototype test panel. It intentionally remains outside normal run progression.</summary>
    public sealed class DebugCheatOverlay : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private List<Button> monsterButtons = new();
        [SerializeField] private List<Button> perkButtons = new();
        [SerializeField] private List<Button> equipmentButtons = new();
        [SerializeField] private Button startBattleButton;
        [SerializeField] private Button infiniteBattleButton;
        [SerializeField] private Button addPerkButton;
        [SerializeField] private Button addEquipmentButton;
        [SerializeField] private Text infiniteBattleLabel;
        [SerializeField] private Text statusLabel;

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

        public void Configure(GameObject newPanel, IEnumerable<Button> newMonsterButtons, IEnumerable<Button> newPerkButtons, IEnumerable<Button> newEquipmentButtons, Button newStartBattleButton, Button newInfiniteBattleButton, Text newInfiniteBattleLabel, Button newAddPerkButton, Button newAddEquipmentButton, Text newStatusLabel)
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
        }

        private void WireButtons()
        {
            WireButton(startBattleButton, RequestBattle);
            WireButton(infiniteBattleButton, ToggleInfiniteBattle);
            WireButton(addPerkButton, AddSelectedPerk);
            WireButton(addEquipmentButton, AddSelectedEquipment);
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
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (panel == null) return;
            panel.SetActive(open);
            VisibilityChanged?.Invoke();
            if (open)
            {
                PopulateLists();
                SetStatus("몬스터를 선택한 뒤 배틀 시작을 누르세요.");
            }
        }

        private void PopulateLists()
        {
            monsters.Clear();
            if (combat != null) monsters.AddRange(combat.GetDebugMonsterCatalog());
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
            BindMonsterButtons();
            BindPerkButtons();
            BindEquipmentButtons();
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
                var prefix = item.IsPotion ? "포션 " : "유물 ";
                SetButton(button, prefix + item.displayName, item == selectedEquipment, () => { selectedEquipment = item; BindEquipmentButtons(); });
            }
        }

        private static void SetButton(Button button, string label, bool selected, Action clicked)
        {
            var text = button.GetComponentInChildren<Text>(true);
            if (text != null) text.text = label;
            if (button.image != null) button.image.color = selected ? new Color(.78f, .56f, .16f) : new Color(.16f, .27f, .42f);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked());
        }

        private void RequestBattle()
        {
            if (selectedMonster == null) { SetStatus("몬스터를 선택하세요."); return; }
            SetOpen(false);
            BattleRequested?.Invoke(selectedMonster);
        }

        private void ToggleInfiniteBattle()
        {
            infiniteBattle = !infiniteBattle;
            combat?.SetInfiniteBattle(infiniteBattle);
            UpdateInfiniteBattleLabel();
            SetStatus(infiniteBattle ? "무한 배틀: 양쪽 HP가 감소하지 않습니다." : "무한 배틀 해제");
        }

        private void AddSelectedPerk()
        {
            if (selectedPerk == null) { SetStatus("퍽을 선택하세요."); return; }
            if (!run.TryGrantPerk(selectedPerk)) { SetStatus("퍽을 더 추가할 수 없습니다."); return; }
            SetStatus($"{selectedPerk.displayName} 추가");
            InventoryChanged?.Invoke();
        }

        private void AddSelectedEquipment()
        {
            if (selectedEquipment == null) { SetStatus("장비를 선택하세요."); return; }
            if (!run.TryGrantEquipment(selectedEquipment)) { SetStatus(selectedEquipment.IsPotion ? "포션 슬롯이 가득 찼습니다." : "이미 보유 중인 유물입니다."); return; }
            SetStatus($"{selectedEquipment.displayName} 추가");
            InventoryChanged?.Invoke();
        }

        private void UpdateInfiniteBattleLabel()
        {
            if (infiniteBattleLabel != null)
                infiniteBattleLabel.text = infiniteBattle ? "무한 배틀: ON" : "무한 배틀: OFF";
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
        }
    }
}
