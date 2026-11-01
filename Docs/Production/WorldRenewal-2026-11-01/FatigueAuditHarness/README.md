생산 C# 소스와 실제 설정 YAML을 참조하는 한정된 지도/피로도 감사. Unity를 실행하지 않는다.

프로젝트 루트에서 `dotnet run --project Docs/Production/WorldRenewal-2026-11-01/FatigueAuditHarness/FatigueMapAudit.csproj -- .`

UnityEngine 스텁은 데이터 속성·수학·선형 곡선 평가만 제공한다. 실제 Unity 곡선/입력/UI 확인은 `MapFatigueLiveChecks.CheckCurrentFloor(run.State,dungeon.State.Graph,Check)`를 기존 단일 라이브 검수에서 호출한다.
