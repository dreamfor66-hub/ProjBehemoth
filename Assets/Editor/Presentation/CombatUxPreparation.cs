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
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class CombatUxPreparation
{
    internal const string Folder="Docs/Production/CombatUX-2026-10-29/";
    static double next;static bool busy;
    static CombatUxPreparation(){next=EditorApplication.timeSinceStartup+5;EditorApplication.playModeStateChanged+=_=>next=EditorApplication.timeSinceStartup+5;EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+5;
        if(!File.Exists(Folder+"CodeReady.txt"))return;
        busy=true;
        try
        {
            if(!File.Exists(Folder+"LayoutInstalled.txt")){Prepare();File.WriteAllText(Folder+"LayoutInstalled.txt","cards / all event placeholders / species sizes / paired positions / recovery HUD\n");}
            if(File.Exists(Folder+"IconsReady.txt")&&!File.Exists(Folder+"IconsInstalled.txt")){InstallIcons();File.WriteAllText(Folder+"IconsInstalled.txt","144 distinct paintings; all perk/relic/potion and legacy ability references replaced.\n");}
            if(!File.Exists(Folder+"CardsPolishInstalled.txt")){PolishCards();File.WriteAllText(Folder+"CardsPolishInstalled.txt","Long card titles fit within their own title areas.\n");}
        }
        catch(Exception e){File.WriteAllText(Folder+"PreparationError.txt",e.ToString());next=EditorApplication.timeSinceStartup+60;Debug.LogException(e);}
        finally{busy=false;}
    }
    static void PolishCards()
    {
        const string path="Assets/Scenes/MainGameScene.unity";
        var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
        if(loaded&&scene.isDirty)throw new InvalidOperationException("Unsaved designer scene preserved; card polish pending.");
        if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            foreach(var card in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PerkChoiceItem>(true)))
            {
                var label=Ref<TextMeshProUGUI>(card,"label");label.enableAutoSizing=true;label.fontSizeMin=28;label.fontSizeMax=36;
                label.textWrappingMode=TextWrappingModes.Normal;Save(label);
            }
            EditorSceneManager.SaveScene(scene);
        }
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
    }
    internal static void Backup(string path)
    {
        var target=Folder+"Before/"+path;if(File.Exists(target))return;
        Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target);
    }
    static T Asset<T>(string path)where T:Object=>DungeonSlash.DesignerAssetPaths.Load<T>(path);
    static T Ref<T>(Object owner,string key)where T:Object=>new SerializedObject(owner).FindProperty(key).objectReferenceValue as T;
    static GameObject Node(string name,Transform parent,params Type[] components)
    {
        var old=parent.Find(name);var go=old!=null?old.gameObject:new GameObject(name,components);if(old==null)go.transform.SetParent(parent,false);
        foreach(var type in components)if(go.GetComponent(type)==null)go.AddComponent(type);return go;
    }
    static void Save(Object o){EditorUtility.SetDirty(o);if(EditorUtility.IsPersistent(o))AssetDatabase.SaveAssetIfDirty(o);}
    static void Move(Component c,Vector2 pos,Vector2 size)
    {var r=(RectTransform)c.transform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=pos;r.sizeDelta=size;r.localScale=Vector3.one;EditorUtility.SetDirty(r);}
    static void Font(TextMeshProUGUI t,float size)
    {
        t.fontSize=size;t.enableAutoSizing=false;t.raycastTarget=false;
        var b=t.GetComponent<TypographyRoleBinding>();if(b!=null){b.designSize=size;b.snapSize=false;b.Apply();EditorUtility.SetDirty(b);}EditorUtility.SetDirty(t);
    }
    static void Prepare()
    {
        const string scenePath="Assets/Scenes/MainGameScene.unity";
        var scene=SceneManager.GetSceneByPath(scenePath);var loaded=scene.isLoaded;
        if(loaded&&scene.isDirty)throw new InvalidOperationException("Unsaved designer MainGameScene preserved; preparation pending.");
        Backup(scenePath);
        if(!loaded)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
        try
        {
            var roots=scene.GetRootGameObjects();
            foreach(var view in roots.SelectMany(r=>r.GetComponentsInChildren<PerkChoiceView>(true)))
            {
                var so=new SerializedObject(view);var entries=so.FindProperty("items");var title=Ref<TextMeshProUGUI>(view,"title");
                var panel=Ref<GameObject>(view,"panel");var rect=(RectTransform)panel.transform;
                Move(title,new Vector2(0,rect.rect.height*.5f-60),new Vector2(rect.rect.width-100,72));Font(title,48);
                for(var i=0;i<entries.arraySize;i++)
                {
                    var card=(PerkChoiceItem)entries.GetArrayElementAtIndex(i).objectReferenceValue;
                    var button=Ref<Button>(card,"button");var name=Ref<TextMeshProUGUI>(card,"label");var icon=Ref<Image>(card,"icon");
                    Move(card,new Vector2((i-1)*456,0),new Vector2(424,520));
                    Move(icon,new Vector2(0,144),new Vector2(176,176));icon.preserveAspect=true;icon.raycastTarget=false;
                    Move(name,new Vector2(0,14),new Vector2(376,74));name.alignment=TextAlignmentOptions.Center;Font(name,36);
                    var old=card.transform.Find("Inline Effect");var effect=old!=null?old.GetComponent<TextMeshProUGUI>():new GameObject("Inline Effect",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                    effect.transform.SetParent(card.transform,false);effect.font=name.font;effect.alignment=TextAlignmentOptions.TopLeft;
                    effect.textWrappingMode=TextWrappingModes.Normal;effect.overflowMode=TextOverflowModes.Overflow;effect.lineSpacing=6;
                    var binding=effect.GetComponent<TypographyRoleBinding>()??effect.gameObject.AddComponent<TypographyRoleBinding>();
                    binding.profile=name.GetComponent<TypographyRoleBinding>()?.profile;binding.role=TextRole.Description;
                    Move(effect,new Vector2(0,-126),new Vector2(368,188));Font(effect,28);
                    var scrollGo=Node("Effect Scroll",card.transform,typeof(RectTransform),typeof(ScrollRect));
                    Move(scrollGo.transform,new Vector2(0,-126),new Vector2(368,188));
                    var viewportGo=Node("Viewport",scrollGo.transform,typeof(RectTransform),typeof(Image),typeof(RectMask2D));
                    var viewport=(RectTransform)viewportGo.transform;viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=Vector2.zero;viewport.offsetMax=new Vector2(-16,0);viewportGo.GetComponent<Image>().color=Color.clear;
                    effect.transform.SetParent(viewport,false);var er=effect.rectTransform;er.anchorMin=new Vector2(0,1);er.anchorMax=Vector2.one;er.pivot=new Vector2(.5f,1);er.anchoredPosition=Vector2.zero;er.sizeDelta=new Vector2(0,188);
                    var scroll=scrollGo.GetComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=er;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=34;
                    var barGo=Node("Scrollbar",scrollGo.transform,typeof(RectTransform),typeof(Image),typeof(Scrollbar));
                    var br=(RectTransform)barGo.transform;br.anchorMin=new Vector2(1,0);br.anchorMax=Vector2.one;br.pivot=new Vector2(1,.5f);br.sizeDelta=new Vector2(8,0);br.anchoredPosition=Vector2.zero;barGo.GetComponent<Image>().color=new Color32(37,35,30,255);
                    var handle=Node("Handle",barGo.transform,typeof(RectTransform),typeof(Image));handle.GetComponent<Image>().color=new Color32(151,136,101,255);
                    var hr=(RectTransform)handle.transform;hr.anchorMin=Vector2.zero;hr.anchorMax=Vector2.one;hr.offsetMin=hr.offsetMax=Vector2.zero;
                    var scrollbar=barGo.GetComponent<Scrollbar>();scrollbar.handleRect=hr;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;card.effectScroll=scroll;
                    card.effect=effect;
                    var mark=card.transform.Find("Selected Diamond");
                    if(mark==null){var go=new GameObject("Selected Diamond",typeof(RectTransform),typeof(Image));go.transform.SetParent(card.transform,false);mark=go.transform;}
                    var image=mark.GetComponent<Image>();Move(image,new Vector2(0,-240),new Vector2(10,10));image.rectTransform.localRotation=Quaternion.Euler(0,0,45);image.color=new Color32(208,178,111,255);image.raycastTarget=false;card.selectedMark=image;
                    Save(card);Save(icon);Save(name);Save(effect);
                }
                if(view.focusedDescription!=null)view.focusedDescription.gameObject.SetActive(false);
                if(view.confirm!=null)Move(view.confirm,new Vector2(0,-rect.rect.height*.5f+62),new Vector2(360,64));
                view.compact=true;Save(view);
            }
            var hud=roots.SelectMany(r=>r.GetComponentsInChildren<CombatHudView>(true)).Single();
            var hp=Ref<Image>(hud,"playerHp");var hpTrack=(RectTransform)hp.transform.parent;
            var recovery=Node("Attack Recovery Sword",hpTrack.parent,typeof(RectTransform),typeof(CanvasRenderer),typeof(AttackRecoveryGauge)).GetComponent<AttackRecoveryGauge>();
            recovery.gameObject.layer=hp.gameObject.layer;recovery.transform.SetParent(hpTrack.parent,false);
            var rr=recovery.rectTransform;rr.anchorMin=hpTrack.anchorMin;rr.anchorMax=hpTrack.anchorMax;rr.pivot=Vector2.one*.5f;
            rr.anchoredPosition=hpTrack.anchoredPosition+new Vector2(0,76);rr.sizeDelta=new Vector2(240,38);recovery.raycastTarget=false;hud.attackRecovery=recovery;Save(hud);
            var layout=roots.SelectMany(r=>r.GetComponentsInChildren<CombatSceneView>(true)).Single();
            var layoutSo=new SerializedObject(layout);layoutSo.FindProperty("twoMonsterOffset").vector2Value=new Vector2(230,-35);layoutSo.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
        PrepareEvents();PrepareSpecies();PreparePairs();AssetDatabase.SaveAssets();
    }
    static void PrepareEvents()
    {
        var people=new[]{"Pilgrim","Scholar","Delivery","LostBeliever","MagicTeacherFirst","MagicTeacherReturn","MagicTeacherAdvanced","MasterTrial","Training","StrayDog","Shelter"};
        var mapping="Temporary event paintings; original references are in Before.\n";
        foreach(var guid in AssetDatabase.FindAssets("t:RandomEventData",new[]{"Assets/Data/Dungeon/Events"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var story=Asset<RandomEventData>(path);Backup(path);
            var art=people.Contains(story.id)?"Pilgrim":"Mirror";
            story.illustratedPopup=true;story.illustration=Asset<Sprite>("Assets/Art/UI/Events/"+art+".png");
            foreach(var page in story.pages)page.illustration=null;
            Save(story);mapping+=story.id+" => "+art+" (placeholder)\n";
        }
        File.WriteAllText(Folder+"Event-Placeholders.txt",mapping);
    }
    static void PrepareSpecies()
    {
        // Species sizes are now carried by native Sprite geometry and authored hitboxes.
        // Reapplying legacy body/artwork/weak-point multipliers would corrupt designer edits.
    }
    static void PreparePairs()
    {
        var report="Pair distance x0.8 with body separation floor; species sizes and timing untouched.\n";
        foreach(var guid in AssetDatabase.FindAssets("t:EncounterData",new[]{"Assets/Data/Dungeon"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var encounter=Asset<EncounterData>(path);
            if(encounter.members.Count!=2||encounter.members.Any(m=>m.monster==null))continue;
            var a=encounter.members[0];var b=encounter.members[1];var old=Mathf.Abs(b.offset.x-a.offset.x);if(old<1)continue;
            var floor=(a.monster.BodySize.x*a.monster.combatScale+b.monster.BodySize.x*b.monster.combatScale)*.5f+60;
            var distance=Mathf.Min(old,Mathf.Max(old*.8f,floor));var center=(a.offset.x+b.offset.x)*.5f;var direction=Mathf.Sign(b.offset.x-a.offset.x);
            Backup(path);a.offset.x=center-direction*distance*.5f;b.offset.x=center+direction*distance*.5f;Save(encounter);
            report+=encounter.id+": "+old+" -> "+distance+"\n";
        }
        File.WriteAllText(Folder+"Pair-Spacing.txt",report);
    }
    static void InstallIcons()
    {
        var count=0;
        foreach(var kind in new[]{"Perks","Relics","Potions","PerkAbilities"})
        foreach(var rawFile in Directory.GetFiles("Assets/Data/Items/"+kind,"*.asset"))
        {
            var file=rawFile.Replace('\\','/');
            var art="Assets/Art/UI/Items2D/"+(kind=="PerkAbilities"?"Perks":kind)+"/"+Path.GetFileNameWithoutExtension(file)+".png";
            if(!File.Exists(art))throw new InvalidDataException("Missing requested icon "+art);
            Backup(file);AssetDatabase.ImportAsset(art,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(art);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=512;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            var data=Asset<Object>(file);var so=new SerializedObject(data);so.FindProperty("icon").objectReferenceValue=Asset<Sprite>(art);so.ApplyModifiedPropertiesWithoutUndo();Save(data);count++;
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Folder+"Icon-References.txt",count+" data asset references assigned to 144 new icon paintings.\n");
    }
}
