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
    const string Folder="Docs/Production/WeakPointVisual/";
    CombatController combat;CombatSceneView scene;CombatInputController input;
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    void Check(bool ok,string message)
    {Directory.CreateDirectory(Folder);File.AppendAllText(Folder+"Live.txt",(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)Debug.LogError("WeakPointVisual: "+message);}
    public void Run(string action){if(action=="check")StartCoroutine(Matrix());else if(action=="preview")Setup(1);}
    void Setup(int hits,bool optional=false)
    {
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Weak point practice clone").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior,false);
        combat=Object.FindFirstObjectByType<CombatController>();scene=Object.FindFirstObjectByType<CombatSceneView>();input=Object.FindFirstObjectByType<CombatInputController>();
        // Keep this visual evidence clear of the unrelated six-second start-controls hint.
        var arcade=Object.FindFirstObjectByType<ArcadeHudController>();
        typeof(ArcadeHudController).GetField("showedCombatHint",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(arcade,true);
        typeof(ArcadeHudController).GetField("hintUntil",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(arcade,0f);
        var enemy=combat.Enemies[0];
        foreach(var attack in enemy.Data.attacks)
        {
            attack.chargeTimeLimit=60;
            attack.chargeWeakPoints=new List<WeakPointDefinition>{new(){id="visual-main",hitRadius=30,requiredChargeHits=hits,normalizedPosition=optional?new Vector2(-88,40):Vector2.zero}};
            if(optional)attack.chargeWeakPoints.Add(new WeakPointDefinition{id="visual-optional",hitRadius=30,requiredChargeHits=1,optional=true,normalizedPosition=new Vector2(88,-40)});
        }
        enemy.StartCharge(scene.MonsterPosition);combat.Player.ResetAttackCooldown();
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
        var frame=Object.FindFirstObjectByType<ArcadeRasterCamera>().Frame;var previous=RenderTexture.active;RenderTexture.active=frame;
        var texture=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);texture.Apply();
        Directory.CreateDirectory(Folder+"Screenshots");File.WriteAllBytes(Folder+"Screenshots/"+name+".png",texture.EncodeToPNG());Object.Destroy(texture);RenderTexture.active=previous;
    }
    IEnumerator Frame(string name){yield return null;yield return new WaitForEndOfFrame();Capture(name);}
    static bool HasSignal(Color signal)
    {
        var frame=Object.FindFirstObjectByType<ArcadeRasterCamera>().Frame;var previous=RenderTexture.active;RenderTexture.active=frame;
        var texture=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);texture.Apply();
        Color32 expected=signal;var found=texture.GetPixels32().Any(p=>Mathf.Abs(p.r-expected.r)<=2&&Mathf.Abs(p.g-expected.g)<=2&&Mathf.Abs(p.b-expected.b)<=2);
        Object.Destroy(texture);RenderTexture.active=previous;return found;
    }
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Live.txt","Scoped live target/particle matrix. Cloned Bat; real frame timing and production pointer handlers.\n");
        Setup(1);yield return Frame("Bat-Target");
        var point=combat.Enemies[0].WeakPoints.Single();var target=Object.FindFirstObjectByType<WeakPointView>().reticle;var radius=point.HitShape.Radius;
        var targetStyle=target.Style;var targetDiameter=target.rectTransform.rect.width;
        var pixels=new List<WeakPointVisualStyle.Pixel>();target.Style.BuildPixels(target.rectTransform.rect.width/4,pixels);
        Check(pixels.Count>0&&pixels.All(p=>p.color.r<.99f||p.color.g<.99f||p.color.b<.99f),"target uses only shared warm crimson/core/ink pixels; no white band");
        Check(HasSignal(target.Style.stroke),"final quantized framebuffer preserves the authored crimson target color");
        yield return Swipe(point,false);Check(point.RemainingHits==1&&!point.IsDestroyed,"normal swipe leaves weak point intact");
        yield return Swipe(point,true);yield return new WaitForEndOfFrame();Capture("OneHit-Break-Start");
        Check(point.RemainingHits==0&&(target==null||!target.gameObject.activeSelf),"charged production swipe destroys one-hit target and removes intact reticle immediately");
        var burst=Object.FindFirstObjectByType<WeakPointBreakFxView>();
        Check(burst!=null&&burst.shatter!=null&&burst.shatter.style==targetStyle&&Mathf.Approximately(burst.shatter.diameter,targetDiameter),"break inherits the destroyed target's exact style and diameter");
        Check(burst!=null&&burst.GetComponentsInChildren<Image>(true).All(i=>!i.gameObject.activeInHierarchy),"legacy gold rectangle particles stay inactive");
        Check(Mathf.Approximately(radius,point.HitShape.Radius),"presentation change preserves the combat hit radius");
        yield return new WaitForSecondsRealtime(.09f);yield return Frame("OneHit-Break-Fragments");
        // A random critical hit may tint the entire first impact frame; sample the surviving particles after that flash.
        Check(HasSignal(targetStyle.stroke),"break fragments preserve the same crimson in the final quantized framebuffer after the impact flash");
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
        Setup(1);yield return new WaitForSecondsRealtime(.8f);yield return Frame("Final-Target");
        File.AppendAllText(Folder+"Live.txt","COMPLETE\n");
    }
}
#endif
