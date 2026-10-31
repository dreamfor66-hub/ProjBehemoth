#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;

// Explicit, narrow verification in the user's existing Editor. No saved gameplay changes.
public sealed class MonsterGroundingLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/MonsterGrounding-2026-10-27/";
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    CombatController combat;
    CombatSceneView scene;
    int failures;
    bool background;
    void Awake() { background = Application.runInBackground; Application.runInBackground = true; }
    void OnDestroy() => Application.runInBackground = background;
    static T Ref<T>(object owner, string name) => (T)owner.GetType().GetField(name, Fields).GetValue(owner);
    static T Prop<T>(object owner, string name) => (T)owner.GetType().GetProperty(name, Fields).GetValue(owner);
    IList Active => Ref<IList>(combat, "activeMonsters");
    MonsterView View(int i) => Prop<MonsterView>(Active[i], "View");
    Vector2 Center(int i) => Prop<Vector2>(Active[i], "Position");
    void Check(bool ok, string text)
    {
        File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + text + "\n");
        if (!ok) { failures++; Debug.LogError("Monster grounding: " + text); }
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame(); var frame = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".png", frame.EncodeToPNG()); Destroy(frame);
    }
    MonsterData Clone(string path)
    {
        var data = Instantiate(DungeonSlash.DesignerAssetPaths.Load<MonsterData>(path));
        data.maxHp = 50000; data.normalAttackInterval = data.chargeInterval = 999;
        data.attacks = data.attacks.Select(a => { var copy = Instantiate(a); copy.chargeTimeLimit = 120; return copy; }).ToList();
        return data;
    }
    void CheckGround(int index, Vector2 bottom)
    {
        var runtime = combat.Enemies[index]; var view = View(index); var data = runtime.Data;
        var actualBottom = Center(index) - Vector2.up * (-data.FootprintY) * runtime.VisualScale;
        Check(Vector2.Distance(actualBottom, bottom) < .01f, data.displayName + " hitbox bottom=" + actualBottom + " requested=" + bottom + " scale=" + runtime.VisualScale);
        var body = Ref<Image>(view, "body"); var layout = data.combatAnimation;
        var sprite = layout.idle;
        var artBottom = Center(index).y + data.ArtworkBounds.yMin * runtime.VisualScale;
        Check(Mathf.Abs(artBottom - bottom.y) < .02f, data.displayName + " visible Idle contact matches bottom=" + artBottom);
        Check(Vector2.Distance(view.Root.anchoredPosition + scene.MonsterPosition, Center(index)) < .01f, data.displayName + " view and collision centers agree");
    }
    public void Run() => StartCoroutine(Review());
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "Live.txt", "Focused existing-Editor verification. Practice clones only; no DPS/balance suite.\n");
        yield return null; yield return null;
        var practice = FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Grounding practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior, false);
        combat = FindFirstObjectByType<CombatController>(); scene = FindFirstObjectByType<CombatSceneView>();
        var skeleton = Clone("Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_해골_전사.asset");
        var spider = Clone("Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_거미.asset");
        var tiny = Instantiate(skeleton); tiny.hitBoxes = new() { new MonsterHitBox { radius = 9 } };
        var encounter = ScriptableObject.CreateInstance<EncounterData>();
        encounter.members = new List<EncounterMember> {
            new() { monster=skeleton, offset=new Vector2(-370,-65) },
            new() { monster=spider, offset=Vector2.zero },
            new() { monster=tiny, offset=new Vector2(370,-65) }
        };
        combat.ResetEncounter(); combat.PrepareFixedEncounter(combat.Run, encounter, 1); combat.BeginPreparedCombat(); combat.InputPaused = true;
        yield return new WaitForSecondsRealtime(.6f);
        for (var i=0;i<3;i++) CheckGround(i, scene.MonsterPosition + encounter.members[i].offset);
        Check(Mathf.Abs(Center(2).y - (scene.MonsterPosition.y -65 + 4.5f)) < .01f, "height 18 at scale .5 places center 4.5 above requested bottom");
        yield return Capture("01-Mixed-Size-Grounding");
        // Player attacks still target the relocated body and its charge weak points.
        combat.InputPaused=false; var hp=combat.Enemies[0].CurrentHp; combat.Player.ResetAttackCooldown();
        var c=Center(0); combat.ExecuteAttack(new AttackSegment(c+Vector2.left*100,c+Vector2.right*100,40),false);
        yield return new WaitForSecondsRealtime(.65f);
        Check(combat.Enemies[0].CurrentHp<hp,"normal slash damages relocated body");
        combat.Enemies[0].StartCharge(Center(0)); yield return null;
        var weak=combat.Enemies[0].WeakPoints.FirstOrDefault(w=>w.IsActive&&!w.IsOptional);
        Check(weak!=null,"relocated body exposes charge weak point");
        if(weak!=null) {
            var p=weak.HitShape.Center; var hits=weak.RemainingHits;
            var screen=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(p));
            Check(scene.TryScreenToLogical(screen,scene.UiCamera,out var logical)&&Vector2.Distance(p,logical)<1,"weak point screen/input mapping follows relocated center");
            combat.Player.ResetAttackCooldown(); combat.ExecuteAttack(new AttackSegment(p+Vector2.left*100,p+Vector2.right*100,40),true);
            yield return new WaitForSecondsRealtime(.65f); Check(weak.RemainingHits<hits,"charged slash hits relocated weak point");
        }
        combat.InputPaused=true;
        // Both complete pose libraries, driven by the real view/animator.
        foreach(var index in new[]{0,1}) {
            var view=View(index); var runtime=combat.Enemies[index]; var library=runtime.Data.combatAnimation;
            Check(new[]{library.idle,library.ready,library.attack,library.dead}.Distinct().Count()==4,"four distinct authored images: "+runtime.Data.displayName);
            foreach(var state in new[]{CombatVisualState.Idle,CombatVisualState.Ready,CombatVisualState.Attack,CombatVisualState.Dead}) {
                var animator=Ref<Image>(view,"body").GetComponent<CombatSpriteAnimator>(); animator.Play(library,state);
                Check(animator.GetComponent<Image>().sprite==library.For(state),runtime.Data.displayName+" displays "+state);
                yield return Capture((index==0?"Skeleton-":"Spider-")+state);
            }
        }
        // A flying placement is the lower hitbox edge at its authored altitude.
        combat.ResetEncounter(); encounter.members=new List<EncounterMember>{new(){monster=spider,offset=new Vector2(0,140)}};
        combat.PrepareFixedEncounter(combat.Run,encounter,1); combat.BeginPreparedCombat();combat.InputPaused=true;yield return null;
        CheckGround(0,scene.MonsterPosition+new Vector2(0,140));yield return Capture("02-Air-Placement");
        // Orbit motion retains the authored spawn center rather than returning to the global origin.
        var orbit=Instantiate(spider);orbit.useOrbit=true;orbit.orbitRadius=110;
        combat.ResetEncounter();encounter.members[0].monster=orbit;
        combat.PrepareFixedEncounter(combat.Run,encounter,1);combat.BeginPreparedCombat();combat.InputPaused=false;yield return new WaitForSecondsRealtime(.15f);
        var origin=orbit.BodyCenterFromPlacement(scene.MonsterPosition+new Vector2(0,140),.6f);
        var angle=combat.Player.CombatTime*orbit.orbitSpeed;
        Check(Vector2.Distance(Center(0),origin+new Vector2(Mathf.Cos(angle)*orbit.orbitRadius,Mathf.Sin(angle)*65))<1,"orbit moves around authored elevated center");
        combat.ResetEncounter();
        foreach(var attack in skeleton.attacks.Where(a=>a.type==MonsterAttackType.Charge))
            attack.chargeSummonMechanics=new ChargeSummonMechanics{summonOnFailedCharge=spider};
        encounter.members[0].monster=skeleton;
        combat.PrepareFixedEncounter(combat.Run,encounter,1);combat.BeginPreparedCombat();combat.InputPaused=true;
        combat.Enemies[0].StartCharge(Center(0));
        typeof(CombatController).GetMethod("ResolveFailedCharge",Fields).Invoke(combat,new[]{Active[0]});
        yield return null;
        Check(combat.Enemies.Count==2,"failed-charge summon uses the real spawn path");
        if(combat.Enemies.Count==2) CheckGround(1,scene.MonsterPosition+new Vector2(-176,124));
        combat.ResetEncounter();
        File.AppendAllText(Folder+"Live.txt","COMPLETE failures="+failures+". Saved stats unchanged. Touch device and broad balance remain outside this scoped check.\n");
        UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
