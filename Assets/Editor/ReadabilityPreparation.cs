using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Applies once to saved scenes; all resulting controls remain editable in the Inspector.
[InitializeOnLoad]
internal static class ReadabilityPreparation
{
    const string Profile = "Assets/Data/Presentation/Arcade224.asset";
    static readonly Color Ink = new Color32(11,12,22,255), Paper = new Color32(222,211,175,255);
    static double next;
    static ReadabilityPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 4;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var profile = AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);
        var content = AssetDatabase.LoadAssetAtPath<GameContent>(BehemothContentBuilder.ContentPath);
        if (profile == null || profile.readabilityRevision >= 1 || profile.feedbackRevision < 4 || content == null || content.fatigueRevision < 1) return;
        if (Enumerable.Range(0,SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) return;
        try { Prepare(); }
        catch (Exception e) { next = double.MaxValue; Debug.LogException(e); }
    }
    internal static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var table = BehemothTextEditor.Table;
        foreach (var pair in new[] {
            ("ui.selection.show.introduction", "소개 보기"),
            ("ui.selection.back", "←  뒤로"),
            ("ui.selection.extra.title", "추가 능력치"),
            ("ui.selection.extra.hint", "추가 수치"),
            ("ui.selection.ability.stats", "HP {0:0}   MP {1:0}\n가드 게이지 {2:0}\n\n일반 공격 피해 {3:0.#}\n차지 공격 피해 {4:0.#}\n공격 쿨타임 {5:0.##}초"),
            ("ui.selection.ability.extra", "차지 준비 {0:0.##}초\n가드 회복 {1:0.#}/초\n가드 피해 감소 {2:P0}") })
            if (!table.Contains(pair.Item1)) table.Set(pair.Item1, pair.Item2, "Selection readability");
        table.Invalidate(); EditorUtility.SetDirty(table);
        File.WriteAllText(BehemothTextEditor.CsvPath, table.ExportCsv(), new System.Text.UTF8Encoding(true));
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset");
        font.TryAddCharacters(string.Concat(table.entries.Where(e=>e.key.StartsWith("ui.selection.")).Select(e=>e.kr)),out string _);
        foreach(var atlas in font.atlasTextures)atlas.filterMode=FilterMode.Point;
        EditorUtility.SetDirty(font);
        foreach (var path in new[] {"Assets/Scenes/OutGameScene.unity", "Assets/Scenes/MainGameScene.unity"})
        {
            var scene = SceneManager.GetSceneByPath(path); var open = scene.IsValid() && scene.isLoaded;
            if (open && scene.isDirty) throw new InvalidOperationException("Preserve the open designer scene; save it before preparing readability.");
            if (!open) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var selection = All<OutGameView>(scene).FirstOrDefault();
                if (selection != null) ConfigureSelection(selection);
                var fatigue = All<FatigueHudView>(scene).FirstOrDefault();
                if (fatigue != null) ConfigureHud(fatigue);
                foreach (var tip in All<ItemTooltipView>(scene)) ConfigureTooltip(tip);
                foreach (var view in All<PerkChoiceView>(scene)) Body(view.focusedDescription);
                foreach (var view in All<ShopView>(scene)) Body(view.focusedDescription);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (!open) EditorSceneManager.CloseScene(scene, true); }
        }
        var profile = AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Profile);
        profile.readabilityRevision = 1; EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/Readability-Preparation.txt", "Saved inline ability panel, relocated back/detail controls, 9px descriptions, top-left fatigue face and shifted HP/gold.\n");
    }
    internal static void ConfigureSelection(OutGameView view)
    {
        var page = view.transform.Find("SelectionCenter");
        var menu = view.GetComponent<OutGameMenuView>();
        if (page == null || menu == null) return;
        // Preserve the legacy authored content without displaying the old blocking overlay.
        if (view.detailsPanel != null) view.detailsPanel.SetActive(false);
        var panel = Child(page, "AbilitySummary", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        Place(panel.transform,-95,18,158,104); panel.color = Ink; panel.raycastTarget = false;
        view.detailsPanel = panel.gameObject;
        view.abilityStats = Text(panel.transform,"PrimaryStats",-1,0,140,88,11);
        view.abilityStats.alignment = TextAlignmentOptions.TopLeft;
        view.abilityStats.textWrappingMode = TextWrappingModes.NoWrap;
        // The six decision-making rows fit without scrolling; secondary numbers stay available on hover.
        var extra = panel.GetComponent<HoverTooltipTrigger>() ?? panel.gameObject.AddComponent<HoverTooltipTrigger>();
        panel.raycastTarget = true; extra.anchor = panel.rectTransform; extra.view = view.startingRelicHover.view;
        view.extraStatsHover = extra;
        var hint = Text(panel.transform,"ExtraStatsHint",3,-44,140,14,9);
        hint.text = GameText.Get("ui.selection.extra.hint"); hint.alignment = TextAlignmentOptions.Right; hint.color = new Color32(216,187,98,255);
        view.abilityStats.raycastTarget = false;
        view.detailsButton = page.Find("Details").GetComponent<Button>();
        while (view.detailsButton.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(view.detailsButton.onClick, 0);
        Place(view.detailsButton.transform,-59,80,72,18);
        view.detailsButtonLabel = view.detailsButton.GetComponentInChildren<TextMeshProUGUI>(true);
        Place(view.detailsButtonLabel.transform,0,0,68,16); Body(view.detailsButtonLabel,10);
        var binding = view.detailsButtonLabel.GetComponent<GameTextBinding>(); if (binding != null) binding.enabled = false;
        Place(menu.back.transform,-124,-103,112,18);
        var back = menu.back.GetComponentInChildren<TextMeshProUGUI>(true);
        Place(back.transform,0,0,108,16); Body(back,11); back.alignment = TextAlignmentOptions.Center;
        var backBinding = back.GetComponent<GameTextBinding>() ?? back.gameObject.AddComponent<GameTextBinding>();
        backBinding.key = "ui.selection.back"; backBinding.Refresh();
        Place(view.characterName.transform,-125,82,102,28); view.characterName.fontSize = 88;
        view.characterName.enableAutoSizing = false; view.characterName.textWrappingMode = TextWrappingModes.NoWrap;
        view.selectedClassSymbol = Child(page,"SelectedClassSymbol",typeof(RectTransform),typeof(CanvasRenderer),typeof(ClassSymbolGraphic)).GetComponent<ClassSymbolGraphic>();
        Place(view.selectedClassSymbol.transform,-113,82,16,16); view.selectedClassSymbol.presentation = view.presentation;
        view.selectedClassSymbol.color = Paper; view.selectedClassSymbol.raycastTarget = false;
        view.Select(view.SelectedIndex); view.SetDetailsVisible(false);
        foreach (var component in new Component[] {view,menu,extra,view.detailsButton,view.selectedClassSymbol}) EditorUtility.SetDirty(component);
    }
    internal static void ConfigureTooltip(ItemTooltipView tip)
    {
        Body(Ref<TextMeshProUGUI>(tip,"title"),10);
        Body(Ref<TextMeshProUGUI>(tip,"description"));
        tip.expandToFit = true; EditorUtility.SetDirty(tip);
    }
    internal static void ConfigureHud(FatigueHudView hud)
    {
        var rect = hud.face.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(34,34)*4;
        hud.face.preserveAspect = true;
        var root = hud.GetComponentInParent<Canvas>().transform;
        var summary = root.Find("ArcadeStatus/CharacterSummary");
        if (summary != null) Place(summary,-83,0,150,24);
        EditorUtility.SetDirty(hud); EditorUtility.SetDirty(hud.face);
    }
    // Read-only evidence from the user's live Editor; no automated balance or encounter suite.
    internal static void Record(string label)
    {
        Canvas.ForceUpdateCanvases();
        var lines = new System.Text.StringBuilder(label+"\n");
        var selection=Object.FindFirstObjectByType<OutGameView>();
        if(selection!=null)
        {
            lines.AppendLine($"Selected={selection.SelectedIndex}; details={selection.DetailsVisible}; intro={selection.selectionStory.gameObject.activeInHierarchy}; label={selection.detailsButtonLabel.text}");
            foreach(var text in new[]{selection.characterName,selection.abilityStats})
            {text.ForceMeshUpdate(true);lines.AppendLine($"{text.name}: font={text.fontSize/4}; preferred={text.preferredWidth/4:0.#}x{text.preferredHeight/4:0.#}; rect={text.rectTransform.rect.size/4}; overflow={text.isTextOverflowing}; text={text.text.Replace("\n"," | ")}");}
        }
        foreach(var tip in Object.FindObjectsByType<ItemTooltipView>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.IsVisible))
        {
            var body=Ref<TextMeshProUGUI>(tip,"description");body.ForceMeshUpdate(true);
            lines.AppendLine($"Tooltip={tip.VisibleTitle}; font={body.fontSize/4}; preferredHeight={body.preferredHeight/4:0.#}; rect={body.rectTransform.rect.size/4}; overflow={body.isTextOverflowing}");
        }
        var hud=Object.FindFirstObjectByType<FatigueHudView>();
        if(hud!=null)lines.AppendLine($"Face: anchor={hud.face.rectTransform.anchorMin}; pivot={hud.face.rectTransform.pivot}; position={hud.face.rectTransform.anchoredPosition}; size={hud.face.rectTransform.sizeDelta/4}; visible={hud.face.gameObject.activeInHierarchy}");
        Directory.CreateDirectory("Docs/Production/Readability/Screenshots");
        File.AppendAllText("Docs/Production/Readability/Live.txt",lines+"\n");
        var frame=Object.FindFirstObjectByType<ArcadeRasterCamera>()?.Frame;
        if(frame==null)return;
        var previous=RenderTexture.active;
        try
        {
            RenderTexture.active=frame;var texture=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);texture.Apply();
            File.WriteAllBytes("Docs/Production/Readability/Screenshots/"+Path.GetFileName(label)+".png",texture.EncodeToPNG());Object.Destroy(texture);
        }
        finally{RenderTexture.active=previous;}
    }
    static void Body(TextMeshProUGUI text,int size=9)
    {
        if (text == null) return;
        text.fontSize = size*4; text.enableAutoSizing = false; text.lineSpacing = 0;
        EditorUtility.SetDirty(text);
    }
    static T Ref<T>(Object owner,string field) where T:Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static T[] All<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
    static GameObject Child(Transform parent,string name,params Type[] types)
    { var existing=parent.Find(name); if(existing!=null)return existing.gameObject;var go=new GameObject(name,types);go.transform.SetParent(parent,false);return go; }
    static void Place(Transform t,float x,float y,float w,float h)
    {var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;}
    static TextMeshProUGUI Text(Transform p,string name,float x,float y,float w,float h,int size)
    {
        var text=Child(p,name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        Place(text.transform,x,y,w,h);text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset");
        Body(text,size);text.color=Paper;text.raycastTarget=false;return text;
    }
}
