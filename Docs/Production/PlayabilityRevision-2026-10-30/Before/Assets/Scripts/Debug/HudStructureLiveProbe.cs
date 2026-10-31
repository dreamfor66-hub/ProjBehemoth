#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class HudStructureLiveProbe : MonoBehaviour
{
    const string Folder="Docs/Production/HudStructure-2026-10-29/";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    CombatController combat;CombatSceneView scene;CombatInputController input;RunController run;
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    void Check(bool ok,string message){File.AppendAllText(Folder+"Live.txt",(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)Debug.LogError("HUD structure: "+message);}
    T Ref<T>(object owner,string field)where T:class=>owner.GetType().GetField(field,Private)?.GetValue(owner) as T;
    void Contact(string action,params object[] args)=>typeof(CombatInputController).GetMethod(action,Private).Invoke(input,args);
    Vector2 ScreenAt(Vector2 p)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(p));
    IEnumerator Capture(string name){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Folder+name+".png");yield return null;}
    public void Run()=>StartCoroutine(Matrix());
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Live.txt","Scoped existing Unity runtime: HUD hierarchy, icon pooling, pointer cooldown and guard feedback. No balance suite.\n");
        var practice=new GameObject("HUD practice clone").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior,true,"Assets/Data/Combat/Monsters/Data_Monster_B1_Minion_Skeleton.asset");
        combat=Object.FindFirstObjectByType<CombatController>();scene=Object.FindFirstObjectByType<CombatSceneView>();input=Object.FindFirstObjectByType<CombatInputController>();run=Object.FindFirstObjectByType<RunController>();
        yield return new WaitForSeconds(.6f);
        var hud=Object.FindFirstObjectByType<CombatHudView>();var structure=Object.FindFirstObjectByType<HudSceneStructure>();var mana=Object.FindFirstObjectByType<ManaHudView>();
        Check(structure!=null&&structure.revision>=2,"saved semantic HUD hierarchy is installed");
        Check(Ref<Image>(hud,"playerHp").transform.parent.parent==structure.playerStatusGauges&&mana.gauge.transform.parent==structure.playerStatusGauges,"HP and mana share PlayerStatusGauges");
        Check(hud.guardGauge.transform.IsChildOf(structure.playerGuard)&&hud.guardGauge.rectTransform.rect.width>hud.guardGauge.rectTransform.rect.height*5,"guard is a horizontal gauge under PlayerGuard");
        Check(hud.attackRecovery.transform.IsChildOf(structure.pointerFeedback),"attack cooldown and charge feedback share PointerFeedback");
        var maxHp=combat.Player.MaxHp;var maxMp=combat.Player.MaxMp;var cooldown=combat.Player.CurrentCooldown;
        var quick=Object.FindFirstObjectByType<QuickInventoryView>();quick.Refresh();
        var original=run.State.EquippedItems.ToArray();var cosmetic=new List<EquipmentData>();
        for(var n=0;n<23;n++)
        {
            var item=ScriptableObject.CreateInstance<EquipmentData>();item.id="hud-pool-"+n;item.displayName="Pool "+n;item.description="Scoped UI test";item.icon=original[0].icon;item.scope=RelicScope.Shared;
            cosmetic.Add(item);run.State.TryEquip(item);
        }
        quick.Refresh();yield return null;Canvas.ForceUpdateCanvases();
        Check(quick.relics.Count==original.Length+23&&quick.relics.Count>18,"quick relic layout grows past the previous 18-slot limit");
        Check(quick.relicPool.content.GetComponentsInChildren<RunHudIconView>().Length==quick.relics.Count,"only rented icons exist in active layout");
        var first=(RectTransform)quick.relics[0].transform;var nextRow=(RectTransform)quick.relics[18].transform;
        Check(nextRow.anchoredPosition.y<first.anchoredPosition.y-60,"layout group wraps overflow to another row");
        var created=quick.relicPool.CreatedCount;var reused=quick.relics.Last().GetInstanceID();
        quick.relics.Last().OnPointerEnter(new PointerEventData(EventSystem.current));
        Check(quick.tooltip.IsVisible&&quick.tooltip.VisibleTitle==cosmetic.Last().displayName,"pooled relic hover uses current item description");
        foreach(var item in cosmetic)run.State.TryRemoveEquipment(item);quick.Refresh();yield return null;
        Check(quick.relics.Count==original.Length&&quick.relicPool.AvailableCount==23&&!quick.tooltip.IsVisible,"removed relics return to inactive pool and dismiss tooltip");
        foreach(var item in cosmetic)run.State.TryEquip(item);quick.Refresh();yield return null;
        Check(quick.relicPool.CreatedCount==created&&quick.relics.Any(v=>v.GetInstanceID()==reused),"growing again reuses returned instances without new allocation");
        foreach(var item in cosmetic)run.State.TryRemoveEquipment(item);quick.Refresh();
        var heal=run.Equipment.First(e=>e.id=="HealingPotion");
        foreach(var item in run.State.Potions.ToArray())run.State.TryRemoveEquipment(item);
        run.State.TryStorePotion(heal);quick.Refresh();yield return null;
        var potionCreated=quick.potionPool.CreatedCount;
        quick.potions[0].GetComponent<Button>().onClick.Invoke();yield return null;
        Check(quick.potionActions.IsOpen&&quick.potionActions.Selected==heal,"pooled potion click opens the existing action menu");
        quick.potionActions.Discard();yield return null;quick.Refresh();
        Check(quick.potions.Count==0&&quick.potionPool.AvailableCount>=1,"discard returns potion icon and clears the row");
        run.State.TryStorePotion(heal);quick.Refresh();yield return null;
        Check(quick.potionPool.CreatedCount==potionCreated,"potion reacquisition reuses its pool");
        var drawers=Object.FindFirstObjectByType<ArcadeHudController>();drawers.inventoryButton.onClick.Invoke();yield return null;
        var inventory=Object.FindFirstObjectByType<RunHudView>();inventory.Bind(null,run.State);yield return null;
        Check(drawers.inventoryPanel.activeInHierarchy&&Ref<RectTransform>(inventory,"relicGrid").GetComponent<RunHudIconPool>().Active.Count==run.State.EquippedItems.Count,"inventory drawer preserves bindings and uses the shared pool");
        drawers.Close();yield return null;
        Check(!combat.InputPaused,"closing inventory restores combat input");
        var anchor=hud.attackRecovery.GetComponent<AttackCooldownAnchor>();anchor.enabled=false;
        var a=ScreenAt(new Vector2(-300,100));var b=ScreenAt(new Vector2(300,60));
        anchor.FollowScreenPoint(a);var p0=hud.attackRecovery.rectTransform.anchoredPosition;
        anchor.FollowScreenPoint(b);var p1=hud.attackRecovery.rectTransform.anchoredPosition;
        Check(Vector2.Distance(p0,p1)>500,"pointer movement moves cooldown across the combat field");
        anchor.FollowScreenPoint(new Vector2(-10000,-10000));var rect=hud.attackRecovery.rectTransform;var area=((RectTransform)rect.parent).rect;
        Check(rect.anchoredPosition.x>=area.xMin+rect.rect.width*.5f&&rect.anchoredPosition.y>=area.yMin+rect.rect.height*.5f,"cooldown remains visible at screen edges");
        anchor.FollowScreenPoint(a);combat.Player.StartAttackCooldown();yield return null;
        Check(hud.attackRecovery.IsRecovering&&hud.attackRecovery.Progress<1,"moving sword displays the actual shared recovery timer");
        yield return new WaitForSeconds(combat.Player.CurrentCooldown+.08f);
        Check(!hud.attackRecovery.IsRecovering&&hud.attackRecovery.Progress==1,"recovery reaches ready and permits the next attack");
        // Keep one hand held across all lift/repress cycles; never cancel all contacts between cycles.
        var guardStart=new Vector2(0,-310);var guardEnd=new Vector2(0,-438);var offense=new Vector2(-300,100);
        combat.Player.ResetAttackCooldown();Contact("Begin",501,ScreenAt(guardStart));Contact("Move",501,ScreenAt(guardEnd));
        Check(combat.Player.IsGuarding,"production downward player gesture starts guard");
        var overlap=true;
        for(var cycle=0;cycle<3;cycle++)
        {
            Contact("Begin",502,ScreenAt(offense));Contact("Move",502,ScreenAt(offense));
            overlap&=input.TryGetAttackPointer(out var point)&&Vector2.Distance(point,offense)<1&&combat.Player.IsGuarding;
            Contact("End",501,ScreenAt(guardEnd),true);Contact("RefreshState");
            overlap&=input.TryGetAttackPointer(out point)&&Vector2.Distance(point,offense)<1;
            Contact("Begin",501,ScreenAt(guardStart));Contact("Move",501,ScreenAt(guardEnd));
            Contact("End",502,ScreenAt(offense),true);Contact("RefreshState");overlap&=combat.Player.IsGuarding;
        }
        Check(overlap,"three overlapping guard/attack lift/repress cycles preserve the other hand and attack pointer ownership");
        var hp=combat.Player.CurrentHp;var oldGuard=combat.Player.ShieldCurrent;
        var damage=combat.Player.ReceiveDamage(new DamageRequest(DamageType.MonsterAttack,14,14,true,combat.Enemies[0]));
        yield return null;yield return null;
        Check(damage.WasGuarded&&combat.Player.CurrentHp==hp&&combat.Player.ShieldCurrent<oldGuard,"guarded impact consumes existing guard resource without changing damage rules");
        Check(hud.guardGauge.LossTrail>hud.guardGauge.amount,"recently lost guard remains visible as a red trail");
        Check(Ref<TMPro.TextMeshProUGUI>(hud,"shieldText").isActiveAndEnabled&&Ref<TMPro.TextMeshProUGUI>(hud,"shieldText").text.Contains("가드"),"guard resource numbers are visible in the Korean text workflow");
        anchor.FollowScreenPoint(ScreenAt(new Vector2(280,80)));yield return Capture("Guard-Impact");
        Contact("End",501,ScreenAt(guardEnd),true);Contact("RefreshState");
        yield return new WaitForSeconds(hud.guardGauge.lossHoldSeconds+hud.guardGauge.lossFadeSeconds+.2f);
        Check(hud.guardGauge.LossTrail-hud.guardGauge.amount<.02f,"guard loss trail settles after its feedback lifetime");
        combat.Player.TrySetGuard(true);combat.Player.ReceiveDamage(new DamageRequest(DamageType.MonsterAttack,1,combat.Player.ShieldMax*2,true,combat.Enemies[0]));
        yield return null;yield return null;
        Check(hud.guardGauge.Broken&&hud.guardGauge.amount<.03f,"guard break shows the broken state and near-empty resource while normal broken-guard regeneration starts");
        yield return Capture("Guard-Broken");combat.Player.RestoreShieldForNavigation();yield return null;
        Check(combat.Player.MaxHp==maxHp&&combat.Player.MaxMp==maxMp&&Mathf.Approximately(combat.Player.CurrentCooldown,cooldown),"UI changes preserve HP, MP and offensive pacing stats");
        var enemy=combat.Enemies[0];
        foreach(var attack in enemy.Data.attacks)attack.chargeTimeLimit=3;
        enemy.StartCharge(enemy.Data.BodyCenterFromPlacement(scene.MonsterPosition,enemy.VisualScale));
        typeof(MonsterRuntime).GetProperty("StateTimer").SetValue(enemy,2.85f);yield return null;
        var monster=Object.FindFirstObjectByType<MonsterView>();var backdrop=Ref<Image>(monster,"chargeTimeBackdrop");
        var surface=backdrop.GetComponentInChildren<InkUiSurface>();Canvas.ForceUpdateCanvases();
        anchor.FollowScreenPoint(ScreenAt(new Vector2(280,80)));yield return Capture("Final-HUD");anchor.enabled=true;
        var countdownMesh=surface.canvasRenderer.GetMesh();
        Check(countdownMesh.vertexCount>0&&countdownMesh.colors32.All(c=>c.r==0&&c.g==0&&c.b==0),"countdown paints only its ink shadow with no duration gauge");
        File.AppendAllText(Folder+"Live.txt","Countdown mesh: "+countdownMesh.vertexCount+" vertices, colors="+string.Join(";",countdownMesh.colors32.Distinct().Take(4))+"\n");
        foreach(var item in cosmetic)Object.Destroy(item);
        File.AppendAllText(Folder+"Live.txt","COMPLETE\n");
    }
}
#endif
