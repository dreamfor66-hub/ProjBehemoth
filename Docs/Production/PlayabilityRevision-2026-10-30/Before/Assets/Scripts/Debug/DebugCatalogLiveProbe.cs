#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Scoped runtime matrix for the F1 catalog, on explicit editor request only.</summary>
    public sealed class DebugCatalogLiveProbe : MonoBehaviour
    {
        const string Folder = "Docs/Production/DebugCatalog-2026-10-29/";
        readonly StringBuilder report = new();
        bool passed = true;
        T Ref<T>(object owner, string field) => (T)owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(owner);
        void Check(bool condition, string label) { passed &= condition; report.AppendLine((condition ? "PASS " : "FAIL ") + label); }
        IEnumerator Capture(string name) { yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Folder + name + ".png"); yield return null; }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            var overlay = FindFirstObjectByType<DebugCheatOverlay>();
            var run = FindFirstObjectByType<RunController>();
            var combat = FindFirstObjectByType<CombatController>();
            var flow = FindFirstObjectByType<GameFlowController>();
            overlay.SetOpen(true); yield return null;
            Check(overlay.IsOpen, "F1 surface opens in the live scene");
            var panelRect = (RectTransform)Ref<GameObject>(overlay, "panel").transform;
            var panelCorners = new Vector3[4]; panelRect.GetWorldCorners(panelCorners);
            var canvas = panelRect.GetComponentInParent<Canvas>();
            Check(panelCorners.Select(p => RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, p)).All(p => p.x >= 0 && p.x <= Screen.width && p.y >= 0 && p.y <= Screen.height), "entire panel including footer stays on screen");
            Check(!string.IsNullOrWhiteSpace(Ref<TextMeshProUGUI>(overlay, "infiniteBattleLabel").text), "infinite battle button label is visible");
            for (var tab = 0; tab < 4; tab++)
            {
                overlay.categoryButtons[tab].onClick.Invoke(); yield return null; yield return null;
                var cells = overlay.catalogScroll.content.GetComponentsInChildren<DebugCatalogTile>();
                var expected = tab == 0 ? overlay.monsterCatalog.Count : tab == 1 ? run.AvailablePerks.Count : run.Equipment.Count(i => i.IsPotion == (tab == 3));
                Check(cells.Length == expected, "tab " + tab + " all entries accessible: " + cells.Length + "/" + expected);
                Check(cells.All(c => c.icon.sprite != null && c.icon.enabled), "tab " + tab + " every entry has a visible image");
                var scroll = overlay.catalogScroll;
                var overflow = scroll.content.rect.height > scroll.viewport.rect.height + 1;
                Check(scroll.verticalScrollbar.gameObject.activeSelf == overflow, "tab " + tab + " scrollbar automatically follows overflow");
                var first = cells.First();
                first.OnPointerEnter(new PointerEventData(EventSystem.current)); yield return null;
                Check(overlay.catalogTooltip.IsVisible && !string.IsNullOrWhiteSpace(overlay.catalogTooltip.VisibleTitle), "tab " + tab + " hover opens named side popup");
                var tip = overlay.catalogTooltip.GetComponent<RectTransform>(); var corners = new Vector3[4]; tip.GetWorldCorners(corners);
                var bounds = ((RectTransform)tip.parent).rect;
                Check(corners.All(p => bounds.Contains(tip.parent.InverseTransformPoint(p))), "tab " + tab + " side popup stays inside panel");
                Check(overlay.catalogTooltip.GetComponentsInChildren<TMP_Text>().All(t => !t.isTextOverflowing), "tab " + tab + " popup text fits");
                yield return Capture("Tab" + tab + "-Hover");
                first.OnPointerExit(new PointerEventData(EventSystem.current));
                Check(!overlay.catalogTooltip.IsVisible, "tab " + tab + " hover exit hides popup");
                var descriptionsFit = true;
                foreach (var cell in cells)
                {
                    cell.OnPointerEnter(new PointerEventData(EventSystem.current));
                    foreach (var label in overlay.catalogTooltip.GetComponentsInChildren<TMP_Text>()) { label.ForceMeshUpdate(); descriptionsFit &= !label.isTextOverflowing; }
                    cell.OnPointerExit(new PointerEventData(EventSystem.current));
                }
                Check(descriptionsFit, "tab " + tab + " all descriptions fit the side popup");
                if (overflow)
                {
                    scroll.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -6) }); yield return null;
                    Check(scroll.verticalNormalizedPosition < .99f, "tab " + tab + " wheel moves the actual content");
                    scroll.verticalNormalizedPosition = 0; yield return null;
                    var last = cells.Last(); last.button.onClick.Invoke();
                    Check(last.selectedFrame.activeSelf && cells.Count(c => c.selectedFrame.activeSelf) == 1, "tab " + tab + " final overflow entry can be selected exclusively");
                    yield return Capture("Tab" + tab + "-Bottom");
                }
                else first.button.onClick.Invoke();
            }
            // Grant only an available simple perk, a common relic and a potion in this temporary run.
            overlay.SelectCategory(1); yield return null;
            var perks = run.AvailablePerks.ToArray();
            var perkIndex = System.Array.FindIndex(perks, p => PerkSystem.IsAvailable(p, run.State));
            if (perkIndex >= 0)
            {
                overlay.catalogScroll.content.GetComponentsInChildren<DebugCatalogTile>()[perkIndex].button.onClick.Invoke();
                var status = Ref<TextMeshProUGUI>(overlay, "statusLabel"); overlay.grantButton.onClick.Invoke();
                Check(status.text.Contains(perks[perkIndex].displayName), "perk grant gives named success feedback");
            }
            overlay.SelectCategory(2); yield return null;
            var relic = run.Equipment.Where(i => !i.IsPotion).ToArray();
            var relicIndex = System.Array.FindIndex(relic, i => i.scope == RelicScope.Shared && !run.State.EquippedItems.Contains(i));
            if (relicIndex >= 0)
            {
                overlay.catalogScroll.content.GetComponentsInChildren<DebugCatalogTile>()[relicIndex].button.onClick.Invoke(); overlay.grantButton.onClick.Invoke();
                Check(run.State.EquippedItems.Contains(relic[relicIndex]), "selected relic is actually granted");
                overlay.grantButton.onClick.Invoke();
                Check(Ref<TextMeshProUGUI>(overlay, "statusLabel").text == GameText.Get("ui.debugcheatoverlay.6d1a009f"), "duplicate relic failure is readable");
            }
            overlay.SelectCategory(3); yield return null;
            var potion = Ref<EquipmentData>(overlay, "selectedEquipment"); var count = run.State.Potions.Count; overlay.grantButton.onClick.Invoke();
            Check(run.State.Potions.Count == count + 1 || Ref<TextMeshProUGUI>(overlay, "statusLabel").text == GameText.Get("ui.debugcheatoverlay.eed3920e"), "potion adds or reports full slots");
            for (var i = 0; i < 10; i++) overlay.grantButton.onClick.Invoke();
            Check(Ref<TextMeshProUGUI>(overlay, "statusLabel").text == GameText.Get("ui.debugcheatoverlay.eed3920e"), "full potion slots report failure without overflowing inventory");
            overlay.SelectCategory(0); yield return null;
            Ref<Button>(overlay, "infiniteBattleButton").onClick.Invoke(); Check(combat.InfiniteBattleEnabled, "infinite battle toggle on");
            Ref<Button>(overlay, "infiniteBattleButton").onClick.Invoke(); Check(!combat.InfiniteBattleEnabled, "infinite battle toggle off");
            overlay.closeButton.onClick.Invoke(); Check(!overlay.IsOpen && !overlay.catalogTooltip.IsVisible, "close also hides side popup");
            overlay.SetOpen(true); Check(overlay.IsOpen, "reopen preserves a usable catalog");
            var monster = Ref<MonsterData>(overlay, "selectedMonster"); Ref<Button>(overlay, "startBattleButton").onClick.Invoke();
            yield return null;
            Check(!overlay.IsOpen && (flow.State == GameFlowState.RoomEvent || flow.State == GameFlowState.Combat) && combat.HasPreparedMonsters, "selected monster battle is prepared and closes overlay");
            report.AppendLine("Runtime: " + Screen.width + "x" + Screen.height + "; selected monster=" + monster.displayName);
            report.AppendLine("Scope: F1 only; no DPS/balance suite. Physical input results are recorded separately. Real device pending.");
            report.Insert(0, (passed ? "PASS" : "FAIL") + " — live F1 matrix\n");
            File.WriteAllText(Folder + "LiveMatrix.txt", report.ToString());
            overlay.SetOpen(true); overlay.SelectCategory(0);
            Destroy(gameObject);
        }
    }
}
#endif
