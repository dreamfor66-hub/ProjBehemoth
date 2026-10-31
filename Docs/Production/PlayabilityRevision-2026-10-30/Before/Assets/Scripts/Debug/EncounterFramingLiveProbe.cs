#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;

public sealed class EncounterFramingLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/EncounterFraming-2026-10-27/";
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    int failures;
    bool previousBackground;
    float previousTimeScale;
    EncounterMessageView message;
    float duration;
    ArcadeHudController hud;
    bool tutorial;
    static T Ref<T>(object owner, string field) => (T)owner.GetType().GetField(field, Fields).GetValue(owner);
    static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Fields).SetValue(owner, value);
    void Awake() { previousBackground = Application.runInBackground; Application.runInBackground = true; previousTimeScale = Time.timeScale; }
    void OnDestroy()
    {
        Application.runInBackground = previousBackground; Time.timeScale = previousTimeScale;
        if (message != null) Set(message, "displaySeconds", duration);
        if (hud != null) Set(hud, "<CombatTutorialShown>k__BackingField", tutorial);
    }
    void Check(bool ok, string text)
    {
        File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + text + "\n");
        if (!ok) { failures++; Debug.LogError("Encounter framing: " + text); }
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame(); var frame = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".png", frame.EncodeToPNG()); Destroy(frame);
    }
    public void Run() => StartCoroutine(Review());
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "Live.txt", "Existing Unity Editor; real GameFlow BeginCombatEvent coroutine with practice clone.\n");
        yield return null; yield return null;
        var practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Announcement practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior, false);
        var combat = FindFirstObjectByType<CombatController>(); var flow = FindFirstObjectByType<GameFlowController>();
        var scene = FindFirstObjectByType<CombatSceneView>(); var player = FindFirstObjectByType<PlayerView>(); var image = Ref<Image>(player, "body");
        var monster = combat.Enemies[0].Data;
        message = Ref<EncounterMessageView>(flow, "encounterMessageView"); duration = Ref<float>(message, "displaySeconds");
        hud = FindFirstObjectByType<ArcadeHudController>(); tutorial = hud.CombatTutorialShown;
        Set(hud, "<CombatTutorialShown>k__BackingField", true);
        combat.ResetEncounter(); combat.ShowPlayer(combat.Run); yield return null;
        var rest = scene.PlayerPosition;
        var room = FindFirstObjectByType<DungeonController>().State.Player.CurrentRoom;
        IEnumerator Event() => (IEnumerator)typeof(GameFlowController).GetMethod("BeginCombatEvent", Fields).Invoke(flow, new object[] { room, new[] { monster }, null });
        var routine = flow.StartCoroutine(Event()); yield return new WaitForSecondsRealtime(.16f);
        var panel = Ref<GameObject>(message, "panel");
        Check(panel.activeInHierarchy, "encounter announcement is visible while framing starts");
        Check(!combat.IsActive && flow.State == GameFlowState.RoomEvent, "combat AI/input remain inactive during announcement");
        Check(scene.PlayerPosition.y < rest.y && scene.PlayerPosition.y > -532, "player position interpolates during announcement");
        Check(image.rectTransform.sizeDelta.x > 640 && image.rectTransform.sizeDelta.x < 960, "player size interpolates during announcement");
        yield return Capture("01-Announcement-Transition");
        yield return new WaitForSecondsRealtime(.45f);
        Check(panel.activeInHierarchy && combat.IsCombatPresentationReady && !combat.IsActive, "framing completes before announcement ends");
        Check(scene.PlayerPosition == new Vector2(0, -532) && image.rectTransform.sizeDelta == new Vector2(960, 1020), "framing reaches authored combat pose under announcement");
        yield return Capture("02-Announcement-Ready"); yield return routine;
        Check(combat.IsActive && !panel.activeSelf, "combat starts only after announcement closes");
        var readyPosition = scene.PlayerPosition; var readySize = image.rectTransform.sizeDelta; yield return new WaitForSecondsRealtime(.2f);
        Check(scene.PlayerPosition == readyPosition && image.rectTransform.sizeDelta == readySize, "combat start does not restart or reverse framing");
        yield return Capture("03-Combat-Started");
        combat.ResetEncounter(); combat.ShowPlayer(combat.Run); Set(message, "displaySeconds", .1f); Time.timeScale = 0;
        routine = flow.StartCoroutine(Event()); yield return new WaitForSecondsRealtime(.18f);
        Check(panel.activeInHierarchy && !combat.IsActive, "short announcement waits for unfinished framing, even at timeScale=0");
        yield return routine;
        Check(combat.IsActive && combat.IsCombatPresentationReady, "short announcement starts combat with framing finished"); Time.timeScale = previousTimeScale;
        combat.ResetEncounter(); combat.ShowPlayer(combat.Run); Set(message, "displaySeconds", duration);
        // Resetting a run during the appearance message must restore the exploration pose.
        routine = flow.StartCoroutine(Event()); yield return new WaitForSecondsRealtime(.12f);
        flow.StopCoroutine(routine); message.Hide(); combat.ResetEncounter(); yield return null;
        Check(scene.PlayerPosition == rest && image.rectTransform.sizeDelta == new Vector2(640, 680), "cancel/reset during announcement restores original framing");
        File.AppendAllText(Folder + "Live.txt", "COMPLETE failures=" + failures + ". No balance suite.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }
}
#endif
