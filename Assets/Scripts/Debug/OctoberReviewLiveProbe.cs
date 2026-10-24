#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Focused, opt-in checks in the already running Editor. Practice data is cloned, never saved.
public sealed class OctoberReviewLiveProbe:MonoBehaviour
{
    const string Log="Logs/OctoberReview-Live.txt",Screens="Docs/Production/FeedbackRevision2/Screenshots/";
    CombatController combat;CombatInputController input;CombatSceneView scene;RunController run;bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;if(input!=null)input.SpellModifierForVerification=null;}
    static T Ref<T>(object owner,string field)where T:class=>owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)?.GetValue(owner)as T;
    void Contact(string method,params object[] args)=>typeof(CombatInputController).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,args);
    Vector2 ScreenAt(Vector2 point)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(point));
    void Check(bool ok,string name){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+name+"\n");if(!ok)Debug.LogError("October review: "+name);}
    public static void Capture(string name)
    {
        var frame=Object.FindFirstObjectByType<ArcadeRasterCamera>()?.Frame;if(frame==null)return;
        var prev=RenderTexture.active;RenderTexture.active=frame;var t=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,t.width,t.height),0,0);t.Apply();Directory.CreateDirectory(Screens);File.WriteAllBytes(Screens+name+".png",t.EncodeToPNG());Object.Destroy(t);RenderTexture.active=prev;
    }
    public void Setup(CharacterKind kind,bool spells=true,string monsterPath="Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Bat.asset")
    {
        combat=Object.FindFirstObjectByType<CombatController>();input=Object.FindFirstObjectByType<CombatInputController>();scene=Object.FindFirstObjectByType<CombatSceneView>();run=Object.FindFirstObjectByType<RunController>();
        RunSession.SelectedCharacter=combat.Content.characters.First(c=>c.character==kind);Object.FindFirstObjectByType<GameFlowController>().StartNewRun();
        Object.FindFirstObjectByType<DungeonController>().HideNavigation();Object.FindFirstObjectByType<RunExpansionView>().HideModal();
        var source=UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>(monsterPath);
        var practice=Object.Instantiate(source);practice.name="Review practice clone";practice.maxHp=50000;practice.normalAttackInterval=999;practice.chargeInterval=999;practice.encounterHint="";
        practice.attacks=source.attacks.Select(a=>{var clone=Object.Instantiate(a);clone.chargeTimeLimit=120;return clone;}).ToList();
        if(spells)foreach(var id in new[]{"EnergyBolt","Ember","Spark","PoisonMist"})run.State.LearnSpell(combat.Content.spells.First(s=>s.id==id),true);
        combat.PrepareEncounter(run.State,new[]{practice},1);combat.BeginPreparedCombat();foreach(var s in run.State.Spellbook)s.ResetCooldown();
    }
    public void Charge(){combat??=Object.FindFirstObjectByType<CombatController>();scene??=Object.FindFirstObjectByType<CombatSceneView>();foreach(var attack in combat.Enemies[0].Data.attacks)attack.chargeTimeLimit=6;combat.Enemies[0].StartCharge(scene.MonsterPosition);}
    public void Run()=>StartCoroutine(Matrix());
    public void Extra()=>StartCoroutine(ExtraMatrix());
    public void OutGame()=>StartCoroutine(SelectionMatrix());
    IEnumerator SelectionMatrix()
    {
        const string path="Logs/OctoberReview-Selection.txt";File.WriteAllText(path,"Live serialized OutGame UI.\n");
        var menu=Object.FindFirstObjectByType<OutGameMenuView>();menu.ShowTitle();yield return null;yield return new WaitForEndOfFrame();Capture("Title");menu.ShowCharacters();yield return null;
        for(var i=0;i<menu.selection.characterButtons.Length;i++)
        {
            menu.selection.characterButtons[i].onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();var selection=menu.selection;var data=selection.content.characters[i];var image=selection.characterButtons[i].transform.Find("Portrait").GetComponent<Image>();
            File.AppendAllText(path,$"{data.character}: original description={selection.selectionStory.text==data.description}, overflow={selection.selectionStory.isTextOverflowing}, faces={selection.characterButtons.Count(b=>b.gameObject.activeInHierarchy)}, square={image.rectTransform.rect.width==image.rectTransform.rect.height}, thumb source={image.sprite.rect.size}, portrait source={data.portrait.rect.size}\n");
            yield return new WaitForEndOfFrame();Capture("Selection-"+data.character);
        }
        menu.selection.startingRelicHover.OnPointerEnter(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));yield return null;yield return new WaitForEndOfFrame();Capture("Selection-RelicPopup");menu.selection.startingRelicHover.OnPointerExit(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
        File.AppendAllText(path,"COMPLETE\n");
    }
    IEnumerator ExtraMatrix()
    {
        File.AppendAllText(Log,"\nFollow-up: Shift modifier state injected in Editor; real frame timer and production pointer handlers.\n");
        Setup(CharacterKind.Wizard);yield return null;var c=scene.MonsterPosition;var start=ScreenAt(c+Vector2.down*280);var end=ScreenAt(c+Vector2.down*80);var hp=combat.Enemies[0].CurrentHp;var mp=combat.Player.CurrentMp;
        input.SpellModifierForVerification=true;yield return new WaitForSeconds(.65f);File.AppendAllText(Log,$"Shift trace: override={input.SpellModifierForVerification}, held={input.IsSpellKeyHeld}, casting={input.IsCastingHeld}/{input.CastingSeconds}, guard={combat.Player.IsGuarding}, active={combat.IsActive}, paused={combat.InputPaused}, enabled={input.enabled}, book={run.State.Spellbook.Count}\n");Check(input.CastingSeconds>.4f&&!combat.Player.IsGuarding,"Shift alone accumulates cast time without guarding");
        Contact("Begin",-1,start);Contact("Move",-1,end);Contact("End",-1,end,false);yield return null;Check(combat.Enemies[0].CurrentHp<hp&&combat.Player.CurrentMp<mp,"Shift + left stroke casts aimed spell");
        input.SpellModifierForVerification=false;yield return null;Check(input.CastingSeconds==0,"Shift release resets casting");input.SpellModifierForVerification=null;
        yield return new WaitForSeconds(.8f);combat.Player.ResetAttackCooldown();combat.Player.RefillMp();run.State.RuntimeFor(run.State.Slot(SwipeDirection.Up)).ResetCooldown();mp=combat.Player.CurrentMp;hp=combat.Enemies[0].CurrentHp;
        Contact("Begin",-2,start);yield return new WaitForSeconds(.04f);Contact("Move",-2,end);Contact("End",-2,end,false);yield return null;Check(Mathf.Approximately(mp,combat.Player.CurrentMp)&&Mathf.Approximately(hp,combat.Enemies[0].CurrentHp),"early cast rejection spends no MP and deals no damage");
        Setup(CharacterKind.Samurai,false);yield return null;start=ScreenAt(scene.PlayerPosition+Vector2.up*90);end=ScreenAt(scene.PlayerPosition+Vector2.down*55);Contact("Begin",300,start);Contact("Move",300,end);yield return new WaitForSeconds(.24f);
        var result=new DamageResolver().ApplyToPlayer(combat.Player,new DamageRequest(DamageType.MonsterAttack,5,5,true,combat.Enemies[0]));Check(result.WasSheathParry,"guard begun 0.24 seconds ago performs sheath parry");Contact("End",300,end,false);combat.Player.ResetAttackCooldown();combat.ExecuteAttack(new AttackSegment(c+Vector2.left*240,c+Vector2.right*240,40),false);yield return new WaitForEndOfFrame();Capture("Samurai-ParryDraw");
        yield return new WaitForSeconds(.85f);Setup(CharacterKind.Samurai,false);yield return null;Contact("Begin",301,start);Contact("Move",301,end);yield return new WaitForSeconds(.36f);
        result=new DamageResolver().ApplyToPlayer(combat.Player,new DamageRequest(DamageType.MonsterAttack,5,5,true,combat.Enemies[0]));Check(!result.WasSheathParry&&result.WasGuarded,"0.36 seconds is ordinary guard rather than sheath parry");Contact("End",301,end,false);combat.Player.ResetAttackCooldown();combat.ExecuteAttack(new AttackSegment(c+Vector2.left*240,c+Vector2.right*240,40),false);yield return new WaitForEndOfFrame();Capture("Samurai-Draw");
        Setup(CharacterKind.Wizard);yield return new WaitForSeconds(6.2f);Charge();yield return null;yield return new WaitForEndOfFrame();Capture("Combat-Final");
        var hud=Object.FindFirstObjectByType<ArcadeHudController>();var expansion=Object.FindFirstObjectByType<RunExpansionView>();
        foreach(var item in new[]{hud.mapButton,hud.inventoryButton,expansion.returnButton}){var r=(RectTransform)item.transform;Check(r.parent.GetComponent<Canvas>()!=null&&r.anchoredPosition.y==400,"top control on root canvas: "+item.name);}
        Check(expansion.spellLabels.All(t=>!t.isTextOverflowing&&t.textInfo.lineCount==1),"four spell labels fit a single line");
        var monster=Object.FindFirstObjectByType<MonsterView>();Check(Mathf.Approximately(((RectTransform)Ref<Image>(monster,"hpFill").transform.parent).rect.width,168),"monster HP bar restored to 42 native pixels");
        Check(Ref<TMPro.TextMeshProUGUI>(monster,"chargeTimeLabel").textInfo.lineCount==1,"charge countdown fits one line below attack name");
        var enemy=combat.Enemies[0];if(UnityEditor.AssetDatabase.Contains(enemy.Data))throw new InvalidOperationException("QA must use a runtime clone");
        foreach(var definition in enemy.CurrentChargeAttack.chargeWeakPoints)definition.requiredChargeHits=3;
        enemy.EndCharge();enemy.StartCharge(scene.MonsterPosition);yield return null;yield return new WaitForEndOfFrame();Capture("WeakPoint-Three-Hits");
        var point=enemy.WeakPoints.First(w=>!w.IsOptional);Check(point.RequiredHits==3&&Object.FindObjectsByType<WeakPointView>(FindObjectsSortMode.None).Any(v=>v.transform.Find("RemainingHits/HitSegments")?.childCount==3||v.GetComponentsInChildren<Image>().Count(i=>i.name=="Fill")>=3),"three-hit weakpoint renders three remaining-hit segments");
        combat.Player.ResetAttackCooldown();combat.ExecuteAttack(new AttackSegment(point.HitShape.Center+Vector2.left*100,point.HitShape.Center+Vector2.right*100,40),true);yield return new WaitForSeconds(.2f);Check(point.RemainingHits==2,"multi-hit weakpoint loses one segment per charge hit");yield return new WaitForEndOfFrame();Capture("WeakPoint-Two-Hits-Remain");
        File.AppendAllText(Log,"FOLLOW-UP COMPLETE\n");
    }
    IEnumerator Matrix()
    {
        File.WriteAllText(Log,"Focused October24 revision. Real frames, existing Editor; contacts injected into the production controller.\n");
        Setup(CharacterKind.Warrior,false);yield return null;yield return null;
        var mana=Object.FindFirstObjectByType<ManaHudView>();var hud=Object.FindFirstObjectByType<CombatHudView>();
        Check(run.State.Spellbook.Count==0&&mana.gauge.activeInHierarchy,"MP visible before learning a spell");
        Check(hud.guardGauge!=null&&hud.guardGauge.GetComponent<CanvasRenderer>()!=null&&hud.guardGauge.isActiveAndEnabled,"slanted guard gauge has renderer and is visible");
        var start=ScreenAt(scene.PlayerPosition+Vector2.up*90);var down=ScreenAt(scene.PlayerPosition+Vector2.down*55);
        Contact("Begin",100,start);yield return new WaitForSeconds(.2f);Check(!combat.Player.IsGuarding,"touch and hold alone does not guard");
        Contact("Move",100,down);yield return null;Check(combat.Player.IsGuarding,"downward drag then hold guards");
        Contact("Begin",101,start);Contact("Move",101,down);Contact("End",100,down,false);yield return null;Check(combat.Player.IsGuarding,"second guard contact survives first lift");
        for(var i=0;i<3;i++){Contact("Begin",100,start);Contact("Move",100,down);Contact("End",101,down,false);yield return null;Check(combat.Player.IsGuarding,"overlap lift/repress A cycle"+i);Contact("Begin",101,start);Contact("Move",101,down);Contact("End",100,down,false);yield return null;Check(combat.Player.IsGuarding,"overlap lift/repress B cycle"+i);}
        Contact("End",101,down,false);yield return null;Check(!combat.Player.IsGuarding,"last release stops guard");
        yield return new WaitForEndOfFrame();Capture("Warrior-MP-GuardGauge");
        Setup(CharacterKind.Wizard);yield return null;yield return null;
        var enemy=combat.Enemies[0];var hp=enemy.CurrentHp;var mp=combat.Player.CurrentMp;var bolt=run.State.Slot(SwipeDirection.Up);var c=scene.MonsterPosition;
        var from=ScreenAt(c+Vector2.down*280);var to=ScreenAt(c+Vector2.down*80);
        Contact("Begin",-2,from);yield return new WaitForSeconds(.65f);Check(input.CastingSeconds>.4f&&!combat.Player.IsGuarding,"right hold casts without guard");Contact("Move",-2,to);yield return new WaitForEndOfFrame();Capture("Wizard-Aim");Contact("End",-2,to,false);yield return null;
        Check(enemy.CurrentHp<hp&&combat.Player.CurrentMp<mp,"right upward stroke damages along aimed bolt and spends MP");Check(input.CastingSeconds==0&&!input.IsGuardHeld,"right release clears cast state");
        yield return new WaitForSeconds(.8f);combat.Player.ResetAttackCooldown();combat.Player.RefillMp();run.State.RuntimeFor(bolt).ResetCooldown();hp=enemy.CurrentHp;mp=combat.Player.CurrentMp;
        var missFrom=ScreenAt(c+new Vector2(-570,-280));var missTo=ScreenAt(c+new Vector2(-570,-80));Contact("Begin",-2,missFrom);yield return new WaitForSeconds(.65f);Contact("Move",-2,missTo);Contact("End",-2,missTo,false);yield return null;
        Check(Mathf.Approximately(hp,enemy.CurrentHp)&&combat.Player.CurrentMp<mp,"bolt aimed away misses while spending MP");
        yield return new WaitForSeconds(.8f);combat.Player.ResetAttackCooldown();combat.Player.RefillMp();var spark=combat.Content.spells.First(s=>s.id=="Spark");run.State.RuntimeFor(spark).ResetCooldown();hp=enemy.CurrentHp;
        Check(combat.TryCastSpell(spark.defaultSlot,1,new AttackSegment(c+Vector2.left*220,c,0)),"point lightning cast accepted");yield return null;Check(enemy.CurrentHp<hp,"point lightning strikes chosen endpoint");
        yield return new WaitForSeconds(.8f);combat.Player.ResetAttackCooldown();combat.Player.RefillMp();run.State.RuntimeFor(spark).ResetCooldown();hp=enemy.CurrentHp;
        combat.TryCastSpell(spark.defaultSlot,1,new AttackSegment(c+new Vector2(-450,0),c+new Vector2(-700,0),0));yield return null;Check(Mathf.Approximately(hp,enemy.CurrentHp),"point lightning off target misses");
        var expansion=Object.FindFirstObjectByType<RunExpansionView>();Check(expansion.spellButtons.All(b=>b.gameObject.activeInHierarchy),"all four spell slots on screen");
        var rects=expansion.spellButtons.Select(b=>(RectTransform)b.transform).ToArray();Check(rects[2].anchoredPosition.y>rects[0].anchoredPosition.y&&rects[3].anchoredPosition.y<rects[0].anchoredPosition.y&&rects[0].anchoredPosition.x<rects[2].anchoredPosition.x&&rects[1].anchoredPosition.x>rects[2].anchoredPosition.x,"spell slots form D-pad");
        Setup(CharacterKind.Monk,false);yield return null;var technique=combat.Player.Data.attackBuffs.First(b=>!b.requiresUnlock&&b.attackPattern.Length>0);foreach(var direction in technique.attackPattern)combat.Player.PrepareAttack(direction,false);yield return null;yield return new WaitForEndOfFrame();Capture("Monk-Status");Check(combat.Player.StatusText.Contains(technique.displayName)&&expansion.characterStatus.rectTransform.anchoredPosition.x==0,"monk technique and arrows centered above player");
        Setup(CharacterKind.Samurai,false);yield return null;var sheath=UnityEditor.AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Combat/Buffs/Sheathed.asset");var prepared=UnityEditor.AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Combat/Buffs/PreparedDraw.asset");var window=UnityEditor.AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Combat/Buffs/SheathParryWindow.asset");
        combat.Player.Buffs.Add(sheath,root:run.State);combat.Player.Buffs.UpdateState(0);var normal=combat.Player.PrepareAttack(SwipeDirection.Right,false);Check(normal.FeedbackKey=="ui.combatcontroller.9b76b68d","ordinary draw uses 발도!");
        combat.Player.Buffs.Add(sheath,root:run.State);combat.Player.Buffs.UpdateState(0);var charged=combat.Player.PrepareAttack(SwipeDirection.Right,true);Check(charged.FeedbackKey=="ui.combatcontroller.9b76b68d","manual charged draw uses 발도!");
        combat.Player.Buffs.Add(sheath,root:run.State);combat.Player.Buffs.Add(prepared,root:run.State);combat.Player.Buffs.UpdateState(0);var parry=combat.Player.PrepareAttack(SwipeDirection.Right,false);Check(parry.FeedbackKey=="ui.combatcontroller.deb723db","prepared parry draw uses 패리 발도");Check(Mathf.Approximately(window.ReleaseData.Time,.3f),"sheath parry duration is 0.3 seconds");
        Setup(CharacterKind.Wizard);yield return null;Charge();yield return null;yield return new WaitForEndOfFrame();Capture("Wizard-WeakPoints-Dpad");
        var reticles=Object.FindObjectsByType<WeakPointReticle>(FindObjectsSortMode.None);Check(reticles.Any(r=>r.isActiveAndEnabled&&r.GetComponent<CanvasRenderer>()!=null&&r.canvasRenderer.absoluteDepth>=0),"weak points actually submitted to Canvas renderer");
        enemy=combat.Enemies[0];var weak=enemy.WeakPoints.First(w=>w.IsActive&&!w.IsOptional);var remaining=weak.RemainingHits;combat.Player.ResetAttackCooldown();combat.ExecuteAttack(new AttackSegment(weak.HitShape.Center+Vector2.left*100,weak.HitShape.Center+Vector2.right*100,40),true);yield return null;Check(weak.RemainingHits<remaining,"charge stroke hits visible weakpoint collision");
        File.AppendAllText(Log,"COMPLETE. Physical Shift-hold and real touchscreen remain manual/device coverage; no balance suite run.\n");
    }
}
#endif
