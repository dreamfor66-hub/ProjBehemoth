#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class HudItemLayoutLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/HudItems-2026-10-31/";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    CombatController combat;CombatInputController input;CombatSceneView scene;QuickInventoryView quick;RunController run;
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;if(input!=null)input.GuardModifierForVerification=null;}
    void Check(bool ok,string text){File.AppendAllText(Folder+"Live.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)Debug.LogError("HUD items: "+text);}
    IEnumerator Capture(string name){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Folder+name+".png");yield return null;}
    Vector2 ScreenAt(Vector2 point)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(point));
    void Contact(string name,params object[] args)=>typeof(CombatInputController).GetMethod(name,Private).Invoke(input,args);
    public void Run()=>StartCoroutine(Matrix());
    IEnumerator Matrix()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Live.txt","Focused live Unity HUD/feedback matrix; runtime-only practice data. No balance suite.\n");
        new GameObject("HUD item practice").AddComponent<OctoberReviewLiveProbe>().Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_해골_전사.asset");
        combat=Object.FindFirstObjectByType<CombatController>();input=Object.FindFirstObjectByType<CombatInputController>();scene=Object.FindFirstObjectByType<CombatSceneView>();quick=Object.FindFirstObjectByType<QuickInventoryView>();run=Object.FindFirstObjectByType<RunController>();
        yield return new WaitForSeconds(.7f);
        foreach(var item in run.State.Potions.ToArray())run.State.TryRemoveEquipment(item);
        quick.Refresh();yield return null;
        Check(quick.potions.Count==0&&quick.potionBag.gameObject.activeInHierarchy&&quick.potionBag.slotBackgrounds.childCount==run.State.PotionSlotCapacity,"empty bag retains the three actual capacity slots");
        yield return Capture("Bag-Empty");
        var rock=run.Equipment.First(e=>e.id=="OpeningRockPotion");var heal=run.Equipment.First(e=>e.id=="HealingPotion");
        run.State.TryStorePotion(rock);run.State.TryStorePotion(heal);
        var source=run.Equipment.First(e=>e.IsRelic&&e.scope==RelicScope.Shared);
        for(var i=0;i<4;i++){var item=Object.Instantiate(source);item.id="hud-visual-"+i;run.State.EquippedItems.Add(item);}
        quick.Refresh();yield return null;Canvas.ForceUpdateCanvases();
        Check(quick.potions.Count==2&&quick.potionBag.slotBackgrounds.childCount==3,"two potions leave the third slot visible");
        var relicGrid=quick.relicPool.content.GetComponent<GridLayoutGroup>();var potionGrid=quick.potionPool.content.GetComponent<GridLayoutGroup>();
        Check(relicGrid.cellSize==potionGrid.cellSize&&quick.relicPool.prefab==quick.potionPool.prefab,"relic and potion artwork use the same enlarged icon prefab and size");
        Check(Mathf.Abs(quick.relicPool.content.anchoredPosition.x-160)<.1f,"relic row begins beside the portrait at the requested yellow-line position");
        yield return Capture("Bag-And-Relics");
        var es=EventSystem.current;var hits=new System.Collections.Generic.List<RaycastResult>();var anchor=(RectTransform)quick.potions[0].transform;
        var center=RectTransformUtility.WorldToScreenPoint(scene.UiCamera,anchor.TransformPoint(anchor.rect.center));
        es.RaycastAll(new PointerEventData(es){position=center},hits);
        Check(hits.Any(h=>h.gameObject.GetComponentInParent<RunHudIconView>()==quick.potions[0]),"bag art and empty-slot backings do not intercept occupied-slot pointer hits");
        quick.potions[0].OnPointerEnter(new PointerEventData(es));Check(quick.tooltip.IsVisible&&quick.tooltip.VisibleTitle==rock.displayName,"enlarged potion hover shows the correct description");quick.tooltip.Hide();
        quick.potions[0].GetComponent<Button>().onClick.Invoke();yield return null;
        Check(quick.potionActions.IsOpen&&quick.potionActions.Selected==rock,"clicking the bag potion opens its existing action menu");
        yield return Capture("Potion-Actions");quick.potionActions.BeginThrow();yield return null;
        Check(quick.potionActions.IsTargeting,"throw action still enters targeting from the new bag");quick.potionActions.Close();
        quick.potions[1].GetComponent<Button>().onClick.Invoke();quick.potionActions.Discard();yield return null;quick.Refresh();yield return null;
        Check(quick.potions.Count==1&&quick.potionBag.slotBackgrounds.childCount==3,"discard returns the icon while preserving empty bag slots");
        var created=quick.potionPool.CreatedCount;run.State.TryStorePotion(heal);quick.Refresh();yield return null;
        Check(quick.potions.Count==2&&quick.potionPool.CreatedCount==created,"reacquisition reuses pooled icon with current bindings");
        run.State.TryStorePotion(rock);quick.Refresh();yield return null;Check(quick.potions.Count==3&&!run.State.HasPotionSpace,"full bag matches the actual three-potion capacity");yield return Capture("Bag-Full");
        while(run.State.EquippedItems.Count<21){var item=Object.Instantiate(source);item.id="hud-wrap-"+run.State.EquippedItems.Count;run.State.EquippedItems.Add(item);}
        quick.Refresh();yield return null;Canvas.ForceUpdateCanvases();
        Check(((RectTransform)quick.relics.Last().transform).anchoredPosition.y<((RectTransform)quick.relics[0].transform).anchoredPosition.y-60,"enlarged relic overflow wraps into another row");yield return Capture("Relic-Wrap");
        while(run.State.EquippedItems.Count>5)run.State.EquippedItems.RemoveAt(run.State.EquippedItems.Count-1);quick.Refresh();yield return null;
        var hud=Object.FindFirstObjectByType<CombatHudView>();var gauge=(RectTransform)typeof(CombatHudView).GetField("chargeGaugeRoot",Private).GetValue(hud);
        var point=ScreenAt(new Vector2(-100,320));Contact("Begin",882,point);var began=Time.unscaledTime;
        while(input.ChargeProgress<1&&Time.unscaledTime-began<4){Contact("Move",882,point);yield return null;}
        Check(gauge.gameObject.activeInHierarchy&&input.ChargeProgress>=1&&gauge.parent.name=="PointerOverlay","held charge reaches full and uses a saved overlay above the HUD");
        Check(gauge.parent.GetSiblingIndex()>quick.transform.parent.GetSiblingIndex(),"charge overlay is later than the top-bar HUD in canvas draw order");yield return Capture("Charge-Over-TopBar");Contact("End",882,point,false);yield return null;
        var actors=(IList)typeof(CombatController).GetField("activeMonsters",Private).GetValue(combat);var attack=combat.Enemies[0].Data.GetAttack(MonsterAttackType.Normal);
        var method=typeof(CombatController).GetMethod("ApplyMonsterAttack",Private);var result=(DamageResult)method.Invoke(combat,new[]{actors[0],(object)attack});yield return null;
        var feedback=combat.pressureFeedback;
        Check(result.HpDamage>0&&feedback.danger.intensity>0&&!feedback.impact.gameObject.activeInHierarchy&&!feedback.impact.IsPlaying,"real unguarded attack damages HP, keeps red edges and emits no hit shape");yield return Capture("Damage-Edges-Only");
        input.GuardModifierForVerification=true;yield return new WaitForSeconds(.4f);result=(DamageResult)method.Invoke(combat,new[]{actors[0],(object)attack});yield return null;
        Check(result.WasGuarded&&!feedback.impact.IsPlaying&&!feedback.impact.gameObject.activeInHierarchy,"real guarded attack emits no guard-impact shape");yield return Capture("Guard-No-Impact");input.GuardModifierForVerification=null;
        File.WriteAllText(Folder+"Complete.txt","Scoped matrix finished; inspect Live.txt and screenshots. Desktop Unity only; no balance suite or physical-device test.\n");
    }
}
#endif
