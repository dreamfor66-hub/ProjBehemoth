#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class MortalisLiveProbe:MonoBehaviour
{
    const string Log="Logs/Mortalis-Live.txt",Screens="Docs/Production/MortalisRevision/Screenshots/";
    RunController run; CombatController combat; PotionActionView menu; QuickInventoryView quick; CombatSceneView scene;
    EquipmentData stone,poison,heal;
    bool previousBackground;
    void Awake(){previousBackground=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=previousBackground;}
    void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)Debug.LogError("Mortalis: "+text);}
    public static void Capture(string name)
    {
        var frame=Object.FindFirstObjectByType<ArcadeRasterCamera>()?.Frame;if(frame==null)return;
        var old=RenderTexture.active;RenderTexture.active=frame;var t=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,t.width,t.height),0,0);t.Apply();Directory.CreateDirectory(Screens);File.WriteAllBytes(Screens+name+".png",t.EncodeToPNG());Object.Destroy(t);RenderTexture.active=old;
    }
    public void Run(string action)
    {
        if(action.StartsWith("capture:")){StartCoroutine(CaptureNext(action.Substring(8)));return;}
        if(action=="title"){StartCoroutine(Title());return;}
        Setup();if(action=="potions")StartCoroutine(Potions());else if(action=="aim")StartCoroutine(Aim());else if(action=="inventory")StartCoroutine(Inventory());
    }
    IEnumerator CaptureNext(string name){yield return new WaitForEndOfFrame();Capture(name);}
    void Setup()
    {
        menu=Object.FindFirstObjectByType<PotionActionView>(FindObjectsInactive.Include);menu.Close();
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Mortalis practice clone").AddComponent<OctoberReviewLiveProbe>();practice.Setup(CharacterKind.Warrior,false);
        run=Object.FindFirstObjectByType<RunController>();combat=Object.FindFirstObjectByType<CombatController>();quick=Object.FindFirstObjectByType<QuickInventoryView>();scene=Object.FindFirstObjectByType<CombatSceneView>();
        stone=run.Equipment.First(e=>e.id=="OpeningRockPotion");poison=run.Equipment.First(e=>e.id=="PoisonPotion");heal=run.Equipment.First(e=>e.id=="HealingPotion");
        foreach(var p in run.State.Potions.ToArray())run.State.TryRemoveEquipment(p);
        run.State.TryStorePotion(heal);run.State.TryStorePotion(stone);run.State.TryStorePotion(poison);
    }
    Vector2 Target=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(scene.MonsterPosition));
    IEnumerator Aim()
    {
        yield return new WaitForSeconds(6.5f);
        menu.Open(stone,(RectTransform)quick.potions[1].transform);menu.BeginThrow();menu.OnPointerMove(new PointerEventData(EventSystem.current){position=Target});
        var parent=(RectTransform)menu.transform;
        Check(parent.rect.Contains(menu.targetRing.rectTransform.anchoredPosition)&&parent.rect.Contains(menu.bottle.rectTransform.anchoredPosition)&&menu.targetRing.enabled,"aim brackets and potion cursor remain inside native canvas");
        yield return new WaitForEndOfFrame();Capture("PotionAim-Final");
    }
    IEnumerator Inventory()
    {
        var drawers=Object.FindFirstObjectByType<ArcadeHudController>();drawers.inventoryButton.onClick.Invoke();yield return null;yield return null;
        var hud=Object.FindFirstObjectByType<RunHudView>(FindObjectsInactive.Include);var grid=(RectTransform)typeof(RunHudView).GetField("potionGrid",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(hud);
        var icons=grid.GetComponentsInChildren<RunHudIconView>();Check(icons.Length>=3&&icons[1].GetComponent<Button>().interactable,"inventory potion icons refresh from current stored items");
        icons[1].GetComponent<Button>().onClick.Invoke();yield return null;
        Check(menu.IsOpen&&menu.Selected==stone&&drawers.inventoryPanel.activeSelf,"inventory potion click opens shared actions without consuming");yield return new WaitForEndOfFrame();Capture("InventoryPotionMenu");
        menu.BeginThrow();yield return null;Check(menu.IsTargeting&&!drawers.inventoryPanel.activeSelf&&combat.PotionInputBlocked,"inventory throw returns to combat targeting with input still blocked");menu.Close();yield return null;
        menu.Open(stone,(RectTransform)quick.potions[1].transform);menu.Discard();drawers.inventoryButton.onClick.Invoke();yield return null;yield return null;
        var third=grid.GetComponentsInChildren<RunHudIconView>()[2];Check(!third.GetComponent<Button>().interactable&&run.State.Potions.Count==2,"inventory removes consumed or discarded potion without room refresh");
        File.AppendAllText(Log,"INVENTORY COMPLETE\n");
    }
    IEnumerator Title()
    {
        var menu=Object.FindFirstObjectByType<OutGameMenuView>();menu.ShowTitle();yield return null;yield return new WaitForEndOfFrame();Capture("Title");
        Check(UnityEditor.PlayerSettings.productName=="Dungeon Mortalis"&&GameText.Get("ui.title")=="Dungeon Mortalis","confirmed title and product name");
        var logo=menu.titlePage.transform.Find("MortalisLogo").GetComponent<Image>();Check(logo.sprite!=null&&logo.material!=null&&!menu.titlePage.transform.Find("PixelLogo").gameObject.activeSelf,"new raster logo replaces rejected wordmark");
        Check(menu.titlePage.transform.Find("BlackBackdrop").GetComponent<Image>().color==Color.black,"title has black background");
        menu.newGame.onClick.Invoke();yield return new WaitForEndOfFrame();Capture("Selection");Check(menu.Page==OutGamePage.Characters&&menu.selection.selectionSplash.sprite!=null,"start still opens illustrated character selection");menu.back.onClick.Invoke();
        File.AppendAllText(Log,"TITLE COMPLETE\n");
    }
    IEnumerator Potions()
    {
        File.AppendAllText(Log,"\nScoped potion checks, live frames\n");yield return null;yield return new WaitForEndOfFrame();
        var count=run.State.Potions.Count;quick.potions[0].OnPointerEnter(new PointerEventData(EventSystem.current));
        Check(quick.tooltip.IsVisible&&run.State.Potions.Count==count,"hover only opens side description");
        quick.potions[0].GetComponent<Button>().onClick.Invoke();yield return null;
        Check(menu.IsOpen&&run.State.Potions.Count==count,"click opens actions without consuming");
        Check(!menu.drink.interactable&&!menu.throwButton.interactable,"full-health healing drink and invalid throw disabled");
        var timer=combat.Enemies[0].NormalAttackTimer;yield return new WaitForSeconds(.25f);Check(Mathf.Approximately(timer,combat.Enemies[0].NormalAttackTimer),"potion choice pauses combat");yield return new WaitForEndOfFrame();Capture("PotionMenu");
        menu.Drink();Check(run.State.Potions.Count==count,"invalid full-health drink preserves potion");menu.Close();
        run.State.Player.TakeEnvironmentalDamage(50);var before=run.State.Player.CurrentHp;menu.Open(heal,(RectTransform)quick.potions[0].transform);menu.drink.onClick.Invoke();yield return null;
        Check(Mathf.Abs(run.State.Player.CurrentHp-before-35)<.01f&&run.State.Potions.Count==count-1&&!menu.IsOpen,"drink heals 35 once and consumes one potion");
        menu.Open(stone,(RectTransform)quick.potions[0].transform);Check(!menu.drink.interactable&&menu.throwButton.interactable,"stone potion offers aimed throw rather than drink");menu.BeginThrow();
        Check(menu.IsTargeting&&combat.InputPaused,"throw enters paused target selection");menu.OnPointerClick(new PointerEventData(EventSystem.current){position=Vector2.zero,button=PointerEventData.InputButton.Left});
        Check(run.State.Potions.Contains(stone)&&menu.IsTargeting,"empty target does not consume potion");menu.cancel.onClick.Invoke();
        Check(!menu.IsOpen&&run.State.Potions.Contains(stone)&&!combat.PotionInputBlocked,"cancel releases potion input lock and preserves item");
        var target=combat.Enemies[0];var hp=target.CurrentHp;menu.Open(stone,(RectTransform)quick.potions[0].transform);menu.BeginThrow();menu.OnPointerMove(new PointerEventData(EventSystem.current){position=Target});yield return new WaitForEndOfFrame();Capture("PotionTarget");
        Check(combat.PotionTargetAt(Target,out var selected,out _)&&selected==target,"target selection resolves visible enemy");menu.OnPointerClick(new PointerEventData(EventSystem.current){position=Target,button=PointerEventData.InputButton.Left});yield return new WaitForSeconds(.3f);
        Check(Mathf.Abs(hp-target.CurrentHp-26)<.01f&&!run.State.Potions.Contains(stone),"stone throw deals authored 26 damage to selected enemy");
        Check(!menu.IsOpen&&!combat.PotionInputBlocked,"throw animation completes and resumes combat");
        menu.Open(poison,(RectTransform)quick.potions[0].transform);menu.BeginThrow();menu.OnPointerClick(new PointerEventData(EventSystem.current){position=Target,button=PointerEventData.InputButton.Left});yield return new WaitForSeconds(.3f);
        Check(target.PoisonStacks==3&&!run.State.Potions.Contains(poison),"poison throw applies three enemy poison stacks");hp=target.CurrentHp;yield return new WaitForSeconds(2.15f);Check(target.CurrentHp<=hp-9,"thrown poison deals real periodic damage");yield return new WaitForEndOfFrame();Capture("PoisonEnemy");
        run.State.TryStorePotion(poison);yield return null;menu.Open(poison,(RectTransform)quick.potions[0].transform);menu.Drink();yield return null;var buff=run.State.Buffs.Find(CombatStatusCatalog.Instance.poison);
        Check(buff!=null&&buff.StackCount==3&&run.State.Player.StatusText.Contains("독"),"drinking poison applies visible harmful owner status");before=run.State.Player.CurrentHp;yield return new WaitForSeconds(2.15f);Check(run.State.Player.CurrentHp<=before-9,"drunk poison damages player on later tick");yield return new WaitForEndOfFrame();Capture("PoisonSelf");
        run.State.Cleanse();yield return null;Check(!run.State.Buffs.Has(CombatStatusCatalog.Instance.poison),"existing cleanse removes self poison");
        run.State.TryStorePotion(poison);yield return null;before=run.State.Player.CurrentHp;menu.Open(poison,(RectTransform)quick.potions[0].transform);menu.Discard();yield return null;
        Check(!run.State.Potions.Contains(poison)&&Mathf.Approximately(before,run.State.Player.CurrentHp),"discard removes item without applying drink or throw effects");
        run.State.TryStorePotion(stone);combat.ResetEncounter();yield return null;menu.Open(stone,(RectTransform)quick.potions[0].transform);Check(!menu.throwButton.interactable&&!run.TryUsePotion(stone),"out-of-combat stone cannot be thrown or pre-consumed");menu.BeginThrow();Check(!menu.IsTargeting&&run.State.Potions.Contains(stone),"invalid out-of-combat throw preserves item");menu.Close();
        Setup();yield return new WaitForEndOfFrame();Capture("PotionsReady");File.AppendAllText(Log,"POTIONS COMPLETE\n");
    }
}
#endif
