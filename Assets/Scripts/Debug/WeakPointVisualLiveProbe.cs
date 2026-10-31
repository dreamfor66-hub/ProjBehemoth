#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Only the changed target/charge-hit/break presentation, in the existing Editor with cloned practice data.
public sealed class WeakPointVisualLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/CombatInk-2026-10-29/";
    CombatController combat;CombatSceneView scene;CombatInputController input;
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    void Check(bool ok,string message)
    {Directory.CreateDirectory(Folder);File.AppendAllText(Folder+"Live.txt",(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)Debug.LogError("WeakPointVisual: "+message);}
    public void Run(string action){if(action=="check")StartCoroutine(Matrix());else if(action=="preview")StartCoroutine(Preview());}
    IEnumerator Preview()
    {
        Setup(1,true,true);
        combat.Enemies[0].CurrentChargeAttack.chargeTimeLimit=3;
        typeof(MonsterRuntime).GetProperty("StateTimer").SetValue(combat.Enemies[0],2.85f);
        yield return Frame("Final-HUD");UnityEditor.EditorApplication.isPaused=true;
    }
    void Setup(int hits,bool optional=false,bool authored=false)
    {
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Weak point practice clone").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_해골_전사.asset");
        combat=Object.FindFirstObjectByType<CombatController>();scene=Object.FindFirstObjectByType<CombatSceneView>();input=Object.FindFirstObjectByType<CombatInputController>();
        // Keep this visual evidence clear of the unrelated six-second start-controls hint.
        var arcade=Object.FindFirstObjectByType<ArcadeHudController>();
        if(arcade!=null&&arcade.hint!=null)arcade.hint.gameObject.SetActive(false);
        var enemy=combat.Enemies[0];
        foreach(var attack in enemy.Data.attacks)
        {
            attack.chargeTimeLimit=60;
            if(authored)continue;
            attack.chargeWeakPoints=new List<WeakPointDefinition>{new(){id="visual-main",hitRadius=30,requiredChargeHits=hits,localPosition=optional?new Vector2(-88,40):Vector2.zero}};
            if(optional)attack.chargeWeakPoints.Add(new WeakPointDefinition{id="visual-optional",hitRadius=30,requiredChargeHits=1,optional=true,localPosition=new Vector2(88,-40)});
        }
        enemy.StartCharge(enemy.Data.BodyCenterFromPlacement(scene.MonsterPosition,enemy.VisualScale));combat.Player.ResetAttackCooldown();
    }
    void Contact(string method,params object[] args)=>typeof(CombatInputController).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,args);
    Vector2 ScreenAt(Vector2 point)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(point));
    IEnumerator Swipe(WeakPointRuntime point,bool charged)
    {
        combat.Player.ResetAttackCooldown();
        var from=ScreenAt(point.HitShape.Center+Vector2.left*140);var to=ScreenAt(point.HitShape.Center+Vector2.right*140);
        Contact("Begin",741,from);
        var held=0f;var duration=charged?combat.Player.CurrentChargeDuration+.1f:.03f;
        while(held<duration){yield return null;held+=Time.deltaTime;Contact("Move",741,from);}
        Contact("Move",741,to);Contact("End",741,to,false);
        yield return null;
    }
    static void Capture(string name)
    {
        Directory.CreateDirectory(Folder+"Screenshots");ScreenCapture.CaptureScreenshot(Folder+"Screenshots/"+name+".png");
    }
    IEnumerator Frame(string name){yield return null;yield return new WaitForEndOfFrame();Capture(name);}
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Live.txt","Scoped live ink target/telegraph matrix. Cloned Skeleton; production pointer handlers.\n");
        Setup(1);yield return Frame("Skeleton-Target");
        var point=combat.Enemies[0].WeakPoints.Single();var target=Object.FindFirstObjectByType<WeakPointView>().reticle;var radius=point.HitShape.Radius;
        var targetStyle=target.Style;var targetDiameter=target.rectTransform.rect.width;
        Check(target.defaultMaterial==target.Style.material&&target.Style.material!=null,"target uses native ink material, continuous curves and authored colors");
        Check(Object.FindFirstObjectByType<ArcadeRasterCamera>().TextRenderer.Draws.Any(d=>d.mesh.name.Contains(target.name)),"target participates in the native resolution composite");
        var monster=Object.FindFirstObjectByType<MonsterView>();
        var timer=typeof(MonsterView).GetField("chargeTimeBackdrop",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(monster) as Image;
        Check(timer.GetComponentInChildren<InkUiSurface>().role==InkSurfaceRole.Countdown,"countdown uses frameless ink shadow without a duration gauge");
        yield return Swipe(point,false);Check(point.RemainingHits==1&&!point.IsDestroyed,"normal swipe leaves weak point intact");
        yield return Swipe(point,true);yield return new WaitForEndOfFrame();Capture("OneHit-Break-Start");
        Check(point.RemainingHits==0&&(target==null||!target.gameObject.activeSelf),"charged production swipe destroys one-hit target and removes intact reticle immediately");
        var burst=Object.FindFirstObjectByType<WeakPointBreakFxView>();
        Check(burst!=null&&burst.shatter!=null&&burst.shatter.style==targetStyle&&Mathf.Approximately(burst.shatter.diameter,targetDiameter),"break inherits the destroyed target's exact style and diameter");
        Check(burst!=null&&burst.GetComponentsInChildren<Image>(true).All(i=>!i.gameObject.activeInHierarchy),"legacy gold rectangle particles stay inactive");
        Check(Mathf.Approximately(radius,point.HitShape.Radius),"presentation change preserves the combat hit radius");
        yield return new WaitForSecondsRealtime(.09f);yield return Frame("OneHit-Break-Fragments");
        // A random critical hit may tint the entire first impact frame; sample the surviving particles after that flash.
        Check(burst!=null&&burst.shatter.progress>0&&burst.shatter.progress<1,"continuous ink fragments disperse during their lifetime");
        yield return new WaitForSecondsRealtime(.48f);Check(Object.FindObjectsByType<WeakPointBreakFxView>(FindObjectsSortMode.None).Length==0,"break particles clean up after their impact lifetime");
        Setup(2,true);yield return Frame("MultiHit-And-Optional");
        point=combat.Enemies[0].WeakPoints.Single(p=>!p.IsOptional);
        var optional=combat.Enemies[0].WeakPoints.Single(p=>p.IsOptional);
        yield return Swipe(point,true);yield return Frame("MultiHit-One-Remaining");
        Check(point.RemainingHits==1&&!point.IsDestroyed&&optional.RemainingHits==1,"partial multi-hit charge removes one hit while preserving optional target");
        Check(Object.FindObjectsByType<WeakPointBreakFxView>(FindObjectsSortMode.None).Length==0,"partial hit never emits final shatter");
        target=Object.FindObjectsByType<WeakPointView>(FindObjectsSortMode.None).First(v=>Vector2.Distance(((RectTransform)v.transform).anchoredPosition,point.HitShape.Center)<1).reticle;
        var alternate=Object.Instantiate(target.Style);alternate.ringThickness=.9f;alternate.stroke=new Color32(193,59,46,255);target.style=alternate;target.SetAllDirty();
        yield return Swipe(point,true);yield return Frame("MultiHit-Final-Break");
        burst=Object.FindFirstObjectByType<WeakPointBreakFxView>();
        Check(point.RemainingHits==0&&burst!=null&&burst.shatter.style==alternate,"final multi-hit break inherits an instance style/shape edit without a separate particle rebuild");
        Check(optional.RemainingHits==1,"breaking a required target does not consume optional target hits");
        yield return new WaitForSecondsRealtime(.6f);Object.Destroy(alternate);
        Setup(1);yield return new WaitForSecondsRealtime(.2f);yield return Frame("Final-Target");
        var enemy=combat.Enemies[0];enemy.EndCharge();enemy.NormalAttack.windupDuration=.5f;enemy.BeginNormalAttack();yield return null;
        monster=Object.FindFirstObjectByType<MonsterView>();
        var timeLabel=typeof(MonsterView).GetField("chargeTimeLabel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(monster) as TMPro.TextMeshProUGUI;
        Check(timeLabel!=null&&timeLabel.enabled,"normal attack also shows the frameless countdown");
        yield return new WaitForSeconds(.7f);
        Check(timeLabel!=null&&!timeLabel.enabled,"timer disappears after normal windup finishes");
        File.AppendAllText(Folder+"Live.txt","COMPLETE\n");
    }
}
#endif
