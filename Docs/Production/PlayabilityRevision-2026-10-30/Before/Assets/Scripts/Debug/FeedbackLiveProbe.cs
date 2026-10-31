#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Object=UnityEngine.Object;

// Only the mechanics and UI changed by the October 24 feedback.
public sealed class FeedbackLiveProbe:MonoBehaviour
{
    const string Log="Logs/Feedback224-Live.txt";
    const string Screens="Docs/Production/Feedback20261024/Screenshots/";
    bool background; public void Run(){background=Application.runInBackground;Application.runInBackground=true;StartCoroutine(Matrix());}
    void Check(bool ok,string name){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+name+"\n");if(!ok)Debug.LogError("Feedback QA: "+name);}
    static T Ref<T>(object owner,string field)where T:class=>owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)?.GetValue(owner)as T;
    public static void Capture(string name){var frame=Object.FindFirstObjectByType<ArcadeRasterCamera>()?.Frame;if(frame==null)return;var prev=RenderTexture.active;RenderTexture.active=frame;var t=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,t.width,t.height),0,0);t.Apply();Directory.CreateDirectory(Screens);File.WriteAllBytes(Screens+name+".png",t.EncodeToPNG());Object.DestroyImmediate(t);RenderTexture.active=prev;}
    IEnumerator Matrix(){File.WriteAllText(Log,"Scoped feedback checks in the existing live Unity Editor.\n");var flow=Object.FindFirstObjectByType<GameFlowController>();var run=Object.FindFirstObjectByType<RunController>();var combat=Object.FindFirstObjectByType<CombatController>();var expansion=Object.FindFirstObjectByType<RunExpansionView>();var scene=Object.FindFirstObjectByType<CombatSceneView>();var input=Object.FindFirstObjectByType<CombatInputController>();var setup=Object.FindFirstObjectByType<AshenVowLiveProbe>()??new GameObject("Feedback practice setup").AddComponent<AshenVowLiveProbe>();
        void Character(CharacterKind kind){RunSession.SelectedCharacter=combat.Content.characters.First(c=>c.character==kind);flow.StartNewRun();setup.Setup();}
        void Contact(string name,params object[] args)=>typeof(CombatInputController).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,args);
        Vector2 ToScreen(Vector2 point)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(point));
        Character(CharacterKind.Rogue);yield return null;var awareness=run.State.Awareness;var count=awareness.CompletedActions;var pos=ToScreen(scene.PlayerPosition+Vector2.up*100);
        for(var i=0;i<3;i++){Contact("Begin",-220,pos);yield return new WaitForSeconds(.15f);Contact("End",-220,pos,false);yield return null;}
        Check(awareness.IsConcealed&&awareness.CompletedActions==count,"Rogue three guard hold/release cycles keep concealment and detection count");
        var center=scene.MonsterPosition;var hit=new AttackSegment(center+Vector2.left*240,center+Vector2.right*240,40);combat.ExecuteAttack(hit,false);yield return null;Check(awareness.CompletedActions==count+1,"weapon action still advances detection");
        Character(CharacterKind.Barbarian);yield return null;var player=run.State.Player;var before=player.CurrentCooldown;
        combat.ExecuteAttack(hit,false);player.ResetAttackCooldown();combat.ExecuteAttack(hit,false);player.ResetAttackCooldown();combat.ExecuteAttack(hit,true);yield return null;
        Check(player.Buffs.Active.Any(b=>b.Data.name=="Rage"),"Barbarian two normals followed by charge enter Rage");Check(player.CurrentCooldown<before*.65f,"Rage has visibly shorter attack interval");Check(!player.StatusText.Contains(".0"),"class resource labels use integers");
        Character(CharacterKind.Wizard);yield return null;var enemy=combat.Enemies[0];var view=Object.FindFirstObjectByType<MonsterView>();var body=Ref<Image>(view,"body");var radius=(-enemy.Data.FootprintY)*enemy.VisualScale;
        Check(body.transform!=view.transform&&body.rectTransform.localScale.x>1.5f&&view.transform.localScale==Vector3.one,"enemy art enlarged in a child; collision root remains unscaled");
        enemy.BeginNormalAttack();yield return null;Check(Ref<TextMeshProUGUI>(view,"chargeTimeLabel").enabled&&Ref<TextMeshProUGUI>(view,"attackNameLabel").enabled,"normal attack windup displays name and countdown");Check(Mathf.Approximately(radius,(-enemy.Data.FootprintY)*enemy.VisualScale),"render changes preserve body hit radius");yield return new WaitForEndOfFrame();Capture("Combat-Normal-Ready");
        var mana=Object.FindFirstObjectByType<ManaHudView>();var hud=Object.FindFirstObjectByType<CombatHudView>();var shield=Ref<Image>(hud,"shield");var hp=Ref<Image>(hud,"playerHp");Check(((RectTransform)shield.transform.parent).rect.height>((RectTransform)shield.transform.parent).rect.width*2,"guard gauge vertical");Check(Mathf.Abs(mana.gauge.transform.position.x-hp.transform.parent.position.x)<2,"HP and MP share lower-left column");
        hud.Bind(player,enemy,PointerGestureState.Charged,1,Vector2.zero);var one=Ref<ArcGaugeGraphic>(hud,"chargeGaugeFill").color;hud.Bind(player,enemy,PointerGestureState.SecondCharged,1,Vector2.zero,1);Check(Ref<ArcGaugeGraphic>(hud,"chargeGaugeSecondFill").color!=one,"charge ready stages use distinct colors");
        combat.ResetEncounter();Object.FindFirstObjectByType<DungeonController>().HideNavigation();var room=Object.FindFirstObjectByType<RoomEventView>(FindObjectsInactive.Include);var routine=room.ShowTimed("오래된 샘에서 물을 마셨다.\n확인을 눌러 계속한다.",combat.Content.events[0].illustration);StartCoroutine(routine);yield return new WaitForSeconds(1.6f);Check(Ref<GameObject>(room,"panel").activeSelf&&Ref<Button>(room,"yesButton").gameObject.activeInHierarchy,"legacy event result stays open until Confirm");yield return new WaitForEndOfFrame();Capture("Event-Confirm");Ref<Button>(room,"yesButton").onClick.Invoke();yield return null;Check(!Ref<GameObject>(room,"panel").activeSelf,"Confirm closes legacy result");
        var relic=combat.Content.equipment.First(e=>!e.IsPotion);expansion.ShowModal("갈림길의 순례자","어두운 통로에서 순례자를 만났다.",new[]{new PresentationChoice{Title="서약을 받는다",Description="유물 · "+relic.displayName,Requirements="30G",PreviewItem=relic,Selected=()=>{}}},combat.Content.events[0].illustration);yield return null;
        var choice=expansion.choices[0];Check(choice.title.text.StartsWith("> ")&&choice.description.text.Contains("조건"),"story action and requirements are separated");var tip=expansion.tooltip;choice.button.GetComponent<ChoicePreview>().OnPointerEnter(new PointerEventData(EventSystem.current){position=new Vector2(Screen.width*.35f,Screen.height*.35f)});yield return null;var tipPanel=Ref<GameObject>(tip,"panel");Check(tipPanel.activeInHierarchy&&tipPanel.transform.GetSiblingIndex()==tipPanel.transform.parent.childCount-1,"offered relic hover is visible above modal");yield return new WaitForEndOfFrame();Capture("Event-Relic-Preview");tip.Hide();expansion.HideModal();
        Character(CharacterKind.Monk);yield return null;var monk=run.State.Player;var technique=monk.Data.attackBuffs.First(b=>!b.requiresUnlock&&b.attackPattern.Length>0);foreach(var direction in technique.attackPattern)monk.PrepareAttack(direction,false);yield return null;
        Check(monk.StatusText.Contains(technique.displayName)&&!monk.StatusText.Contains("차지"),"Monk shows direction chain and active technique without charge instructions");yield return new WaitForEndOfFrame();Capture("Monk-Technique");
        Character(CharacterKind.Warrior);yield return null;Check(!run.State.Player.StatusText.Contains("빈 주문 슬롯"),"no empty spell slot instructions");
        var knight=UnityEditor.AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data/Combat/Monsters"}).Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First(m=>m.name.Contains("Skeleton"));setup.Setup(knight);yield return null;enemy=combat.Enemies[0];enemy.StartCharge(scene.MonsterPosition);yield return null;
        Check(Object.FindObjectsByType<WeakPointReticle>(FindObjectsSortMode.None).Any(r=>r.gameObject.activeInHierarchy),"charged attack exposes high-contrast weak point reticles");yield return new WaitForEndOfFrame();Capture("Combat-Weak-Points");
        run.TryGrantEquipment(combat.Content.equipment.First(e=>e.id=="StrayDog"));yield return new WaitForSeconds(.4f);var dog=Object.FindFirstObjectByType<CompanionView>();Check(dog!=null&&dog.body.sizeDelta.x==288,"rear companion uses the larger matching-density sprite");yield return new WaitForEndOfFrame();Capture("Companion-Scale");
        var rat=UnityEditor.AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data/Combat/Monsters"}).Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<MonsterData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).First(m=>m.name.Contains("TunnelRat"));setup.Setup(rat);yield return null;enemy=combat.Enemies[0];enemy.TakeDamage(enemy.CurrentHp);view=Object.FindFirstObjectByType<MonsterView>();view.Bind(enemy);Check(Ref<Image>(view,"body").sprite==rat.combatAnimation.dead,"TunnelRat death uses corrected isolated atlas rectangle");yield return new WaitForEndOfFrame();Capture("TunnelRat-Dead");
        File.AppendAllText(Log,"COMPLETE\n");Application.runInBackground=background;
    }
}
#endif



