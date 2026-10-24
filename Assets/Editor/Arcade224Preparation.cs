using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class Arcade224Preparation
{
    const string Root="Assets/Art/Arcade224/";
    const string Settings="Assets/Data/Presentation/Arcade224.asset";
    const string Backup="Assets/Editor/Arcade-ArtReferences.asset";
    static TMP_FontAsset font;
    static ArcadePresentation art;
    static readonly Color Ink=new Color32(11,12,22,255), Paper=new Color32(222,211,175,255), Gold=new Color32(216,187,98,255);
    static Arcade224Preparation()=>EditorApplication.delayCall+=Prepare;
    internal static void Prepare()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Prepare;return;}
        if(!File.Exists(Root+"Actors.png")||!File.Exists(Root+"Castle.png")||!File.Exists(Root+"Icons.png"))return;
        art=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Settings);
        if(art!=null&&art.revision>=1){if(art.revision<3)Polish();if(art.revision<4&&File.Exists(Root+"RearHeroes.png")&&File.Exists(Root+"FrontEnemies.png")&&File.Exists(Root+"FirstPersonHall.png"))FirstPerson();return;}
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)){EditorApplication.delayCall+=Prepare;return;}
        try
        {
            Directory.CreateDirectory("Assets/Data/Presentation");
            if(art==null){art=ScriptableObject.CreateInstance<ArcadePresentation>();AssetDatabase.CreateAsset(art,Settings);}
            font=MakeFont();art.frame=Frame();art.actors=Slice(Root+"Actors.png",6,4,256);art.icons=Slice(Root+"Icons.png",8,8,128);
            art.rooms=new[]{Import(Root+"Castle.png",512)};
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"ArcadePalette.shader");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Palette.mat");if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"Palette.mat");}art.paletteMaterial=material;
            var archive=CaptureReferences();ApplyArcadeArt(archive);SeedText();
            foreach(var path in new[]{"Assets/Scenes/OutGameScene.unity","Assets/Scenes/MainGameScene.unity"})Scene(path);
            Prefabs();InstallRenderer();
            art.revision=1;art.arcade=true;EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
            PlayerSettings.defaultScreenWidth=1152;PlayerSettings.defaultScreenHeight=672;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            File.WriteAllText("Logs/Arcade224-Preparation.txt","384x224 /16 colors. Saved selection, full-width combat, contextual drawers, focused reward/shop and geometry art archive prepared.");
        }catch(Exception error){Debug.LogException(error);}
    }
    static ArtReferenceArchive CaptureReferences()
    {
        var archive=AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>(Backup);if(archive!=null)return archive;
        archive=ScriptableObject.CreateInstance<ArtReferenceArchive>();AssetDatabase.CreateAsset(archive,Backup);
        using var zip=ZipFile.OpenRead("Docs/Production/ArcadeRevision/Geometry-Visual-Baseline.zip");
        foreach(var guid in AssetDatabase.FindAssets("t:ScriptableObject",new[]{"Assets/Data/Behemoth","Assets/Data/DungeonSlashPrototype"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var target=AssetDatabase.LoadMainAssetAtPath(path);var so=new SerializedObject(target);var p=so.GetIterator();
            string yaml="";var entry=zip.GetEntry(path);if(entry!=null){using var reader=new StreamReader(entry.Open());yaml=reader.ReadToEnd();}
            while(p.NextVisible(true))
            {
                if(p.propertyType!=SerializedPropertyType.ObjectReference||!p.type.Contains("Sprite"))continue;
                Object legacy=null;
                if(!p.propertyPath.Contains("Array"))
                {
                    var match=Regex.Match(yaml,@"(?m)^  "+Regex.Escape(p.name)+@": \{fileID: (-?\d+), guid: ([a-f0-9]+)");
                    if(match.Success){var assetPath=AssetDatabase.GUIDToAssetPath(match.Groups[2].Value);legacy=AssetDatabase.LoadAllAssetsAtPath(assetPath).FirstOrDefault(a=>AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a,out string _,out long id)&&id.ToString()==match.Groups[1].Value);}
                }
                archive.entries.Add(new(){asset=target,property=p.propertyPath,geometry=legacy,arcade=p.objectReferenceValue});
            }
        }
        var fontEntry=zip.GetEntry("Assets/Art/TextMeshPro/Resources/TMP Settings.asset");
        if(fontEntry!=null){using var reader=new StreamReader(fontEntry.Open());var match=Regex.Match(reader.ReadToEnd(),@"m_defaultFontAsset: \{fileID: \d+, guid: ([a-f0-9]+)");if(match.Success)archive.geometryFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(match.Groups[1].Value));}
        EditorUtility.SetDirty(archive);return archive;
    }
    static void ApplyArcadeArt(ArtReferenceArchive archive)
    {
        var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/Behemoth/GameContent.asset");
        foreach(var e in archive.entries)
        {
            var so=new SerializedObject(e.asset);var p=so.FindProperty(e.property);Sprite sprite=null;
            if(e.asset is PlayerCombatData player)sprite=e.property=="combatSprite"&&art.rearHeroes?.Length==6?art.rearHeroes[new[]{0,1,4,2,3,5}[(int)player.character]]:art.actors[(int)player.character];
            else if(e.asset is MonsterData monster)
            {
                var names=new[]{"MazeGuardian","Minotaur","Bat","Leech","Skeleton","FrostQueen","FrostOgre","FrostBat","IceWraith","Yeti","SoulEater","SoulKnight","AshHound","ShadeLancer","VoidMaw","MerchantMutant","Mimic"};
                var indices=new[]{10,9,6,8,7,11,12,13,14,15,16,17,18,19,20,21,22};
                var n=Array.FindIndex(names,name=>monster.name.Contains(name));sprite=art.frontEnemies?.Length==18?art.frontEnemies[n<0?4:n]:art.actors[n<0?7:indices[n]];
            }
            else if(e.asset is CompanionData)sprite=art.frontEnemies?.Length==18?art.frontEnemies[17]:art.actors[23];
            else if(e.asset is RandomEventData story)sprite=story.name.Contains("MagicTeacher")?art.actors[5]:story.name.Contains("StrayDog")?art.actors[23]:art.icons[48+Mathf.Abs(content.events.IndexOf(story))%16];
            else if(e.asset is SpellData spell){var index=content.spells.IndexOf(spell);sprite=art.icons[Mathf.Clamp(32+index,32,47)];}
            else if(e.asset is PerkData perk){sprite=art.icons[(content.perks.IndexOf(perk)+6)%64];}
            else if(e.asset is EquipmentData item){var index=content.equipment.IndexOf(item);sprite=art.icons[Mathf.Abs(index)%64];}
            if(p!=null){p.objectReferenceValue=sprite;so.ApplyModifiedPropertiesWithoutUndo();e.arcade=sprite;EditorUtility.SetDirty(e.asset);}
        }
        EditorUtility.SetDirty(archive);
    }
    [MenuItem("Behemoth/Presentation/Restore original geometric art (keep systems)")]
    internal static void Geometry()=>ApplyStyle(false);
    [MenuItem("Behemoth/Presentation/Use 224p arcade art")]
    internal static void Arcade()=>ApplyStyle(true);
    internal static void ApplyStyle(bool arcade)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before changing saved presentation.");
        art=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Settings);var archive=AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>(Backup);
        if(art==null||archive==null)throw new InvalidOperationException("Presentation archive is not ready.");
        foreach(var e in archive.entries){var so=new SerializedObject(e.asset);var p=so.FindProperty(e.property);if(p==null)continue;p.objectReferenceValue=arcade?e.arcade:e.geometry;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(e.asset);}
        art.arcade=arcade;EditorUtility.SetDirty(art);
        EnsureColors(archive);
        foreach(var tint in archive.colors){var so=new SerializedObject(tint.asset);so.FindProperty(tint.property).colorValue=arcade?tint.arcade:tint.geometry;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(tint.asset);}
        CombatAnimationPreparation.ApplyPrefabStyle(arcade);
        foreach(var path in new[]{"Assets/Scenes/MainGameScene.unity","Assets/Scenes/OutGameScene.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var bindings=All<PresentationImageBindings>(scene).FirstOrDefault();
            if(bindings==null)
            {
                var canvas=All<Canvas>(scene).First();bindings=canvas.gameObject.AddComponent<PresentationImageBindings>();
                bindings.bindings=All<Image>(scene).Where(i=>i.sprite!=null&&(AssetDatabase.GetAssetPath(i.sprite).Contains("Arcade224")||AssetDatabase.GetAssetPath(i.sprite).Contains("AshenVow"))).Select(i=>new PresentationImageBindings.Binding{image=i,arcade=i.sprite,color=i.color}).ToArray();
            }
            bindings.Apply(arcade);EditorUtility.SetDirty(bindings);
            foreach(var title in All<ArtTitleView>(scene))title.Refresh();
            foreach(var text in All<TextMeshProUGUI>(scene))if(!arcade&&archive.geometryFont!=null)text.font=archive.geometryFont;else if(arcade)text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"ArcadeFont.asset");
            var backdrop=All<StageBackdropView>(scene).FirstOrDefault();if(backdrop!=null)backdrop.stages=arcade?art.rooms:Array.Empty<Sprite>();
            var selection=All<OutGameView>(scene).FirstOrDefault();if(selection!=null)selection.Select(0);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!open)EditorSceneManager.CloseScene(scene,true);
        }
        AssetDatabase.SaveAssets();
    }
    static void EnsureColors(ArtReferenceArchive archive)
    {
        foreach(var e in archive.entries.Where(e=>e.geometry==null&&e.property.EndsWith("icon")))e.geometry=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/RunIcons/Perk_BrightEdge.png");
        foreach(var e in archive.entries.Where(e=>e.geometry==null&&e.property=="illustration"&&e.asset is RandomEventData))e.geometry=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/T_UI_EventMerchant.png");
        if(archive.colors.Count>0)return;
        using var zip=ZipFile.OpenRead("Docs/Production/ArcadeRevision/Geometry-Visual-Baseline.zip");
        foreach(var target in archive.entries.Select(e=>e.asset).OfType<PlayerCombatData>().Distinct())
        {
            var entry=zip.GetEntry(AssetDatabase.GetAssetPath(target));if(entry==null)continue;using var reader=new StreamReader(entry.Open());
            var m=Regex.Match(reader.ReadToEnd(),@"characterColor: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}");if(!m.Success)continue;
            float V(int n)=>float.Parse(m.Groups[n].Value,System.Globalization.CultureInfo.InvariantCulture);
            archive.colors.Add(new(){asset=target,property="characterColor",geometry=new Color(V(1),V(2),V(3),V(4)),arcade=target.characterColor});
        }
        EditorUtility.SetDirty(archive);
    }
    internal static void VerifyRollback()
    {
        var archive=AssetDatabase.LoadAssetAtPath<ArtReferenceArchive>(Backup);EnsureColors(archive);
        string Fingerprint()
        {
            var list=new List<string>();foreach(var asset in archive.entries.Select(e=>e.asset).Distinct())
            {var clone=Object.Instantiate(asset);var so=new SerializedObject(clone);foreach(var e in archive.entries.Where(e=>e.asset==asset)){var p=so.FindProperty(e.property);if(p!=null)p.objectReferenceValue=null;}foreach(var tint in archive.colors.Where(t=>t.asset==asset))so.FindProperty(tint.property).colorValue=Color.clear;so.ApplyModifiedPropertiesWithoutUndo();list.Add(EditorJsonUtility.ToJson(clone));Object.DestroyImmediate(clone);}
            using var hash=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",list))));
        }
        var before=Fingerprint();bool geometry=false,restored=false;string during="",after="";
        try{ApplyStyle(false);during=Fingerprint();geometry=archive.entries.All(e=>new SerializedObject(e.asset).FindProperty(e.property).objectReferenceValue==e.geometry);}
        finally{ApplyStyle(true);after=Fingerprint();restored=archive.entries.All(e=>new SerializedObject(e.asset).FindProperty(e.property).objectReferenceValue==e.arcade);}
        File.WriteAllText("Logs/Arcade224-Rollback.txt",$"Geometry references: {geometry}\nArcade references restored: {restored}\nGameplay unchanged: {before==during&&before==after}\nBindings: {archive.entries.Count}\nBefore: {before}\nGeometry: {during}\nRestored: {after}\n");
        if(!geometry||!restored||before!=during||before!=after)throw new InvalidOperationException("Art-only round trip failed. See Arcade224-Rollback.txt");
    }
    internal static void Polish()
    {
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"ArcadeFont.asset");art=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Settings);SeedText();
        foreach(var path in new[]{"Assets/Scenes/OutGameScene.unity","Assets/Scenes/MainGameScene.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var selection=All<OutGameView>(scene).FirstOrDefault();
            if(selection!=null)
            {
                var center=selection.transform.GetComponentInParent<Canvas>().transform.Find("SelectionCenter");selection.characterName.transform.SetParent(center,false);selection.characterName.gameObject.SetActive(false);
                var heading=center.Find("Title").GetComponent<TextMeshProUGUI>();Label(heading,22);Put(heading.transform,0,86,320,30);
                var label=selection.startButton.GetComponentInChildren<TextMeshProUGUI>(true);Put(label.transform,0,0,150,22);Label(label);label.text=GameText.Get("ui.arcade.start");var binding=label.GetComponent<GameTextBinding>();if(binding!=null)binding.key="ui.arcade.start";
                foreach(var button in selection.characterButtons){var picture=button.transform.Find("Portrait");Put(picture,0,9,40,56);}
            }
            else
            {
                var right=All<Canvas>(scene).First().transform.Find("RightPanel");foreach(var t in right.GetComponentsInChildren<TextMeshProUGUI>(true))if(t.transform.parent==right&&!t.name.StartsWith("Section"))t.gameObject.SetActive(false);
                var expansion=All<RunExpansionView>(scene).First();Label(expansion.stageTitle);Label(expansion.returnButton.GetComponentInChildren<TextMeshProUGUI>(),11);
                var hud=All<ArcadeHudController>(scene).First();Put(hud.hint.transform,0,55,350,28);
                var book=All<ManaHudView>(scene).First().spellbook;Label(book.GetComponentInChildren<TextMeshProUGUI>(),11);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!open)EditorSceneManager.CloseScene(scene,true);
        }
        GameViewSize();art.revision=3;EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
    }
    internal static void FirstPerson()
    {
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"ArcadeFont.asset");
        art=AssetDatabase.LoadAssetAtPath<ArcadePresentation>(Settings);
        art.rearHeroes=Slice(Root+"RearHeroes.png",6,1,512,true);
        art.frontEnemies=Slice(Root+"FrontEnemies.png",6,3,512,true);
        art.rooms=new[]{Import(Root+"FirstPersonHall.png",512)};
        var archive=CaptureReferences();ApplyArcadeArt(archive);
        foreach(var path in new[]{"Assets/Scenes/OutGameScene.unity","Assets/Scenes/MainGameScene.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;
            if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var canvas=All<Canvas>(scene).First();var bg=canvas.transform.Find("Background").GetComponent<Image>();bg.sprite=art.rooms[0];bg.color=Color.white;
            var bindings=All<PresentationImageBindings>(scene).FirstOrDefault();
            if(bindings!=null)foreach(var binding in bindings.bindings)if(binding.image==bg){binding.arcade=art.rooms[0];binding.color=Color.white;}
            var flow=All<GameFlowController>(scene).FirstOrDefault();
            if(flow!=null&&bindings!=null)
            {
                bindings.flow=flow;bindings.arcadeEvents=new[]{art.icons[48],art.icons[50],art.icons[53],art.icons[55]};
                using var zip=ZipFile.OpenRead("Docs/Production/ArcadeRevision/Geometry-Visual-Baseline.zip");using var reader=new StreamReader(zip.GetEntry(path).Open());var yaml=reader.ReadToEnd();
                bindings.geometryEvents=new[]{"goldEventSprite","fountainEventSprite","goddessEventSprite","merchantEventSprite"}.Select(key=>
                {var m=Regex.Match(yaml,key+@": \{fileID: (-?\d+), guid: ([a-f0-9]+)");return m.Success?AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(m.Groups[2].Value)).OfType<Sprite>().FirstOrDefault(s=>AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out string _,out long id)&&id.ToString()==m.Groups[1].Value):null;}).ToArray();
                flow.SetEventArt(bindings.arcadeEvents);EditorUtility.SetDirty(bindings);EditorUtility.SetDirty(flow);
            }
            var backdrop=All<StageBackdropView>(scene).FirstOrDefault();if(backdrop!=null)backdrop.stages=art.rooms;
            var expansion=All<RunExpansionView>(scene).FirstOrDefault();
            if(expansion!=null)
            {
                var menu=expansion.returnButton.GetComponentInChildren<TextMeshProUGUI>();menu.text=GameText.Get("ui.arcade.menu");var b=menu.GetComponent<GameTextBinding>();if(b!=null)b.key="ui.arcade.menu";
                var map=All<DungeonMapView>(scene).First();var so=new SerializedObject(map);so.FindProperty("spacing").floatValue=64;so.ApplyModifiedPropertiesWithoutUndo();
                var hud=All<ArcadeHudController>(scene).First();
                Text("Legend",hud.mapPanel.transform,0,-70,224,14,GameText.Get("ui.arcade.map.legend"));
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!open)EditorSceneManager.CloseScene(scene,true);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
            var map=root.GetComponent<MapRoomIcon>();if(map!=null){map.compact=true;var label=Ref<TextMeshProUGUI>(map,"label");if(label!=null){Put(label.transform,0,0,14,14);Label(label);}}
            var door=root.GetComponent<RoomChoiceButton>();if(door!=null){door.arcade=true;Ref<Image>(door,"icon").sprite=null;}
            PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        art.revision=4;EditorUtility.SetDirty(art);EditorUtility.SetDirty(archive);AssetDatabase.SaveAssets();
        File.WriteAllText("Logs/Arcade224-Perspective.txt","First-person corridor, six rear-view heroes and eighteen frontal enemies. Runtime visual/input verification pending.");
    }
    internal static void GameViewSize()
    {
        var assembly=typeof(Editor).Assembly;var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
        var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var instance=singleton.GetProperty("instance").GetValue(null);var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group=sizesType.GetMethod("GetGroup").Invoke(instance,new[]{Enum.Parse(groupType,"Standalone")});
        var sizeType=assembly.GetType("UnityEditor.GameViewSize");var kind=assembly.GetType("UnityEditor.GameViewSizeType");
        var size=Activator.CreateInstance(sizeType,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic,null,new[]{Enum.Parse(kind,"FixedResolution"),(object)1152,672,"Arcade 384x224 (3x)"},null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
        var count=(int)group.GetType().GetMethod("GetBuiltinCount").Invoke(group,null)+(int)group.GetType().GetMethod("GetCustomCount").Invoke(group,null);
        var game=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(game,count-1);
        var zoom=game.GetType().GetField("m_ZoomArea",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(game);
        if(zoom!=null)for(var type=zoom.GetType();type!=null;type=type.BaseType)
        {var field=type.GetField("m_Scale",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.DeclaredOnly);if(field?.FieldType==typeof(Vector2)){field.SetValue(zoom,Vector2.one);break;}}
        game.Repaint();
    }
    static IEnumerable<T> All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true));
    static T Ref<T>(Object owner,string key)where T:Object=>new SerializedObject(owner).FindProperty(key)?.objectReferenceValue as T;
    static void Put(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;}
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var t=parent.Find(name);if(t==null){t=new GameObject(name,typeof(RectTransform)).transform;t.SetParent(parent,false);}Put(t,x,y,w,h);return (RectTransform)t;}
    static Image Box(string name,Transform parent,float x,float y,float w,float h,bool frame=true){var r=Rect(name,parent,x,y,w,h);var i=r.GetComponent<Image>()??r.gameObject.AddComponent<Image>();i.sprite=frame?art.frame:null;i.type=Image.Type.Sliced;i.color=frame?Color.white:Ink;i.raycastTarget=true;return i;}
    static TextMeshProUGUI Text(string name,Transform parent,float x,float y,float w,float h,string value,int size=11){var r=Rect(name,parent,x,y,w,h);var t=r.GetComponent<TextMeshProUGUI>()??r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size*4;t.enableAutoSizing=false;t.text=value;t.color=Paper;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;t.textWrappingMode=TextWrappingModes.Normal;return t;}
    static Button Button(string name,Transform parent,float x,float y,float w,float h,string label){var i=Box(name,parent,x,y,w,h);var b=i.GetComponent<Button>()??i.gameObject.AddComponent<Button>();b.targetGraphic=i;Text("Label",b.transform,0,0,w-4,h-2,label);Theme(b);return b;}
    static void Theme(Button b){var i=b.GetComponent<Image>();if(i!=null){i.sprite=art.frame;i.type=Image.Type.Sliced;i.color=Color.white;}var c=b.colors;c.normalColor=Color.white;c.highlightedColor=Gold;c.selectedColor=Gold;c.pressedColor=new Color(.55f,.55f,.55f);c.disabledColor=new Color(.35f,.35f,.35f);b.colors=c;}
    static void Label(TextMeshProUGUI t,int size=11){t.font=font;t.fontSize=size*4;t.enableAutoSizing=false;t.fontSizeMin=t.fontSizeMax=size*4;t.color=Paper;t.textWrappingMode=TextWrappingModes.Normal;}
    static void Scene(string path)
    {
        var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var canvas=All<Canvas>(scene).First();var camera=All<Camera>(scene).First();camera.allowHDR=false;camera.allowMSAA=false;camera.backgroundColor=Ink;
        var raster=camera.GetComponent<ArcadeRasterCamera>()??camera.gameObject.AddComponent<ArcadeRasterCamera>();raster.presentation=art;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;canvas.pixelPerfect=true;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1536,896);scaler.matchWidthOrHeight=.5f;
        foreach(var t in All<TextMeshProUGUI>(scene))Label(t);
        foreach(var b in All<Button>(scene))Theme(b);
        var bg=canvas.transform.Find("Background").GetComponent<Image>();bg.sprite=art.rooms[0];bg.color=Color.white;
        var selection=All<OutGameView>(scene).FirstOrDefault();if(selection!=null)Selection(selection,canvas);else Main(scene,canvas);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(!open)EditorSceneManager.CloseScene(scene,true);
    }
    static void Selection(OutGameView view,Canvas canvas)
    {
        view.compact=true;var center=canvas.transform.Find("SelectionCenter");Put(center,0,0,384,224);center.GetComponent<Image>().enabled=false;
        foreach(var name in new[]{"Subtitle","KoreanTitle","TopAccent","Controls"})if(center.Find(name)!=null)center.Find(name).gameObject.SetActive(false);
        var title=center.Find("Title").GetComponent<TextMeshProUGUI>();Put(title.transform,0,88,300,24);Label(title,16);title.text=GameText.Get("ui.title.kr");var binding=title.GetComponent<GameTextBinding>();if(binding!=null)binding.key="ui.title.kr";
        var detail=center.Find("CharacterDetails");detail.GetComponent<Image>().enabled=false;Put(detail,0,0,384,224);
        view.portrait.gameObject.SetActive(false);view.characterDescription.gameObject.SetActive(false);view.stats.gameObject.SetActive(false);if(view.startingRelicScroll!=null)view.startingRelicScroll.gameObject.SetActive(false);
        view.characterName.transform.SetParent(center,false);Put(view.characterName.transform,0,61,300,16);Label(view.characterName);
        view.compactDescription=Text("ClassHook",center,0,-48,356,32,"",11);
        for(var n=0;n<view.characterButtons.Length;n++)
        {
            var b=view.characterButtons[n];Put(b.transform,-150+n*60,8,54,72);var picture=b.transform.Find("Portrait").GetComponent<Image>();Put(picture.transform,0,8,32,44);picture.sprite=art.actors[n];picture.color=Color.white;
            Put(view.characterLabels[n].transform,0,-25,52,13);Label(view.characterLabels[n]);Put(view.selectionMarks[n].transform,0,-34,48,1);
        }
        Put(view.startButton.transform,61,-87,154,24);var startLabel=view.startButton.GetComponentInChildren<TextMeshProUGUI>();Put(startLabel.transform,0,0,150,22);Label(startLabel);startLabel.text=GameText.Get("ui.arcade.start");
        view.detailsPanel=Box("CharacterDetailOverlay",center,0,0,360,200).gameObject;var p=view.detailsPanel.transform;
        view.stats.transform.SetParent(p,false);Put(view.stats.transform,0,34,330,62);view.stats.gameObject.SetActive(true);Label(view.stats);
        view.startingRelic.transform.SetParent(p,false);Put(view.startingRelic.transform,0,-25,330,48);view.startingRelic.gameObject.SetActive(true);Label(view.startingRelic);
        var close=Button("Close",p,0,-80,100,20,GameText.Get("ui.common.close"));UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(close.onClick,view.detailsPanel.SetActive,false);
        var info=Button("Details",center,-105,-87,80,24,GameText.Get("ui.arcade.details"));UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(info.onClick,view.detailsPanel.SetActive,true);view.detailsPanel.SetActive(false);
        view.Select(0);EditorUtility.SetDirty(view);
    }
    static void Main(Scene scene,Canvas canvas)
    {
        var root=canvas.transform;var expansion=All<RunExpansionView>(scene).First();expansion.compact=true;
        var sceneView=All<CombatSceneView>(scene).First();Put(sceneView.LogicalArea,0,0,384,224);
        var top=Box("ArcadeStatus",root,0,100,384,24).transform;
        var oldLeft=root.Find("LeftPanel");var summary=oldLeft.Find("CharacterSummary");summary.SetParent(top,false);Put(summary,-113,0,150,24);summary.GetComponent<Image>().enabled=false;
        foreach(var name in new[]{"Portrait","CharacterName","ExperienceTrack","HealthTrack"})if(summary.Find(name)!=null)summary.Find(name).gameObject.SetActive(false);
        var hp=summary.Find("HealthTrack/Health");if(hp!=null){hp.SetParent(summary,false);hp.gameObject.SetActive(true);Put(hp,-24,0,96,16);Label(hp.GetComponent<TextMeshProUGUI>());}
        var xp=summary.Find("Experience");if(xp!=null)xp.gameObject.SetActive(false);
        var gold=summary.Find("Gold");if(gold!=null){Put(gold,76,0,84,16);Label(gold.GetComponent<TextMeshProUGUI>());}
        var hud=All<ArcadeHudController>(scene).FirstOrDefault()??canvas.gameObject.AddComponent<ArcadeHudController>();hud.run=All<RunController>(scene).First();hud.combat=All<CombatController>(scene).First();hud.expansion=expansion;
        hud.mapPanel=oldLeft.gameObject;Put(oldLeft,-60,0,244,176);hud.closeMap=Button("CloseMap",oldLeft,94,73,40,18,"X");
        var map=oldLeft.Find("DungeonMapView");Put(map,0,-2,224,140);var mapContent=map.Find("MapContent");Put(mapContent,0,0,220,130);
        var oldRight=root.Find("RightPanel");hud.inventoryPanel=oldRight.gameObject;Put(oldRight,0,0,364,188);hud.closeInventory=Button("CloseInventory",oldRight,156,79,40,18,"X");
        var gridNames=new[]{"PerkGrid","RelicGrid","PotionGrid"};
        for(var n=0;n<gridNames.Length;n++)
        {
            var grid=oldRight.Find(gridNames[n]);Put(grid,-6,44-n*52,332,42);
            var layout=grid.GetComponent<GridLayoutGroup>();if(layout!=null){layout.cellSize=new Vector2(64,64);layout.spacing=new Vector2(8,8);layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=18;}
            Text("Section"+n,oldRight,-150,70-n*52,40,14,GameText.Get("ui.arcade.inventory."+n));
        }
        foreach(var t in oldRight.GetComponentsInChildren<TextMeshProUGUI>(true))if(t.transform.parent==oldRight&&!t.name.StartsWith("Section"))t.gameObject.SetActive(false);
        hud.mapButton=Button("MapToggle",top,103,0,46,20,GameText.Get("ui.arcade.map"));hud.inventoryButton=Button("InventoryToggle",top,158,0,58,20,GameText.Get("ui.arcade.bag"));
        hud.hint=Text("ContextHint",root,0,70,350,15,"",11);hud.mapPanel.SetActive(false);hud.inventoryPanel.SetActive(false);
        Put(expansion.stageTitle.transform,0,77,220,14);Label(expansion.stageTitle,8);
        Put(expansion.returnButton.transform,166,-99,40,18);expansion.returnButton.GetComponentInChildren<TextMeshProUGUI>().text=GameText.Get("ui.arcade.menu");Label(expansion.returnButton.GetComponentInChildren<TextMeshProUGUI>(),8);
        var layer=root.Find("CenterModalLayer");Put(layer,0,0,384,224);
        var modal=expansion.modal.transform;Put(modal,0,0,370,208);var story=modal.Find("StoryPanel");Put(story,0,0,360,200);
        Put(expansion.modalTitle.transform,0,85,340,16);Label(expansion.modalTitle);
        Put(expansion.modalIllustration.transform,-131,38,64,64);expansion.modalIllustration.preserveAspect=true;
        Put(expansion.narrativeScroll.transform,35,38,260,64);expansion.narrativeScroll.viewport.sizeDelta=new Vector2(1020,256);Label(expansion.modalNarrative);
        expansion.illustratedTextY=152;expansion.illustratedTextHeight=256;expansion.plainTextY=152;expansion.plainTextHeight=256;
        foreach(var scroll in modal.GetComponentsInChildren<ScrollRect>(true))if(scroll.verticalScrollbar!=null)scroll.verticalScrollbar.gameObject.SetActive(false);
        var previous=expansion.choices;
        for(var n=0;n<previous.Length;n++)
        {
            previous[n].button.gameObject.SetActive(n<2);if(n>=2)continue;
            var c=previous[n];Put(c.button.transform,0,-24-n*46,348,42);Put(c.title.transform,0,11,330,14);Label(c.title);Put(c.description.transform,0,-9,330,24);Label(c.description);c.icon.gameObject.SetActive(false);
        }
        expansion.choices=previous.Take(2).ToArray();Put(expansion.previousPage.transform,-145,-94,36,16);Put(expansion.nextPage.transform,145,-94,36,16);Put(expansion.pageLabel.transform,0,-94,180,14);
        for(var n=0;n<4;n++)
        {
            var b=expansion.spellButtons[n];b.transform.SetParent(root,false);Put(b.transform,-64+n*40,-96,36,28);Put(expansion.spellLabels[n].transform,7,-3,22,24);Label(expansion.spellLabels[n],8);
            var icon=b.transform.Find("SpellIcon");if(icon!=null)Put(icon,-10,4,14,14);
        }
        var mana=root.Find("ManaAndSpellbook");mana.SetAsLastSibling();Put(mana,0,-74,384,10);Put(mana.Find("ManaGauge"),20,174,92,4);Put(mana.Find("Spellbook"),115,-24,48,18);Label(mana.Find("Spellbook/Label").GetComponent<TextMeshProUGUI>(),8);
        var manaLabel=mana.Find("ManaGauge/Value");Put(manaLabel,0,0,100,10);Label(manaLabel.GetComponent<TextMeshProUGUI>(),8);
        foreach(var name in new[]{"PlayerHpBar","PlayerShieldBar"})
        {var bar=sceneView.LogicalArea.Find(name);if(bar!=null){Put(bar,-132,name=="PlayerHpBar"?-91:-103,92,5);var value=bar.Find("Value");if(value!=null)value.gameObject.SetActive(false);}}
        var perks=All<PerkChoiceView>(scene).First();var perkPanel=Ref<GameObject>(perks,"panel");Put(perkPanel.transform,0,0,364,196);perks.compact=true;
        var cards=perkPanel.GetComponentsInChildren<PerkChoiceItem>(true);for(var n=0;n<cards.Length;n++){Put(cards[n].transform,-116+n*116,25,108,83);var icon=Ref<Image>(cards[n],"icon");Put(icon.transform,0,13,24,24);var label=Ref<TextMeshProUGUI>(cards[n],"label");Put(label.transform,0,-19,102,28);Label(label);label.alignment=TextAlignmentOptions.Center;}
        var perkTitle=Ref<TextMeshProUGUI>(perks,"title");Put(perkTitle.transform,0,83,320,16);Label(perkTitle);
        perks.focusedDescription=Text("FocusedEffect",perkPanel.transform,0,-42,338,42,"",11);perks.focusedDescription.alignment=TextAlignmentOptions.TopLeft;
        perks.confirm=Button("Take",perkPanel.transform,0,-82,120,22,GameText.Get("ui.arcade.take"));
        var shop=All<ShopView>(scene).First();shop.compact=true;var shopPanel=Ref<GameObject>(shop,"panel");Put(shopPanel.transform,0,0,370,208);
        var shopCards=shopPanel.GetComponentsInChildren<ShopItemView>(true);for(var n=0;n<shopCards.Length;n++)
        {var item=shopCards[n];Put(item.transform,-88+n%2*176,58-n/2*28,168,24);var icon=Ref<Image>(item,"icon");Put(icon.transform,-71,0,16,16);var title=Ref<TextMeshProUGUI>(item,"title");Put(title.transform,-7,0,110,24);Label(title);var status=Ref<TextMeshProUGUI>(item,"status");Put(status.transform,63,0,36,14);Label(status,8);}
        foreach(var t in shopPanel.GetComponentsInChildren<TextMeshProUGUI>(true))if(t.name=="Title")Put(t.transform,-128,80,74,16);else if(t.name=="Wallet")Put(t.transform,70,80,160,16);
        shop.focusedDescription=Text("FocusedEffect",shopPanel.transform,0,-45,338,52,"",11);shop.focusedDescription.alignment=TextAlignmentOptions.TopLeft;
        shop.purchase=Button("Purchase",shopPanel.transform,-50,-88,162,22,"");shop.purchaseLabel=shop.purchase.GetComponentInChildren<TextMeshProUGUI>();Put(Ref<Button>(shop,"leaveButton").transform,116,-88,70,22);
        Put(Ref<Button>(shop,"leaveButton").GetComponentInChildren<TextMeshProUGUI>().transform,0,0,68,20);
        var reward=All<RewardPopupView>(scene).First();var rewardPanel=Ref<GameObject>(reward,"panel");Put(rewardPanel.transform,0,0,280,156);
        foreach(var t in rewardPanel.GetComponentsInChildren<TextMeshProUGUI>(true)){var y=t.name=="Title"?54:t.name=="Experience"?22:t.name=="Gold"?-10:-52;Put(t.transform,0,y,250,18);Label(t);}
        Put(Ref<Button>(reward,"confirmButton").transform,0,-55,180,24);Put(Ref<Button>(reward,"confirmButton").GetComponentInChildren<TextMeshProUGUI>().transform,0,0,176,20);Put(Ref<Image>(reward,"experienceFill").transform.parent,0,5,230,4);
        var result=All<ResultView>(scene).First();Put(Ref<GameObject>(result,"panel").transform,0,0,340,180);Put(Ref<TextMeshProUGUI>(result,"title").transform,0,60,310,20);Put(Ref<TextMeshProUGUI>(result,"details").transform,0,0,310,80);Put(Ref<Button>(result,"restart").transform,0,-60,180,24);
        Put(Ref<Button>(result,"restart").GetComponentInChildren<TextMeshProUGUI>().transform,0,0,176,20);
        var backdrop=All<StageBackdropView>(scene).First();backdrop.stages=art.rooms;
        foreach(var door in All<RoomChoiceButton>(scene))door.arcade=true;
        foreach(var dither in All<PixelDitherTransition>(scene))Put(dither.transform,0,0,384,224);
        var tip=All<ItemTooltipView>(scene).First();var tipPanel=Ref<GameObject>(tip,"panel");Put(tipPanel.transform,0,0,280,96);tipPanel.transform.SetAsLastSibling();Put(Ref<TextMeshProUGUI>(tip,"title").transform,0,30,260,16);Put(Ref<TextMeshProUGUI>(tip,"description").transform,0,0,260,60);
        foreach(var component in new Component[]{expansion,perks,shop,hud})EditorUtility.SetDirty(component);
    }
    static void Prefabs()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var p=PrefabUtility.LoadPrefabContents(path);
            foreach(var text in p.GetComponentsInChildren<TextMeshProUGUI>(true))Label(text);
            foreach(var image in p.GetComponentsInChildren<Image>(true))if(image.sprite!=null&&AssetDatabase.GetAssetPath(image.sprite).Contains("AshenVow/Frame"))image.sprite=art.frame;
            var door=p.GetComponent<RoomChoiceButton>();if(door!=null){door.arcade=true;Ref<Image>(door,"icon").sprite=null;}
            PrefabUtility.SaveAsPrefabAsset(p,path);PrefabUtility.UnloadPrefabContents(p);
        }
    }
    static void InstallRenderer()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:ScriptableRendererData",new[]{"Assets"}))
        {
            var renderer=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(AssetDatabase.GUIDToAssetPath(guid));if(renderer.rendererFeatures.Any(f=>f is ArcadeRasterFeature))continue;
            var feature=ScriptableObject.CreateInstance<ArcadeRasterFeature>();feature.name="Arcade 224p palette";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);feature.Create();EditorUtility.SetDirty(renderer);
        }
    }
    static TMP_FontAsset MakeFont()
    {
        var path=Root+"ArcadeFont.asset";var f=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(f!=null)return f;
        f=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/Galmuri/Galmuri11.ttf"),11,1,GlyphRenderMode.RASTER_HINTED,2048,2048,AtlasPopulationMode.Dynamic,true);
        AssetDatabase.CreateAsset(f,path);f.TryAddCharacters(new string(string.Concat(BehemothTextEditor.Table.entries.Select(e=>e.kr)).Distinct().ToArray()),out string _);f.material.shader=Shader.Find("TextMeshPro/Bitmap");AssetDatabase.AddObjectToAsset(f.material,f);foreach(var atlas in f.atlasTextures){atlas.filterMode=FilterMode.Point;if(!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,f);}EditorUtility.SetDirty(f);return f;
    }
    static Sprite Import(string path,int max)
    {AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.filterMode=FilterMode.Point;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=max;i.npotScale=TextureImporterNPOTScale.None;i.alphaIsTransparency=true;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);}
    static Sprite[] Slice(string path,int cols,int rows,int max,bool trim=false)
    {
        Import(path,max);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.GetSourceTextureWidthAndHeight(out var w,out var h);i.spriteImportMode=SpriteImportMode.Multiple;
        Texture2D source=null;Color32[] pixels=null;if(trim){source=new Texture2D(2,2);ImageConversion.LoadImage(source,File.ReadAllBytes(path));pixels=source.GetPixels32();}
        var list=new List<SpriteMetaData>();for(var n=0;n<cols*rows;n++)
        {
            int x0=n%cols*w/cols,x1=(n%cols+1)*w/cols,y0=(rows-1-n/cols)*h/rows,y1=(rows-n/cols)*h/rows;
            if(path.EndsWith("FrontEnemies.png"))
            {
                // Sprite atlas gutters are uneven; rectangles follow the actual isolated silhouettes.
                int[][] cuts={new[]{0,268,526,817,1039,1290,1536},new[]{0,281,557,787,1066,1299,1536},new[]{0,253,514,795,1075,1313,1536}};
                x0=Mathf.RoundToInt(cuts[n/cols][n%cols]*w/1536f);x1=Mathf.RoundToInt(cuts[n/cols][n%cols+1]*w/1536f);
                int[] rowsAt={0,365,692,1024};y0=h-Mathf.RoundToInt(rowsAt[n/cols+1]*h/1024f);y1=h-Mathf.RoundToInt(rowsAt[n/cols]*h/1024f);
            }
            var rect=new Rect(x0,y0,x1-x0,y1-y0);
            if(trim)
            {
                int left=x1,right=x0,bottom=y1,top=y0;
                for(var y=y0;y<y1;y++)for(var x=x0;x<x1;x++)if(pixels[y*w+x].a>127){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                if(right>left&&top>bottom)rect=new Rect(Math.Max(x0,left-2),Math.Max(y0,bottom-2),Math.Min(x1,right+3)-Math.Max(x0,left-2),Math.Min(y1,top+3)-Math.Max(y0,bottom-2));
            }
            list.Add(new SpriteMetaData{name=Path.GetFileNameWithoutExtension(path)+"_"+n.ToString("D2"),rect=rect,alignment=(int)SpriteAlignment.Center,pivot=Vector2.one*.5f});
        }
        if(source!=null)Object.DestroyImmediate(source);
#pragma warning disable CS0618
        i.spritesheet=list.ToArray();
#pragma warning restore CS0618
        i.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
    }
    static Sprite Frame()
    {
        var path=Root+"Frame.png";if(!File.Exists(path)){var t=new Texture2D(8,8,TextureFormat.RGBA32,false);for(var y=0;y<8;y++)for(var x=0;x<8;x++)t.SetPixel(x,y,x==0||y==0||x==7||y==7?new Color32(75,74,96,255):Ink);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);}
        Import(path,32);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.spriteBorder=Vector4.one*2;i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    internal static void RefreshText()=>SeedText();
    static void SeedText()
    {
        var values=new Dictionary<string,string>{{"ui.arcade.map","지도"},{"ui.arcade.bag","보유품"},{"ui.arcade.menu","메뉴"},{"ui.arcade.details","능력"},{"ui.arcade.take","획득"},{"ui.arcade.price","{0}<color=#D8BB62>G</color>"},{"ui.arcade.buy","{0}<color=#D8BB62>G</color> 구매"},{"ui.arcade.inventory.0","능력"},{"ui.arcade.inventory.1","유물"},{"ui.arcade.inventory.2","포션"},{"ui.arcade.explore.hint","문을 골라 이동 · Tab 지도 · I 보유품"},{"ui.arcade.combat.hint","긋기 공격 · 누른 뒤 긋기 차지 · 몸을 누르면 가드"},{"ui.arcade.class.0","막아낸 뒤 되받아치는 검과 방패"},{"ui.arcade.class.1","독을 쌓거나 첫 일격을 노리는 단검"},{"ui.arcade.class.2","격노를 모아 몰아치는 도끼"},{"ui.arcade.class.3","칼을 거두고 기회를 베는 발도"},{"ui.arcade.class.4","초식을 잇고 흐름을 고르는 권법"},{"ui.arcade.class.5","가드로 마력을 모으는 주문술사"},{"ui.hud.gold","{0}<color=#D8BB62>G</color>"},{"ui.hud.experience","Lv {0}"}};
        values["ui.arcade.start"]="성으로";
        values["ui.arcade.map.legend"]="! 보스  + 정예  $ 상점  C 상자  ~ 샘";
        values["ui.arcade.direction.0"]="↑";values["ui.arcade.direction.1"]="←";values["ui.arcade.direction.2"]="→";values["ui.arcade.direction.3"]="↓";
        foreach(var pair in values)BehemothTextEditor.Table.Set(pair.Key,pair.Value,"Arcade224");BehemothTextEditor.SyncAll();BehemothTextEditor.ExportDefault();
    }
}
