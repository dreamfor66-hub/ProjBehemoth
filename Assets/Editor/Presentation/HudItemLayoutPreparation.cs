using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class HudItemLayoutPreparation
{
    internal const string Folder="Docs/Production/HudItems-2026-10-31/";
    const string BagPath="Assets/Art/UI/T_UI_Common_PotionBag.png",IconPath="Assets/Prefabs/UI/LargeQuickInventoryIcon.prefab";
    static bool done,busy;
    static HudItemLayoutPreparation(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(done||busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(BagPath))return;
        busy=true;
        try
        {
            var complete=true;
            foreach(var path in new[]{"Assets/Scenes/MainGameScene.unity","Assets/Scenes/Trials/Angular01_Main.unity"})
            {
                var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
                if(loaded&&scene.isDirty){complete=false;continue;}
                try
                {
                    if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    var quick=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<QuickInventoryView>(true)).Single();
                    if(quick.layoutRevision>=1)continue;
                    Directory.CreateDirectory(Folder+"Before");
                    var backup=Folder+"Before/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);
                    Prepare(quick);EditorSceneManager.MarkSceneDirty(scene);
                    if(!EditorSceneManager.SaveScene(scene))throw new IOException("HUD save failed: "+path);
                }
                finally{if(!loaded&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}
            }
            done=complete;
        }
        catch(Exception e){done=true;Debug.LogException(e);}
        finally{busy=false;}
    }
    static RectTransform Node(Transform parent,string name)
    {
        var r=parent.Find(name) as RectTransform;if(r!=null)return r;
        var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;r=(RectTransform)go.transform;r.SetParent(parent,false);return r;
    }
    static void Stretch(RectTransform r)
    {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    static RunHudIconView Icon()
    {
        var existing=AssetDatabase.LoadAssetAtPath<RunHudIconView>(IconPath);if(existing!=null)return existing;
        var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/QuickInventoryIcon.prefab"));
        source.name="LargeQuickInventoryIcon";var root=(RectTransform)source.transform;root.sizeDelta=new Vector2(72,72);
        var icon=(RectTransform)source.transform.Find("Icon");icon.sizeDelta=new Vector2(76,76);
        icon.GetComponent<Image>().preserveAspect=true;
        var saved=PrefabUtility.SaveAsPrefabAsset(source,IconPath).GetComponent<RunHudIconView>();Object.DestroyImmediate(source);return saved;
    }
    static GridLayoutGroup Grid(RectTransform r)
    {
        foreach(var old in r.GetComponents<LayoutGroup>())Object.DestroyImmediate(old);
        var g=r.gameObject.AddComponent<GridLayoutGroup>();g.cellSize=new Vector2(72,72);g.spacing=new Vector2(12,6);g.childAlignment=TextAnchor.UpperLeft;return g;
    }
    static void Prepare(QuickInventoryView quick)
    {
        var canvas=quick.GetComponentInParent<Canvas>();var root=(RectTransform)canvas.transform;
        var icon=Icon();
        if(quick.relicPool==null||quick.potionPool==null)
        {
            foreach(var old in quick.GetComponentsInChildren<RunHudIconView>(true))Object.DestroyImmediate(old.gameObject);
            var storage=Node(quick.transform,"Pool");storage.gameObject.SetActive(false);
            RunHudIconPool Pool(string name){var content=Node(quick.transform,name);var pool=content.GetComponent<RunHudIconPool>()??content.gameObject.AddComponent<RunHudIconPool>();pool.content=content;pool.prefab=icon;pool.storage=Node(storage,name);return pool;}
            quick.ConfigurePools(Pool("Relics"),Pool("Potions"));
        }
        quick.relicPool.prefab=quick.potionPool.prefab=icon;
        var relics=quick.relicPool.content;relics.SetParent(quick.transform,false);
        relics.anchorMin=new Vector2(0,1);relics.anchorMax=Vector2.one;relics.pivot=new Vector2(0,1);
        relics.anchoredPosition=new Vector2(160,-104);relics.sizeDelta=new Vector2(-176,72);
        Grid(relics).constraint=GridLayoutGroup.Constraint.Flexible;
        var fit=relics.GetComponent<ContentSizeFitter>()??relics.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var bagRoot=Node(quick.transform,"PotionBag");bagRoot.anchorMin=bagRoot.anchorMax=bagRoot.pivot=new Vector2(0,1);bagRoot.anchoredPosition=new Vector2(0,-124);
        var bag=bagRoot.GetComponent<PotionBagView>()??bagRoot.gameObject.AddComponent<PotionBagView>();bag.items=quick.potionPool;bag.padding=new RectOffset(20,20,24,24);
        var art=Node(bagRoot,"Bag Artwork");Stretch(art);bag.artwork=art.GetComponent<Image>()??art.gameObject.AddComponent<Image>();
        var importer=(TextureImporter)AssetImporter.GetAtPath(BagPath);
        if(importer.textureType!=TextureImporterType.Sprite||importer.mipmapEnabled||importer.alphaIsTransparency==false){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();}
        bag.artwork.sprite=DungeonSlash.DesignerAssetPaths.Load<Sprite>(BagPath);bag.artwork.raycastTarget=false;
        bag.slotBackgrounds=Node(bagRoot,"Slot Backgrounds");
        var slot=Node(bag.slotBackgrounds,"Potion Slot 1");bag.slotTemplate=slot.GetComponent<Image>()??slot.gameObject.AddComponent<Image>();
        bag.slotTemplate.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");bag.slotTemplate.type=Image.Type.Sliced;bag.slotTemplate.color=new Color32(123,106,65,230);bag.slotTemplate.raycastTarget=false;
        var potions=quick.potionPool.content;potions.SetParent(bagRoot,false);Grid(potions).constraint=GridLayoutGroup.Constraint.FixedColumnCount;potions.GetComponent<GridLayoutGroup>().constraintCount=1;
        var potionFit=potions.GetComponent<ContentSizeFitter>();if(potionFit!=null)Object.DestroyImmediate(potionFit);
        bag.PrepareSlots(RunState.DefaultPotionSlotCapacity);quick.potionBag=bag;
        var oldItems=quick.transform.Find("Items");if(oldItems!=null&&oldItems.childCount==0)Object.DestroyImmediate(oldItems.gameObject);
        var hud=root.GetComponentInChildren<CombatHudView>(true);
        var charge=(RectTransform)new SerializedObject(hud).FindProperty("chargeGaugeRoot").objectReferenceValue;
        var overlay=Node(root,"PointerOverlay");Stretch(overlay);charge.SetParent(overlay,false);overlay.SetAsLastSibling();
        var itemOverlay=root.Find("ItemOverlays");if(itemOverlay!=null)overlay.SetSiblingIndex(itemOverlay.GetSiblingIndex());
        foreach(var impact in root.GetComponentsInChildren<MonsterImpactGraphic>(true)){impact.Clear();impact.enabled=false;impact.gameObject.SetActive(false);}
        quick.layoutRevision=1;EditorUtility.SetDirty(quick);
    }
}
