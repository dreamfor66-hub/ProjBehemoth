using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class HudStructurePreparation
{
    internal const string Folder="Docs/Production/HudStructure-2026-10-29/";
    static bool done,busy;
    static HudStructurePreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(done||busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||EditorApplication.isPlayingOrWillChangePlaymode)return;
        const string path="Assets/Scenes/MainGameScene.unity";
        var scene=SceneManager.GetSceneByPath(path);var loaded=scene.isLoaded;
        if(loaded&&scene.isDirty)return; // Never overwrite unsaved designer edits.
        busy=true;
        try
        {
            if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var canvas=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<QuickInventoryView>(true)).Single().GetComponentInParent<Canvas>();
            var structure=canvas.GetComponent<HudSceneStructure>();
            if(structure!=null&&structure.revision>=1)
            {
                if(structure.revision<2)
                {
                    var guardText=Ref<TextMeshProUGUI>(canvas.GetComponentInChildren<CombatHudView>(true),"shieldText");
                    guardText.gameObject.SetActive(true);guardText.raycastTarget=false;structure.revision=2;
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                if(structure.revision<3)
                {
                    var table=BehemothTextEditor.Table;table.Set("ui.combathudview.82e84bd4","가드 {0:0} / {1:0}");table.Invalidate();EditorUtility.SetDirty(table);
                    structure.revision=3;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                }
                done=true;return;
            }
            Directory.CreateDirectory(Folder+"Before");
            if(!File.Exists(Folder+"Before/MainGameScene.unity"))File.Copy(path,Folder+"Before/MainGameScene.unity");
            Prepare(canvas);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();done=true;
        }
        catch(Exception e){done=true;Debug.LogException(e);}
        finally{if(!loaded&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);busy=false;}
    }
    static RectTransform Group(Transform parent,string name)
    {
        var child=parent.Find(name) as RectTransform;
        if(child!=null)return child;
        var go=new GameObject(name,typeof(RectTransform));go.layer=parent.gameObject.layer;
        var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        return r;
    }
    static T Ref<T>(Component owner,string name)where T:Object=>new SerializedObject(owner).FindProperty(name)?.objectReferenceValue as T;
    static void Prepare(Canvas canvas)
    {
        var root=(RectTransform)canvas.transform;var structure=canvas.GetComponent<HudSceneStructure>()??canvas.gameObject.AddComponent<HudSceneStructure>();
        var report=new StringBuilder("One-time saved hierarchy migration; existing references retained. World-corner deltas for reparented nodes:\n");
        var nodes=root.GetComponentsInChildren<RectTransform>(true);
        RectTransform Find(string name)=>nodes.FirstOrDefault(n=>n.name==name&&n.parent==root);
        void Move(Transform item,RectTransform parent)
        {
            if(item==null)return;
            var r=(RectTransform)item;var a=new Vector3[4];var b=new Vector3[4];r.GetWorldCorners(a);r.SetParent(parent,true);r.GetWorldCorners(b);
            report.AppendLine(item.name+": "+Enumerable.Range(0,4).Max(i=>Vector3.Distance(a[i],b[i])).ToString("F4"));
        }
        void MoveNames(RectTransform parent,params string[] names){foreach(var name in names)Move(Find(name),parent);}
        structure.world=Group(root,"WorldPresentation");MoveNames(structure.world,"Background","FatigueHaze");
        var combat=root.GetComponentInChildren<CombatHudView>(true);structure.combat=(RectTransform)combat.transform;
        structure.drawers=Group(root,"Drawers");MoveNames(structure.drawers,"LeftPanel","RightPanel");
        structure.messages=Group(root,"GameplayMessages");MoveNames(structure.messages,"EncounterMessagePanel","RunExpansion");
        structure.developerTools=Group(root,"DeveloperTools");MoveNames(structure.developerTools,"DebugPanel","DebugCheatPanel");
        structure.decisions=Find("CenterModalLayer");
        var transitions=Group(root,"ScreenTransitions");MoveNames(transitions,"RoomPixelTransition");
        structure.hud=Group(root,"HUD");MoveNames(structure.hud,"ArcadeStatus","QuickInventory","FatigueHUD","ContextHint");
        structure.spellControls=Group(structure.hud,"SpellControls");MoveNames(structure.spellControls,"Spell_Left","Spell_Right","Spell_Up","Spell_Down","ManaAndSpellbook","Spellbook");
        structure.itemOverlays=Group(root,"ItemOverlays");MoveNames(structure.itemOverlays,"RunItemTooltip","PotionActions");
        structure.playerStatusGauges=Group(structure.hud,"PlayerStatusGauges");
        Move(Ref<Image>(combat,"playerHp").transform.parent,structure.playerStatusGauges);Move(Find("ManaGauge"),structure.playerStatusGauges);
        var exploration=Group(structure.combat,"Exploration");
        var actors=Group(structure.combat,"Actors");var effects=Group(structure.combat,"CombatEffects");
        var legacy=Group(structure.combat,"LegacyHud");
        structure.playerGuard=Group(structure.combat,"PlayerGuard");structure.pointerFeedback=Group(structure.combat,"PointerFeedback");
        var flashes=Group(structure.combat,"ScreenEffects");
        var combatNodes=structure.combat.Cast<Transform>().ToArray();
        void CombatMove(RectTransform group,params string[] names){foreach(var name in names)Move(combatNodes.FirstOrDefault(n=>n.name==name),group);}
        CombatMove(exploration,"WalkBackdrop","NavigationChoiceView");CombatMove(actors,"MonsterSpawnRoot","PlayerSpawnRoot");
        CombatMove(effects,"WeakPointRoot","AttackFxRoot","TelegraphRoot","DamageNumberRoot","SpellAimGuide");
        CombatMove(legacy,"MonsterHpBar","PlayerStats","MonsterStateInfo");
        CombatMove(flashes,"CriticalFlash");
        var shield=Ref<Image>(combat,"shield");var guard=(RectTransform)shield.transform.parent;Move(guard,structure.playerGuard);
        guard.name="GuardGauge";guard.anchorMin=guard.anchorMax=new Vector2(.5f,0);guard.pivot=Vector2.one*.5f;guard.anchoredPosition=new Vector2(0,32);guard.sizeDelta=new Vector2(300,28);
        var label=Ref<TextMeshProUGUI>(combat,"shieldText");
        if(label!=null)
        {
            label.gameObject.SetActive(true);label.raycastTarget=false;
            var r=label.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,18);r.sizeDelta=new Vector2(260,28);label.fontSize=22;
            var typography=label.GetComponent<TypographyRoleBinding>();if(typography!=null){typography.designSize=22;typography.Apply();}
        }
        CombatMove(structure.pointerFeedback,"ChargeGaugeRoot","Attack Recovery Sword");
        var recovery=combat.attackRecovery;recovery.name="AttackCooldown";recovery.rectTransform.sizeDelta=new Vector2(128,26);
        recovery.material=AssetDatabase.LoadAssetAtPath<InkUiStyle>(InkUiPreparation.StylePath).material;recovery.raycastTarget=false;
        var anchor=recovery.GetComponent<AttackCooldownAnchor>()??recovery.gameObject.AddComponent<AttackCooldownAnchor>();
        anchor.scene=root.GetComponentInChildren<CombatSceneView>(true);anchor.combat=canvas.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatController>(true)).Single();anchor.input=root.GetComponentInChildren<CombatInputController>(true);anchor.graphic=recovery;anchor.authoredPosition=recovery.rectTransform.anchoredPosition;
        foreach(var t in new[]{exploration,actors,effects,legacy,structure.playerGuard,structure.pointerFeedback,flashes})t.SetAsLastSibling();
        var quick=root.GetComponentInChildren<QuickInventoryView>(true);PrepareQuick(quick);
        var runHud=root.GetComponentInChildren<RunHudView>(true);
        foreach(var field in new[]{"perkGrid","relicGrid","potionGrid"})
        {
            var grid=Ref<RectTransform>(runHud,field);if(grid==null)continue;
            var pool=grid.GetComponent<RunHudIconPool>()??grid.gameObject.AddComponent<RunHudIconPool>();pool.content=grid;pool.prefab=Ref<RunHudIconView>(runHud,"iconPrefab");
            pool.storage=Group(grid,"Pool");pool.storage.gameObject.SetActive(false);
        }
        foreach(var t in new[]{structure.world,structure.combat,structure.drawers,structure.messages,structure.developerTools,structure.decisions,transitions,structure.hud,structure.itemOverlays})if(t!=null)t.SetAsLastSibling();
        var textTable=BehemothTextEditor.Table;textTable.Set("ui.combathudview.82e84bd4","가드 {0:0} / {1:0}");textTable.Invalidate();EditorUtility.SetDirty(textTable);
        structure.revision=3;
        File.WriteAllText(Folder+"Migration.txt",report+"Intentional layout changes: pooled quick rows, pointer cooldown, horizontal guard under the player.\n");
    }
    static void PrepareQuick(QuickInventoryView quick)
    {
        const string path="Assets/Prefabs/UI/QuickInventoryIcon.prefab";
        var old=quick.GetComponentsInChildren<RunHudIconView>(true);var prefab=AssetDatabase.LoadAssetAtPath<RunHudIconView>(path);
        if(prefab==null)
        {
            var source=Object.Instantiate(old[0].gameObject);source.name="QuickInventoryIcon";
            var rect=(RectTransform)source.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(64,64);source.SetActive(true);
            prefab=PrefabUtility.SaveAsPrefabAsset(source,path).GetComponent<RunHudIconView>();Object.DestroyImmediate(source);
        }
        foreach(var icon in old)Object.DestroyImmediate(icon.gameObject);
        var stack=Group(quick.transform,"Items");stack.anchorMin=stack.anchorMax=Vector2.one*.5f;stack.pivot=new Vector2(0,1);stack.anchoredPosition=new Vector2(-536,340);stack.sizeDelta=new Vector2(1220,0);
        var layout=stack.gameObject.AddComponent<VerticalLayoutGroup>();layout.childAlignment=TextAnchor.UpperLeft;layout.spacing=12;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
        var fit=stack.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var storage=Group(quick.transform,"Pool");storage.gameObject.SetActive(false);
        RunHudIconPool Pool(string name,int columns,float gap)
        {
            var row=Group(stack,name);var grid=row.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(64,64);grid.spacing=new Vector2(gap,8);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=columns;grid.childAlignment=TextAnchor.UpperLeft;
            var pool=row.gameObject.AddComponent<RunHudIconPool>();pool.prefab=prefab;pool.content=row;pool.storage=Group(storage,name);return pool;
        }
        quick.ConfigurePools(Pool("Relics",18,4),Pool("Potions",4,12));
    }
}
