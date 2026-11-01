using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using DungeonSlash;

var root=Path.GetFullPath(args.Length>0?args[0]:Path.Combine(AppContext.BaseDirectory,"../../../../../../../"));
var output=Path.Combine(root,"Docs/Production/WorldRenewal-2026-11-01");Directory.CreateDirectory(output);
var generation=new DungeonGenerationSettings();var exploration=new ExplorationSettings();var fatigue=new FatigueSettings();generation.exploration=exploration;
Load(generation,"Assets/Data/Settings/Data_DungeonGenerationSettings_미궁_생성_규칙.asset");
Load(exploration,"Assets/Data/Presentation/Data_Exploration_탐험.asset");
Load(fatigue,"Assets/Data/Settings/Data_FatigueSettings_피로도_규칙.asset");
var checks=0;var failures=new List<string>();
void Check(bool valid,string label){checks++;if(!valid)failures.Add(label);}
var csv=new StringBuilder("act,seed,rooms,annex_rooms,direct_boss_steps,coverage_steps,key_return_steps,cost_per_step,old100_survives_without_camp,old100_survives_with_one_camp,capacity,final_without_camp,final_with_one_camp,first_camp_step,route\n");
var report=new StringBuilder("# 실제 지도 기반 피로도 예산\n\n");
report.AppendLine("현재 생산용 생성기·실제 설정(YAML)을 그대로 참조하는 제한된 .NET 감사다. UnityEngine의 데이터 속성·수학·곡선은 최소 스텁이며, Unity 런타임/입력/곡선 보간 품질 검증은 별도다. 전투/DPS 검사는 실행하지 않았다.\n");
report.AppendLine("지도마다 시작→모든 방→보스의 합법적인 유한 경로를 만든다. 보스 최단 경로를 마지막 축으로 남기고, 나머지는 실제 연결을 왕복한다. 루프의 지름길은 최적화하지 않는 보수적 탐험 경로다. 숨은 입구 발견 클릭은 이동이 아니며 이동비용이 없다. 잠긴 가지에서 열쇠를 아직 못 찾았으면 기본 지도를 탐색한 다음 실제 입구까지 되돌아오는 거리를 포함한다. 방의 내용을 알고 최단 이동하는 가정이나 무한 왕복을 버틸 가정은 하지 않는다.\n");
report.AppendLine("`최대 피로도 = 10 × ceil(max(100, (전체 경로 이동 수 + 1) × 실제 이동 비용) / 10)`으로 정한다. 0이 사망이므로 마지막 방 도착 후 실제 한 걸음 비용을 남긴다. 캠프나 물약을 필수로 쓰지 않아도 정해진 한 차례 전체 탐색은 이동 때문에 막히지 않는 기준이다. 전투 성능 페널티는 기존 0–100 곡선에 잔량 비율을 대입한다. 캠프는 최대치, 짧은 휴식은 최대치의 기존60%, 회복 포션은 기존 실제30(비용2일 때15보)이다. 실제 캠프 한 번을 쓰는 경우도 함께 계산해 증가량을 숨기지 않았다. 반복 짧은 휴식/물약을 생존 계산에 더하지 않았다.\n");
report.AppendLine("|막|표본|전체 방|탐색 이동|기존100 이동사망 (무야영/1야영)|최대치|1야영 후 최종 잔량|\n|---|---:|---:|---:|---:|---:|---:|");
for(var act=1;act<=3;act++)
{
    var rows=new List<(int rooms,int steps,int cap,float campFinal,bool old,bool oldCamp)>();
    for(var sample=0;sample<24;sample++)
    {
        var seed=unchecked(185917*sample+63071*act+7117);var state=new DungeonGenerator(generation).Generate(seed,act);var graph=state.Graph;
        var budget=FatigueFloorBudget.For(graph,fatigue.costPerStep);var route=budget.CoverageRoute;
        Check(route[0]==graph.StartRoom.RoomId,$"{act}/{seed} begins at start");
        Check(route.Distinct().Count()==graph.Rooms.Count,$"{act}/{seed} covers every actual room");
        Check(graph.ById(route[^1]).Type==RoomEncounterType.Boss&&!route.Take(route.Count-1).Any(i=>graph.ById(i).Type==RoomEncounterType.Boss),$"{act}/{seed} boss last");
        Check(budget.Capacity>=100&&budget.Capacity%10==0,$"{act}/{seed} min100 ceiling10");
        var keys=new HashSet<int>(graph.StartRoom.KeyTokens);var oldValue=100f;var oldCampValue=100f;var value=(float)budget.Capacity;var campValue=value;var oldAlive=true;var oldCampAlive=true;var firstCamp=-1;
        for(var n=1;n<route.Count;n++)
        {
            var from=graph.ById(route[n-1]);var room=graph.ById(route[n]);
            Check(from.Connections.Contains(room.RoomId),$"{act}/{seed} step{n} is connected");
            var passage=graph.Passage(from,room);Check(passage is not {Locked:true}||keys.Contains(passage.KeyToken),$"{act}/{seed} step{n} key acquired before locked gate");keys.UnionWith(room.KeyTokens);
            oldValue-=fatigue.costPerStep;oldCampValue-=fatigue.costPerStep;value-=fatigue.costPerStep;campValue-=fatigue.costPerStep;oldAlive&=oldValue>0;oldCampAlive&=oldCampValue>0;
            if(room.Type==RoomEncounterType.Camp&&firstCamp<0){firstCamp=n;if(oldCampAlive)oldCampValue=100;campValue=budget.Capacity;}
        }
        var runtime=new FatigueRuntime();runtime.BeginFloor(fatigue,graph,act);Check(runtime.Capacity==budget.Capacity&&runtime.Value==budget.Capacity,"runtime starts at derived cap");
        for(var n=0;n<budget.CoverageSteps;n++)runtime.Step();Check(runtime.Value>0&&!runtime.IsDead&&runtime.Value==value,"complete walk survives without camp");
        runtime.Camp();Check(runtime.Value==budget.Capacity&&runtime.CampUsed&&!runtime.Camp(),"camp restores derived cap once");
        while(runtime.Percent>40)runtime.Step();Check(runtime.ShortRest()&&Math.Abs(runtime.Percent-fatigue.shortRestCeiling)<.001f,"short rest preserves authored percentage");
        runtime.Recover(100000);Check(runtime.Value==runtime.Capacity,"recovery cannot exceed current capacity");
        runtime.Step();Check(runtime.Value==runtime.Capacity-fatigue.costPerStep&&runtime.GraceRemaining==0,"every step uses actual cost with no grace");
        rows.Add((graph.Rooms.Count,budget.CoverageSteps,budget.Capacity,campValue,oldAlive,oldCampAlive));
        csv.AppendLine(FormattableString.Invariant($"{act},{seed},{graph.Rooms.Count},{graph.Rooms.Count(r=>r.IsOptionalAnnex)},{budget.DirectBossSteps},{budget.CoverageSteps},{budget.KeyReturnSteps},{fatigue.costPerStep},{oldAlive},{oldCampAlive},{budget.Capacity},{value},{campValue},{firstCamp},{string.Join('-',route)}"));
    }
    report.AppendLine($"|{act}|{rows.Count}|{rows.Min(r=>r.rooms)}–{rows.Max(r=>r.rooms)}|{rows.Min(r=>r.steps)}–{rows.Max(r=>r.steps)}|{rows.Count(r=>!r.old)}/{rows.Count(r=>!r.oldCamp)}|{rows.Min(r=>r.cap)}–{rows.Max(r=>r.cap)}|{rows.Min(r=>r.campFinal):0}–{rows.Max(r=>r.campFinal):0}|");
}
// Shared rule must react to authored cost as well as graph size, and keep old null/debug behavior.
var fixture=new DungeonGraph();var previous=fixture.AddRoom(new DungeonPosition(0,0));for(var n=1;n<35;n++){var r=fixture.AddRoom(new DungeonPosition(n,0));fixture.Connect(previous,r);previous=r;}previous.Type=RoomEncounterType.Boss;
Check(FatigueFloorBudget.For(fixture,4).Capacity==140,"cost4 and35-room direct path derives140");Check(FatigueFloorBudget.For(null,2).Capacity==100,"null fixture preserves100");
var final=$"{checks} checks, {failures.Count} failures. 72 natural seeded maps. ";report.AppendLine("\n"+final);foreach(var fail in failures)report.AppendLine("FAIL "+fail);
File.WriteAllText(Path.Combine(output,"FatigueBudget.md"),report.ToString());File.WriteAllText(Path.Combine(output,"FatigueBudget.csv"),csv.ToString());Console.WriteLine(report.ToString());
if(failures.Count>0)Environment.ExitCode=1;
void Load(object target,string path)
{
    var text=File.ReadAllText(Path.Combine(root,path));foreach(var field in target.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance))
    {
        var match=Regex.Match(text,@"^  "+Regex.Escape(field.Name)+@": ([^\r\n]+)",RegexOptions.Multiline);if(!match.Success)continue;
        if(field.FieldType==typeof(int))field.SetValue(target,int.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture));
        if(field.FieldType==typeof(float))field.SetValue(target,float.Parse(match.Groups[1].Value,CultureInfo.InvariantCulture));
    }
}
