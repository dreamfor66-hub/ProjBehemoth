using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class AshenVowDiagnostics
{
    static AshenVowDiagnostics()
    {
        EditorApplication.update += Poll;
        Application.logMessageReceived += (message, stack, type) => { if (type is LogType.Error or LogType.Exception) { Directory.CreateDirectory("Logs"); File.AppendAllText("Logs/AshenVow-Errors.txt", message + "\n" + stack + "\n"); } };
    }
    private static void Poll()
    {
        const string file = "Temp/AshenVow.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(file)) return;
        string command; try { command = File.ReadAllText(file).Trim(); File.Delete(file); } catch (IOException) { return; }
        Directory.CreateDirectory("Logs");
        if(command=="weakpoint-visual-prepare"&&!EditorApplication.isPlaying)WeakPointVisualPreparation.Prepare();
        if(command.StartsWith("weakpoint-visual:")&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<WeakPointVisualLiveProbe>()??new GameObject("Weak point scoped visual QA").AddComponent<WeakPointVisualLiveProbe>();probe.Run(command.Substring(17));}
        if(command.StartsWith("fatigue:")&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<FatigueLiveProbe>()??new GameObject("Fatigue scoped QA").AddComponent<FatigueLiveProbe>();if(command=="fatigue:check")probe.Run();else probe.Manual(command.Substring(8));}
        if(command=="content-review-export")ContentReviewExport.Export();
        if(command=="content-review-check"&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<ContentReviewLiveProbe>()??new GameObject("Content review scoped QA").AddComponent<ContentReviewLiveProbe>();probe.Run();}
        if(command=="spellbook-pool-check"&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<SpellbookPoolLiveProbe>()??new GameObject("Spellbook pool scoped QA").AddComponent<SpellbookPoolLiveProbe>();probe.Run();}
        if(command=="mana-offer-check"&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<ManaOfferLiveProbe>()??new GameObject("MP offer scoped QA").AddComponent<ManaOfferLiveProbe>();probe.Run();}
        if(command.StartsWith("mortalis:")&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<MortalisLiveProbe>()??new GameObject("Mortalis scoped QA").AddComponent<MortalisLiveProbe>();probe.Run(command.Substring(9));}
        if(command=="review-missing"){var output=new StringBuilder();for(var i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)foreach(var root in UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true)){var count=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);if(count>0)output.AppendLine(PathOf(t)+": "+count);}File.WriteAllText("Logs/OctoberReview-Missing.txt",output.Length==0?"No missing scripts in loaded scenes.":output.ToString());}
        if(command.StartsWith("review:")&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("October review scoped QA").AddComponent<OctoberReviewLiveProbe>();var action=command.Substring(7);if(action=="matrix")probe.Run();else if(action=="extra")probe.Extra();else if(action=="selection")probe.OutGame();else if(action=="charge")probe.Charge();else if(action.StartsWith("capture:"))OctoberReviewLiveProbe.Capture(action.Substring(8));else if(System.Enum.TryParse<CharacterKind>(action,out var kind))probe.Setup(kind,kind==CharacterKind.Wizard);}
        if(command.StartsWith("revision3:")&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<FeedbackRevision3LiveProbe>()??new GameObject("Revision3 scoped QA").AddComponent<FeedbackRevision3LiveProbe>();var action=command.Substring(10);if(action.StartsWith("capture:"))FeedbackRevision3LiveProbe.Capture(action.Substring(8));else probe.Run(action);}
        if(command=="outgame-restore-options"){GamePreferences.SetVolume(1);GamePreferences.SetFullscreen(false);GamePreferences.SetWindowScale(3);GamePreferences.SetScreenShake(true);GamePreferences.Save();}
        if(command=="outgame-status"){
            var menu=Object.FindFirstObjectByType<OutGameMenuView>();var output=new StringBuilder();output.AppendLine("Playing: "+EditorApplication.isPlaying);
            if(menu!=null){output.AppendLine("Page: "+menu.Page+" / title="+menu.titlePage.activeSelf+" characters="+menu.characterPage.activeSelf+" options="+menu.optionsPage.activeSelf);output.AppendLine("Popup: "+menu.selection.startingRelicHover.view.IsVisible+" / "+menu.selection.startingRelicHover.view.VisibleTitle);output.AppendLine("Selected: "+menu.selection.SelectedIndex+" / visible cards="+menu.selection.characterButtons.Count(b=>b.gameObject.activeInHierarchy));output.AppendLine("Volume: "+GamePreferences.Volume+" / AudioListener="+AudioListener.volume+" / scale="+GamePreferences.WindowScale+" fullscreen="+GamePreferences.Fullscreen+" shake="+GamePreferences.ScreenShake);
                foreach(var character in menu.selection.content.characters)output.AppendLine(character.character+": rect="+character.portrait.rect+" texture="+character.portrait.texture.width+"x"+character.portrait.texture.height);
            }
            foreach(var raster in Object.FindObjectsByType<ArcadeRasterCamera>(FindObjectsSortMode.None))output.AppendLine("Raster: "+raster.gameObject.scene.name+" "+raster.presentation.width+"x"+raster.presentation.height+" quantize="+raster.quantizeColors);
            File.WriteAllText("Logs/OutGameShell-Status.txt",output.ToString());
        }
        if(command=="feedback-hit"&&EditorApplication.isPlaying){
            var lines=new StringBuilder();lines.AppendLine("Mouse "+Input.mousePosition+" screen "+Screen.width+"x"+Screen.height);
            var results=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();var es=UnityEngine.EventSystems.EventSystem.current;
            if(es!=null){lines.AppendLine("Input module "+es.currentInputModule);es.RaycastAll(new UnityEngine.EventSystems.PointerEventData(es){position=Input.mousePosition},results);foreach(var hit in results)lines.AppendLine("Hit "+PathOf(hit.gameObject.transform));}
            foreach(var t in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(t=>t.name is "Title" or "Name"))lines.AppendLine(t.name+": "+t.text+" enabled="+t.enabled+" pos="+t.transform.position+" rect="+t.rectTransform.rect+" align="+t.alignment);
            File.WriteAllText("Logs/Feedback224-Hit.txt",lines.ToString());
        }
        if(command.StartsWith("feedback-ui:")&&EditorApplication.isPlaying){
            var flow=Object.FindFirstObjectByType<GameFlowController>();flow.StartNewRun();
            var dungeon=Object.FindFirstObjectByType<DungeonController>();var run=Object.FindFirstObjectByType<RunController>();var content=Object.FindFirstObjectByType<CombatController>().Content;var hud=Object.FindFirstObjectByType<ArcadeHudController>();
            var kind=command.Substring(12);
            if(kind=="inventory"){
                foreach(var perk in content.perks.Where(p=>p.scope==RelicScope.Shared).Take(5))run.TryGrantPerk(perk);
                foreach(var relic in content.equipment.Where(e=>!e.IsPotion&&e.scope==RelicScope.Shared).Take(5))run.TryGrantEquipment(relic);
                foreach(var potion in content.equipment.Where(e=>e.IsPotion).Take(3))run.TryGrantEquipment(potion);
                typeof(GameFlowController).GetMethod("RefreshHud",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(flow,null);hud.inventoryButton.onClick.Invoke();
            }
            if(kind=="map"){
                foreach(var room in dungeon.State.Graph.Rooms.Take(12))room.IsVisited=true;
                Object.FindFirstObjectByType<DungeonMapView>(FindObjectsInactive.Include).Render(dungeon.State);hud.mapButton.onClick.Invoke();
            }
            if(kind=="fountain")typeof(GameFlowController).GetMethod("ShowFountainChoice",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(flow,new object[]{dungeon.State.Player.CurrentRoom});
        }
        if(command=="capture-native")
        {
            var raster=Object.FindFirstObjectByType<ArcadeRasterCamera>();
            if(raster?.Frame!=null){var old=RenderTexture.active;RenderTexture.active=raster.Frame;var t=new Texture2D(raster.Frame.width,raster.Frame.height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,t.width,t.height),0,0);t.Apply();File.WriteAllBytes("Logs/Arcade224-Native.png",t.EncodeToPNG());Object.DestroyImmediate(t);RenderTexture.active=old;}
        }
        if (command == "selection" && !EditorApplication.isPlaying) EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity");
        if (command == "main" && !EditorApplication.isPlaying) EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
        if (command == "play") EditorApplication.isPlaying = true;
        if (command == "select-wizard") RunSession.SelectedCharacter = AssetDatabase.LoadAssetAtPath<PlayerCombatData>("Assets/Data/Characters/Wizard.asset");
        if(command=="qa-feedback"&&EditorApplication.isPlaying){var probe=Object.FindFirstObjectByType<FeedbackLiveProbe>()??new GameObject("Feedback scoped QA").AddComponent<FeedbackLiveProbe>();probe.Run();}
        if (command == "qa-spells" && EditorApplication.isPlaying)
        { var probe = Object.FindFirstObjectByType<AshenVowLiveProbe>() ?? new GameObject("AshenVow Scoped Live QA").AddComponent<AshenVowLiveProbe>(); probe.RunSpells(); }
        if (command == "qa-manual" && EditorApplication.isPlaying)
        { var probe = Object.FindFirstObjectByType<AshenVowLiveProbe>() ?? new GameObject("AshenVow Scoped Live QA").AddComponent<AshenVowLiveProbe>(); probe.SetupForManual(); }
        if (command == "qa-input" && EditorApplication.isPlaying)
        { var probe = Object.FindFirstObjectByType<AshenVowLiveProbe>() ?? new GameObject("AshenVow Scoped Live QA").AddComponent<AshenVowLiveProbe>(); probe.RunInput(); }
        if(command=="qa-poses"&&EditorApplication.isPlaying)
        {var probe=Object.FindFirstObjectByType<AshenVowLiveProbe>()??new GameObject("AshenVow Scoped Live QA").AddComponent<AshenVowLiveProbe>();probe.RunPoses();}
        if (command == "qa-events" && EditorApplication.isPlaying)
        { var probe = Object.FindFirstObjectByType<AshenVowLiveProbe>() ?? new GameObject("AshenVow Scoped Live QA").AddComponent<AshenVowLiveProbe>(); probe.RunEvents(); }
        if (command.StartsWith("qa-event:") && EditorApplication.isPlaying) AshenVowLiveProbe.ShowEvent(command.Substring(9));
        if(command=="qa-reward"&&EditorApplication.isPlaying)
        {
            var flow=Object.FindFirstObjectByType<GameFlowController>();var run=Object.FindFirstObjectByType<RunController>();Object.FindFirstObjectByType<CombatController>().ResetEncounter();Object.FindFirstObjectByType<RunExpansionView>().HideModal();Object.FindFirstObjectByType<DungeonController>().HideNavigation();
            typeof(GameFlowController).GetMethod("GrantAndShowReward",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(flow,new object[]{new CombatReward(run.State.RequiredExperience,20),null});
        }
        if(command=="qa-shop"&&EditorApplication.isPlaying)
        {
            var flow=Object.FindFirstObjectByType<GameFlowController>();var dungeon=Object.FindFirstObjectByType<DungeonController>();Object.FindFirstObjectByType<CombatController>().ResetEncounter();Object.FindFirstObjectByType<RunExpansionView>().HideModal();dungeon.HideNavigation();
            typeof(GameFlowController).GetMethod("ShowShop",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(flow,new object[]{dungeon.State.Player.CurrentRoom});
        }
        if(command=="qa-wizard"&&EditorApplication.isPlaying){RunSession.SelectedCharacter=AssetDatabase.LoadAssetAtPath<PlayerCombatData>("Assets/Data/Characters/Wizard.asset");Object.FindFirstObjectByType<GameFlowController>().StartNewRun();}
        if(command=="qa-result"&&EditorApplication.isPlaying)
        {
            Object.FindFirstObjectByType<CombatController>().ResetEncounter();Object.FindFirstObjectByType<DungeonController>().HideNavigation();
            typeof(GameFlowController).GetMethod("ShowResult",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(Object.FindFirstObjectByType<GameFlowController>(),new object[]{false});
        }
        if (command == "stop") EditorApplication.isPlaying = false;
        if (command == "capture" && EditorApplication.isPlaying) ScreenCapture.CaptureScreenshot("Logs/AshenVow-Game.png");
        if (command == "status")
        {
            var text = new StringBuilder(); text.AppendLine("Playing: " + EditorApplication.isPlaying);
            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) { var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i); text.AppendLine($"Scene {s.path} dirty={s.isDirty}"); }
            var content = AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");
            if (content != null) text.AppendLine($"Perks {content.perks.Count}, spells {content.spells.Count}, events {content.events.Count}, revisions {content.gameplayRevision}/{content.artRevision}/{content.narrativeRevision}");
            var controller=Object.FindFirstObjectByType<RunController>();if(controller?.State!=null){var player=controller.State.Player;text.AppendLine($"Runtime HP {player.CurrentHp}/{player.MaxHp}; shield {player.ShieldCurrent}/{player.ShieldMax}; MP {player.CurrentMp}/{player.MaxMp}; actions {controller.State.ActionSerial}");}
            foreach (var rect in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (rect.GetComponentInParent<Canvas>() != null) text.AppendLine($"{PathOf(rect)} | {rect.anchoredPosition} | {rect.sizeDelta} | {rect.gameObject.activeSelf}");
            File.WriteAllText("Logs/AshenVow-Status.txt", text.ToString());
        }
    }
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}


