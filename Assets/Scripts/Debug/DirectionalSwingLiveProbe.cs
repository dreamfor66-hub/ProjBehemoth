#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;

public sealed class DirectionalSwingLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/DirectionalSwing-2026-10-28/";
    CombatController combat;CombatInputController input;CombatSceneView scene;PlayerView player;Image body;CombatSpriteAnimator pose;
    bool background;int failures;PlayerCombatData savedCharacter;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;savedCharacter=RunSession.SelectedCharacter;}
    void OnDestroy(){Application.runInBackground=background;RunSession.SelectedCharacter=savedCharacter;if(input!=null)input.SpellModifierForVerification=null;}
    static T Ref<T>(object owner,string name)=>(T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
    void Contact(string name,params object[] args)=>typeof(CombatInputController).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,args);
    Vector2 ScreenAt(Vector2 point)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(point));
    void Check(bool ok,string message){File.AppendAllText(Folder+"Runtime.txt",(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)failures++;}
    public void Run()=>StartCoroutine(Review());
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+name+".png",texture.EncodeToPNG());Destroy(texture);
    }
    IEnumerator Stroke(int id,bool fromLeft,bool capture,bool charged=false)
    {
        combat.Player.ResetAttackCooldown();
        var start=ScreenAt(scene.MonsterPosition+Vector2.right*(fromLeft?-320:320));
        var end=ScreenAt(scene.MonsterPosition+Vector2.right*(fromLeft?320:-320));
        var direction=fromLeft?SwordSwingDirection.LeftToRight:SwordSwingDirection.RightToLeft;
        Contact("Begin",id,start);yield return null;yield return null;
        Check(input.IsPreparingAttack&&input.PreparingSwing==direction,"press latches "+direction);
        if(combat.Player.IsGuarding)Check(pose.State==CombatVisualState.Guard,"guard painting takes priority while preparing "+direction);
        else Check(pose.State==CombatVisualState.Ready&&body.sprite==pose.library.For(CombatVisualState.Ready,direction),"correct authored Ready "+direction);
        if(capture)yield return Capture(direction+"-Ready");
        if(charged)
        {
            var until=Time.time+combat.Player.CurrentChargeDuration+.06f;
            while(Time.time<until){Contact("Move",id,start);yield return null;}
            Check(pose.State==CombatVisualState.Ready&&pose.SwingDirection==direction,"charged hold keeps its authored Ready "+direction);
        }
        // Sample a real, fast traversal after the long held Ready capture.
        Contact("Move",id,start);yield return null;
        Contact("Move",id,Vector2.Lerp(start,end,.2f));yield return null;
        Contact("Move",id,end);Contact("End",id,end,false);yield return null;
        Check(combat.Player.AttackCooldownRemaining>0&&pose.State==CombatVisualState.Attack,"production swipe accepts "+direction);
        Check(Ref<CharacterAttack>(combat,"currentAttack").Charged==charged,"normal/charged attack semantics retained "+direction+" charged="+charged);
        Check(pose.SwingDirection==direction&&body.sprite==pose.library.For(CombatVisualState.Attack,direction),"matching Attack survives crossing screen center "+direction);
        if(capture)yield return Capture(direction+"-Attack");
        yield return new WaitForSeconds(.36f);
    }
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Runtime.txt","Existing Unity Editor. Production Begin/Move/End with synthetic independent finger IDs. No balance/DPS tests.\n");
        yield return null;yield return null;
        var practice=new GameObject("Directional swing practice clones").AddComponent<OctoberReviewLiveProbe>();practice.Setup(CharacterKind.Warrior,false);
        combat=FindFirstObjectByType<CombatController>();input=FindFirstObjectByType<CombatInputController>();scene=FindFirstObjectByType<CombatSceneView>();
        player=FindFirstObjectByType<PlayerView>();body=Ref<Image>(player,"body");pose=body.GetComponent<CombatSpriteAnimator>();input.SpellModifierForVerification=false;
        yield return new WaitForSecondsRealtime(.6f);
        var position=body.rectTransform.anchoredPosition;var scale=body.rectTransform.localScale;
        Check(pose.library.readyLeftToRight!=null&&pose.library.attackLeftToRight!=null,"alternate paintings assigned in saved Warrior library");
        yield return Stroke(981,true,true);yield return Stroke(982,false,true);
        yield return Stroke(987,true,false,true);yield return Stroke(988,false,false,true);
        Check(pose.State==CombatVisualState.Idle&&body.enabled&&body.sprite!=null,"both directions return to a valid Idle");
        var left=ScreenAt(scene.MonsterPosition+Vector2.left*320);var right=ScreenAt(scene.MonsterPosition+Vector2.right*320);
        Contact("Begin",983,left);Contact("Begin",984,right);yield return null;
        Check(input.PreparingSwing==SwordSwingDirection.LeftToRight,"second contact does not overwrite held first direction");
        Contact("End",983,left,true);yield return null;yield return null;
        Check(input.IsPreparingAttack&&input.PreparingSwing==SwordSwingDirection.RightToLeft&&body.sprite==pose.library.ready,"independent cancellation exposes remaining contact Ready");
        Contact("End",984,right,true);yield return null;yield return null;
        Check(!input.IsPreparingAttack&&pose.State==CombatVisualState.Idle,"cancel restores Idle without firing");
        var guardStart=ScreenAt(new Vector2(scene.PlayerPosition.x,scene.LogicalArea.rect.yMin+140));
        var guardEnd=ScreenAt(new Vector2(scene.PlayerPosition.x,scene.LogicalArea.rect.yMin+40));
        Contact("Begin",985,guardStart);Contact("Move",985,guardEnd);yield return null;
        for(var cycle=0;cycle<3;cycle++)
        {
            yield return Stroke(986,cycle%2==0,false);
            Check(combat.Player.IsGuarding&&Ref<System.Collections.IDictionary>(input,"contacts").Count==1,"guard survives overlap/lift/repress cycle "+cycle);
        }
        Contact("End",985,guardEnd,false);yield return null;yield return null;
        Check(!combat.Player.IsGuarding&&!input.IsPreparingAttack,"final guard release clears only its own contact");
        Check(body.rectTransform.anchoredPosition==position&&body.rectTransform.localScale==scale,"poses do not animate actor transforms");
        var fallback=ScriptableObject.CreateInstance<CombatSpriteAnimation>();fallback.ready=pose.library.ready;fallback.attack=pose.library.attack;
        Check(fallback.For(CombatVisualState.Ready,SwordSwingDirection.LeftToRight)==fallback.ready&&fallback.For(CombatVisualState.Attack,SwordSwingDirection.LeftToRight)==fallback.attack,"unconfigured classes retain their existing paintings");Destroy(fallback);
        File.AppendAllText(Folder+"Runtime.txt","COMPLETE failures="+failures+". Real touch-device feel remains untested.\n");
        UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
