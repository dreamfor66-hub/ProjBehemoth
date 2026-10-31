using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Automatic once, then all surfaces and art-direction values remain saved and editable.
[InitializeOnLoad]
internal static class InkUiPreparation
{
    internal const string StylePath = "Assets/Data/Presentation/InkUi.asset";
    const string Folder = "Docs/Production/InkUI-2026-10-27/";
    const int Revision = 9;
    static bool done;
    static InkUiPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (done || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var style = AssetDatabase.LoadAssetAtPath<InkUiStyle>(StylePath);
        if (style != null && style.appliedRevision >= Revision) { done = true; return; }
        var shader = Shader.Find("UI/Behemoth/Ink Surface"); if (shader == null) return;
        done = true;
        try
        {
            if (style == null) { style = ScriptableObject.CreateInstance<InkUiStyle>(); AssetDatabase.CreateAsset(style, StylePath); }
            if (style.material == null)
            {
                const string path = "Assets/Art/Materials/InkUi.mat";
                style.material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (style.material == null) { style.material = new Material(shader); AssetDatabase.CreateAsset(style.material,path); }
            }
            var count = 0; var skipped = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/UI","Assets/Prefabs/Combat"}))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var root = PrefabUtility.LoadPrefabContents(path);
                try { if (Apply(root,style,ref count)) PrefabUtility.SaveAsPrefabAsset(root,path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var scene = SceneManager.GetSceneByPath(path); var loaded = scene.isLoaded;
                if (loaded && scene.isDirty) { skipped.Add(path); continue; }
                if (!loaded) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    var changed = false;
                    foreach (var root in scene.GetRootGameObjects()) changed |= Apply(root,style,ref count);
                    if (changed) EditorSceneManager.SaveScene(scene);
                }
                finally { if (!loaded) EditorSceneManager.CloseScene(scene,true); }
            }
            if (skipped.Count == 0) style.appliedRevision = Revision;
            EditorUtility.SetDirty(style); AssetDatabase.SaveAssets(); Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder+"Preparation.txt","Ink surfaces visited/serialized: "+count+"\nSkipped dirty scenes: "+string.Join(", ",skipped)+"\nTop black strip and its top controls excluded. Existing input targets and listeners retained.\n");
        }
        catch(Exception e) { Debug.LogException(e); Directory.CreateDirectory(Folder); File.WriteAllText(Folder+"PreparationError.txt",e.ToString()); }
    }
    static bool Plain(Image image) => image != null && (image is CrispPanelImage || image.sprite == null || image.sprite.texture.width <= 16 || AssetDatabase.GetAssetPath(image.sprite).Contains("PanelFrame") || AssetDatabase.GetAssetPath(image.sprite)=="Assets/Art/UI/Frame.png");
    static bool TopStrip(Graphic image)
    {
        if(image==null)return false;
        for(var t=image.transform;t!=null;t=t.parent)if(t.name=="ArcadeStatus")return true;
        var r=image.rectTransform;
        return image.transform.parent!=null && image.transform.parent.GetComponent<Canvas>()!=null && r.rect.height<=140 &&
            (r.anchoredPosition.y>300 || r.anchorMin.y>.99f && r.anchoredPosition.y>=-100);
    }
    static bool Diagnostic(Component c)
    {
        for(var parent=c.transform;parent!=null;parent=parent.parent)if(parent.name=="DebugCheatPanel")return true;
        return false;
    }
    static T Ref<T>(Component owner,string field) where T : UnityEngine.Object => new SerializedObject(owner).FindProperty(field)?.objectReferenceValue as T;

    static bool Apply(GameObject root,InkUiStyle style,ref int count)
    {
        var changed = false;
        foreach(var old in root.GetComponentsInChildren<InkUiSurface>(true))
            if(old.source!=null&&TopStrip(old.source)){UnityEngine.Object.DestroyImmediate(old.gameObject);changed=true;}
        foreach(var camera in root.GetComponentsInChildren<ArcadeRasterCamera>(true))
        {camera.preserveNativeUiColors=true;EditorUtility.SetDirty(camera);changed=true;}
        InkUiSurface Skin(Graphic source,InkSurfaceRole role,bool band=false,Color? resource=null,Selectable control=null)
        {
            if(source==null || TopStrip(source) || Diagnostic(source)) return null;
            foreach(var shadow in source.GetComponents<Shadow>())shadow.enabled=false;
            var prior = source.transform.Find("Ink Surface");
            var skin = prior != null ? prior.GetComponent<InkUiSurface>() : null;
            if(skin==null)
            {
                var go = new GameObject("Ink Surface",typeof(RectTransform),typeof(CanvasRenderer),typeof(InkUiSurface));
                go.layer = source.gameObject.layer; go.transform.SetParent(source.transform,false); go.transform.SetAsFirstSibling();
                skin = go.GetComponent<InkUiSurface>();
            }
            var rect=skin.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            skin.source=source;skin.style=style;skin.role=role;skin.titleBand=band;skin.material=style.material;skin.resourceColor=resource??style.bone;skin.control=control;skin.raycastTarget=false;
            // Existing compact views signal their persistent selection through their authored Image tint.
            skin.trackAuthoredSelection=control!=null && (control.GetComponent<PerkChoiceItem>()!=null || control.GetComponent<ShopItemView>()!=null || control.GetComponentInParent<SpellbookView>(true)?.slots.Contains(control as Button)==true);
            skin.titleLabel=source.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t=>t.transform.parent==source.transform && t.GetComponent<TypographyRoleBinding>()?.role is TextRole.Title or TextRole.Heading);
            skin.resource=resource==style.hp?InkResource.Health:resource==style.mana?InkResource.Mana:resource==style.guard?InkResource.Guard:resource==style.brass?InkResource.Gold:InkResource.Custom;
            if(control!=null)
            {
                var feedback=control.GetComponent<InkUiFeedback>()??control.gameObject.AddComponent<InkUiFeedback>();feedback.surface=skin;EditorUtility.SetDirty(feedback);
            }
            EditorUtility.SetDirty(skin);skin.SetAllDirty();changed=true;
            return skin;
        }
        void Gauge(Image fill,Color color)
        {
            if(fill==null || TopStrip(fill))return;
            if(fill.transform.parent.TryGetComponent<Image>(out var track))Skin(track,InkSurfaceRole.GaugeTrack);
            Skin(fill,InkSurfaceRole.GaugeFill,false,color);
        }
        foreach(var panel in root.GetComponentsInChildren<CrispPanelImage>(true))
        {
            if(TopStrip(panel)||Diagnostic(panel))continue;
            var labels=panel.GetComponentsInChildren<TMP_Text>(true);
            var band=panel.rectTransform.rect.height>240 && labels.Any(t=>t.transform.parent==panel.transform&&t.GetComponent<TypographyRoleBinding>()?.role is TextRole.Title or TextRole.Heading);
            Skin(panel,InkSurfaceRole.Panel,band);
            foreach(var label in labels)
            {
                if(label.GetComponentInParent<Canvas>()==null)continue;
                var binding=label.GetComponent<TypographyRoleBinding>();
                if(binding!=null && binding.role is TextRole.Narrative or TextRole.Description or TextRole.Title or TextRole.Heading)
                {label.color=style.bone;EditorUtility.SetDirty(label);}
            }
        }
        foreach(var control in root.GetComponentsInChildren<Selectable>(true))
        {
            if(control.targetGraphic is not Image image || !Plain(image) || TopStrip(image) || Diagnostic(image))continue;
            var slot=control.GetComponent<RunHudIconView>()!=null || control.GetComponentInParent<SpellbookView>(true)!=null;
            Skin(image,slot?InkSurfaceRole.Slot:InkSurfaceRole.Button,false,null,control);
        }
        foreach(var view in root.GetComponentsInChildren<RunExpansionView>(true))
        {
            // modal is the full-screen input dimmer; the title/body live in its authored child panel.
            var body=view.modalTitle.GetComponentInParent<CrispPanelImage>(true);
            if(body!=null)Skin(body,InkSurfaceRole.Panel,true);
            foreach(var row in view.choices.Concat(view.compactChoices??Array.Empty<ExpansionChoiceButton>()))
                if(row?.button!=null)Skin(row.button.targetGraphic,InkSurfaceRole.Choice,false,null,row.button);
            foreach(var fill in view.spellFills)Gauge(fill,style.mana);
        }
        foreach(var hud in root.GetComponentsInChildren<CombatHudView>(true))
        {
            Gauge(Ref<Image>(hud,"playerHp"),style.hp);Gauge(Ref<Image>(hud,"monsterHp"),style.hp);Gauge(Ref<Image>(hud,"shield"),style.guard);
            if(hud.guardGauge!=null){hud.guardGauge.inkStyle=style;hud.guardGauge.material=style.material;EditorUtility.SetDirty(hud.guardGauge);changed=true;}
        }
        foreach(var hud in root.GetComponentsInChildren<ManaHudView>(true))Gauge(hud.fill,style.mana);
        foreach(var hud in root.GetComponentsInChildren<RunHudView>(true))
        {Gauge(Ref<Image>(hud,"healthFill"),style.hp);Gauge(Ref<Image>(hud,"experienceFill"),style.brass);}
        foreach(var book in root.GetComponentsInChildren<SpellbookView>(true))
        {
            foreach(var fill in book.slotFills)Gauge(fill,style.mana);
            Skin(book.knownScroll.GetComponent<Image>(),InkSurfaceRole.Panel);
            var title=book.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if(title!=null)
            {
                var rect=title.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,.5f);
                rect.anchoredPosition=new Vector2(32,-50);rect.sizeDelta=new Vector2(500,72);
                SetTextSize(title,48);EditorUtility.SetDirty(rect);
            }
            if(book.close!=null)
            {
                var rect=(RectTransform)book.close.transform;rect.anchorMin=rect.anchorMax=Vector2.one;rect.pivot=new Vector2(1,.5f);
                rect.anchoredPosition=new Vector2(-24,-50);rect.sizeDelta=new Vector2(216,72);EditorUtility.SetDirty(rect);
            }
            if(book.heading!=null)
            {
                SetTextSize(book.heading,52);book.heading.rectTransform.anchoredPosition=new Vector2(248,240);
                book.heading.rectTransform.sizeDelta=new Vector2(816,96);EditorUtility.SetDirty(book.heading.rectTransform);
            }
        }
        foreach(var tooltip in root.GetComponentsInChildren<ItemTooltipView>(true))
            Skin(Ref<GameObject>(tooltip,"panel")?.GetComponent<Image>(),InkSurfaceRole.Panel);
        foreach(var potion in root.GetComponentsInChildren<PotionActionView>(true))
            Skin(potion.menu.GetComponent<Image>(),InkSurfaceRole.Panel);
        foreach(var map in root.GetComponentsInChildren<DungeonMapView>(true))
            Skin(map.GetComponent<Image>(),InkSurfaceRole.Field);
        foreach(var icon in root.GetComponentsInChildren<MapRoomIcon>(true))
        {
            var arrow=Ref<TextMeshProUGUI>(icon,"facingArrow");
            if(arrow!=null){arrow.rectTransform.sizeDelta=new Vector2(56,56);EditorUtility.SetDirty(arrow.rectTransform);changed=true;}
        }
        foreach(var shop in root.GetComponentsInChildren<ShopView>(true))
        {
            var hint=Ref<GameObject>(shop,"panel")?.transform.Find("StockHint")?.GetComponent<TextMeshProUGUI>();
            if(hint==null)continue;
            hint.rectTransform.anchoredPosition=new Vector2(0,-240);hint.rectTransform.sizeDelta=new Vector2(1300,48);
            SetTextSize(hint,28);EditorUtility.SetDirty(hint.rectTransform);changed=true;
        }
        foreach(var arc in root.GetComponentsInChildren<ArcGaugeGraphic>(true))
        {
            arc.inkStyle=style;arc.inkTrack=arc.name.Contains("Track");arc.material=style.material;EditorUtility.SetDirty(arc);changed=true;
        }
        foreach(var scroll in root.GetComponentsInChildren<Scrollbar>(true))
        {
            if(Diagnostic(scroll)||TopStrip(scroll.image))continue;
            if(scroll.GetComponent<Image>() is Image track)track.color=style.recess;
            if(scroll.targetGraphic!=null)scroll.targetGraphic.color=style.edge;
        }
        foreach(var view in root.GetComponentsInChildren<MonsterView>(true))
        {
            Gauge(Ref<Image>(view,"hpFill"),style.hp);Gauge(Ref<Image>(view,"directionalGuardFill"),style.guard);
            Skin(Ref<Image>(view,"attackNameBackdrop"),InkSurfaceRole.Button);
            Skin(Ref<Image>(view,"chargeTimeBackdrop"),InkSurfaceRole.Button);
        }
        foreach(var view in root.GetComponentsInChildren<RewardPopupView>(true))Gauge(Ref<Image>(view,"experienceFill"),style.brass);
        foreach(var image in root.GetComponentsInChildren<Image>(true))
        {
            if((image.name=="TopAccent"||image.name=="Divider"||image.name=="Accent") && !TopStrip(image)&&Plain(image)){image.color=style.iron;Skin(image,InkSurfaceRole.Divider);}
            if(image.name=="CastingProgress" && Plain(image))Skin(image,InkSurfaceRole.GaugeFill,false,style.mana);
        }
        count+=root.GetComponentsInChildren<InkUiSurface>(true).Length;
        return changed;
    }
    static void SetTextSize(TextMeshProUGUI label,float size)
    {
        label.fontSize=size;var binding=label.GetComponent<TypographyRoleBinding>();
        if(binding!=null){binding.designSize=size;binding.Apply();EditorUtility.SetDirty(binding);}
        EditorUtility.SetDirty(label);
    }
}
