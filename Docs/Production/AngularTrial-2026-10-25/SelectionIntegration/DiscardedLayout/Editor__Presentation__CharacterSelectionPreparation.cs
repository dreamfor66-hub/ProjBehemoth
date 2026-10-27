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

// Saved UI, prepared once per revision. No manual baking or runtime hierarchy assembly.
[InitializeOnLoad]
internal static class CharacterSelectionPreparation
{
    const string ScenePath = "Assets/Scenes/OutGameScene.unity";
    const string ArtPath = "Assets/Art/Characters/Selection/Approved/";
    const string ProfilePath = "Assets/Data/Presentation/CharacterSelectionNative.asset";
    const string Revision = "selection-approved-1";
    const string Stamp = "Temp/CharacterSelection.signature";
    const string Request = "Temp/CharacterSelection.request";
    static bool busy;
    static double next;
    static readonly string[] Names = { "Warrior", "Rogue", "Barbarian", "Samurai", "Monk", "Wizard" };
    static readonly RectInt[] Figures = {
        new(62,55,948,1462),new(80,0,889,1334),new(7,29,1008,1500),
        new(151,52,796,1478),new(102,81,907,1429),new(133,17,772,1509)
    };
    // Source pixels measured from the top-left. Crops never repaint or resample a face.
    static readonly RectInt[] Faces = {
        new(415,55,300,310),new(545,110,250,250),new(340,40,350,350),
        new(315,95,295,295),new(375,80,270,280),new(400,95,290,290)
    };
    static readonly Color Ink = C("151715"), Panel = C("22261f"), Card = C("2b3027"),
        Border = C("4b5141"), Accent = C("c3a15d"), Bone = C("e4dbc5"), Muted = C("aeb49e");
    static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
    static CharacterSelectionPreparation() { EditorApplication.update += Poll; }

    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists(Request))
        {
            var command = File.ReadAllText(Request).Trim(); File.Delete(Request);
            if (command == "prepare" && !EditorApplication.isPlaying) Prepare();
            if (command == "open" && !EditorApplication.isPlaying) EditorSceneManager.OpenScene(ScenePath);
            if (command == "review" && EditorApplication.isPlaying)
            {
                var review = Object.FindFirstObjectByType<CharacterSelectionReview>() ?? new GameObject("Selection scoped review").AddComponent<CharacterSelectionReview>();
                review.Run();
            }
            if (command == "show" && EditorApplication.isPlaying) Object.FindFirstObjectByType<OutGameMenuView>()?.ShowCharacters();
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 3;
        if (Names.Any(n => !File.Exists(ArtPath + n + ".png"))) return;
        var signature = Revision + string.Join("/", Names.Select(n => File.GetLastWriteTimeUtc(ArtPath + n + ".png").Ticks));
        if (File.Exists(Stamp) && File.ReadAllText(Stamp) == signature) return;
        if (Prepare()) File.WriteAllText(Stamp, signature);
    }

    static bool Prepare()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        var loaded = scene.isLoaded;
        if (loaded && scene.isDirty) return false;
        busy = true;
        try
        {
            foreach (var name in Names)
            {
                var path = ArtPath + name + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            AddText();
            var profile = AssetDatabase.LoadAssetAtPath<ArcadePresentation>(ProfilePath);
            if (profile == null) { profile = ScriptableObject.CreateInstance<ArcadePresentation>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
            profile.arcade = false; profile.width = 1920; profile.height = 1080;
            EditorUtility.SetDirty(profile);
            if (!loaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            var menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<OutGameMenuView>(true)).Single();
            Build(menu, profile);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Docs/Production/AngularTrial-2026-10-25/SelectionIntegration");
            File.WriteAllText("Docs/Production/AngularTrial-2026-10-25/SelectionIntegration/Prepared.txt", "Saved production OutGameScene, six unchanged approved source images, native selection presentation.\n");
            return true;
        }
        catch (Exception e) { Debug.LogException(e); return false; }
        finally { if (!loaded && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); busy = false; }
    }

    static void AddText()
    {
        var table = BehemothTextEditor.Table;
        var entries = new[] {
            ("ui.selection.roster", "모험가"),
            ("ui.selection.keyboard", "← → 선택    ·    Enter 출발    ·    Esc 뒤로"),
            ("ui.selection.depart", "미궁으로"),
            ("ui.selection.starting", "시작 장비"),
            ("ui.selection.combat.style", "전투 방식")
        };
        foreach (var (key, value) in entries)
        {
            table.Set(key, value, "Character selection");
            // Append only these keys; never overwrite unrelated external CSV edits.
            if (!File.ReadLines(BehemothTextEditor.CsvPath).Any(line => line.StartsWith(key + ",", StringComparison.Ordinal) || line.StartsWith("\"" + key + "\",", StringComparison.Ordinal)))
                File.AppendAllText(BehemothTextEditor.CsvPath, "\r\n" + key + "," + value + ",Character selection");
        }
        EditorUtility.SetDirty(table);
    }

    static void Build(OutGameMenuView menu, ArcadePresentation profile)
    {
        var view = menu.selection;
        var page = (RectTransform)menu.characterPage.transform;
        var oldComposition = page.Find("Approved selection composition");
        if (oldComposition != null) Object.DestroyImmediate(oldComposition.gameObject);
        foreach (Transform child in page) child.gameObject.SetActive(false);
        var composition = Rect("Approved selection composition", page, 0, 0, 1536, 896);
        composition.anchorMin = Vector2.zero; composition.anchorMax = Vector2.one;
        composition.offsetMin = composition.offsetMax = Vector2.zero;
        var bg = composition.gameObject.AddComponent<Image>(); bg.color = Ink; bg.raycastTarget = false;
        var layout = menu.GetComponent<CharacterSelectionLayout>() ?? menu.gameObject.AddComponent<CharacterSelectionLayout>();
        menu.characterLayout = layout;
        layout.background = Ink; layout.card = Panel; layout.selectedCard = Card; layout.border = Border;
        layout.accent = Accent; layout.text = Bone; layout.muted = Muted;
        layout.raster = sceneCamera(menu).GetComponent<ArcadeRasterCamera>();
        layout.normalPresentation = view.presentation; layout.nativePresentation = profile;
        var canvas = menu.GetComponent<Canvas>();
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.referenceResolution = new Vector2(1536, 896);
        // Figure on a quiet solid field, with no inherited pixel material or backdrop.
        Block("Figure field", composition, 854, 0, 682, 896, C("1e221c"));
        Block("Figure divider", composition, 854, 0, 1, 896, C("343b2e"));
        Block("Top rule", composition, 96, 115, 1328, 1, Border);
        var figureArea = Rect("Full figure area", composition, 860, 130, 610, 600);
        var figureRect = Rect("Approved full figure", figureArea, 0, 0, 610, 600);
        layout.figure = figureRect.gameObject.AddComponent<RawImage>(); layout.figure.raycastTarget = false;
        layout.figureAspect = figureRect.gameObject.AddComponent<AspectRatioFitter>();
        layout.figureAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        layout.artwork = Names.Select((name, i) => new CharacterSelectionLayout.Artwork {
            character = (CharacterKind)i, texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath + name + ".png"),
            figureUv = Uv(Figures[i]), faceUv = Uv(Faces[i])
        }).ToArray();

        Text("Page heading", composition, 96, 58, 400, 40, 27, Muted, "ui.outgame.characters");
        var back = Button("Back", composition, 1284, 44, 156, 52, Panel);
        ButtonText(back, view.characterName, 23, "ui.selection.back", Bone);
        menu.back = back;
        view.characterName = Text("Selected name", composition, 96, 156, 540, 90, 72, Bone);
        view.selectedClassSymbol = null;
        view.detailsButton = Button("Ability details", composition, 648, 177, 158, 48, Panel);
        view.detailsButtonLabel = ButtonText(view.detailsButton, view.characterName, 22, "ui.outgame.details", Accent);
        Text("Combat style heading", composition, 100, 275, 470, 30, 19, Accent, "ui.selection.combat.style");
        view.compactDescription = Text("Combat style", composition, 96, 316, 664, 88, 31, Bone);
        view.selectionStory = Text("Introduction", composition, 100, 427, 650, 108, 23, Muted);
        view.selectionStats = Text("Primary stats", composition, 100, 555, 650, 36, 25, Bone);
        view.detailsPanel = Rect("Ability panel", composition, 96, 420, 680, 180).gameObject;
        view.abilityStats = Text("Ability stats", (RectTransform)view.detailsPanel.transform, 4, 0, 650, 164, 24, Bone);
        view.abilityStats.text = GameText.Format("ui.selection.ability.stats", 100, 30, 35, 14, 24, .85f);

        var relic = Button("Starting equipment", composition, 96, 616, 680, 88, Panel);
        view.selectionRelicIcon = Block("Relic icon", (RectTransform)relic.transform, 20, 20, 48, 48, Color.white);
        Text("Relic heading", (RectTransform)relic.transform, 92, 12, 520, 26, 18, Muted, "ui.selection.starting");
        view.selectionRelic = Text("Relic name", (RectTransform)relic.transform, 92, 42, 550, 34, 25, Bone);
        view.startingRelicScroll = null;

        Text("Roster heading", composition, 96, 730, 400, 27, 19, Muted, "ui.selection.roster");
        layout.pageNumber = Text("Roster number", composition, 745, 730, 145, 27, 19, Muted);
        layout.pageNumber.alignment = TextAlignmentOptions.TopRight;
        view.characterButtons = new Button[6]; view.characterLabels = new TextMeshProUGUI[6]; view.selectionMarks = new Image[6];
        layout.faces = new RawImage[6]; layout.names = new TextMeshProUGUI[6]; layout.borders = new Image[6]; layout.indicators = new Image[6];
        for (var i = 0; i < 6; i++)
        {
            var x = 96 + i * 136;
            var frame = Block(Names[i] + " frame", composition, x, 768, 122, 104, Border);
            layout.borders[i] = frame;
            var button = Button(Names[i], frame.rectTransform, 2, 2, 118, 100, Panel);
            var faceRect = Rect("Face crop", (RectTransform)button.transform, 18, 3, 82, 72);
            var face = faceRect.gameObject.AddComponent<RawImage>(); face.raycastTarget = false;
            // Square source crops with generous head room, clipped only by their own image rect.
            layout.faces[i] = face;
            var label = Text("Name", (RectTransform)button.transform, 0, 76, 118, 24, 19, Bone);
            label.alignment = TextAlignmentOptions.Top;
            view.characterButtons[i] = button; view.characterLabels[i] = layout.names[i] = label;
            var mark = Block("Selected", frame.rectTransform, 0, 101, 122, 3, Accent);
            view.selectionMarks[i] = layout.indicators[i] = mark;
            var nav = button.navigation; nav.mode = Navigation.Mode.Explicit;
            button.navigation = nav;
        }
        for (var i = 0; i < 6; i++)
        {
            var nav = view.characterButtons[i].navigation;
            nav.selectOnLeft = view.characterButtons[(i + 5) % 6]; nav.selectOnRight = view.characterButtons[(i + 1) % 6];
            view.characterButtons[i].navigation = nav;
        }
        view.startButton = Button("Enter labyrinth", composition, 1068, 784, 356, 72, Accent);
        ButtonText(view.startButton, view.characterName, 31, "ui.selection.depart", Ink);
        // Keep the navigation hint above the cards, away from their click surfaces.
        Text("Keyboard hint", composition, 1000, 737, 424, 26, 17, Muted, "ui.selection.keyboard").alignment = TextAlignmentOptions.TopRight;

        var popup = Rect("Equipment tooltip", composition, 400, 400, 520, 240);
        var popupBg = popup.gameObject.AddComponent<Image>(); popupBg.color = C("30362b"); popupBg.raycastTarget = false;
        var tipTitle = Text("Title", popup, 24, 16, 472, 44, 26, Bone);
        var tipBody = Text("Description", popup, 24, 70, 472, 150, 23, Bone);
        var tip = popup.gameObject.AddComponent<ItemTooltipView>(); tip.Configure(popup.gameObject, tipTitle, tipBody, composition); tip.expandToFit = true; tip.maximumWidth = 700;
        view.startingRelicHover = relic.gameObject.AddComponent<HoverTooltipTrigger>(); view.startingRelicHover.view = tip; view.startingRelicHover.anchor = (RectTransform)relic.transform;
        // Equipment is readable with pointer hover and with keyboard selection.
        view.extraStatsHover = view.detailsPanel.AddComponent<HoverTooltipTrigger>(); view.extraStatsHover.view = tip;
        view.extraStatsHover.anchor = (RectTransform)view.detailsPanel.transform;
        menu.detailHeading = view.characterName;
        view.detailsPanel.SetActive(false);
        // Editor preview uses the same serialized surfaces as Play mode.
        view.Select(0); view.SetDetailsVisible(false); layout.Refresh(view, 0);
        menu.titlePage.SetActive(false); menu.optionsPage.SetActive(false); menu.characterPage.SetActive(true);
        layout.ShowPage(true);
        EditorUtility.SetDirty(menu); EditorUtility.SetDirty(view); EditorUtility.SetDirty(layout);
    }

    static Camera sceneCamera(Component component) => component.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First();
    static Rect Uv(RectInt top) => new(top.x / 1024f, (1536 - top.y - top.height) / 1536f, top.width / 1024f, top.height / 1536f);
    static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
    }
    static Image Block(string name, Transform parent, float x, float y, float width, float height, Color color)
    {
        var image = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    static Button Button(string name, Transform parent, float x, float y, float width, float height, Color color)
    {
        var image = Block(name, parent, x, y, width, height, color); image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.pressedColor = new Color(.7f, .7f, .7f); colors.selectedColor = Color.white; colors.fadeDuration = .08f; button.colors = colors;
        return button;
    }
    static TextMeshProUGUI Text(string name, Transform parent, float x, float y, float width, float height, float size, Color color, string key = null)
    {
        var label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        var typography = AssetDatabase.FindAssets("t:TypographyProfile").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<TypographyProfile>).First();
        label.font = typography.Font(Typeface.Galmuri11); label.fontSize = size; label.color = color; label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.TopLeft; label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        var style = label.gameObject.AddComponent<TypographyRoleBinding>(); style.profile = typography; style.role = TextRole.Base; style.designSize = size; style.snapSize = false;
        if (!string.IsNullOrEmpty(key)) { var binding = label.gameObject.AddComponent<GameTextBinding>(); binding.key = key; binding.Refresh(); }
        style.Apply(); return label;
    }
    static TextMeshProUGUI ButtonText(Button button, TextMeshProUGUI template, float size, string key, Color color)
    {
        var rect = (RectTransform)button.transform;
        var label = Text("Label", rect, 8, 0, rect.rect.width - 16, rect.rect.height, size, color, key);
        label.alignment = TextAlignmentOptions.Center; return label;
    }
}
