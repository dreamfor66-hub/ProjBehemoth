using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class ExplorationDepthVerification
{
    const string Folder=ExplorationDepthPreparation.Folder;
    static bool ready;
    static ExplorationDepthVerification()
    {EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=s=>ready=s==PlayModeStateChange.EnteredPlayMode;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        const string request="Temp/ExplorationDepth.request";
        if(File.Exists(request))
        {
            string command;try{command=File.ReadAllText(request).Trim();File.Delete(request);}catch(IOException){return;}
            if(command=="status")File.WriteAllText(Folder+"Status.txt","playing="+EditorApplication.isPlaying+";scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            if(command=="audit"&&!EditorApplication.isPlaying)Audit();
            if((command=="run"||command.StartsWith("final"))&&!EditorApplication.isPlaying&&File.Exists(Folder+"Installed.txt"))
            {
                if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
                {File.WriteAllText(Folder+"RuntimePending.txt","Unsaved scene edits; runtime not started.");return;}
                EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");SessionState.SetString("ExplorationDepth.Mode",command);SessionState.SetBool("ExplorationDepth.Run",true);EditorApplication.isPlaying=true;
            }
        }
        if(ready&&EditorApplication.isPlaying&&SessionState.GetBool("ExplorationDepth.Run",false)&&UnityEngine.Object.FindFirstObjectByType<GameFlowController>()!=null)
        {SessionState.SetBool("ExplorationDepth.Run",false);var probe=new GameObject("Exploration depth scoped live review").AddComponent<ExplorationDepthLiveProbe>();var mode=SessionState.GetString("ExplorationDepth.Mode","run");if(mode.StartsWith("final"))probe.RunFinal(mode);else probe.Run();}
    }
    static void Audit()
    {
        var log=new StringBuilder();int checks=0,failures=0;
        void Check(bool value,string message){checks++;if(!value){failures++;log.AppendLine("FAIL "+message);}}
        var watch=System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var settings=AssetDatabase.FindAssets("t:DungeonGenerationSettings").Select(g=>AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(g))).First(g=>g.exploration!=null);
            var presence=new int[4];var lengths=new int[3];
            for(var act=1;act<=3;act++)
            {
                float combats=0,extra=0;
                for(var sample=0;sample<24;sample++)
                {
                    var seed=unchecked(185917*sample+63071*act+7117);var state=new DungeonGenerator(settings).Generate(seed,act);var graph=state.Graph;
                    Check(DungeonGraphValidator.HasExpectedStructure(state,settings),$"B{act}/{seed} budgets and connectivity");
                    Check(DungeonRoutePacing.HardViolations(graph)==0,$"B{act}/{seed} no five-threat or four-opportunity path");
                    Check(graph.Rooms.Count(r=>!r.IsOptionalAnnex&&r.Type==RoomEncounterType.Event)==2,"two base events");
                    Check(graph.Rooms.Count(r=>r.Type==RoomEncounterType.Elite)==act,"elite growth count");
                    Check(graph.Rooms.Single(r=>r.Type==RoomEncounterType.Camp).Connections.Count==1,"camp is a dead end");
                    var hidden=graph.Passages.Any(p=>p.Concealed);var locked=graph.Passages.Any(p=>p.Locked);presence[(hidden?1:0)+(locked?2:0)]++;
                    combats+=graph.Rooms.Count(r=>!r.IsOptionalAnnex&&r.Type==RoomEncounterType.Combat);extra+=graph.Rooms.Count(r=>r.IsExplorationAlcove);
                    foreach(var p in graph.Passages)
                    {
                        Check(!state.CanTravel(p.Origin,p.Annex),"initial gate inaccessible");
                        Check(p.Concealed!=p.Locked,"hidden versus locked identity");
                        if(p.Concealed)Check(p.Origin.Type==RoomEncounterType.Empty&&p.Origin.Connections.Count==2&&p.Origin.KeyTokens.Count==0,"secret begins in object-free empty base dead end");
                        else Check(graph.Rooms.Any(r=>!r.IsOptionalAnnex&&r.KeyTokens.Contains(p.KeyToken)),"key exists on base map");
                        var chain=graph.DistancesFrom(p.Annex).Where(k=>graph.ById(k.Key).IsOptionalAnnex).Select(k=>graph.ById(k.Key)).ToArray();
                        // Branches never touch one another; follow only this connected annex component.
                        var found=new System.Collections.Generic.HashSet<int>{p.Annex.RoomId};var queue=new System.Collections.Generic.Queue<DungeonRoom>();queue.Enqueue(p.Annex);
                        while(queue.Count>0)foreach(var r in graph.Neighbors(queue.Dequeue()).Where(r=>r.IsOptionalAnnex))if(found.Add(r.RoomId))queue.Enqueue(r);
                        Check(found.Count>=1&&found.Count<=3,"zero to two ordinary rooms before terminal");lengths[found.Count-1]++;
                        var terminal=found.Select(graph.ById).Single(r=>r.Connections.Count==1);
                        Check(terminal.Type is RoomEncounterType.Chest or RoomEncounterType.Event,"reward/event terminal");
                        Check(found.Where(id=>id!=terminal.RoomId).All(id=>graph.ById(id).Type is RoomEncounterType.Empty or RoomEncounterType.Combat),"ordinary intermediates");
                        if(p.Concealed){Check(!p.Search(3)&&!p.Search(3)&&p.Search(3),"exactly three searches");}
                        else{Check(!p.Unlock(new System.Collections.Generic.HashSet<int>()),"no unlock without key");Check(p.Unlock(new System.Collections.Generic.HashSet<int>{p.KeyToken}),"key opens visible lock");}
                    }
                }
                log.AppendLine($"B{act}: 24 seeded natural maps; mean base normal combats {combats/24:F2}; added empty pockets {extra/24:F2}.");
            }
            Check(presence.All(x=>x>0),"neither/secret/locked/both all occur");Check(lengths.All(x=>x>0),"all branch lengths occur");
            log.AppendLine("Presence neither,secret,locked,both: "+string.Join(",",presence));log.AppendLine("Branch lengths ordinary 0,1,2: "+string.Join(",",lengths));
        }
        catch(Exception e){Check(false,e.ToString());}
        log.AppendLine($"{checks} checks; {failures} failures; {watch.Elapsed.TotalSeconds:F2}s. Changed generation only; no DPS/balance suite. Player-run feel remains for user feedback.");
        File.WriteAllText(Folder+"GenerationAudit.txt",log.ToString());
    }
}
