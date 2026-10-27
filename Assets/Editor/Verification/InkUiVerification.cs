using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

// Explicit, scoped visual requests in the existing Editor. No background suite.
[InitializeOnLoad]
internal static class InkUiVerification
{
    const string Request = "Temp/InkUi.request";
    const string Folder = "Docs/Production/InkUI-2026-10-27/";
    static string capture;
    static double captureAt;
    static InkUiVerification() => EditorApplication.update += Poll;
    static T Ref<T>(object owner, string field) where T : class => owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(owner) as T;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (capture != null && EditorApplication.timeSinceStartup > captureAt) { Capture(capture); capture = null; }
        if (!File.Exists(Request)) return;
        string command;
        try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch(IOException) { return; } // An external writer can still be closing the explicit request.
        Directory.CreateDirectory(Folder + "Screenshots");
        if (command == "inspect")
        {
            var report = new StringBuilder();
            foreach (var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if(image.gameObject.scene.IsValid())report.AppendLine(image.transform.parent?.name+"/"+image.name+" | "+image.GetType().Name+" | "+image.rectTransform.rect+" | "+(image.sprite!=null?AssetDatabase.GetAssetPath(image.sprite):"none")+" | "+image.color);
            File.WriteAllText(Folder+"ImageInventory.txt",report.ToString());return;
        }
        if (command == "start") { EditorApplication.isPlaying = true; return; }
        if (!EditorApplication.isPlaying) return;
        var view = Object.FindFirstObjectByType<RunExpansionView>();
        if(command=="navigation")
        {
            Object.FindFirstObjectByType<GameFlowController>().StartNewRun();view.HideModal();
            Object.FindFirstObjectByType<ArcadeHudController>().Close();
        }
        if (command == "setup")
        {
            var practice = Object.FindFirstObjectByType<OctoberReviewLiveProbe>() ?? new GameObject("Scoped UI preview clones").AddComponent<OctoberReviewLiveProbe>();
            practice.Setup(CharacterKind.Warrior, true);
        }
        if (command == "event")
        {
            var data = AssetDatabase.LoadAssetAtPath<RandomEventData>("Assets/Data/Dungeon/Events/Hourglass.asset");
            view.ShowModal(data.displayName, data.narrative, data.options.Select(o => new PresentationChoice { Title = o.title, Description = o.description }), data.illustration);
        }
        if (command == "close")
        {
            view.HideModal();Object.FindFirstObjectByType<ArcadeHudController>().Close();
            Object.FindFirstObjectByType<ItemTooltipView>(FindObjectsInactive.Include).Hide();Object.FindFirstObjectByType<PerkChoiceView>(FindObjectsInactive.Include).Hide();Object.FindFirstObjectByType<RewardPopupView>(FindObjectsInactive.Include).Hide();
            Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include).Hide();
        }
        if(command.StartsWith("feedback:"))
        {
            var button=view.ActiveChoices[0].button;
            var mode=command.Substring(9);button.interactable=mode!="disabled";
            EventSystem.current.SetSelectedGameObject(null);
            var pointer=new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);
            if(mode is "hover" or "pressed")ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);
            if(mode=="pressed")ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
        }
        if (command == "inventory" || command == "map")
        {
            view.HideModal(); var hud = Object.FindFirstObjectByType<ArcadeHudController>(); hud.Close();
            (command == "map" ? hud.mapButton : hud.inventoryButton).onClick.Invoke();
        }
        if (command == "tooltip")
        {
            var item = Object.FindFirstObjectByType<CombatController>().Content.equipment.First();
            Object.FindFirstObjectByType<ItemTooltipView>().Show(item.displayName, item.description, new Vector2(Screen.width * .65f, Screen.height * .65f));
        }
        if(command=="spellbook")view.spellbookView.Open(Object.FindFirstObjectByType<CombatController>().Run ?? Object.FindFirstObjectByType<RunController>().State);
        if(command=="shop")
        {
            view.HideModal();Object.FindFirstObjectByType<ArcadeHudController>().Close();
            var shop=Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);var run=Object.FindFirstObjectByType<RunController>();
            shop.Show(Object.FindFirstObjectByType<CombatController>().Content.equipment.Take(3).ToArray(),run.State,run.Shop,_=>{},()=>shop.Hide());
        }
        if(command=="reward")
        {
            view.HideModal();Object.FindFirstObjectByType<ArcadeHudController>().Close();
            var popup=Object.FindFirstObjectByType<RewardPopupView>(FindObjectsInactive.Include);
            popup.Show(new CombatReward(10,10),1,0,100,Object.FindFirstObjectByType<RunController>().State,null,()=>popup.Hide());
        }
        if(command.StartsWith("charge:"))
        {
            var combat=Object.FindFirstObjectByType<CombatController>();combat.InputPaused=true;
            var progress=float.Parse(command.Substring(7),System.Globalization.CultureInfo.InvariantCulture);
            Object.FindFirstObjectByType<CombatHudView>().Bind(combat.Player,null,PointerGestureState.Charging,progress,Vector2.zero);
        }
        if (command == "perks")
        {
            view.HideModal();
            Object.FindFirstObjectByType<PerkChoiceView>(FindObjectsInactive.Include).Show(Object.FindFirstObjectByType<CombatController>().Content.perks.Take(3).ToArray(), _ => Object.FindFirstObjectByType<PerkChoiceView>(FindObjectsInactive.Include).Hide());
        }
        if (command.StartsWith("capture:")) { capture = command.Substring(8); captureAt = EditorApplication.timeSinceStartup + .4; }
        if (command.StartsWith("resources:"))
        {
            view.HideModal();
            var combat=Object.FindFirstObjectByType<CombatController>();var player=combat.Player;
            var fraction=float.Parse(command.Substring(10),System.Globalization.CultureInfo.InvariantCulture);
            // This is a cloned practice encounter only. Pause its simulation before displaying zero HP.
            combat.InputPaused=true;
            player.Heal(player.MaxHp);player.RefillMp();player.RestoreShieldForNavigation();
            player.TakeEnvironmentalDamage(player.MaxHp*(1-fraction));player.TrySpendMp(player.MaxMp*(1-fraction));
            player.SetKeyboardGuard(true);
            new DamageResolver().ApplyToPlayer(player,new DamageRequest(DamageType.MonsterAttack,0,player.ShieldMax*(1-fraction),true,null));
            player.SetKeyboardGuard(false);
            // IsAlive is false at zero, so write only the presentation's practice snapshot for that case.
            if(fraction<=0)typeof(PlayerCombatRuntime).GetProperty("ShieldCurrent").SetValue(player,0f);
            typeof(CombatController).GetMethod("RefreshHud",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(combat,null);
        }
    }
    static void Capture(string name)
    {
        Canvas.ForceUpdateCanvases(); var report = new StringBuilder(name + " / " + Screen.width + "x" + Screen.height + "\n");
        foreach (var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)))
        {
            label.ForceMeshUpdate();
            report.AppendLine(label.name + " | " + label.rectTransform.rect + " | overflow=" + label.isTextOverflowing + " | " + label.text.Replace('\n', ' '));
        }
        var combat = Object.FindFirstObjectByType<CombatController>();
        if (combat?.Player != null) report.AppendLine("Runtime HP=" + combat.Player.CurrentHp + "/" + combat.Player.MaxHp + "; MP=" + combat.Player.CurrentMp + "/" + combat.Player.MaxMp + "; guard=" + combat.Player.ShieldCurrent + "/" + combat.Player.ShieldMax);
        foreach(var skin in Object.FindObjectsByType<InkUiSurface>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            report.AppendLine("Skin: "+skin.source?.name+" | "+skin.role+" | "+skin.rectTransform.rect+" | nonblocking="+!skin.raycastTarget+" | hover="+skin.IsHovered+" | pressed="+skin.IsPressed+" | active="+(skin.control==null||skin.control.IsInteractable())+" | vertices="+skin.canvasRenderer.GetMesh()?.vertexCount+" | material="+skin.material.shader.name);
        File.WriteAllText(Folder + name + ".txt", report.ToString());
        ScreenCapture.CaptureScreenshot(Folder + "Screenshots/" + name + ".png");
    }
}
