using System;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Scoped, opt-in verification against the already open editor. No extra Unity process.
[InitializeOnLoad]
internal static class ExplorationLoreVerification
{
    const string Folder=ExplorationLorePreparation.Folder, Pending="Behemoth.ExplorationLore.Verify";
    static bool ready;
    static ExplorationLoreVerification(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=s=>ready=s==PlayModeStateChange.EnteredPlayMode;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        const string path="Temp/ExplorationLore.request";
        if(File.Exists(path))
        {
            var command=File.ReadAllText(path).Trim();File.Delete(path);
            if(command=="status")File.WriteAllText(Folder+"Status.txt","playing="+EditorApplication.isPlaying+"; scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            if(command=="audit"&&!EditorApplication.isPlaying)Audit();
            if((command=="run"||command=="finish"||command=="art"||command=="input")&&!EditorApplication.isPlaying&&File.Exists(Folder+"Installed.txt")){if(command=="finish")ExplorationLorePreparation.FinishProduction();SessionState.SetString(Pending+".mode",command);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;}
        }
        if(SessionState.GetBool(Pending,false)&&ready&&EditorApplication.isPlaying&&UnityEngine.Object.FindFirstObjectByType<GameFlowController>()!=null)
        {SessionState.SetBool(Pending,false);var probe=new GameObject("Scoped exploration lore review").AddComponent<ExplorationLoreLiveProbe>();var mode=SessionState.GetString(Pending+".mode","");if(mode=="finish")probe.RunFinishing();else if(mode=="art")probe.RunArtSides();else if(mode=="input")probe.RunEntryCombat();else probe.Run();}
    }
    static void Audit()
    {
        var log=new StringBuilder();int checks=0,fail=0;
        void Check(bool value,string message){checks++;if(!value){fail++;log.AppendLine("FAIL "+message);}}
        try
        {
            var generation=AssetDatabase.FindAssets("t:DungeonGenerationSettings").Select(g=>AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(g))).First(s=>s.exploration!=null);
            var content=AssetDatabase.FindAssets("t:GameContent").Select(g=>AssetDatabase.LoadAssetAtPath<GameContent>(AssetDatabase.GUIDToAssetPath(g))).First();
            var events=content.events.Where(e=>e!=null&&!e.excludedFromRandomPool).ToArray();
            Check(events.Length==20,"20 distinct active events");Check(events.Select(e=>e.id).Distinct().Count()==events.Length,"event IDs unique");
            Check(events.All(e=>e.worldObject!=null)&&events.Select(e=>e.worldObject).Distinct().Count()==events.Length,"dedicated subject data");
            Check(events.Select(e=>e.worldObject.front).Distinct().Count()==events.Length,"dedicated event front paintings");
            Check(events.Where(e=>e.encounterKind==EventEncounterKind.Person).All(e=>!string.IsNullOrWhiteSpace(e.authoringLore)),"all human NPCs have authored motive/lore");
            foreach(var story in events)
            {
                var options=story.pages.Count==0?story.options:story.pages.SelectMany(p=>p.options).ToList();
                Check(options.Count>0,"choices exist "+story.id);
                foreach(var option in options)
                {
                    foreach(var effect in option.effects)
                    {Check(effect.kind!=EventEffectKind.Fight||effect.encounter!=null,"fight encounter reference "+story.id);Check(effect.kind!=EventEffectKind.Relic||effect.relic!=null,"relic reference "+story.id);}
                    if(option.nextPage<0)Check(!string.IsNullOrEmpty(option.revisitTextKey),"terminal aftermath localized "+story.id);
                }
                if(story.roomLayout==EventRoomLayout.Alcove)Check(story.roomInteriors.Length==3&&story.roomInteriors.All(s=>s!=null),"all three alcove paintings "+story.id);
                log.AppendLine($"EVENT {story.id}: {story.encounterKind}; weight={story.weight}; acts={story.minAct}-{story.maxAct}; {story.displayName}");
            }
            Check(events.Single(e=>e.id=="MagicTeacherReturn").forbiddenFlag=="RobbedFieldScholar"&&events.Single(e=>e.id=="MagicTeacherAdvanced").forbiddenFlag=="RobbedFieldScholar","robbed scholar blocks future tuition");
            foreach(var act in new[]{1,2,3})
            {
                float combats=0,annex=0,secret=0,locked=0,annexCombat=0,alcoves=0;
                for(var seed=0;seed<128;seed++)
                {
                    var state=new DungeonGenerator(generation).Generate(unchecked(seed*197927+6871),act);var graph=state.Graph;
                    Check(DungeonGraphValidator.HasExpectedStructure(state,generation),$"B{act}/{seed} connected structure");
                    Check(graph.Rooms.Count(r=>r.Type==RoomEncounterType.Shop)==1,$"B{act}/{seed} one supplier");
                    Check(graph.Rooms.Count(r=>!r.IsOptionalAnnex&&r.Type==RoomEncounterType.Event)==2,$"B{act}/{seed} two event rooms");
                    Check(graph.Rooms.Count(r=>r.Type==RoomEncounterType.Elite)==act,$"B{act}/{seed} elite growth preserved");
                    Check(graph.Rooms.Single(r=>r.Type==RoomEncounterType.Boss).Connections.Count==1,$"B{act}/{seed} dedicated boss chamber");
                    Check(graph.Rooms.Count(r=>r.Type==RoomEncounterType.Camp)==1&&graph.Rooms.Single(r=>r.Type==RoomEncounterType.Camp).Connections.Count==1,$"B{act}/{seed} alcove camp");
                    combats+=graph.Rooms.Count(r=>!r.IsOptionalAnnex&&r.Type==RoomEncounterType.Combat);alcoves+=graph.Rooms.Count(r=>r.Type==RoomEncounterType.Event&&r.Connections.Count==1);
                    foreach(var passage in graph.Passages)
                    {
                        annex++;if(passage.Concealed)secret++;else locked++;
                        Check(passage.Concealed!=passage.Locked,$"B{act}/{seed} secret and locked are exclusive");
                        Check(!state.CanTravel(passage.Origin,passage.Annex),$"B{act}/{seed} closed passage inaccessible");
                        if(passage.Locked)Check(graph.Rooms.Any(r=>!r.IsOptionalAnnex&&r.KeyTokens.Contains(passage.KeyToken)),"key on base route");
                        if(passage.Annex.Type==RoomEncounterType.Combat)annexCombat++;
                    }
                    foreach(var a in graph.Rooms.Where(r=>DungeonGenerator.FacilityFamily(r.Type) is RoomEncounterType.Shop or RoomEncounterType.Goddess or RoomEncounterType.Fountain))
                        foreach(var b in graph.Rooms.Where(r=>r.RoomId>a.RoomId&&DungeonGenerator.FacilityFamily(r.Type)==DungeonGenerator.FacilityFamily(a.Type)))
                            Check(graph.DistancesFrom(a)[b.RoomId]>=generation.minimumRepeatedFacilityDistance,"facility minimum distance");
                }
                var candidates=events.Where(e=>act>=e.minAct&&act<=e.maxAct&&string.IsNullOrEmpty(e.requiredFlag)).ToArray();
                log.AppendLine($"B{act}: mean base combats={combats/128:F2}; event alcoves={alcoves}; annexes={annex} (secret={secret},locked={locked},combat={annexCombat}); human weight share without history/required flags={candidates.Where(e=>e.encounterKind==EventEncounterKind.Person).Sum(e=>e.weight)/candidates.Sum(e=>e.weight):P1}");
            }
        }
        catch(Exception e){Check(false,e.ToString());}
        log.AppendLine($"{checks} checks; {fail} failures. 128 seeds per act; only changed generation/content contracts. Candidate weight shares are not player-run experience measurements.");
        File.WriteAllText(Folder+"GenerationAudit.txt",log.ToString());
    }
}
