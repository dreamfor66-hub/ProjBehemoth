using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Idempotent saved migration of the F1 surface; never rebuilds gameplay content.</summary>
[InitializeOnLoad]
internal static class DebugCatalogPreparation
{
    const string ScenePath = "Assets/Scenes/MainGameScene.unity";
    const string PrefabPath = "Assets/Prefabs/UI/DebugCatalogTile.prefab";
    const string PortraitRoot = "Assets/Art/Enemies/Portraits/Catalog/";
    const string Report = "Docs/Production/DebugCatalog-2026-10-29/";
    const string Stamp = "Assets/Data/Presentation/DebugCatalogInstalled.txt";
    const string Revision = "image-catalog-3";
    static double next;
    static TypographyProfile typography;
    static readonly Color Panel = new Color32(23, 24, 28, 255), Card = new Color32(39, 41, 47, 255), Gold = new Color32(210, 170, 88, 255);
    static DebugCatalogPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 4;
        if (File.Exists(Stamp) && File.ReadAllText(Stamp) == Revision) return;
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (scene.isLoaded && scene.isDirty) return;
        try
        {
            Directory.CreateDirectory(Report);
            PreparePortraits(); PrepareText(); PrepareScene();
            AssetDatabase.SaveAssets();
            File.WriteAllText(Stamp, Revision); AssetDatabase.ImportAsset(Stamp);
            File.WriteAllText(Report + "Installed.txt", "44 current-art portrait crops saved as native Sprite assets. F1 catalog: four image tabs, dynamically growing pool, masked vertical scrolling with auto scrollbar, anchored side tooltip, selected border/name, close and grant/battle feedback.\n");
        }
        catch (Exception e) { Directory.CreateDirectory(Report); File.WriteAllText(Report + "PreparationError.txt", e.ToString()); next += 60; Debug.LogException(e); }
    }

    // Normalized top-left framing authored after reviewing the installed Idle paintings.
    // They only select a texture rectangle: no repainting or change to the combat sprites.
    static readonly Vector4[] FaceCrops = {
        new(.21f,.17f,.45f,.45f), new(.21f,.05f,.40f,.40f), new(.34f,.16f,.48f,.48f), new(.31f,.01f,.36f,.36f),
        new(.25f,.20f,.54f,.54f), new(.38f,.02f,.34f,.34f), new(.32f,.02f,.36f,.36f), new(.20f,.32f,.62f,.62f),
        new(.36f,.24f,.30f,.48f), new(.24f,.06f,.50f,.50f), new(.26f,.14f,.52f,.52f), new(.33f,.44f,.34f,.34f),
        new(.30f,.17f,.40f,.40f), new(.22f,.02f,.39f,.39f), new(.22f,.07f,.50f,.50f), new(.38f,.19f,.45f,.45f),
        new(.38f,.05f,.38f,.38f), new(.27f,.09f,.50f,.50f), new(.38f,.22f,.43f,.43f), new(.32f,.11f,.37f,.37f),
        new(.41f,.27f,.52f,.52f), new(.31f,.01f,.42f,.42f), new(.21f,.23f,.64f,.64f), new(.27f,.03f,.59f,.59f),
        new(.13f,.10f,.67f,.67f), new(.33f,.01f,.37f,.37f), new(.28f,.17f,.52f,.52f), new(.28f,.22f,.51f,.51f),
        new(.23f,.22f,.54f,.54f), new(.36f,.16f,.49f,.49f), new(.27f,.27f,.48f,.48f), new(.29f,.05f,.45f,.45f),
        new(.29f,.00f,.42f,.42f), new(.32f,.15f,.42f,.42f), new(.32f,.07f,.40f,.40f), new(.36f,.17f,.40f,.40f),
        new(.22f,.01f,.46f,.46f), new(.32f,.19f,.49f,.49f), new(.20f,.16f,.60f,.60f), new(.33f,.00f,.33f,.33f),
        new(.33f,.00f,.33f,.33f), new(.33f,.00f,.33f,.33f), new(.30f,.23f,.53f,.53f), new(.29f,.24f,.45f,.45f)
    };
    static void PreparePortraits()
    {
        Directory.CreateDirectory(PortraitRoot); AssetDatabase.Refresh();
        var monsters = AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data/Combat/Monsters" })
            .Select(g => DungeonSlash.DesignerAssetPaths.Load<MonsterData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(m => m.name, StringComparer.Ordinal).ToArray();
        if (monsters.Length != FaceCrops.Length) throw new InvalidDataException("Portrait source roster changed; review new face framing before installing.");
        for (var i = 0; i < monsters.Length; i++)
        {
            var monster = monsters[i]; var source = monster.combatAnimation != null ? monster.combatAnimation.idle : monster.portrait;
            if (source == null) throw new InvalidDataException("Missing portrait source: " + monster.name);
            var path = PortraitRoot + monster.name + ".asset";
            var portrait = DungeonSlash.DesignerAssetPaths.Load<Sprite>(path);
            if (portrait == null)
            {
                var r = source.rect; var crop = FaceCrops[i];
                var side = Mathf.Min(source.texture.width, source.texture.height, Mathf.Max(r.width * crop.z, r.height * crop.w));
                var center = new Vector2(r.x + r.width * (crop.x + crop.z * .5f), r.yMax - r.height * (crop.y + crop.w * .5f));
                var rect = new Rect(Mathf.Clamp(center.x - side * .5f, 0, source.texture.width - side), Mathf.Clamp(center.y - side * .5f, 0, source.texture.height - side), side, side);
                portrait = Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
                portrait.name = monster.name + "-Portrait"; AssetDatabase.CreateAsset(portrait, path);
            }
            monster.portrait = portrait; EditorUtility.SetDirty(monster);
        }
    }

    static void PrepareText()
    {
        var table = BehemothTextEditor.Table;
        var rows = new[] {
            ("title", "테스트 도구"), ("hint", "아이콘 선택 → 아래 버튼으로 적용 · 휠 / 드래그로 스크롤"),
            ("tab0", "몬스터"), ("tab1", "퍽"), ("tab2", "유물"), ("tab3", "포션"),
            ("count", "{0}  ·  {1}개"), ("selected", "선택: {0}"), ("choose", "아이콘을 선택하세요."),
            ("grant0", ""), ("grant1", "퍽 추가"), ("grant2", "유물 추가"), ("grant3", "포션 추가"),
            ("close", "닫기 · F1"), ("hover", "아이콘 위에 마우스를 올리면\n상세 정보가 표시됩니다."),
            ("battle", "배틀 시작"), ("monsterstats", "HP {0:0.#}\n일반 공격 간격 {1:0.#}초 · 강타 간격 {2:0.#}초")
        };
        var csv = ScriptableObject.CreateInstance<GameTextDatabase>(); csv.ImportCsv(File.ReadAllText(BehemothTextEditor.CsvPath));
        foreach (var row in rows) { table.Set("ui.debugcatalog." + row.Item1, row.Item2, "F1 image catalog"); csv.Set("ui.debugcatalog." + row.Item1, row.Item2, "F1 image catalog"); }
        File.WriteAllText(BehemothTextEditor.CsvPath, csv.ExportCsv(), new System.Text.UTF8Encoding(true));
        Object.DestroyImmediate(csv); EditorUtility.SetDirty(table); AssetDatabase.ImportAsset(BehemothTextEditor.CsvPath);
    }

    static RectTransform Node(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var node = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); node.SetParent(parent, false);
        node.anchorMin = node.anchorMax = node.pivot = new Vector2(.5f, .5f); node.anchoredPosition = position; node.sizeDelta = size; return node;
    }
    static Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    { var image = Node(name, parent, position, size).gameObject.AddComponent<Image>(); image.color = color; return image; }
    static TextMeshProUGUI Label(string name, Transform parent, Vector2 pos, Vector2 size, string key, float fontSize = 28)
    {
        var label = Node(name, parent, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = typography.Font(Typeface.Base); label.fontSize = fontSize; label.color = new Color32(237, 230, 209, 255);
        label.text = string.IsNullOrEmpty(key) ? "" : GameText.Get(key); label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.MidlineLeft; label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Ellipsis;
        var role = label.gameObject.AddComponent<TypographyRoleBinding>(); role.profile = typography; role.role = TextRole.Hint; role.designSize = fontSize; role.snapSize = false; role.Apply();
        if (!string.IsNullOrEmpty(key)) { var binding = label.gameObject.AddComponent<GameTextBinding>(); binding.key = key; }
        return label;
    }
    static Button Button(string name, Transform parent, Vector2 pos, Vector2 size, string key, out TextMeshProUGUI label)
    {
        var image = Image(name, parent, pos, size, Card); var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1.35f,1.28f,1.1f); colors.selectedColor = Color.white; button.colors = colors;
        label = Label("Label", image.transform, Vector2.zero, size - new Vector2(16, 4), key, 28); label.alignment = TextAlignmentOptions.Center;
        return button;
    }
    static DebugCatalogTile CreateTile(Transform parent)
    {
        var bg = Image("CatalogTile", parent, Vector2.zero, new Vector2(104, 104), Card);
        var tile = bg.gameObject.AddComponent<DebugCatalogTile>(); tile.button = bg.gameObject.AddComponent<Button>(); tile.button.targetGraphic = bg;
        var nav = tile.button.navigation; nav.mode = Navigation.Mode.None; tile.button.navigation = nav;
        var frame = Image("Selected", bg.transform, Vector2.zero, new Vector2(104, 104), Gold); frame.raycastTarget = false;
        var inset = Image("Inset", frame.transform, Vector2.zero, new Vector2(96, 96), Card); inset.raycastTarget = false;
        tile.selectedFrame = frame.gameObject; frame.gameObject.SetActive(false);
        tile.icon = Image("Icon", bg.transform, Vector2.zero, new Vector2(88, 88), Color.white); tile.icon.preserveAspect = true; tile.icon.raycastTarget = false;
        tile.fallback = Label("MissingIcon", bg.transform, Vector2.zero, new Vector2(80, 80), null, 30); tile.fallback.alignment = TextAlignmentOptions.Center; tile.fallback.gameObject.SetActive(false);
        return tile;
    }
    static void PrepareScene()
    {
        typography = AssetDatabase.LoadAssetAtPath<TypographyProfile>(TypographyPreparation.ProfilePath);
        var scene = SceneManager.GetSceneByPath(ScenePath); var loaded = scene.isLoaded;
        if (!loaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            var overlay = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DebugCheatOverlay>(true)).Single();
            var so = new SerializedObject(overlay); var panel = (GameObject)so.FindProperty("panel").objectReferenceValue;
            var root = (RectTransform)panel.transform;
            while (root.childCount > 0) Object.DestroyImmediate(root.GetChild(0).gameObject);
            foreach (var component in panel.GetComponents<MonoBehaviour>()) if (component is not UnityEngine.UI.Image) Object.DestroyImmediate(component);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f,.5f); root.anchoredPosition = new Vector2(0,-24); root.sizeDelta = new Vector2(1380,860);
            panel.AddComponent<DebugCatalogPanelLayout>();
            var bg = panel.GetComponent<Image>(); if(bg==null)bg=panel.AddComponent<Image>(); bg.sprite=null; bg.color=Panel;
            var outline=panel.GetComponent<Outline>()??panel.AddComponent<Outline>();outline.effectColor=Gold;outline.effectDistance=new Vector2(2,-2);
            Label("Title",root,new Vector2(-420,378),new Vector2(480,60),"ui.debugcatalog.title",38);
            overlay.closeButton=Button("Close",root,new Vector2(566,378),new Vector2(188,48),"ui.debugcatalog.close",out _);
            overlay.categoryButtons=new Button[4];
            for(var i=0;i<4;i++)overlay.categoryButtons[i]=Button("Category"+i,root,new Vector2(-553+i*226,302),new Vector2(212,52),"ui.debugcatalog.tab"+i,out _);
            overlay.catalogLabel=Label("CatalogCount",root,new Vector2(-218,246),new Vector2(880,40),null,24);
            var scrollBg=Image("CatalogScroll",root,new Vector2(-212,-10),new Vector2(900,470),new Color32(17,18,21,255));
            var scroll=scrollBg.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=64;
            var viewport=Node("Viewport",scrollBg.transform,new Vector2(-10,0),new Vector2(872,470));viewport.gameObject.AddComponent<RectMask2D>();
            var content=Node("Content",viewport,Vector2.zero,new Vector2(872,0)); content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;
            var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(104,104);grid.spacing=new Vector2(16,16);grid.padding=new RectOffset(8,8,8,8);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=7;
            var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;
            var track=Image("Scrollbar",scrollBg.transform,new Vector2(436,0),new Vector2(12,470),Card);var scrollbar=track.gameObject.AddComponent<Scrollbar>();
            var handle=Image("Handle",track.transform,Vector2.zero,Vector2.zero,Gold);scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
            overlay.catalogScroll=scroll;
            var template=CreateTile(root);overlay.tilePrefab=PrefabUtility.SaveAsPrefabAsset(template.gameObject,PrefabPath).GetComponent<DebugCatalogTile>();Object.DestroyImmediate(template.gameObject);
            var monsters=AssetDatabase.FindAssets("t:MonsterData",new[]{"Assets/Data/Combat/Monsters"}).Select(g=>DungeonSlash.DesignerAssetPaths.Load<MonsterData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(m=>m.name,StringComparer.Ordinal).ToArray();
            overlay.monsterCatalog=monsters.ToList();
            foreach(var monster in monsters){var tile=(GameObject)PrefabUtility.InstantiatePrefab(overlay.tilePrefab.gameObject,content);var view=tile.GetComponent<DebugCatalogTile>();view.icon.sprite=monster.portrait;view.previewIcon=monster.portrait;}
            overlay.selectionLabel=Label("SelectedName",root,new Vector2(-212,-282),new Vector2(900,44),"ui.debugcatalog.choose",28);
            var battle=Button("StartBattle",root,new Vector2(-486,-342),new Vector2(340,56),"ui.debugcatalog.battle",out _);
            var infinite=Button("InfiniteBattle",root,new Vector2(-74,-342),new Vector2(340,56),null,out var infiniteLabel);
            infiniteLabel.text=GameText.Get("ui.debugcheatoverlay.841ae6d2");
            var grant=Button("Grant",root,new Vector2(-486,-342),new Vector2(340,56),"ui.debugcatalog.grant1",out var grantLabel);grant.gameObject.SetActive(false);overlay.grantButton=grant;overlay.grantLabel=grantLabel;
            var status=Label("Status",root,new Vector2(0,-400),new Vector2(1320,36),"ui.debugcatalog.hint",23);
            Label("HoverHint",root,new Vector2(466,160),new Vector2(350,112),"ui.debugcatalog.hover",25);
            overlay.tooltipAnchor=Node("TooltipAnchor",root,new Vector2(243,230),new Vector2(2,1));
            var tip=Image("CatalogTooltip",root,new Vector2(454,100),new Vector2(366,260),Card);tip.raycastTarget=false;tip.gameObject.AddComponent<CanvasGroup>().blocksRaycasts=false;
            var tipTitle=Label("Title",tip.transform,new Vector2(0,95),new Vector2(310,62),null,32);
            var tipBody=Label("Description",tip.transform,Vector2.zero,new Vector2(310,170),null,25);tipBody.alignment=TextAlignmentOptions.TopLeft;
            overlay.catalogTooltip=tip.gameObject.AddComponent<ItemTooltipView>();overlay.catalogTooltip.Configure(tip.gameObject,tipTitle,tipBody,root);
            so=new SerializedObject(overlay);
            so.FindProperty("startBattleButton").objectReferenceValue=battle;
            so.FindProperty("infiniteBattleButton").objectReferenceValue=infinite;so.FindProperty("infiniteBattleLabel").objectReferenceValue=infiniteLabel;
            so.FindProperty("statusLabel").objectReferenceValue=status;
            foreach(var field in new[]{"monsterButtons","perkButtons","equipmentButtons"})so.FindProperty(field).ClearArray();
            so.FindProperty("addPerkButton").objectReferenceValue=null;so.FindProperty("addEquipmentButton").objectReferenceValue=null;
            so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(overlay);panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally { if(!loaded)EditorSceneManager.CloseScene(scene,true); }
    }
}
