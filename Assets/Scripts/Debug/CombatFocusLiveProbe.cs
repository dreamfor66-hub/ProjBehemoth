#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;

// Only framing, the Warrior pose, and the directly affected input/finish paths.
public sealed class CombatFocusLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/CombatFocus-2026-10-27/";
    CombatController combat;
    CombatSceneView scene;
    PlayerView player;
    Image body;
    CombatInputController input;
    OctoberReviewLiveProbe practice;
    Vector2 restPosition;
    Vector3 restScale;
    float savedTimeScale;
    bool savedBackground;
    int failures;
    string logFile = "Live.txt";
    static T Ref<T>(object owner, string field) => (T)owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    void Check(bool ok, string name)
    {
        File.AppendAllText(Folder + logFile, (ok ? "PASS " : "FAIL ") + name + "\n");
        if (!ok) { failures++; Debug.LogError("Combat focus: " + name); }
    }
    void Awake() { savedTimeScale = Time.timeScale; savedBackground = Application.runInBackground; Application.runInBackground = true; }
    void OnDestroy() { Time.timeScale = savedTimeScale; Application.runInBackground = savedBackground; }
    public void Run() => StartCoroutine(Review());
    public void RunContacts() => StartCoroutine(ReviewContacts());
    IEnumerator ReviewContacts()
    {
        logFile = "InputOverlap.txt";
        File.WriteAllText(Folder + logFile, "Scoped relocated-body two-contact overlap/lift/repress, no all-cancel between cycles.\n");
        yield return null; yield return null;
        practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Combat focus practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior, false); BindViews(); yield return new WaitForSecondsRealtime(.55f);
        var start = ScreenAt(new Vector2(scene.PlayerPosition.x, scene.LogicalArea.rect.yMin + 140));
        var end = ScreenAt(new Vector2(scene.PlayerPosition.x, scene.LogicalArea.rect.yMin + 40));
        Check(start.y >= 0 && end.y >= 0 && start.y <= Screen.height && end.y <= Screen.height, "both guard gesture endpoints are inside the actual screen");
        var target = scene.MonsterPosition;
        Contact("Begin", 861, start); Contact("Move", 861, end); yield return null;
        int Count() => Ref<System.Collections.IDictionary>(input, "contacts").Count;
        for (var cycle = 1; cycle <= 3; cycle++)
        {
            combat.Player.ResetAttackCooldown();
            var attackStart = ScreenAt(target + Vector2.left * 120); var attackEnd = ScreenAt(target + Vector2.right * 120);
            Contact("Begin", 862, attackStart); yield return null; Contact("Move", 862, attackEnd); yield return null;
            Check(Count() == 2 && combat.Player.IsGuarding, cycle + " guard survives concurrent second-finger swipe");
            Contact("End", 862, attackEnd, false); yield return null;
            Check(combat.Player.AttackCooldownRemaining > 0, cycle + " second-finger swipe release executes attack while first guard stays held");
            Check(Count() == 1 && combat.Player.IsGuarding, cycle + " second finger lifts without dropping first guard");
            Contact("Begin", 862, attackStart); yield return null;
            Contact("End", 861, end, false); yield return null;
            Check(Count() == 1 && !combat.Player.IsGuarding, cycle + " guard lifts independently while second contact stays");
            Contact("Begin", 861, start); Contact("Move", 861, end); yield return null;
            Check(Count() == 2 && combat.Player.IsGuarding, cycle + " guard repress succeeds without clearing second contact");
            Contact("End", 862, attackStart, true); yield return null;
        }
        yield return new WaitForSecondsRealtime(.35f); yield return Capture("08-Overlap-Guard");
        Contact("End", 861, end, false); yield return null;
        Check(Count() == 0 && !combat.Player.IsGuarding, "final release clears guard and both contacts");
        File.AppendAllText(Folder + logFile, "COMPLETE failures=" + failures + ". Synthetic finger IDs through production Begin/Move/End. Real device pending.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + "Screenshots/" + name + ".png", texture.EncodeToPNG()); Destroy(texture);
    }
    void BindViews()
    {
        combat = FindFirstObjectByType<CombatController>(); scene = FindFirstObjectByType<CombatSceneView>();
        player = FindFirstObjectByType<PlayerView>(); body = Ref<Image>(player, "body"); input = FindFirstObjectByType<CombatInputController>();
        input.SpellModifierForVerification = false;
    }
    void Contact(string method, params object[] args) => typeof(CombatInputController).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(input, args);
    Vector2 ScreenAt(Vector2 point) => RectTransformUtility.WorldToScreenPoint(scene.UiCamera, scene.LogicalArea.TransformPoint(point));
    void CheckCombat(string name)
    {
        Check(Vector2.Distance(scene.PlayerPosition, new Vector2(0, -532)) < .01f, name + " Root=(0,-532): " + scene.PlayerPosition);
        Check(Vector2.Distance(body.rectTransform.sizeDelta, new Vector2(960, 1020)) < .01f, name + " image=960x1020 (1.5x)");
        Check(player.Root.localScale == restScale && scene.PlayerAnchor.localScale == Vector3.one, name + " object scale unchanged");
    }
    void CheckRest(string name)
    {
        Check(Vector2.Distance(scene.PlayerPosition, restPosition) < .01f, name + " original Root restored: " + scene.PlayerPosition);
        Check(Vector2.Distance(body.rectTransform.sizeDelta, new Vector2(640, 680)) < .01f, name + " original image size restored");
    }
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder + "Screenshots"); File.WriteAllText(Folder + "Live.txt", "Existing Unity Editor, scoped runtime matrix. Practice runtime clones only.\n");
        yield return null; yield return null;
        practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Combat focus practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior, false); BindViews(); combat.ResetEncounter(); combat.ShowPlayer(combat.Run);
        yield return null;
        restPosition = scene.PlayerPosition; restScale = player.Root.localScale; CheckRest("Navigation");
        yield return Capture("01-Navigation");
        var monster = Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Bat.asset"));
        monster.maxHp = 50000; monster.normalAttackInterval = monster.chargeInterval = 999;
        combat.PrepareEncounter(combat.Run, new[] { monster }, 1); yield return null;
        CheckRest("Encounter prepared before start");
        var enemyPosition = scene.MonsterPosition; var expectedEnemy = FindFirstObjectByType<MonsterView>().Root.anchoredPosition;
        combat.BeginPreparedCombat(); yield return new WaitForSecondsRealtime(.16f);
        Check(scene.PlayerPosition.y < restPosition.y && scene.PlayerPosition.y > -532, "start interpolates rather than snapping");
        Check(body.rectTransform.sizeDelta.x > 640 && body.rectTransform.sizeDelta.x < 960, "image interpolates with Root");
        yield return Capture("02-Starting"); yield return new WaitForSecondsRealtime(.45f); CheckCombat("Started");
        Check(scene.MonsterPosition == enemyPosition && FindFirstObjectByType<MonsterView>().Root.anchoredPosition == expectedEnemy, "flying monster authored position unchanged");
        yield return Capture("03-Combat-Idle");
        var imageInfo = FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i => i.enabled && RectTransformUtility.RectangleContainsScreenPoint(i.rectTransform, new Vector2(730, 200), scene.UiCamera))
            .Select(i => (i.transform.parent != null ? i.transform.parent.name : "") + "/" + i.name + " rect=" + i.rectTransform.rect + " color=" + i.color + " sprite=" + (i.sprite != null ? i.sprite.name : "none"));
        File.WriteAllLines(Folder + "Visible-UI.txt", imageInfo);
        var pose = body.GetComponent<CombatSpriteAnimator>();
        // A downward hold in the visible part of the relocated body must still guard.
        var start = ScreenAt(new Vector2(scene.PlayerPosition.x, scene.LogicalArea.rect.yMin + 140));
        var end = ScreenAt(new Vector2(scene.PlayerPosition.x, scene.LogicalArea.rect.yMin + 40));
        Check(start.y >= 0 && end.y >= 0 && start.y <= Screen.height && end.y <= Screen.height, "both guard gesture endpoints are inside the actual screen");
        Contact("Begin", 861, start); Contact("Move", 861, end); yield return null; yield return null;
        Check(combat.Player.IsGuarding && pose.State == CombatVisualState.Guard, "visible relocated body drag-hold enters Guard");
        Check(UnityEditor.AssetDatabase.GetAssetPath(body.sprite) == "Assets/Art/World/Angular/States/Heroes/Warrior-Guard.png", "Warrior uses revised frontal defense sprite");
        yield return Capture("04-Warrior-Guard"); Contact("End", 861, end, false); yield return null;
        Check(!combat.Player.IsGuarding && pose.State == CombatVisualState.Idle, "guard release restores Idle");
        var center = scene.MonsterPosition; combat.Player.ResetAttackCooldown();
        combat.ExecuteAttack(new AttackSegment(center + Vector2.left * 100, center + Vector2.right * 100, 40), false); yield return null;
        Check(pose.State == CombatVisualState.Attack, "accepted attack uses Attack pose"); CheckCombat("Attacking");
        var cooldown = Ref<Image>(player, "cooldown"); var corners = new Vector3[4]; cooldown.rectTransform.GetWorldCorners(corners);
        var screenCorner = RectTransformUtility.WorldToScreenPoint(scene.UiCamera, corners[0]);
        Check(cooldown.enabled && screenCorner.y >= 0 && screenCorner.y <= Screen.height, "attack cooldown remains on screen after relocation");
        yield return Capture("05-Attack-Cooldown"); yield return new WaitForSecondsRealtime(.6f); CheckCombat("Repeated Bind");
        // Exercise actual win handling and its final-impact/fade delay.
        foreach (var enemy in combat.Enemies) enemy.TakeDamage(enemy.CurrentHp);
        combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(new AttackSegment(center + Vector2.left * 100, center + Vector2.right * 100, 40), false);
        Check(!combat.IsActive, "lethal encounter enters normal victory finish"); yield return new WaitForSecondsRealtime(.16f); CheckCombat("Final hit hold");
        yield return new WaitForSecondsRealtime(1.5f); CheckRest("Victory"); yield return Capture("06-Victory-Return");
        // Repeated 2/3-enemy starts preserve formation and do not multiply image size again.
        foreach (var count in new[] { 2, 3 })
        {
            practice.Setup(CharacterKind.Warrior, false); BindViews(); combat.ResetEncounter();
            var encounter = ScriptableObject.CreateInstance<EncounterData>();
            for (var index = 0; index < count; index++)
                encounter.members.Add(new EncounterMember { monster = monster, scale = 1f, offset = new Vector2((index - (count - 1) * .5f) * 370f, count == 3 && index == 1 ? 0 : -65f) });
            combat.PrepareFixedEncounter(combat.Run, encounter, 1);
            yield return null; // Destroyed views from the preceding practice leave at frame end.
            var before = FindObjectsByType<MonsterView>(FindObjectsSortMode.None).OrderBy(v => v.GetInstanceID()).Select(v => v.Root.anchoredPosition).ToArray();
            combat.BeginPreparedCombat(); yield return new WaitForSecondsRealtime(.55f); CheckCombat(count + " monsters");
            var after = FindObjectsByType<MonsterView>(FindObjectsSortMode.None).OrderBy(v => v.GetInstanceID()).Select(v => v.Root.anchoredPosition).ToArray();
            Check(before.SequenceEqual(after), count + " monsters retain their existing formation");
            yield return Capture("07-Formation-" + count);
        }
        combat.Player.TakeEnvironmentalDamage(combat.Player.CurrentHp); yield return new WaitForSecondsRealtime(.6f); CheckRest("Defeat");
        practice.Setup(CharacterKind.Warrior, false); BindViews(); yield return new WaitForSecondsRealtime(.12f); combat.ResetEncounter(); yield return null; CheckRest("Reset during entry");
        combat.PrepareEncounter(combat.Run, new[] { monster }, 1); Time.timeScale = 0; combat.BeginPreparedCombat();
        yield return new WaitForSecondsRealtime(.55f); CheckCombat("Unscaled entry at timeScale=0");
        player.SetCombatFraming(scene.PlayerAnchor, false); yield return new WaitForSecondsRealtime(.55f); CheckRest("Unscaled return at timeScale=0"); Time.timeScale = savedTimeScale;
        // One other class checks shared presentation, without altering its art or testing balance.
        practice.Setup(CharacterKind.Rogue, false); BindViews(); yield return new WaitForSecondsRealtime(.55f); CheckCombat("Other class shared framing");
        combat.ResetEncounter(); yield return null; CheckRest("Other class reset");
        File.AppendAllText(Folder + "Live.txt", "COMPLETE failures=" + failures + ". No DPS/balance suite. Real-device visual feel remains a manual check.\n");
        yield return ReviewContacts();
    }
}
#endif
