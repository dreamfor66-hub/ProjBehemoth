#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.UI;

// Only the changed act-one formations with saved, actual reward builds. Existing Editor only.
public sealed class ActOneGrowthLiveProbe : MonoBehaviour
{
    const string Folder="Docs/Production/WarriorActOne-2026-10-28/";
    CombatController combat;CombatSceneView scene;RunController run;ArcadeHudController hud;RunExpansionView expansion;
    bool pressureOnly;
    string Live=>Folder+(pressureOnly?"MistakeLive.txt":"BalanceLive.txt");
    string Telemetry=>Folder+(pressureOnly?"MistakeTelemetry.tsv":"BalanceTelemetry.tsv");
    string Trace=>Folder+(pressureOnly?"MistakeTrace.tsv":"PressureTrace.tsv");
    PlayerCombatData previous;bool background;float timeScale;int review;
    static T Ref<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(o);
    void Awake(){previous=RunSession.SelectedCharacter;background=Application.runInBackground;timeScale=Time.timeScale;Application.runInBackground=true;}
    void OnDestroy(){RunSession.SelectedCharacter=previous;Application.runInBackground=background;Time.timeScale=timeScale;}
    public void Run()=>StartCoroutine(Matrix());
    public void RunPressure(){pressureOnly=true;StartCoroutine(Matrix());}
    IEnumerator Matrix()
    {
        Time.timeScale=2; // Automated production-timer audit; elapsed telemetry is game seconds.
        Directory.CreateDirectory(Folder+"Screenshots");
        File.WriteAllText(Live,"Actual act-one reward builds. 2x simulation, telemetry in game seconds. Tight pilot honors attack cooldown, manual-duration charge/casting, timed guard; no normalized damage/HP injection. Human feel is separate.\n");
        File.WriteAllText(Telemetry,"build\tencounter\tlevel\tfatigue\tseconds\thpLost\tminGuard\tbreaks\tnormalTelegraphs\tcasts\tresult\n");
        File.WriteAllText(Trace,"build\tencounter\tseconds\tguardBefore\tguardAfter\thp\tshieldState\tenemyState\tremainingNormalHits\n");
        yield return null;yield return null;
        combat=FindFirstObjectByType<CombatController>();scene=FindFirstObjectByType<CombatSceneView>();run=FindFirstObjectByType<RunController>();
        hud=FindFirstObjectByType<ArcadeHudController>();expansion=FindFirstObjectByType<RunExpansionView>();
        foreach(var flow in FindObjectsByType<GameFlowController>(FindObjectsSortMode.None)){flow.StopAllCoroutines();flow.enabled=false;}
        typeof(CombatController).GetField("Completed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(combat,null);
        FindFirstObjectByType<CombatInputController>().enabled=false;
        FindFirstObjectByType<DungeonController>().HideNavigation();
        RunSession.SelectedCharacter=combat.Content.characters.First(c=>c.character==CharacterKind.Warrior);
        CheckRewardDefinitions();
        var budget=UnityEditor.AssetDatabase.LoadAssetAtPath<ActGrowthBudget>("Assets/Data/Balance/Act1GrowthBudget.asset");
        var stage=budget.stage;
        if(pressureOnly)File.AppendAllText(Live,"NEGATIVE CONTROL: guard held continuously, weak points ignored. Same actual rewards and production cooldowns.\n");
        var settings=UnityEditor.AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>("Assets/Data/Settings/DungeonGenerationSettings.asset");
        var graph=new DungeonGenerator(settings).Generate(281028,1).Graph;
        var builds=pressureOnly?new[]{budget.referenceBuilds.First(b=>b.checkpoint==GrowthCheckpoint.Opening),budget.referenceBuilds.First(b=>b.checkpoint==GrowthCheckpoint.Middle),budget.referenceBuilds.First(b=>b.checkpoint==GrowthCheckpoint.PreBoss&&b.scenario==GrowthScenario.Required)}:budget.referenceBuilds.ToArray();
        foreach(var build in builds)
        {
            var pool=build.checkpoint==GrowthCheckpoint.Opening?stage.earlyCombats:build.checkpoint==GrowthCheckpoint.Middle?stage.hardCombats.Concat(stage.elites):stage.bosses;
            foreach(var encounter in pool.Distinct())
            {
                combat.ResetEncounter();hud.StopAllCoroutines();run.BeginRun();expansion.HideModal();hud.Close();combat.InputPaused=false;
                build.Apply(run.State);
                run.State.Fatigue.BeginFloor(combat.Content.fatigue,graph,1);
                while(run.State.Fatigue.Value>build.fatigue)run.State.Fatigue.Step();
                var levelPerks=build.perks.Where(p=>!p.fromEvent).Sum(p=>p.stacks);
                if(levelPerks>run.State.Level-1)throw new InvalidOperationException("Unearned level-up perks: "+build.label);
                File.AppendAllText(Live,$"BUILD {build.label}: level={run.State.Level}, normal={run.State.Player.NormalAttackDamage:F2}, charge={run.State.Player.ChargeAttackDamage:F2}, HP={run.State.Player.MaxHp:F1}, guard={run.State.Player.ShieldMax:F1}, fatigue={run.State.Fatigue.Value}, steps={run.State.Fatigue.Steps}; {build.acquisitionBasis}\n");
                combat.PrepareFixedEncounter(run.State,encounter,1);combat.BeginPreparedCombat();
                if(!Mathf.Approximately(run.State.Rule(AbilityRule.GuardRecoveryPerSecond),2))throw new InvalidOperationException("Warrior passive guard recovery must survive combat start");
                float elapsed=0,preparation=0,casting=0,minGuard=combat.Player.ShieldCurrent,initialHp=combat.Player.CurrentHp,hpLoss=0;
                var oldHp=initialHp;var oldShield=combat.Player.ShieldCurrent;int breaks=0,telegraphs=0,slashes=0;
                var states=new Dictionary<MonsterRuntime,MonsterState>();var captured=false;
                while(combat.IsActive&&combat.Player.IsAlive&&elapsed<130)
                {
                    if(combat.InputPaused)throw new InvalidOperationException("Unexpected modal pause in scoped pilot");
                    var enemies=combat.Enemies.Where(e=>e.IsAlive).ToArray();if(enemies.Length==0)break;
                    elapsed+=Time.deltaTime;minGuard=Mathf.Min(minGuard,combat.Player.ShieldCurrent);
                    if(oldShield-combat.Player.ShieldCurrent>.1f||combat.Player.CurrentHp<oldHp)
                        File.AppendAllText(Trace,$"{build.label}\t{encounter.displayName}\t{elapsed:F2}\t{oldShield:F2}\t{combat.Player.ShieldCurrent:F2}\t{combat.Player.CurrentHp:F2}\t{combat.Player.ShieldState}\t{string.Join("/",enemies.Select(e=>e.State))}\t{string.Join("/",enemies.Select(e=>e.RemainingNormalAttackHits))}\n");
                    oldShield=combat.Player.ShieldCurrent;
                    if(combat.Player.CurrentHp<oldHp)hpLoss+=oldHp-combat.Player.CurrentHp;oldHp=combat.Player.CurrentHp;
                    combat.Player.TrySetGuard(pressureOnly||enemies.Any(e=>(e.State is MonsterState.Attacking or MonsterState.Charging)&&e.StateTimer<=.20f));
                    foreach(var e in enemies)
                    {
                        var before=states.TryGetValue(e,out var s)?s:MonsterState.Idle;
                        if(e.State==MonsterState.Stunned&&before!=e.State)breaks++;
                        if(e.State==MonsterState.Attacking&&before!=e.State)telegraphs++;
                        states[e]=e.State;
                    }
                    if(combat.Player.CanNormalAttack())
                    {
                        var charging=pressureOnly?null:enemies.Where(e=>e.State==MonsterState.Charging&&e.WeakPoints.Any(p=>!p.IsDestroyed&&!p.IsOptional)).OrderBy(e=>e.StateTimer).FirstOrDefault();
                        if(charging!=null)
                        {
                            casting=0;preparation+=Time.deltaTime;
                            if(preparation>=combat.Player.CurrentChargeDuration){combat.ExecuteAttack(WeakSegment(charging),true);preparation=0;}
                        }
                        else
                        {
                            preparation=0;
                            var spell=run.State.Spellbook.FirstOrDefault(s=>s.Cooldown<=0&&s.Data.mpCost<=combat.Player.CurrentMp);
                            if(spell!=null)
                            {
                                casting+=Time.deltaTime;
                                if(casting>=spell.Data.castingSeconds*combat.Player.SpellCastingFor(spell.Data))
                                {combat.TryCastSpell(spell.Data.defaultSlot,casting,NormalSegment(enemies,slashes++));casting=0;}
                            }
                            else {casting=0;combat.ExecuteAttack(NormalSegment(enemies,slashes++),false);}
                        }
                    }
                    // One gameplay capture per reference encounter; no pose/input regression suite.
                    if(!pressureOnly&&!captured&&build.scenario==GrowthScenario.Required&&elapsed>2)
                    {captured=true;yield return Capture(build.checkpoint+"-"+encounter.name+"-"+budget.referenceBuilds.IndexOf(build));}
                    yield return null;
                }
                if(combat.Player.CurrentHp<oldHp)hpLoss+=oldHp-combat.Player.CurrentHp;
                var won=combat.Enemies.All(e=>!e.IsAlive);var result=won?"won":combat.Player.IsAlive?"timeout":"lost";
                var needsReview=!pressureOnly&&build.scenario==GrowthScenario.Required&&(!won||hpLoss>.01f||telegraphs==0);
                if(needsReview)review++;
                File.AppendAllText(Telemetry,$"{build.label}\t{encounter.displayName}\t{run.State.Level}\t{run.State.Fatigue.Value:F0}\t{elapsed:F2}\t{hpLoss:F2}\t{minGuard:F2}\t{breaks}\t{telegraphs}\t{run.State.SpellCasts}\t{result}\n");
                File.AppendAllText(Live,(needsReview?"REVIEW ":"RESULT ")+$"{build.label}/{encounter.displayName}: {result}, HP damage={hpLoss:F2}, guard minimum={minGuard:F2}, seconds={elapsed:F2}, breaks={breaks}, telegraphs={telegraphs}\n");
                yield return new WaitForSecondsRealtime(.65f);
            }
        }
        File.AppendAllText(Live,$"COMPLETE review={review}. Human/mobile feel pending; this is a reproducible mechanical ceiling with actual production rewards, not ten random-run averages.\n");
        UnityEditor.EditorApplication.isPlaying=false;
    }
    void CheckRewardDefinitions()
    {
        var balance=UnityEditor.AssetDatabase.LoadAssetAtPath<RunBalanceSettings>("Assets/Data/Settings/RunBalanceSettings.asset");
        var sample=new RunState(RunSession.SelectedCharacter,balance);
        sample.TryEquip(RunSession.SelectedCharacter.startingRelic);sample.BeginCombat();sample.BeginCombat();
        if(!Mathf.Approximately(sample.Rule(AbilityRule.GuardRecoveryPerSecond),2))throw new InvalidOperationException("Starting guard recovery lost at BeginCombat");
        var wire=UnityEditor.AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Items/Relics/ThunderWire.asset");sample.TryEquip(wire);
        if(Mathf.Abs(sample.Player.NormalAttackDamage-16.8f)>.01f||Mathf.Abs(sample.Player.CurrentCooldown-.748f)>.01f)throw new InvalidOperationException("ThunderWire real acquisition stats mismatch");
        var hunter=UnityEditor.AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Items/Relics/HunterLantern.asset");sample.TryEquip(hunter);
        sample.Player.TakeEnvironmentalDamage(30);var before=sample.Player.CurrentHp;sample.EmitEvent(TriggerType.OnKill,default);
        if(Mathf.Abs(sample.Player.CurrentHp-before-7)>.01f)throw new InvalidOperationException("HunterLantern real kill healing mismatch");
        File.AppendAllText(Live,"PASS starting guard recovery survives repeated BeginCombat; actual ThunderWire damage/cooldown and HunterLantern kill-heal match data.\n");
    }
    Vector2 Position(MonsterRuntime enemy)
    {
        foreach(var entry in Ref<System.Collections.IEnumerable>(combat,"activeMonsters"))
            if(ReferenceEquals(entry.GetType().GetProperty("Runtime").GetValue(entry),enemy))return(Vector2)entry.GetType().GetProperty("Position").GetValue(entry);
        throw new InvalidOperationException("Missing enemy position");
    }
    AttackSegment WeakSegment(MonsterRuntime enemy)
    {
        var points=enemy.WeakPoints.Where(p=>!p.IsDestroyed&&!p.IsOptional).ToArray();
        var candidates=new List<AttackSegment>();
        foreach(var a in points)foreach(var b in points)
        {
            var d=a==b?Vector2.right:(b.HitShape.Center-a.HitShape.Center).normalized;
            var start=a.HitShape.Center-d*50;var end=b.HitShape.Center+d*50;
            var rect=scene.LogicalArea.rect;start.x=Mathf.Clamp(start.x,rect.xMin+20,rect.xMax-20);end.x=Mathf.Clamp(end.x,rect.xMin+20,rect.xMax-20);
            start.y=Mathf.Clamp(start.y,rect.yMin+20,rect.yMax-20);end.y=Mathf.Clamp(end.y,rect.yMin+20,rect.yMax-20);
            candidates.Add(new AttackSegment(start,end,combat.Player.SegmentWidth));
        }
        return candidates.OrderByDescending(s=>WeakPointHitResolver.Resolve(s,points).Count()).First();
    }
    AttackSegment NormalSegment(MonsterRuntime[] enemies,int sequence)
    {
        var target=enemies.OrderBy(e=>e.CurrentHp).First();var center=Position(target);
        var guard=target.Data.GetAttack(MonsterAttackType.Normal)?.combatMechanics?.directionalGuard;
        if(target.IsDirectionalGuardActive&&guard!=null&&(guard.breakByAttackWay&TriggerAttackWayFilter.Horizontal)==0)
            return new AttackSegment(center-Vector2.up*180,center+Vector2.up*180,combat.Player.SegmentWidth);
        var ys=enemies.Select(e=>Position(e).y+e.Data.BodySize.y*.3f).ToArray();
        var candidates=ys.Concat(ys.SelectMany(a=>ys.Select(b=>(a+b)/2)));
        var rect=scene.LogicalArea.rect;
        var best=candidates.Select(y=>new AttackSegment(new Vector2(rect.xMin+35,y),new Vector2(rect.xMax-35,y),combat.Player.SegmentWidth))
            .OrderByDescending(s=>enemies.Count(e=>e.Data.IntersectsBody(s,Position(e),e.VisualScale))).First();
        return sequence%2==0?best:new AttackSegment(best.End,best.Start,best.Width);
    }
    IEnumerator Capture(string name)
    {
        expansion.HideModal();hud.Close();yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"Screenshots/"+name+".png",image.EncodeToPNG());Destroy(image);
    }
}
#endif
