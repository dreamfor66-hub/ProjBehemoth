#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEngine;

// Only the changed spawn height, formation and relocated target interaction.
public sealed class MonsterSpawnHeightLiveProbe : MonoBehaviour
{
    const string Folder="Docs/Production/MonsterSpawnHeight-2026-10-27/";
    int failures;
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy()=>Application.runInBackground=background;
    void Check(bool ok,string message){File.AppendAllText(Folder+"Live.txt",(ok?"PASS ":"FAIL ")+message+"\n");if(!ok){failures++;Debug.LogError("Spawn height: "+message);}}
    IEnumerator Capture(string name){yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",image.EncodeToPNG());Destroy(image);}
    public void Run()=>StartCoroutine(Review());
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Live.txt","Scoped existing Editor test, practice clones; no balance suite.\n");
        yield return null;yield return null;
        var practice=FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Spawn height practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_해골_전사.asset");
        var combat=FindFirstObjectByType<CombatController>();var scene=FindFirstObjectByType<CombatSceneView>();
        yield return new WaitForSecondsRealtime(.6f);
        Check(scene.MonsterPosition==new Vector2(0,-150),"saved spawn root=(0,-150)");
        var skeleton=combat.Enemies[0].Data;var view=FindFirstObjectByType<MonsterView>();
        var center=view.Root.anchoredPosition+scene.MonsterPosition;
        Check(Mathf.Abs(center.y-(-skeleton.FootprintY)*view.Root.localScale.y+150)<.01f,"single skeleton hitbox bottom=-150");
        Check(scene.PlayerPosition==new Vector2(0,-532),"foreground player remains at -532");
        combat.InputPaused=true;yield return Capture("01-Skeleton-Lowered");combat.InputPaused=false;
        combat.Enemies[0].StartCharge(center);yield return null;
        var weak=combat.Enemies[0].WeakPoints.FirstOrDefault(w=>w.IsActive&&!w.IsOptional);
        Check(weak!=null,"lowered target exposes its weak point");
        if(weak!=null){var p=weak.HitShape.Center;var screen=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(p));
            Check(scene.TryScreenToLogical(screen,scene.UiCamera,out var logical)&&Vector2.Distance(p,logical)<1,"lowered weak point screen/input mapping");
            var remaining=weak.RemainingHits;combat.Player.ResetAttackCooldown();combat.ExecuteAttack(new AttackSegment(p+Vector2.left*100,p+Vector2.right*100,40),true);
            yield return new WaitForSecondsRealtime(.65f);Check(weak.RemainingHits<remaining,"charged slash hits lowered weak point");}
        combat.InputPaused=true;yield return Capture("02-Skeleton-Charge");
        combat.ResetEncounter();combat.PrepareEncounter(combat.Run,new[]{skeleton,skeleton,skeleton},1);combat.BeginPreparedCombat();combat.InputPaused=true;
        yield return new WaitForSecondsRealtime(.6f);
        var views=FindObjectsByType<MonsterView>(FindObjectsSortMode.None).OrderBy(v=>v.Root.anchoredPosition.x).ToArray();
        var bottoms=new[]{new Vector2(-370,-215),new Vector2(0,-150),new Vector2(370,-215)};
        Check(views.Length==3,"three monsters spawned");
        for(var i=0;i<views.Length;i++){var bottom=views[i].Root.anchoredPosition+scene.MonsterPosition-Vector2.up*(-skeleton.FootprintY)*views[i].Root.localScale.y;Check(Vector2.Distance(bottom,bottoms[i])<.01f,"formation bottom="+bottom);}
        yield return Capture("03-Trio-Lowered");combat.ResetEncounter();
        File.AppendAllText(Folder+"Live.txt","COMPLETE failures="+failures+". Play stopped after scoped test.\n");UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
