#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Verifies renamed artwork in the existing Editor; all encounter changes are runtime-only.
public sealed class ArtResourceNamingLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/ArtResourceRules-2026-10-31/";
    int passed, failed;
    bool background;
    PlayerCombatData previousSelection;
    void Awake() { background = Application.runInBackground; Application.runInBackground = true; previousSelection = RunSession.SelectedCharacter; }
    void OnDestroy() { Application.runInBackground = background; RunSession.SelectedCharacter = previousSelection; }
    void Check(bool valid, string message)
    { if (valid) passed++; else failed++; File.AppendAllText(Folder + "Live.txt", (valid ? "PASS " : "FAIL ") + message + "\n"); }
    static bool Valid(Sprite sprite) => sprite != null && sprite.texture != null && sprite.rect.width > 0 && sprite.rect.height > 0;
    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    public void Run() => StartCoroutine(Verify());
    IEnumerator Verify()
    {
        File.WriteAllText(Folder + "Live.txt", "Existing Unity Editor. Renamed sprite bindings, UI icons and a live combat frame; no balance suite or input changes.\n");
        var combat = Object.FindFirstObjectByType<CombatController>();
        var flow = Object.FindFirstObjectByType<GameFlowController>();
        var run = Object.FindFirstObjectByType<RunController>();
        foreach (var guid in AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data" }))
        {
            var monster = AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(guid));
            Check(Valid(monster.portrait) && monster.combatAnimation != null &&
                  new[] { CombatVisualState.Idle, CombatVisualState.Ready, CombatVisualState.Attack, CombatVisualState.Stunned, CombatVisualState.Dead }.All(s => Valid(monster.combatAnimation.For(s))), monster.displayName + " portrait and five combat state bindings");
        }
        foreach (var character in combat.Content.characters)
            Check(Valid(character.portrait) && Valid(character.selectionIllustration) && character.combatAnimation != null &&
                  new[] { CombatVisualState.Idle, CombatVisualState.Ready, CombatVisualState.Attack, CombatVisualState.Guard }.All(s => Valid(character.combatAnimation.For(s))), character.character + " selection, portrait and four combat states");
        var icons = combat.Content.equipment.Select(e => e.icon).Concat(combat.Content.perks.Select(p => p.icon)).Concat(combat.Content.spells.Select(s => s.icon)).ToArray();
        Check(icons.Length > 100 && icons.All(Valid), "all " + icons.Length + " content UI icon bindings resolve to textures");
        RunSession.SelectedCharacter = combat.Content.characters.First(c => c.character == CharacterKind.Monk);
        flow.StartNewRun();
        Object.FindFirstObjectByType<RunExpansionView>().HideModal();
        Object.FindFirstObjectByType<DungeonController>().HideNavigation();
        yield return new WaitForSecondsRealtime(.4f);
        combat.ResetEncounter();
        var source = AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data" }).Select(g => AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(g))).First(m => DesignerDataNamingCompatible(m.displayName) == "겨울의일각수");
        var clone = Object.Instantiate(source); clone.maxHp = 50000; clone.normalAttackInterval = clone.chargeInterval = 999;
        clone.supportRoster = new();
        combat.PrepareEncounter(run.State, new[] { clone }, 2); combat.BeginPreparedCombat();
        yield return new WaitForSecondsRealtime(.5f);
        combat.Enemies[0].StartCharge(Object.FindFirstObjectByType<CombatSceneView>().MonsterPosition);
        var player = Object.FindFirstObjectByType<PlayerView>(); player.PlayAttack();
        yield return null; Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
        var monsterView = Object.FindFirstObjectByType<MonsterView>();
        var monsterBody = Field<Image>(monsterView, "body");
        var playerBody = Field<Image>(player, "body");
        Check(Valid(monsterBody.sprite) && monsterBody.sprite == source.combatAnimation.ready, "visible winter unicorn uses renamed authored Ready sprite");
        Check(Valid(playerBody.sprite) && playerBody.sprite == run.State.Player.Data.combatAnimation.attack, "visible Monk attack uses renamed authored Attack sprite");
        var hud = Object.FindFirstObjectByType<FatigueHudView>(); var bag = Object.FindFirstObjectByType<PotionBagView>();
        Check(hud != null && Valid(hud.face.sprite) && bag != null && Valid(bag.artwork.sprite), "live HUD portrait and potion bag remain assigned");
        var texture = ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Folder + "RenamedCombat.png", texture.EncodeToPNG()); Object.Destroy(texture);
        File.WriteAllText(Folder + "LiveComplete.txt", passed + " PASS / " + failed + " FAIL\n");
    }
    static string DesignerDataNamingCompatible(string name) => name.Replace(" ", "");
}
#endif
