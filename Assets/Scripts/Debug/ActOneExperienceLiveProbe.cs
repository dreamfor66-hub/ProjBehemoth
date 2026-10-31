#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ActOneExperienceLiveProbe : MonoBehaviour
{
    const string Folder = "Docs/Production/ActOneExperience-2026-10-28/";
    CombatController combat; CombatSceneView scene; CombatInputController input; RunController run;
    GameFlowController flow; ArcadeHudController hud; RunExpansionView expansion;
    bool background; float speed; PlayerCombatData previousCharacter; int failures;
    static T Ref<T>(object owner, string field) => (T)owner.GetType().GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
    void Awake() { background = Application.runInBackground; speed = Time.timeScale; previousCharacter = RunSession.SelectedCharacter; Application.runInBackground = true; }
    void OnDestroy() { Application.runInBackground = background; Time.timeScale = speed; RunSession.SelectedCharacter = previousCharacter; }
    void Check(bool ok, string message)
    { File.AppendAllText(Folder + "Live.txt", (ok ? "PASS " : "FAIL ") + message + "\n"); if (!ok) failures++; }
    public void RunPoses(bool before) => StartCoroutine(PoseReview(before));
    public void RunMonsterPoses() => StartCoroutine(PoseReview(false,true));
    public void RunWarriorPoses() => StartCoroutine(PoseReview(false,false,true));
    IEnumerator PoseReview(bool before,bool monstersOnly=false,bool warriorOnly=false)
    {
        const string poseFolder = "Docs/Production/ImageOnlyPoses-2026-10-28/";
        var phase = warriorOnly ? "WarriorCounterReady" : monstersOnly ? "MonstersAfter" : before ? "Before" : "After";
        Directory.CreateDirectory(poseFolder + "Screenshots");
        var log = poseFolder + phase + ".txt"; File.WriteAllText(log,"Scoped image transitions and held-input rendering only. No balance changes or combat suite.\n");
        yield return null; yield return null;
        combat=FindFirstObjectByType<CombatController>(); scene=FindFirstObjectByType<CombatSceneView>(); input=FindFirstObjectByType<CombatInputController>();
        run=FindFirstObjectByType<RunController>(); hud=FindFirstObjectByType<ArcadeHudController>(); expansion=FindFirstObjectByType<RunExpansionView>();
        foreach(var controller in FindObjectsByType<GameFlowController>(FindObjectsSortMode.None)) {controller.StopAllCoroutines();controller.enabled=false;}
        typeof(CombatController).GetField("Completed",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(combat,null);
        FindFirstObjectByType<DungeonController>().HideNavigation();
        var fixture=Instantiate(DungeonSlash.DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_해골_전사.asset"));
        fixture.maxHp=50000; fixture.normalAttackInterval=999; fixture.chargeInterval=999;
        if(monstersOnly){RunSession.SelectedCharacter=combat.Content.characters.First(c=>c.character==CharacterKind.Warrior);FreshRun();yield return new WaitForSecondsRealtime(.6f);}
        foreach(var data in combat.Content.characters.Where(c=>!monstersOnly&&(!warriorOnly||c.character==CharacterKind.Warrior)))
        {
            RunSession.SelectedCharacter=data; FreshRun(); input.enabled=true;
            combat.PrepareEncounter(run.State,new[]{fixture},1); combat.BeginPreparedCombat();
            yield return new WaitForSecondsRealtime(.6f);
            var view=FindFirstObjectByType<PlayerView>(); var body=Ref<Image>(view,"body"); var pose=body.GetComponent<CombatSpriteAnimator>();
            void Record(bool ok,string message) => File.AppendAllText(log,(ok?"PASS ":"FAIL ")+data.character+": "+message+"\n");
            var library=data.combatAnimation;
            Record(body.sprite!=null,"Idle image="+body.sprite?.name);
            if(!before) Record(library.ready!=null && library.ready!=library.idle && library.ready!=library.attack,"dedicated Ready painting");
            var center=Position(combat.Enemies[0]); var a=ScreenAt(center+Vector2.left*120); var b=ScreenAt(center+Vector2.right*120);
            Contact("Begin",970,a);
            for(float t=0;t<.25f;t+=Time.deltaTime){Contact("Move",970,a);yield return null;}
            Record(input.IsPreparingAttack&&pose.State==CombatVisualState.Ready&&body.sprite!=null,"hold shows Ready without null sprite");
            yield return PoseCapture(poseFolder+"Screenshots/"+phase+"-"+data.character+"-Ready.png");
            for(float t=0;t<combat.Player.CurrentChargeDuration;t+=Time.deltaTime){Contact("Move",970,a);yield return null;}
            yield return PoseCapture(poseFolder+"Screenshots/"+phase+"-"+data.character+"-Charged.png");
            var blanks=FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i=>i.isActiveAndEnabled&&i.sprite==null&&i.color.a>.8f&&i.color.r>.8f&&i.color.g>.8f&&i.color.b>.8f&&i.rectTransform.rect.width>40&&i.rectTransform.rect.height>40);
            File.AppendAllText(log,"Visible sprite-less pale images: "+string.Join("; ",blanks.Select(i=>i.name+" parent="+i.transform.parent?.name+" rect="+i.rectTransform.rect))+"\n");
            Contact("Move",970,b);Contact("End",970,b,false);yield return null;
            Record(pose.State==CombatVisualState.Attack&&body.sprite==library.attack,"release switches Attack painting");
            yield return PoseCapture(poseFolder+"Screenshots/"+phase+"-"+data.character+"-Attack.png");
            yield return new WaitForSecondsRealtime(.4f);
            Record(!input.IsPreparingAttack&&pose.State==CombatVisualState.Idle&&body.sprite==library.idle,"release recovers Idle");
            if(!before)
            {
                var savedPosition=body.rectTransform.anchoredPosition; var savedScale=body.rectTransform.localScale; var savedRotation=body.rectTransform.localRotation;
                foreach(var state in new[]{CombatVisualState.Idle,CombatVisualState.Ready,CombatVisualState.Attack,CombatVisualState.Guard})
                {
                    var check=Instantiate(library);check.illustratedMotion=true;check.emphasizePoses=true;check.readyTilt=40;check.attackTilt=70;check.readyOffset=new Vector2(99,88);check.attackOffset=new Vector2(77,66);
                    pose.Play(check,state,true);
                    using(var vertices=new VertexHelper())
                    {
                        var vertex=UIVertex.simpleVert; vertex.position=new Vector3(10,20,0);vertices.AddVert(vertex);pose.ModifyMesh(vertices);vertices.PopulateUIVertex(ref vertex,0);
                        var expected=new Vector3(10,20,0)*check.canvasScale;
                        Record(Vector3.Distance(vertex.position,expected)<.01f,"legacy keyed transforms ignored for "+state);
                    }
                    Destroy(check);
                }
                pose.Play(library,CombatVisualState.Idle);
                Record(body.rectTransform.anchoredPosition==savedPosition&&body.rectTransform.localScale==savedScale&&body.rectTransform.localRotation==savedRotation,"no pose transform mutation");
                var missing=Instantiate(library);missing.ready=null;pose.Play(missing,CombatVisualState.Ready);Record(body.sprite==library.idle&&body.enabled,"missing Ready safely falls back to Idle");Destroy(missing);pose.Play(library,CombatVisualState.Idle);
                var broken=Instantiate(library);var ghost=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f);
                DestroyImmediate(ghost);broken.ready=ghost;pose.Play(broken,CombatVisualState.Ready);
                Record(body.sprite==library.idle&&body.enabled,"destroyed Unity sprite wrapper safely falls back to Idle");Destroy(broken);
                var empty=ScriptableObject.CreateInstance<CombatSpriteAnimation>();pose.Play(empty,CombatVisualState.Ready);
                Record(!body.enabled,"no images hides graphic rather than drawing white quad");Destroy(empty);pose.Play(library,CombatVisualState.Idle);
                var guardShape=(RectHitShape)typeof(CombatController).GetMethod("GetPlayerBodyShape",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(combat,null);
                var guardA=ScreenAt(guardShape.Center+Vector2.up*45);var guardB=ScreenAt(guardShape.Center+Vector2.down*45);
                Contact("Begin",971,guardA);Contact("Move",971,guardB);yield return null;
                for(var cycle=0;cycle<(data.character==CharacterKind.Warrior?3:1);cycle++)
                {
                    Contact("Begin",972,a);yield return null;yield return null;
                    Record(combat.Player.IsGuarding&&input.IsPreparingAttack&&pose.State==CombatVisualState.Guard&&body.sprite==library.guard,"overlap "+cycle+" Guard painting wins while attack held");
                    Contact("End",972,a,true);yield return null;
                    Record(combat.Player.IsGuarding&&!input.IsPreparingAttack,"attack cancel preserves guard");
                    Contact("Begin",972,a);Contact("End",971,guardB,false);yield return null;yield return null;
                    Record(!combat.Player.IsGuarding&&input.IsPreparingAttack&&body.sprite==library.ready,"guard release preserves Ready painting");
                    Contact("Begin",971,guardA);Contact("Move",971,guardB);Contact("End",972,a,true);yield return null;
                }
                Contact("End",971,guardB,false);yield return null;yield return null;
                Record(!input.IsPreparingAttack&&!combat.Player.IsGuarding&&body.sprite==library.idle,"final cancel/release returns Idle");
            }
        }
        if(!before&&!warriorOnly)
        {
            input.enabled=false;combat.ResetEncounter();combat.enabled=false;expansion.HideModal();hud.Close();
            var monsterIds=new[]{"Data_Monster_B1_Minion_Bat","TunnelRat","Data_Monster_B1_Minion_Skeleton","Data_Monster_B1_Minion_Leech","AlarmHound","MirrorKnight","ArmoredExecutioner","Data_Monster_B1_Elite_Minotaur","BellKeeper","BroodKeeper","OrbitBat","Data_Monster_B1_Boss_MazeGuardian"};
            foreach(var id in monsterIds)
            {
                var data=DungeonSlash.DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/"+id+".asset");
                var runtime=new MonsterRuntime(data);var library=data.combatAnimation;
                var view=Instantiate(Ref<MonsterView>(combat,"monsterPrefab"),scene.LogicalArea);view.Root.anchoredPosition=Vector2.zero;
                var body=Ref<Image>(view,"body");
                void Record(bool ok,string message)=>File.AppendAllText(log,(ok?"PASS ":"FAIL ")+data.displayName+": "+message+"\n");
                Record(library!=null&&library.idle!=null&&library.ready!=null&&library.attack!=null&&new[]{library.idle,library.ready,library.attack}.Distinct().Count()==3,"distinct Idle/Ready/Attack paintings");
                view.Bind(runtime);Record(body.sprite==library.idle,"Idle painting");
                runtime.BeginNormalAttack();view.Bind(runtime);Record(body.sprite==library.ready,"windup uses Ready painting");
                if(id=="Data_Monster_B1_Boss_MazeGuardian")yield return PoseCapture(poseFolder+"Screenshots/After-Sphinx-Ready.png");
                view.PlayAttack();view.Bind(runtime);Record(body.sprite==library.attack,"attack uses Attack painting");
                var pose=body.GetComponent<CombatSpriteAnimator>();
                using(var vertices=new VertexHelper())
                {
                    var vertex=UIVertex.simpleVert;vertex.position=new Vector3(10,20,0);vertices.AddVert(vertex);pose.ModifyMesh(vertices);vertices.PopulateUIVertex(ref vertex,0);
                    var center=(Vector3)body.rectTransform.rect.center;
                    Record(Vector3.Distance(vertex.position,center+(new Vector3(10,20,0)-center)*library.canvasScale)<.01f&&body.rectTransform.localRotation==Quaternion.identity,"fixed framing only; no keyed rotation/translation/squash");
                }
                if(id=="Data_Monster_B1_Boss_MazeGuardian")yield return PoseCapture(poseFolder+"Screenshots/After-Sphinx-Attack.png");
                Destroy(view.gameObject);yield return null;
            }
        }
        Destroy(fixture);File.AppendAllText(log,"COMPLETE\n");UnityEditor.EditorApplication.isPlaying=false;
    }
    IEnumerator PoseCapture(string path)
    {
        expansion.HideModal();hud.Close();Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,texture.EncodeToPNG());Destroy(texture);
    }
    public void RunSphinx() => StartCoroutine(SphinxReview());
    IEnumerator SphinxReview()
    {
        Directory.CreateDirectory(Folder + "Screenshots");
        yield return null; yield return null;
        combat = FindFirstObjectByType<CombatController>(); scene = FindFirstObjectByType<CombatSceneView>(); input = FindFirstObjectByType<CombatInputController>();
        run = FindFirstObjectByType<RunController>(); hud = FindFirstObjectByType<ArcadeHudController>(); expansion = FindFirstObjectByType<RunExpansionView>();
        foreach (var controller in FindObjectsByType<GameFlowController>(FindObjectsSortMode.None)) { controller.StopAllCoroutines(); controller.enabled = false; }
        typeof(CombatController).GetField("Completed", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(combat, null);
        RunSession.SelectedCharacter = combat.Content.characters.First(c => c.character == CharacterKind.Warrior);
        FreshRun();
        var budget = DungeonSlash.DesignerAssetPaths.Load<ActGrowthBudget>("Assets/Data/Balance/Act1GrowthBudget.asset");
        if (budget != null) budget.ApplyReferenceBuild(run.State, GrowthScenario.Required);
        var data = DungeonSlash.DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Boss_미궁_스핑크스.asset");
        combat.PrepareEncounter(run.State, new[] { data }, 1); combat.BeginPreparedCombat();
        FindFirstObjectByType<DungeonController>().HideNavigation();
        yield return null; yield return null;
        var enemy = combat.Enemies[0]; var center = Position(enemy);
        var view = FindObjectsByType<MonsterView>(FindObjectsSortMode.None).First(v => ReferenceEquals(Ref<MonsterData>(v,"boundData"),data));
        var soloSize = view.Root.rect.size; var soloScale = enemy.VisualScale;
        File.WriteAllText(Folder + "SphinxRuntime.txt", $"Sphinx-only scoped prototype. Growth={budget?.requiredUnits}U. Area={scene.LogicalArea.rect}, center={center}, art={soloSize}, body={data.BodySize}.\n");
        yield return Capture("Sphinx-Idle");
        combat.enabled = false;
        var animator = Ref<Image>(view,"body").GetComponent<CombatSpriteAnimator>();
        animator.Play(data.combatAnimation, CombatVisualState.Ready, true); yield return Capture("Sphinx-Ready");
        animator.Play(data.combatAnimation, CombatVisualState.Attack, true); yield return Capture("Sphinx-Attack");
        combat.enabled = true;
        input.enabled = false;
        while (enemy.State != MonsterState.Charging) { combat.Player.TrySetGuard(true); yield return null; }
        combat.enabled = false; input.enabled = true; yield return Capture("Sphinx-Weakpoints");
        var points = enemy.WeakPoints.Where(p=>!p.IsOptional).Select(p=>p.HitShape.Center).ToArray();
        File.AppendAllText(Folder + "SphinxRuntime.txt", "Weakpoints=" + string.Join("; ", points.Select(p=>p.ToString())) + "\n");
        var bounds = scene.LogicalArea.rect;
        var inBounds = points.All(p => bounds.Contains(p));
        var partsHit = points.All(p => data.IntersectsBody(new AttackSegment(p-Vector2.right*15,p+Vector2.right*15,10),center));
        var outsideMiss = !data.IntersectsBody(new AttackSegment(center+new Vector2(-650,300),center+new Vector2(-600,300),10),center);
        File.AppendAllText(Folder + "SphinxRuntime.txt", $"{(inBounds && partsHit && outsideMiss ? "PASS" : "FAIL")} targets accessible, wing/paw body hits and outside miss: {inBounds}/{partsHit}/{outsideMiss}\n");
        // A short center gesture cannot clear spread wing/paw targets.
        yield return SphinxGesture(910, center+Vector2.left*60, center+Vector2.right*60);
        var shortRemaining=enemy.WeakPoints.Count(p=>!p.IsDestroyed);
        File.AppendAllText(Folder + "SphinxRuntime.txt", $"{(shortRemaining==4?"PASS":"FAIL")} short center gesture leaves four separated targets: {shortRemaining}\n");
        yield return SphinxGesture(911, points[0] + new Vector2(-25,35), points[1] + new Vector2(25,-35));
        File.AppendAllText(Folder + "SphinxRuntime.txt", $"After left wing/paw gesture remaining={enemy.WeakPoints.Count(p=>!p.IsDestroyed)}\n");
        yield return SphinxGesture(912, points[2] + new Vector2(-25,-35), points[3] + new Vector2(25,35));
        File.AppendAllText(Folder + "SphinxRuntime.txt", $"{(enemy.IsStunned?"PASS":"FAIL")} two long wing/paw gestures interrupt charge: {enemy.State}\n");
        yield return Capture("Sphinx-Break");
        combat.enabled=true;
        FreshRun();
        var small = DungeonSlash.DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_고블린사냥꾼.asset");
        combat.PrepareEncounter(run.State,new[]{data,small},1); combat.BeginPreparedCombat(); yield return null;
        var group = combat.Enemies.First(e=>e.Data==data);
        var groupView=FindObjectsByType<MonsterView>(FindObjectsSortMode.None).First(v=>ReferenceEquals(Ref<MonsterData>(v,"boundData"),data));
        File.AppendAllText(Folder + "SphinxRuntime.txt", $"{(group.VisualScale==soloScale && groupView.Root.rect.size==soloSize?"PASS":"FAIL")} group keeps species size. solo={soloSize}/{soloScale}, group={groupView.Root.rect.size}/{group.VisualScale}\n");
        File.AppendAllText(Folder + "SphinxRuntime.txt", "COMPLETE. Art composition and input footprint only; full act balance and human gesture feel pending.\n");
        UnityEditor.EditorApplication.isPlaying=false;
    }
    IEnumerator SphinxGesture(int id, Vector2 a, Vector2 b)
    {
        for (float t = 0; t < 1.1f; t += Time.deltaTime) { combat.Player.Tick(Time.deltaTime); yield return null; }
        Contact("Begin",id,ScreenAt(a));
        for (float t = 0; t < combat.Player.CurrentChargeDuration+.12f; t += Time.deltaTime)
        { combat.Player.Tick(Time.deltaTime); Contact("Move",id,ScreenAt(a)); yield return null; }
        Contact("Move",id,ScreenAt(b)); Contact("End",id,ScreenAt(b),false);
        yield return null;
    }
    public void Run() => StartCoroutine(Review());
    public void RunEncounters() => StartCoroutine(EncounterReview());
    public void RunPressure() => StartCoroutine(EncounterReview(true));
    Vector2 ScreenAt(Vector2 point) => RectTransformUtility.WorldToScreenPoint(scene.UiCamera, scene.LogicalArea.TransformPoint(point));
    void Contact(string method, params object[] args) => typeof(CombatInputController).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(input, args);
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + "Screenshots/" + name + ".png", texture.EncodeToPNG()); Destroy(texture);
    }
    void FreshRun()
    {
        foreach (var controller in FindObjectsByType<GameFlowController>(FindObjectsSortMode.None)) controller.StopAllCoroutines();
        hud.StopAllCoroutines();
        combat.ResetEncounter(); run.BeginRun(); expansion.HideModal(); hud.Close();
        combat.ShowPlayer(run.State); combat.InputPaused = false;
    }
    Vector2 Position(MonsterRuntime enemy)
    {
        var entries = Ref<System.Collections.IEnumerable>(combat, "activeMonsters");
        foreach (var entry in entries)
            if (ReferenceEquals(entry.GetType().GetProperty("Runtime").GetValue(entry), enemy))
                return (Vector2)entry.GetType().GetProperty("Position").GetValue(entry);
        throw new InvalidOperationException("Missing live enemy position");
    }
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder + "Screenshots"); File.WriteAllText(Folder + "Live.txt", "Pilot v3: existing Editor, isolated room-flow callbacks. Changed act-one scope only. No full class/DPS or whole-game suite.\n");
        yield return null; yield return null;
        combat = FindFirstObjectByType<CombatController>(); scene = FindFirstObjectByType<CombatSceneView>(); input = FindFirstObjectByType<CombatInputController>();
        run = FindFirstObjectByType<RunController>(); flow = FindFirstObjectByType<GameFlowController>(); hud = FindFirstObjectByType<ArcadeHudController>(); expansion = FindFirstObjectByType<RunExpansionView>();
        flow.StopAllCoroutines();
        // Disabling a behaviour does not unsubscribe its events or stop existing coroutines.
        // Detach only this temporary pilot from the normal room/tutorial/reward flow.
        typeof(GameFlowController).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(flow, null);
        flow.enabled = false; RunSession.SelectedCharacter = combat.Content.characters.First(c => c.character == CharacterKind.Warrior);
        foreach (var controller in FindObjectsByType<GameFlowController>(FindObjectsSortMode.None)) { controller.StopAllCoroutines(); controller.enabled = false; }
        // Runtime-only isolation is discarded when Play mode exits.
        typeof(CombatController).GetField("Completed", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(combat, null);
        FreshRun();
        var dungeon = FindFirstObjectByType<DungeonController>(); dungeon.BeginRun(281028);
        typeof(DungeonController).GetField("RoomEntered", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(dungeon, null);
        dungeon.State.Player.CurrentRoom.IsCleared = true;
        hud.mapButton.onClick.Invoke(); yield return new WaitForSecondsRealtime(.7f);
        Check(hud.mapPanel.activeSelf && !combat.InputPaused, "map stays open without blocking navigation input");
        var before = dungeon.State.Player.CurrentRoom;
        var icon = hud.mapPanel.GetComponentsInChildren<MapRoomIcon>(true).First(i => Ref<Action>(i, "selected") != null);
        icon.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }); yield return null;
        Check(dungeon.State.Player.CurrentRoom != before && hud.mapPanel.activeSelf, "click connected map tile moves while map remains open");
        Check(dungeon.State.Player.Facing == FacingDirection.North, "map uses absolute graph direction");
        yield return Capture("01-Map-Travel"); dungeon.HideNavigation(); hud.Close();

        // Preparation and sustained overlapping contacts use production Begin/Move/End, without all-cancel between cycles.
        FreshRun();
        var monster = DungeonSlash.DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_해골_전사.asset");
        combat.PrepareEncounter(run.State, new[] { monster }, 1); combat.BeginPreparedCombat(); yield return new WaitForSecondsRealtime(.5f);
        var player = FindFirstObjectByType<PlayerView>();
        var inputCenter = Position(combat.Enemies[0]);
        var start = ScreenAt(inputCenter + Vector2.left * 130); var end = ScreenAt(inputCenter + Vector2.right * 130);
        Contact("Begin", 901, start); yield return null; yield return null;
        Check(input.IsPreparingAttack && Ref<Image>(player, "body").GetComponent<CombatSpriteAnimator>().State == CombatVisualState.Ready, "held attack enters ready pose before charge completes");
        yield return Capture("02-Player-Ready");
        Contact("Move", 901, end); Contact("End", 901, end, false); yield return null;
        Check(!input.IsPreparingAttack, "release clears preparation"); yield return Capture("03-Slash");
        combat.ResetEncounter();
        combat.PrepareEncounter(run.State, new[] { monster }, 1); combat.BeginPreparedCombat(); yield return new WaitForSecondsRealtime(.5f);
        var guardShape = (RectHitShape)typeof(CombatController).GetMethod("GetPlayerBodyShape", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(combat, null);
        var guardStart = ScreenAt(guardShape.Center + Vector2.up * 45);
        var guardEnd = ScreenAt(guardShape.Center + Vector2.down * 45);
        File.AppendAllText(Folder + "Live.txt", $"INPUT context: active={combat.IsActive}, paused={combat.InputPaused}, modal={expansion.IsDecisionOpen}, shape={guardShape.Center}/{guardShape.Size}, contacts={Ref<System.Collections.IDictionary>(input,"contacts").Count}\n");
        Contact("Begin", 902, guardStart); Contact("Move", 902, guardEnd); yield return null;
        for (var cycle = 0; cycle < 3; cycle++)
        {
            Contact("Begin", 903, start); yield return null;
            Check(combat.Player.IsGuarding && input.IsPreparingAttack, "overlap " + cycle + ": guard and attack contact remain independent");
            Contact("End", 903, start, true); yield return null;
            Check(combat.Player.IsGuarding && !input.IsPreparingAttack, "overlap " + cycle + ": attack lift preserves guard");
            Contact("Begin", 903, start); Contact("End", 902, guardEnd, false); yield return null;
            Check(input.IsPreparingAttack && !combat.Player.IsGuarding, "overlap " + cycle + ": guard lift preserves attack preparation");
            Contact("Begin", 902, guardStart); Contact("Move", 902, guardEnd); Contact("End", 903, start, true); yield return null;
        }
        Contact("End", 902, guardEnd, false); yield return null;
        Check(!input.IsPreparingAttack && !combat.Player.IsGuarding, "final release restores idle input");
        input.enabled = false;

        // One Warrior pilot for each changed monster. Production data, cooldown, AI and damage, no stat boosts or forced breaks.
        // This is a feasibility/telemetry pass, not a claim that all builds and encounter combinations are balanced.
        var paths = new[] { "Data_Monster_B1_Minion_Bat", "TunnelRat", "Data_Monster_B1_Minion_Skeleton", "Data_Monster_B1_Minion_Leech", "AlarmHound", "MirrorKnight", "ArmoredExecutioner", "Data_Monster_B1_Elite_Minotaur", "BellKeeper", "BroodKeeper", "OrbitBat", "Data_Monster_B1_Boss_MazeGuardian" };
        File.WriteAllText(Folder + "Combat-Telemetry.tsv", "monster\tseconds\thpLost\tminimumGuard\tnormalActions\tbreaks\tresult\n");
        foreach (var path in paths)
        {
            FreshRun(); input.enabled = false;
            var data = DungeonSlash.DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/" + path + ".asset");
            combat.PrepareEncounter(run.State, new[] { data }, 1); combat.BeginPreparedCombat();
            var enemy = combat.Enemies[0]; float elapsed = 0, preparation = 0, minGuard = combat.Player.ShieldCurrent;
            Check(!combat.InputPaused && !expansion.IsDecisionOpen, data.displayName + ": pilot starts without a modal pause");
            int normalActions = 0, breaks = 0, slashes = 0; var last = enemy.State;
            while (combat.IsActive && combat.Player.IsAlive && elapsed < 95)
            {
                if (combat.InputPaused) { Check(false, "Unexpected modal/pause during isolated pilot"); break; }
                combat.Player.TrySetGuard(true);
                elapsed += Time.deltaTime; minGuard = Mathf.Min(minGuard, combat.Player.ShieldCurrent);
                if (last != enemy.State && enemy.State == MonsterState.Attacking) normalActions++;
                if (last != enemy.State && enemy.State == MonsterState.Stunned) breaks++;
                last = enemy.State;
                if (combat.Player.CanNormalAttack())
                {
                    var points = enemy.WeakPoints.Where(p => !p.IsDestroyed && !p.IsOptional).ToArray();
                    if (enemy.State == MonsterState.Charging && points.Length > 0)
                    {
                        preparation += Time.deltaTime;
                        if (preparation >= combat.Player.CurrentChargeDuration)
                        {
                            var a = points[0].HitShape.Center; var b = points.Length > 1 ? points[1].HitShape.Center : a + Vector2.right * 10;
                            var direction = (b - a).normalized;
                            combat.ExecuteAttack(new AttackSegment(a - direction * 100, b + direction * 100, combat.Player.SegmentWidth), true);
                            preparation = 0;
                        }
                    }
                    else
                    {
                        preparation = 0;
                        var horizontal = slashes++ % 2 == 0;
                        var direction = horizontal ? Vector2.right : Vector2.up;
                        var center = Position(enemy);
                        combat.ExecuteAttack(new AttackSegment(center - direction * 200, center + direction * 200, combat.Player.SegmentWidth), false);
                    }
                }
                if (path == "TunnelRat" && elapsed > 2.05f && elapsed < 2.05f + Time.deltaTime * 2) yield return Capture("04-Spider-Ready");
                if (path == "Data_Monster_B1_Minion_Skeleton" && enemy.State == MonsterState.Stunned && !File.Exists(Folder + "Screenshots/05-Skeleton-Break.png")) yield return Capture("05-Skeleton-Break");
                yield return null;
            }
            var result = !enemy.IsAlive ? "won" : combat.Player.IsAlive ? "timeout" : "lost";
            File.AppendAllText(Folder + "Combat-Telemetry.tsv", $"{data.displayName}\t{elapsed:F2}\t{100 - combat.Player.CurrentHp:F2}\t{minGuard:F2}\t{normalActions}\t{breaks}\t{result}\n");
            Check(normalActions > 0 || data.diesWithLeader, data.displayName + ": normal action opportunity, or leader-dependent summon excluded from solo pacing criterion");
            Check(enemy.IsAlive == false, data.displayName + ": baseline Warrior pilot can finish without stat boosts (HP loss recorded separately)");
            yield return new WaitForSecondsRealtime(.8f);
        }
        File.AppendAllText(Folder + "Live.txt", "COMPLETE failures=" + failures + ". Human feel, varied progression/builds, encounter combinations and real device remain pending.\n");
        UnityEditor.EditorApplication.isPlaying = false;
    }

    IEnumerator EncounterReview(bool tight = false)
    {
        Directory.CreateDirectory(Folder + "Screenshots");
        File.WriteAllText(Folder + "Encounters.txt", "Actual act-one formations with declared normalized growth budgets. Required pre-boss growth; under-budget at earlier checkpoints. Not random-run averages.\n");
        yield return null; yield return null;
        combat = FindFirstObjectByType<CombatController>(); scene = FindFirstObjectByType<CombatSceneView>(); input = FindFirstObjectByType<CombatInputController>();
        run = FindFirstObjectByType<RunController>(); hud = FindFirstObjectByType<ArcadeHudController>(); expansion = FindFirstObjectByType<RunExpansionView>();
        foreach (var controller in FindObjectsByType<GameFlowController>(FindObjectsSortMode.None)) { controller.StopAllCoroutines(); controller.enabled = false; }
        typeof(CombatController).GetField("Completed", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(combat, null);
        var dungeon = FindFirstObjectByType<DungeonController>();
        typeof(DungeonController).GetField("RoomEntered", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(dungeon, null);
        RunSession.SelectedCharacter = combat.Content.characters.First(c => c.character == CharacterKind.Warrior);
        FreshRun(); input.enabled = false; dungeon.BeginRun(281028); dungeon.State.Player.CurrentRoom.IsCleared = true;
        hud.mapButton.onClick.Invoke(); yield return new WaitForSecondsRealtime(.6f); yield return Capture("06-Map-Polished"); hud.Close(); dungeon.HideNavigation();
        var stage = combat.Content.stages.First(s => s.act == 1);
        var encounters = stage.hardCombats.Concat(stage.elites).Concat(stage.bosses).Distinct().ToArray();
        if (tight)
        {
            var rows = File.ReadAllLines(Folder + "Encounter-Telemetry.tsv").Skip(1).Select(line=>line.Split('\t')).ToArray();
            var review = rows.Where(row => row.Length >= 8 && (float.Parse(row[4],System.Globalization.CultureInfo.InvariantCulture) > .01f || row[7] != "won")).Select(row => row[0]).ToHashSet();
            encounters = encounters.Where(e => review.Contains(e.displayName)).ToArray();
            File.AppendAllText(Folder + "Encounters.txt", "Targeted retry only: guard during final 0.2s of telegraphs, recover between them, and use long in-bounds swipes to connect separated enemies. No stat/data changes.\n");
        }
        var budget = DungeonSlash.DesignerAssetPaths.Load<ActGrowthBudget>("Assets/Data/Balance/Act1GrowthBudget.asset");
        File.WriteAllText(Folder + "Encounter-Telemetry.tsv", "encounter\tlevel\tgrowthProfile_U\tseconds\thpLost\tminimumGuard\tbreaks\tresult\n");
        foreach (var encounter in encounters)
        {
            FreshRun(); input.enabled = false;
            var profile = stage.bosses.Contains(encounter) ? GrowthScenario.Required : GrowthScenario.UnderBudget;
            budget.ApplyReferenceBuild(run.State, profile);
            var initialHp = run.State.Player.CurrentHp;
            combat.PrepareFixedEncounter(run.State, encounter, 1); combat.BeginPreparedCombat();
            float elapsed = 0, preparation = 0, minimum = combat.Player.ShieldCurrent; int slashes = 0, breaks = 0;
            var broken = new System.Collections.Generic.Dictionary<MonsterRuntime, bool>();
            var actions = new System.Collections.Generic.Dictionary<MonsterRuntime, bool>();
            var wasReady = new System.Collections.Generic.HashSet<MonsterRuntime>();
            while (combat.IsActive && combat.Player.IsAlive && elapsed < 100)
            {
                if (combat.InputPaused) { failures++; File.AppendAllText(Folder + "Encounters.txt", "FAIL unexpected input pause\n"); break; }
                var enemies = combat.Enemies.Where(e => e.IsAlive).ToArray();
                combat.Player.TrySetGuard(!tight || enemies.Any(e => (e.State is MonsterState.Attacking or MonsterState.Charging) && e.StateTimer <= .2f));
                elapsed += Time.deltaTime; minimum = Mathf.Min(minimum, combat.Player.ShieldCurrent);
                foreach (var enemy in enemies)
                {
                    if (enemy.IsStunned && (!broken.TryGetValue(enemy, out var stunned) || !stunned)) breaks++;
                    broken[enemy] = enemy.IsStunned;
                    var view = FindObjectsByType<MonsterView>(FindObjectsSortMode.None).FirstOrDefault(v => ReferenceEquals(Ref<MonsterData>(v, "boundData"), enemy.Data));
                    if (view == null) continue;
                    var animator = Ref<Image>(view, "body").GetComponent<CombatSpriteAnimator>();
                    if (animator != null && animator.State == CombatVisualState.Ready && wasReady.Add(enemy))
                        yield return Capture("Pose-" + enemy.Data.name + "-Ready");
                    if (animator != null && animator.State == CombatVisualState.Attack && !actions.ContainsKey(enemy))
                    { actions[enemy] = true; yield return Capture("Pose-" + enemy.Data.name + "-Attack"); }
                }
                if (combat.Player.CanNormalAttack() && enemies.Length > 0)
                {
                    var charge = enemies.Where(e => e.State == MonsterState.Charging && e.WeakPoints.Any(p => !p.IsOptional && !p.IsDestroyed)).OrderBy(e => e.StateTimer).FirstOrDefault();
                    if (charge != null)
                    {
                        preparation += Time.deltaTime;
                        if (preparation >= combat.Player.CurrentChargeDuration)
                        {
                            var points = charge.WeakPoints.Where(p => !p.IsOptional && !p.IsDestroyed).ToArray();
                            var a = points[0].HitShape.Center; var b = points.Length > 1 ? points[1].HitShape.Center : a + Vector2.right * 10;
                            var direction = (b - a).normalized;
                            combat.ExecuteAttack(new AttackSegment(a - direction * 100, b + direction * 100, combat.Player.SegmentWidth), true); preparation = 0;
                        }
                    }
                    else
                    {
                        preparation = 0; var target = enemies.OrderBy(e => e.CurrentHp).First();
                        var guard = target.Data.GetAttack(MonsterAttackType.Normal)?.combatMechanics?.directionalGuard;
                        var vertical = target.IsDirectionalGuardActive && guard != null && (guard.breakByAttackWay & TriggerAttackWayFilter.Horizontal) == 0;
                        var direction = (vertical ? Vector2.up : Vector2.right) * (slashes++ % 2 == 0 ? 1 : -1);
                        var center = Position(target);
                        var segment = new AttackSegment(center - direction * 260, center + direction * 260, combat.Player.SegmentWidth);
                        if (tight && !vertical)
                        {
                            var candidates = enemies.Select(e=>Position(e).y).Concat(enemies.SelectMany(a=>enemies.Select(b=>(Position(a).y+Position(b).y)*.5f)));
                            segment = candidates.Select(y => new AttackSegment(new Vector2(scene.LogicalArea.rect.xMin+45,y),new Vector2(scene.LogicalArea.rect.xMax-45,y),combat.Player.SegmentWidth))
                                .OrderByDescending(s=>enemies.Count(e=>e.Data.IntersectsBody(s,Position(e),e.VisualScale))).First();
                            if (slashes % 2 == 0) segment = new AttackSegment(segment.End, segment.Start, segment.Width);
                        }
                        combat.ExecuteAttack(segment, false);
                    }
                }
                yield return null;
            }
            var won = combat.Enemies.All(e => !e.IsAlive);
            var lost = initialHp - combat.Player.CurrentHp;
            File.AppendAllText(Folder + "Encounter-Telemetry.tsv", $"{encounter.displayName}\t{run.State.Level}\t{profile}:{budget.UnitsFor(profile):F2}\t{elapsed:F2}\t{lost:F2}\t{minimum:F2}\t{breaks}\t{(won ? "won" : combat.Player.IsAlive ? "timeout" : "lost")}\n");
            if (!won || lost > .01f) failures++;
            File.AppendAllText(Folder + "Encounters.txt", (won && lost <= .01f ? "PASS " : "REVIEW ") + encounter.displayName + $": HP loss={lost:F2}, time={elapsed:F2}, guard minimum={minimum:F2}, breaks={breaks}\n");
            yield return new WaitForSecondsRealtime(1);
        }
        File.AppendAllText(Folder + "Encounters.txt", "COMPLETE review=" + failures + ". Automated pilot uses production cooldowns, actual placements and manual-length charge preparation. Human encounter feel and varied builds remain pending.\n");
        if (!tight) yield return CompareGuardRoles();
        UnityEditor.EditorApplication.isPlaying = false;
    }
    IEnumerator CompareGuardRoles()
    {
        combat.ResetEncounter();
        var balance = Ref<RunBalanceSettings>(run, "balanceSettings");
        var source = DungeonSlash.DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Normal_흡혈박쥐.asset");
        var attack = source.GetAttack(MonsterAttackType.Normal);
        var interval = source.normalAttackInterval + attack.windupDuration;
        var roles = new[] { CharacterKind.Warrior, CharacterKind.Rogue }.Select(kind =>
        {
            var data = combat.Content.characters.First(c => c.character == kind); var state = new RunState(data, balance);
            if (data.startingRelic != null) state.TryEquip(data.startingRelic); state.BeginCombat(); return state;
        }).ToArray();
        var damage = new DamageResolver(); var counts = new int[2];
        float elapsed = 0; var next = interval; var hits = 0;
        while (hits < 6)
        {
            var dt = Time.deltaTime; elapsed += dt;
            foreach (var state in roles) { state.Player.TrySetGuard(true); state.Player.Tick(dt); }
            if (elapsed >= next)
            {
                for (var i = 0; i < roles.Length; i++)
                {
                    var result = damage.ApplyToPlayer(roles[i].Player, new DamageRequest(DamageType.MonsterAttack, attack.damage, attack.shieldDamage, true, null));
                    if (result.HpDamage > 0) counts[i]++;
                }
                hits++; next += interval;
            }
            yield return null;
        }
        File.WriteAllText(Folder + "GuardRole.txt", $"Same six incoming {source.displayName} normal attacks, every {interval:F3}s. Production guard/recovery and starting relics; no attack/DPS comparison.\nWarrior HP hits={counts[0]}, HP lost={roles[0].Player.MaxHp - roles[0].Player.CurrentHp:F2}\nRogue HP hits={counts[1]}, HP lost={roles[1].Player.MaxHp - roles[1].Player.CurrentHp:F2}\n");
        File.AppendAllText(Folder + "GuardRole.txt", counts[0] < counts[1] ? "PASS Warrior absorbs more identical pressure before HP loss.\n" : "REVIEW no measurable Warrior defense advantage in this pressure sample.\n");
    }
}
#endif
