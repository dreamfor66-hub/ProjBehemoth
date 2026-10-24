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

[InitializeOnLoad]
internal static class OctoberReviewPreparation
{
    static double next;
    static OctoberReviewPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+4;
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset");
        if(profile==null||profile.feedbackRevision>=2||!File.Exists("Assets/Art/Arcade224/TitleLabyrinth.png"))return;
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))return;
        try{Prepare();}catch(Exception e){next=double.MaxValue;Debug.LogException(e);}
    }
    internal static void Prepare()
    {
        if(EditorApplication.isPlaying)return;
        OutGameShellPreparation.Prepare();
        var profile=AssetDatabase.LoadAssetAtPath<ArcadePresentation>("Assets/Data/Presentation/Arcade224.asset");
        const string path="Assets/Scenes/MainGameScene.unity";var scene=SceneManager.GetSceneByPath(path);var open=scene.IsValid()&&scene.isLoaded;if(!open)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        var canvas=All<Canvas>(scene).First(c=>c.isRootCanvas);var root=canvas.transform;
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Art/Arcade224/ArcadeFont.asset");
        var hud=All<CombatHudView>(scene).First();hud.presentation=profile;var fill=Ref<Image>(hud,"shield");var track=fill.transform.parent.GetComponent<Image>();Place(track.transform,-47,-71,12,70);track.enabled=false;fill.enabled=false;
        var gauge=Child(track.transform,"Slanted",typeof(RectTransform),typeof(SlantedGuardGauge)).GetComponent<SlantedGuardGauge>();Stretch(gauge.rectTransform);gauge.fillColor=new Color32(147,171,182,255);gauge.raycastTarget=false;gauge.SetAllDirty();hud.guardGauge=gauge;
        var mana=All<ManaHudView>(scene).First();mana.gauge.transform.SetParent(root,false);Place(mana.gauge.transform,-132,-103,92,5);mana.label.transform.SetParent(mana.gauge.transform,false);Place(mana.label.transform,0,11,104,14);Font(mana.label,11);mana.fill.color=new Color32(124,167,163,255);
        var expansion=All<RunExpansionView>(scene).First();Place(expansion.characterStatus.transform,0,-12,180,40);Font(expansion.characterStatus,11);expansion.characterStatus.alignment=TextAlignmentOptions.Center;
        Place(expansion.encounterHint.transform,0,33,300,30);Font(expansion.encounterHint,11);
        var arcade=All<ArcadeHudController>(scene).First();arcade.combatHintPosition=new Vector2(0,80);Place(arcade.hint.transform,-22,-102,310,44);Font(arcade.hint,11);arcade.hint.alignment=TextAlignmentOptions.Center;
        foreach(var button in new[]{arcade.mapButton,arcade.inventoryButton,expansion.returnButton})button.transform.SetParent(root,false);
        Place(arcade.mapButton.transform,66,100,42,18);Place(arcade.inventoryButton.transform,120,100,50,18);Place(expansion.returnButton.transform,173,100,34,18);
        foreach(var button in new[]{arcade.mapButton,arcade.inventoryButton,expansion.returnButton})foreach(var label in button.GetComponentsInChildren<TextMeshProUGUI>(true)){Font(label,11);Place(label.transform,0,0,((RectTransform)button.transform).rect.width/4-2,16);}
        var points=new[]{new Vector2(96,-78),new Vector2(152,-78),new Vector2(124,-56),new Vector2(124,-100)};
        for(var i=0;i<4;i++)
        {
            var button=expansion.spellButtons[i];Place(button.transform,points[i].x,points[i].y,44,20);
            var icon=button.transform.Find("SpellIcon").GetComponent<Image>();Place(icon.transform,-12,1,12,12);icon.preserveAspect=true;
            var label=expansion.spellLabels[i];Place(label.transform,8,1,28,14);Font(label,11);label.alignment=TextAlignmentOptions.Center;
            expansion.spellFills[i].transform.SetParent(button.transform,false);
        }
        if(mana.spellbook!=null){mana.spellbook.transform.SetParent(root,false);Place(mana.spellbook.transform,147,74,66,18);}
        var area=All<CombatSceneView>(scene).First().LogicalArea;
        var guide=Child(area,"SpellAimGuide",typeof(RectTransform),typeof(SpellAimGuide)).GetComponent<SpellAimGuide>();Stretch(guide.rectTransform);guide.input=All<CombatInputController>(scene).First();guide.combat=All<CombatController>(scene).First();guide.raycastTarget=false;
        var parry=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/SheathParryWindow.asset");parry.ReleaseData.Time=.3f;EditorUtility.SetDirty(parry);
        var sheath=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/Sheathed.asset");sheath.attackFeedbackKey="ui.combatcontroller.9b76b68d";sheath.attackFeedbackPriority=1;EditorUtility.SetDirty(sheath);
        var prepared=AssetDatabase.LoadAssetAtPath<BuffData>("Assets/Data/Behemoth/Buffs/PreparedDraw.asset");prepared.attackFeedbackKey="ui.combatcontroller.deb723db";prepared.attackFeedbackPriority=2;BehemothTextEditor.SyncAsset(prepared,true);EditorUtility.SetDirty(prepared);
        foreach(var spell in All<CombatController>(scene).First().Content.spells){spell.targeting=spell.element==SpellElement.Lightning&&spell.shape is SpellShape.Pierce or SpellShape.Chain or SpellShape.Burst?SpellTargeting.Point:spell.shape==SpellShape.Pierce?SpellTargeting.SwipeRay:SpellTargeting.Automatic;BehemothTextEditor.SyncAsset(spell,true);EditorUtility.SetDirty(spell);}
        var prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Combat/MonsterView.prefab");try{var v=prefab.GetComponent<MonsterView>();Place(Ref<Image>(v,"hpFill").transform.parent,0,16,42,3);var timer=Ref<TextMeshProUGUI>(v,"chargeTimeLabel");Font(timer,22);Place(timer.transform,100,-28,96,26);timer.textWrappingMode=TextWrappingModes.NoWrap;Place(Ref<Image>(v,"chargeTimeBackdrop").transform,100,-28,100,28);PrefabUtility.SaveAsPrefabAsset(prefab,"Assets/Prefabs/Combat/MonsterView.prefab");}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        foreach(var graphic in All<Graphic>(scene))if(graphic.GetComponent<CanvasRenderer>()==null)graphic.gameObject.AddComponent<CanvasRenderer>();
        var weakPrefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Combat/WeakPointView.prefab");try{foreach(var graphic in weakPrefab.GetComponentsInChildren<Graphic>(true))if(graphic.GetComponent<CanvasRenderer>()==null)graphic.gameObject.AddComponent<CanvasRenderer>();PrefabUtility.SaveAsPrefabAsset(weakPrefab,"Assets/Prefabs/Combat/WeakPointView.prefab");}finally{PrefabUtility.UnloadPrefabContents(weakPrefab);}
        profile.feedbackRevision=2;EditorUtility.SetDirty(profile);EditorUtility.SetDirty(hud);EditorUtility.SetDirty(mana);EditorUtility.SetDirty(expansion);EditorUtility.SetDirty(arcade);EditorUtility.SetDirty(guide);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!open)EditorSceneManager.CloseScene(scene,true);
        Directory.CreateDirectory("Logs");File.WriteAllText("Logs/OctoberReview-Preparation.txt","Saved revised selection, title, HUD, guard silhouette, D-pad, aimed spells, parry0.3. "+DateTime.Now);
        void Font(TextMeshProUGUI text,int size){text.font=font;text.fontSize=size*4;text.enableAutoSizing=false;}
    }
    static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
    static T Ref<T>(UnityEngine.Object owner,string field)where T:UnityEngine.Object=>new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static GameObject Child(Transform parent,string name,params Type[] types){var t=parent.Find(name);if(t!=null)return t.gameObject;var g=new GameObject(name,types);g.transform.SetParent(parent,false);return g;}
    static void Place(Transform t,float x,float y,float w,float h){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.localScale=Vector3.one;r.anchoredPosition=new Vector2(x,y)*4;r.sizeDelta=new Vector2(w,h)*4;}
    static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=Vector2.one*.5f;r.offsetMin=r.offsetMax=Vector2.zero;}
}
