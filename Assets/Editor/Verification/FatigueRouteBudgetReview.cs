using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

internal static class FatigueRouteBudgetReview
{
    internal static void Run()
    {
        var settings=AssetDatabase.FindAssets("t:DungeonGenerationSettings").Select(g=>AssetDatabase.LoadAssetAtPath<DungeonGenerationSettings>(AssetDatabase.GUIDToAssetPath(g))).First();
        var fatigue=AssetDatabase.FindAssets("t:FatigueSettings").Select(g=>AssetDatabase.LoadAssetAtPath<FatigueSettings>(AssetDatabase.GUIDToAssetPath(g))).First();
        var csv=new StringBuilder("act,seed,route,steps,first_camp_step,final_fatigue,speed,damage,survives\n");
        var report=new StringBuilder("# 피로도 경로 예산 검증\n\n실제 생성 설정으로 막별 32개 고정 시드(0–31)를 생성했다. 전투 DPS나 체력 소모를 가정하지 않고 이동 자원만 검증한다. ‘보상 경유’는 모든 상자·정예·야영지, ‘서비스 경유’는 여기에 보상·상점·여신·샘을 더한 최단 이동이다. 보스 방에 들어가면 종료하며 목표 수집 전에는 보스 방을 통과하지 않는다. 첫 야영지 도착 즉시 한 번 100으로 회복한다고 가정한다. 짧은 휴식·피로 회복 물약·지도 미지로 인한 길 찾기 비용은 제외했다. 따라서 전체 플레이 밸런스 확정 수치가 아니다.\n\n");
        for(var act=1;act<=3;act++)
        {
            var summaries=new Dictionary<string,List<(int steps,float value,bool alive)>>();var unavailable=new Dictionary<string,int>();
            foreach(var mode in new[]{"boss","rewards","services"}){summaries[mode]=new();unavailable[mode]=0;}
            for(var seed=0;seed<32;seed++)
            {
                var graph=new DungeonGenerator(settings).Generate(seed,act).Graph;
                foreach(var mode in summaries.Keys.ToArray())
                {
                    var goals=graph.Rooms.Where(r=>mode!="boss"&&(r.Type is RoomEncounterType.Chest or RoomEncounterType.Elite or RoomEncounterType.Camp
                        ||mode=="services"&&r.Type is RoomEncounterType.Shop or RoomEncounterType.Reward or RoomEncounterType.Goddess or RoomEncounterType.Fountain)).ToArray();
                    var route=Route(graph,goals);
                    if(route==null){unavailable[mode]++;csv.AppendLine($"{act},{seed},{mode},unreachable,,,,,");continue;}
                    var value=100f;var firstCamp=-1;var alive=true;
                    for(var n=1;n<route.Count;n++)
                    {
                        value=Mathf.Max(0,value-fatigue.costPerStep);alive&=value>0;
                        if(alive&&route[n].Type==RoomEncounterType.Camp&&firstCamp<0){firstCamp=n;value=100;}
                    }
                    summaries[mode].Add((route.Count-1,value,alive));
                    csv.AppendLine(FormattableString.Invariant($"{act},{seed},{mode},{route.Count-1},{firstCamp},{value:F0},{fatigue.attackSpeed.Evaluate(value):F4},{fatigue.finalDamage.Evaluate(value):F4},{alive}"));
                }
            }
            report.AppendLine($"## {act}막\n\n|경로|가능 시드|이동 수 최소–최대|야영 가정 후 도착 피로도|공속 페널티 도착|피해 페널티 도착|이동 사망|목표 전 보스가 길을 막음|\n|---|---|---|---|---|---|---|---|");
            foreach(var pair in summaries)
            {
                var rows=pair.Value;var label=pair.Key=="boss"?"보스 직행":pair.Key=="rewards"?"보상 경유":"서비스 경유";
                report.AppendLine(rows.Count==0?$"|{label}|0|||||||":$"|{label}|{rows.Count}|{rows.Min(r=>r.steps)}–{rows.Max(r=>r.steps)}|{rows.Min(r=>r.value):F0}–{rows.Max(r=>r.value):F0}|{rows.Count(r=>r.value<70)}|{rows.Count(r=>r.value<45)}|{rows.Count(r=>!r.alive)}|{unavailable[pair.Key]}|");
            }
            report.AppendLine();
        }
        File.WriteAllText(CombatContentRevisionTools.Folder+"FatigueRoutes.csv",csv.ToString());File.WriteAllText(CombatContentRevisionTools.Folder+"FatigueRoutes.md",report.ToString());
    }
    static List<DungeonRoom> Route(DungeonGraph graph,DungeonRoom[] goals)
    {
        var count=graph.Rooms.Count;var bits=goals.Select((room,n)=>(room.RoomId,bit:1<<n)).ToDictionary(v=>v.RoomId,v=>v.bit);var complete=(1<<goals.Length)-1;
        var boss=graph.Rooms.Single(r=>r.Type==RoomEncounterType.Boss);var queue=new Queue<int>();var previous=new Dictionary<int,int>();
        var start=graph.StartRoom.RoomId;queue.Enqueue(start);previous[start]=-1;
        while(queue.Count>0)
        {
            var key=queue.Dequeue();var room=graph.ById(key%count);var mask=key/count;
            if(room==boss&&mask==complete)
            {var route=new List<DungeonRoom>();for(var cursor=key;cursor>=0;cursor=previous[cursor])route.Add(graph.ById(cursor%count));route.Reverse();return route;}
            foreach(var next in graph.Neighbors(room))
            {
                var nextMask=mask|(bits.TryGetValue(next.RoomId,out var bit)?bit:0);if(next==boss&&nextMask!=complete)continue;
                var nextKey=next.RoomId+nextMask*count;if(previous.ContainsKey(nextKey))continue;
                previous[nextKey]=key;queue.Enqueue(nextKey);
            }
        }
        return null;
    }
}
