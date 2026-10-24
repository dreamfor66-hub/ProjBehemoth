#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using Object=UnityEngine.Object;

// Scoped QA in the user's existing editor. Never serialized into a production scene.
public sealed class AshenVowLiveProbe : MonoBehaviour
{
    private RunController controller;
    private CombatController combat;
    private RunExpansionView expansion;
    private MonsterData practice;
    private bool previousBackground;
    private void Awake(){previousBackground=Application.runInBackground;Application.runInBackground=true;}
    public void Setup(MonsterData source=null)
    {
        controller=Object.FindFirstObjectByType<RunController>();combat=Object.FindFirstObjectByType<CombatController>();expansion=Object.FindFirstObjectByType<RunExpansionView>();
        var content=expansion.GetComponentInParent<Canvas>().GetComponentInChildren<OutGameView>(true)?.content??combat.Content;
        if(content==null)throw new InvalidOperationException("GameContent missing");
        Object.FindFirstObjectByType<DungeonController>().HideNavigation();expansion.HideModal();
        if(practice!=null)Object.Destroy(practice);
        practice=Object.Instantiate(source??content.stages[0].earlyCombats[0].members[0].monster);practice.name="AshenVow QA practice clone";practice.maxHp=50000;practice.normalAttackInterval=999;practice.chargeInterval=999;practice.encounterHint="";practice.pattern=MonsterPattern.Standard;
        foreach(var id in new[]{"EnergyBolt","Ember","EnchantFire","PoisonMist"})controller.State.LearnSpell(content.spells.First(s=>s.id==id),true);
        combat.PrepareEncounter(controller.State,new[]{practice},1);combat.BeginPreparedCombat();
    }
    public void RunSpells(){Setup();StartCoroutine(SpellMatrix());}
    public void RunInput(){SetupForManual();StartCoroutine(InputMatrix());}
    public void RunEvents(){StartCoroutine(EventMatrix());}
    public void RunPoses(){StartCoroutine(PoseMatrix());}
    private IEnumerator PoseMatrix()
    {
        const string log="Logs/Arcade224-Live-Poses.txt";File.WriteAllText(log,"Scoped live representative-image state transitions. One sprite per state; no frame playback.\n");
        combat=Object.FindFirstObjectByType<CombatController>();var flow=Object.FindFirstObjectByType<GameFlowController>();var content=combat.Content;
        var source=UnityEditor.AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data/DungeonSlashPrototype/Monsters"}).Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First(m=>m.name.Contains("Skeleton"));
        void Check(bool ok,string text){File.AppendAllText(log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)Debug.LogError("Pose QA: "+text);}
        var scene=Object.FindFirstObjectByType<CombatSceneView>();var input=Object.FindFirstObjectByType<CombatInputController>();
        void Contact(string method,params object[] args)=>typeof(CombatInputController).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(input,args);
        foreach(var character in content.characters)
        {
            RunSession.SelectedCharacter=character;flow.StartNewRun();yield return null;Setup(source);yield return null;
            var player=Object.FindFirstObjectByType<PlayerView>();var pose=player.GetComponentInChildren<CombatSpriteAnimator>();
            Check(pose!=null&&pose.State==CombatVisualState.Idle,character.character+" Idle");yield return new WaitForSeconds(.3f);
            Check(pose.GetComponent<UnityEngine.UI.Image>().sprite==character.combatAnimation.idle,character.character+" Idle stays on one image");
            var c=scene.MonsterPosition;combat.ExecuteAttack(new AttackSegment(c+Vector2.left*240,c+Vector2.right*240,40),false);yield return null;
            Check(pose.State==CombatVisualState.Attack&&pose.GetComponent<UnityEngine.UI.Image>().sprite==character.combatAnimation.attack,character.character+" real attack switches image");
            yield return new WaitForSeconds(.35f);Check(pose.State==CombatVisualState.Idle,character.character+" attack returns to Idle");
            var screen=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(scene.PlayerPosition+Vector2.up*150));
            Contact("Begin",-190,screen);yield return null;
            Check(input.IsGuardHeld&&pose.State==CombatVisualState.Guard,character.character+" enlarged upper-body touch guards");
            yield return new WaitForEndOfFrame();CaptureNative("Hero-"+character.character+"-Guard");
            Contact("End",-190,screen,true);yield return null;Check(pose.State==CombatVisualState.Idle,character.character+" release returns to Idle");
        }
        yield return new WaitForSeconds(6.1f);
        controller.TryGrantEquipment(content.equipment.First(e=>e.id=="StrayDog"));yield return null;
        yield return new WaitForSeconds(.4f);
        var dog=Object.FindFirstObjectByType<CompanionView>();var dogData=controller.State.Companions.First();
        Check(dog!=null&&dog.State==CombatVisualState.Idle&&dog.body.GetComponent<UnityEngine.UI.Image>().sprite==dogData.combatAnimation.idle,"companion rear Idle beside player");
        Check(dogData.combatAnimation.guard==null&&dogData.combatAnimation.ready==null&&dogData.combatAnimation.dead==null,"companion has exactly Idle and Attack");
        controller.State.Player.ResetAttackCooldown();var center=scene.MonsterPosition;
        var guardScreen=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(scene.PlayerPosition));Contact("Begin",-191,guardScreen);
        var dogDeadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<dogDeadline&&dog.State!=CombatVisualState.Attack)yield return null;
        Check(dog.State==CombatVisualState.Attack&&controller.State.Player.IsGuarding,"real companion damage switches rear Attack while player guards");yield return new WaitForEndOfFrame();CaptureNative("Companion-Attack");Contact("End",-191,guardScreen,true);yield return new WaitForSeconds(.4f);
        var enemy=combat.Enemies[0];var enemyView=Object.FindFirstObjectByType<MonsterView>();var enemyPose=enemyView.GetComponentInChildren<CombatSpriteAnimator>();enemy.BeginNormalAttack();yield return null;
        Check(enemyPose.State==CombatVisualState.Ready,"monster windup uses Ready");yield return new WaitForEndOfFrame();CaptureNative("Enemy-Ready");
        var deadline=Time.realtimeSinceStartup+5;while(Time.realtimeSinceStartup<deadline&&enemyPose.State!=CombatVisualState.Attack)yield return null;
        Check(enemyPose.State==CombatVisualState.Attack,"monster hit switches Attack");yield return new WaitForEndOfFrame();CaptureNative("Enemy-Attack");yield return new WaitForSeconds(.4f);
        controller.State.Player.ResetAttackCooldown();enemy.TakeDamage(enemy.CurrentHp-1);combat.ExecuteAttack(new AttackSegment(center+Vector2.left*240,center+Vector2.right*240,40),false);
        Check(enemyPose.State==CombatVisualState.Dead&&enemyPose.GetComponent<UnityEngine.UI.Image>().sprite==source.combatAnimation.dead,"lethal damage switches Dead before fade");yield return new WaitForEndOfFrame();CaptureNative("Enemy-Dead");File.AppendAllText(log,"COMPLETE\n");
    }
    private static void CaptureNative(string name)
    {
        var raster=Object.FindFirstObjectByType<ArcadeRasterCamera>();if(raster?.Frame==null)return;
        var previous=RenderTexture.active;RenderTexture.active=raster.Frame;var texture=new Texture2D(raster.Frame.width,raster.Frame.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();
        Directory.CreateDirectory("Docs/Production/ArcadeRevision/Screenshots");File.WriteAllBytes("Docs/Production/ArcadeRevision/Screenshots/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);RenderTexture.active=previous;
    }
    private IEnumerator ClickUntil(string text)
    {
        for(var step=0;step<24;step++)
        {
            var active=expansion.choices.Where(c=>c.button.gameObject.activeInHierarchy).ToArray();
            var choice=active.FirstOrDefault(c=>c.title.text.Contains(text));
            if(choice!=null){if(!choice.button.interactable)throw new InvalidOperationException("Disabled choice: "+text);choice.button.onClick.Invoke();yield return new WaitForSeconds(.12f);yield break;}
            var next=active.FirstOrDefault(c=>c.title.text==GameText.Get("ui.event.continue"));
            if(next==null&&expansion.nextPage.gameObject.activeInHierarchy&&expansion.nextPage.interactable){expansion.nextPage.onClick.Invoke();yield return null;continue;}
            if(next==null)throw new InvalidOperationException("Missing choice: "+text+" / "+string.Join("|",active.Select(c=>c.title.text)));
            next.button.onClick.Invoke();yield return new WaitForSeconds(.12f);
        }
        throw new InvalidOperationException("Too many paragraphs before: "+text);
    }
    private IEnumerator DrainConclusion()
    {
        for(var i=0;i<20&&expansion.IsModalOpen;i++)
        {var next=expansion.choices.FirstOrDefault(c=>c.button.gameObject.activeInHierarchy&&c.title.text==GameText.Get("ui.event.continue"));if(next==null)yield break;next.button.onClick.Invoke();yield return new WaitForSeconds(.12f);}
    }
    private IEnumerator EventMatrix()
    {
        const string path="Logs/AshenVow-Live-Events.txt";File.WriteAllText(path,"Scoped live story and spellbook UI matrix, existing Unity instance.\n");
        controller=Object.FindFirstObjectByType<RunController>();combat=Object.FindFirstObjectByType<CombatController>();expansion=Object.FindFirstObjectByType<RunExpansionView>();var flow=Object.FindFirstObjectByType<GameFlowController>();
        RunSession.SelectedCharacter=combat.Content.characters.First(c=>c.character==CharacterKind.Warrior);flow.StartNewRun();yield return null;
        var run=controller.State;var content=combat.Content;
        void Check(bool ok,string name){File.AppendAllText(path,(ok?"PASS ":"FAIL ")+name+"\n");if(!ok)Debug.LogError("AshenVow event probe: "+name);}
        void Perk(string id){controller.TryGrantPerk(content.perks.Single(p=>p.id==id));}
        ShowEvent("MagicTeacherFirst");yield return ClickUntil("노인을 덮치려는");yield return ClickUntil("배워 보고");yield return ClickUntil("파이어볼트를 배운다");yield return DrainConclusion();
        Check(run.RuntimeFor(content.spells.Single(s=>s.id=="Ember"))!=null&&run.EventFlags.Contains("TeacherLesson1"),"rescue -> first lesson learns Firebolt and records lesson");
        var known=run.Spellbook.Count;ShowEvent("MagicTeacherReturn");yield return DrainConclusion();
        Check(expansion.modalNarrative.text.Contains("아직")||expansion.modalNarrative.text.Contains("책들을"),"insufficient growth uses different meeting dialogue");
        yield return ClickUntil("떠난다");yield return DrainConclusion();Check(run.Spellbook.Count==known&&!run.EventFlags.Contains("TeacherLesson2"),"missed lesson grants no spell or lesson flag");
        Perk("ManaVessel");run.SpellCasts=3;ShowEvent("MagicTeacherReturn");yield return ClickUntil("더 배워");yield return ClickUntil("아주 큰 마법");
        Check(expansion.modalNarrative.text.Contains("자질"),"premature advanced request receives refusal");yield return ClickUntil("가능한 다른 수업");yield return ClickUntil("불을 한꺼번에");
        if(run.RuntimeFor(content.spells.Single(s=>s.id=="FireBloom"))==null)yield return ClickUntil("화염폭발");yield return DrainConclusion();
        Check(run.EventFlags.Contains("TeacherLesson2"),"grown reunion provides another lesson");
        run.LearnSpell(content.spells.Single(s=>s.id=="FrostRing"),true);Perk("ManaVessel");run.SpellCasts=12;ShowEvent("MagicTeacherAdvanced");yield return ClickUntil("번개를 한곳");
        Check(expansion.modalNarrative.text.Contains("자질"),"unsupported advanced element refuses without consuming other choice");yield return ClickUntil("다른 수업");var prepared=run.Slot(content.spells.Single(s=>s.id=="Meteor").defaultSlot);yield return ClickUntil("큰 불덩이");
        if(run.RuntimeFor(content.spells.Single(s=>s.id=="Meteor"))==null){expansion.choices[1].button.onClick.Invoke();yield return new WaitForSeconds(.12f);}yield return DrainConclusion();
        Check(run.RuntimeFor(content.spells.Single(s=>s.id=="Meteor"))!=null&&run.RuntimeFor(content.spells.Single(s=>s.id=="Ember"))!=null&&run.EventFlags.Contains("TeacherMastered"),"advanced lesson ADDS Meteor and preserves Firebolt");
        Check(run.Slot(content.spells.Single(s=>s.id=="Meteor").defaultSlot)==prepared,"learning into spellbook preserves currently prepared spell");
        expansion.ShowSpellbook();yield return ClickUntil("파이어볼트");Check(run.Slot(content.spells.Single(s=>s.id=="Ember").defaultSlot).id=="Ember","spellbook re-equips retained Firebolt");
        var meat=run.Player.Data.startingSupplies.First().supply;var before=run.SupplyCount(meat);var hp=run.Player.CurrentHp;ShowEvent("StrayDog");yield return ClickUntil("목줄을 풀어");yield return ClickUntil("먹을 것을");yield return DrainConclusion();
        Check(run.SupplyCount(meat)==before-1&&run.Companions.Any()&&Mathf.Approximately(hp,run.Player.CurrentHp),"dog joins after meat payment without healing player");
        Check(!expansion.IsModalOpen,"story conclusion returns to navigation");File.AppendAllText(path,"COMPLETE\n");
    }
    private IEnumerator InputMatrix()
    {
        const string path="Logs/AshenVow-Live-Input.txt";File.WriteAllText(path,"Live CombatInputController contact lifecycle in existing Unity runtime; physical multitouch remains device-only verification.\n");
        var input=Object.FindFirstObjectByType<CombatInputController>();var scene=Object.FindFirstObjectByType<CombatSceneView>();
        Vector2 Screen(Vector2 logical)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(logical));
        void Call(string method,params object[] args)=>typeof(CombatInputController).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(input,args);
        void Check(bool ok,string name){File.AppendAllText(path,(ok?"PASS ":"FAIL ")+name+"\n");if(!ok)Debug.LogError("AshenVow input probe: "+name);}
        var center=scene.MonsterPosition;var from=center+Vector2.left*240;var to=center+Vector2.right*240;var hp=combat.Enemies[0].CurrentHp;
        Call("Begin",-101,Screen(from));
        for(var i=1;i<=4;i++){Call("Move",-101,Screen(Vector2.Lerp(from,to,i/4f)));yield return null;}
        Call("End",-101,Screen(to),false);yield return null;Check(combat.Enemies[0].CurrentHp<hp,"normal swipe damages target");
        yield return new WaitForSeconds(1);
        var player=Screen(scene.PlayerPosition);Call("Begin",-110,player);yield return new WaitForSeconds(.6f);
        var before=controller.State.Player.CurrentMp;Call("Begin",-111,player);Call("End",-111,player+Vector2.right*150,false);yield return null;
        Check(controller.State.Player.CurrentMp<before-7,"overlapping guard + right release casts Firebolt");Check(input.IsGuardHeld,"first contact remains held after second lifted");
        before=controller.State.Player.CurrentMp;var rejected=combat.TryCastSpell(SwipeDirection.Right,1);Check(!rejected&&Mathf.Abs(before-controller.State.Player.CurrentMp)<.01,"cooldown rejection keeps MP");
        yield return new WaitForSeconds(.9f);Call("Begin",-112,player);before=controller.State.Player.CurrentMp;Call("End",-112,player+Vector2.up*150,false);yield return null;
        Check(controller.State.Player.CurrentMp<before-7&&input.IsGuardHeld,"repress second contact casts ice without releasing guard");
        controller.State.Player.TrySpendMp(controller.State.Player.CurrentMp-2);input.ResetCasting();yield return new WaitForSeconds(.6f);
        Call("Begin",-113,player);before=controller.State.Player.CurrentMp;Call("End",-113,player+Vector2.left*150,false);yield return null;
        Check(controller.State.Player.CurrentMp>=before-.01&&controller.State.Player.CurrentMp<4,"insufficient MP rejects mist without payment");
        Call("End",-110,player,false);yield return null;Check(!input.IsGuardHeld&&!controller.State.Player.IsGuarding,"last contact lift releases guard");
        before=controller.State.Player.CurrentMp;Call("Begin",-114,player);yield return new WaitForSeconds(2);Check(controller.State.Player.CurrentMp-before>1.5f,"Wizard sustained guard restores MP");Call("End",-114,player,true);
        Check(!input.IsGuardHeld,"cancel releases final contact");File.AppendAllText(path,"COMPLETE\n");
    }
    private IEnumerator SpellMatrix()
    {
        const string path="Logs/AshenVow-Live-Spells.txt";File.WriteAllText(path,"Scoped live Unity runtime spell matrix. Practice enemy is a runtime clone; production balance is unchanged.\n");
        foreach(var spell in combat.Content.spells.ToArray())
        {
            combat.ResetEncounter();controller.State.LearnSpell(spell,true);controller.State.EquipKnownSpell(spell);combat.PrepareEncounter(controller.State,new[]{practice},1);combat.BeginPreparedCombat();expansion.HideModal();
            yield return null;var before=controller.State.Player.CurrentMp;var success=combat.TryCastSpell(spell.defaultSlot,1);
            var spent=before-controller.State.Player.CurrentMp;var ok=success&&Mathf.Abs(spent-spell.mpCost)<.05f;
            File.AppendAllText(path,$"{(ok?"PASS":"FAIL")} {spell.displayName}: cast={success}, MP={spent:0.##}/{spell.mpCost:0.##}, enemy HP={combat.Enemies[0].CurrentHp:0.##}\n");
            if(!ok)Debug.LogError("AshenVow spell probe failed: "+spell.displayName);
            yield return new WaitForSeconds(1);
            if(spell.id.StartsWith("Enchant"))
            {
                var scene=Object.FindFirstObjectByType<CombatSceneView>();var center=scene.MonsterPosition;
                var hp=combat.Enemies[0].CurrentHp;combat.ExecuteAttack(new AttackSegment(center+Vector2.left*160,center+Vector2.right*160,40),false);
                yield return null;File.AppendAllText(path,$"  enchanted weapon hit={hp-combat.Enemies[0].CurrentHp:0.##}, poison={combat.Enemies[0].PoisonStacks}, slow={combat.Enemies[0].SlowRemaining:0.##}\n");
            }
            if(spell.id=="PoisonMist")File.AppendAllText(path,$"  mist poison={combat.Enemies[0].PoisonStacks}, slow={combat.Enemies[0].SlowRemaining:0.##}\n");
        }
        SetupForManual();File.AppendAllText(path,"COMPLETE: 16 casts checked; scene remains ready for manual swipe/guard/MP checks.\n");
    }
    public void SetupForManual()
    {
        if(combat==null)Setup();
        foreach(var id in new[]{"EnergyBolt","Ember","EnchantFire","PoisonMist"})controller.State.LearnSpell(combat.Content.spells.First(s=>s.id==id),true);
        foreach(var id in new[]{"EnergyBolt","Ember","EnchantFire","PoisonMist"})controller.State.EquipKnownSpell(combat.Content.spells.First(s=>s.id==id));
        combat.ResetEncounter();combat.PrepareEncounter(controller.State,new[]{practice},1);combat.BeginPreparedCombat();expansion.HideModal();
    }
    public static void ShowEvent(string id)
    {
        var flow=Object.FindFirstObjectByType<GameFlowController>();var dungeon=Object.FindFirstObjectByType<DungeonController>();var combat=Object.FindFirstObjectByType<CombatController>();var expansion=Object.FindFirstObjectByType<RunExpansionView>();
        combat.ResetEncounter();dungeon.HideNavigation();expansion.HideModal();
        var story=combat.Content.events.First(s=>s.id==id);
        typeof(GameFlowController).GetMethod("ShowEventSequence",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(flow,new object[]{dungeon.State.Player.CurrentRoom,story});
    }
    private void OnDestroy(){Application.runInBackground=previousBackground;if(practice!=null)Object.Destroy(practice);}
}
#endif
