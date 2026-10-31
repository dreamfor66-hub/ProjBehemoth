#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using Object=UnityEngine.Object;

// Explicitly requested, scoped art/input review in the user's one existing Editor.
public sealed class AngularWorldLiveProbe : MonoBehaviour
{
    const string Folder="Docs/Production/AngularWorld-2026-10-26/";
    [Serializable] sealed class Entry{public string baseline,id,name;public int floor;public string[] normalLabels;}
    [Serializable] sealed class Roster{public Entry[] items;}
    bool wasBackground;
    void Awake(){wasBackground=Application.runInBackground;Application.runInBackground=true;Directory.CreateDirectory(Folder+"Screenshots");}
    void OnDestroy(){Application.runInBackground=wasBackground;}
    void Check(bool ok,string text){File.AppendAllText(Folder+"Live.txt",(ok?"PASS ":"FAIL ")+text+"\n");}
    Entry[] Entries()=>JsonUtility.FromJson<Roster>("{\"items\":"+File.ReadAllText(Folder+"Roster.json").TrimStart('\uFEFF')+"}").items;
    public void Run(string mode){StartCoroutine(mode=="selection"?Selection():mode=="visual"?Visual():World());}
    static Vector2 EnemyCenter(CombatController combat)
    {
        var list=(IList)typeof(CombatController).GetField("activeMonsters",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(combat);
        return (Vector2)list[0].GetType().GetProperty("Position").GetValue(list[0]);
    }
    IEnumerator Visual()
    {
        File.AppendAllText(Folder+"Live.txt","\nVISUAL revised layout + hero pointer-input checks\n");
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Angular practice").AddComponent<OctoberReviewLiveProbe>();
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        {
            practice.Setup(kind,false,"Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_거미.asset");yield return null;
            var combat=Object.FindFirstObjectByType<CombatController>();var enemy=combat.Enemies[0];
            var scene=Object.FindFirstObjectByType<CombatSceneView>();var input=Object.FindFirstObjectByType<CombatInputController>();
            var player=Object.FindFirstObjectByType<PlayerView>();
            Check(player.presentation.preserveIllustrationColors&&combat.Player.Data.combatAnimation.illustratedMotion,kind+" native rear artwork");
            yield return Capture("Hero-"+kind+"-Idle");
            var center=EnemyCenter(combat);var hp=enemy.CurrentHp;combat.Player.ResetAttackCooldown();
            Vector2 ScreenAt(Vector2 p)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(p));
            void Contact(string method,params object[] args)=>typeof(CombatInputController).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,args);
            var from=ScreenAt(center+Vector2.left*110);var to=ScreenAt(center+Vector2.right*110);
            Contact("Begin",-1,from);Contact("Move",-1,to);Contact("End",-1,to,false);yield return null;
            Check(enemy.CurrentHp<hp,kind+" production pointer swipe hits relocated target");
            yield return Capture("Hero-"+kind+"-Attack");
            yield return new WaitForSeconds(.4f);
            var guardFrom=ScreenAt(scene.PlayerPosition+Vector2.up*90);var guardTo=ScreenAt(scene.PlayerPosition+Vector2.down*55);
            Contact("Begin",340,guardFrom);Contact("Move",340,guardTo);yield return new WaitForSeconds(.24f);
            Check(combat.Player.IsGuarding&&player.GetComponent<CombatSpriteAnimator>().State==CombatVisualState.Guard,kind+" guard artwork responds");
            yield return Capture("Hero-"+kind+"-Guard");Contact("End",340,guardTo,false);
        }
        practice.Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_거미.asset");yield return null;
        var battle=Object.FindFirstObjectByType<CombatController>();var target=battle.Enemies[0];
        var first=target.NormalAttack;target.BeginNormalAttack();yield return new WaitForSeconds(first.windupDuration+.35f);
        Check(target.State==MonsterState.Idle&&target.NormalAttack!=first,"controller resolves first sequence action and advances");
        var second=target.NormalAttack;target.BeginNormalAttack();yield return new WaitForSeconds(second.windupDuration+.35f);
        Check(target.State==MonsterState.Idle&&target.NormalAttack==first,"controller resolves slower second action and wraps");
        target.StartCharge(EnemyCenter(battle));yield return null;
        Check(target.WeakPoints.All(p=>Vector2.Distance(p.HitShape.Center,EnemyCenter(battle))<230),"charge weak points follow relocated body");
        yield return Capture("Grounded-WeakPoints");
        File.AppendAllText(Folder+"Live.txt","VISUAL COMPLETE\n");
    }
    IEnumerator Capture(string name)
    {
        yield return null;yield return new WaitForEndOfFrame();
        var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"Screenshots/"+name+".png",texture.EncodeToPNG());Object.Destroy(texture);
    }
    IEnumerator Selection()
    {
        File.AppendAllText(Folder+"Live.txt","\nSELECTION live saved OutGameScene\n");
        var menu=Object.FindFirstObjectByType<OutGameMenuView>();menu.ShowCharacters();
        Check(menu.characterPage.name=="SelectionPage","page named SelectionPage");
        for(var i=0;i<menu.selection.content.characters.Count;i++)
        {
            menu.selection.characterButtons[i].onClick.Invoke();yield return null;
            var data=menu.selection.content.characters[i];
            Check(menu.selection.SelectedIndex==i&&menu.selection.characterButtons.Count(b=>b.gameObject.activeInHierarchy)==6,data.character+" original six-choice interaction");
            Check(menu.selection.selectionSplash.sprite==data.selectionIllustration&&data.selectionIllustration.texture.width==1536,data.character+" high-resolution approved-art crop");
            yield return Capture("Selection-"+data.character);
        }
        menu.selection.SetDetailsVisible(true);yield return Capture("Selection-Details");menu.selection.SetDetailsVisible(false);
        menu.back.onClick.Invoke();Check(menu.titlePage.activeSelf,"original Back returns to Title");
        menu.newGame.onClick.Invoke();Check(menu.characterPage.activeSelf,"original New Game opens selection");menu.selection.Select(0);
        File.AppendAllText(Folder+"Live.txt","SELECTION COMPLETE\n");
    }
    static DungeonRunState State(int mask,FacingDirection facing)
    {
        var graph=new DungeonGraph();var delta=DirectionUtility.Offset(facing);
        var origin=graph.AddRoom(new DungeonPosition(-delta.X,-delta.Y));var room=graph.AddRoom(new DungeonPosition(0,0));graph.Connect(origin,room);
        for(var i=0;i<3;i++)
        {
            var offset=DirectionUtility.Offset(DirectionUtility.ToAbsolute(facing,(RelativeDirection)i));
            var neighbor=graph.AddRoom(offset);if((mask&(1<<i))!=0)graph.Connect(room,neighbor);
        }
        var state=new DungeonRunState(1,graph);state.Player.MoveTo(room);return state;
    }
    IEnumerator World()
    {
        File.AppendAllText(Folder+"Live.txt","\nWORLD live saved MainGameScene\n");
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Angular scoped practice").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior,false);
        var combat=Object.FindFirstObjectByType<CombatController>();var run=Object.FindFirstObjectByType<RunController>();
        var backdrop=Object.FindFirstObjectByType<StageBackdropView>();var dungeon=Object.FindFirstObjectByType<DungeonController>();
        var scene=Object.FindFirstObjectByType<CombatSceneView>();
        foreach(FacingDirection facing in Enum.GetValues(typeof(FacingDirection)))for(var mask=0;mask<8;mask++)
        {
            var state=State(mask,facing);backdrop.Refresh(1,state);
            Check(StageBackdropView.ConnectionMask(state)==mask&&backdrop.doorLayers.Select((l,i)=>l.enabled==((mask&(1<<i))!=0)).All(v=>v),$"door mask={mask}, facing={facing}; adjacent unconnected rooms stay walls");
        }
        foreach(var entry in Entries())
        {
            practice.Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/"+entry.baseline+".asset");
            combat=Object.FindFirstObjectByType<CombatController>();var enemy=combat.Enemies[0];
            var data=enemy.Data;
            Check(data.displayName==entry.name&&data.portrait!=null&&data.combatAnimation.illustratedMotion,entry.id+" artwork + localized identity");
            if(data.normalAttackSequence.Count>0)
            {
                var runtime=new MonsterRuntime(data);var expected=data.normalAttackSequence.ToArray();
                for(var index=0;index<expected.Length*2;index++)
                {
                    Check(runtime.NormalAttack==expected[index%expected.Length],entry.id+" sequence step "+index);
                    runtime.BeginNormalAttack();var before=runtime.NormalAttack;
                    var guard=0;while(runtime.State==MonsterState.Attacking&&guard++<20){runtime.Tick(runtime.StateTimer+.01f,Vector2.zero);runtime.AdvanceNormalAttack();}
                    Check(runtime.State==MonsterState.Idle&&guard<20,entry.id+" normal action resolves");
                    Check(Mathf.Approximately(before.damage,expected[0].damage),entry.id+" original per-action damage budget");
                }
            }
            combat.InputPaused=true;run.State.CurrentAct=Mathf.Max(1,entry.floor);backdrop.enabled=false;
            backdrop.Refresh(run.State.CurrentAct,State(5,FacingDirection.North));
            yield return Capture("Monster-"+entry.id);
            if(data.HasChargeForReview())
            {
                enemy.StartCharge(EnemyCenter(combat));yield return null;
                var view=Object.FindFirstObjectByType<MonsterView>();view.Bind(enemy);
                Check(enemy.State==MonsterState.Charging&&enemy.WeakPoints.Count>0,entry.id+" authored charge weak points");
                if(data.normalAttackSequence.Count>0)yield return Capture("Ready-"+entry.id);
            }
            combat.InputPaused=false;
        }
        combat.ResetEncounter();dungeon.HideNavigation();
        for(var floor=1;floor<=3;floor++)
        {
            run.State.CurrentAct=floor;backdrop.Refresh(floor,State(0,FacingDirection.North));yield return Capture("B"+floor+"-Walls");
            backdrop.Refresh(floor,State(7,FacingDirection.North));yield return Capture("B"+floor+"-ThreeDoors");
            backdrop.Refresh(floor,State(5,FacingDirection.North));yield return Capture("B"+floor+"-ForwardRight");
        }
        backdrop.enabled=true;Object.FindFirstObjectByType<GameFlowController>().StartNewRun();
        File.AppendAllText(Folder+"Live.txt","WORLD COMPLETE. Full balance/DPS regression and real-device input unrun.\n");
    }
}
internal static class AngularReviewMonsterExtension
{
    public static bool HasChargeForReview(this MonsterData data)=>data.attacks.Any(a=>a!=null&&a.type==MonsterAttackType.Charge&&a.chargeWeakPoints.Count>0&&string.IsNullOrEmpty(a.requiredSupportId));
}
#endif
