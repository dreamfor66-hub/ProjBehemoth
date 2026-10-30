# L→R Ready 명도와 Guard 장검

사용자 요청: L→R Ready가 아직 밝으므로 원본과 다시 비교하여 수정하고, Guard의 검 길이도 늘린다.

- Ready는 내장 image_gen으로 기존 천·가죽·철색 면의 명도를 낮췄다. 선택된 긴 칼날과 스카프는 원본 픽셀을 유지했다. 재작화로 자세가 바뀌지 않도록 기존 투명도/실루엣과 검정 먹선 픽셀을 보존하고 원본 내부에 생성된 색을 적용했다. alpha/ink/blade/scarf 일치 검사가 모두 true다. 망토나 천 끝을 다시 늘리지 않았다.
- 기존 Idle·Guard 및 사용자 원본03-armored-hood-fighter.jpg / 04-brawler-back.jpg를 실제로 열어 비교했다. 원본6종 배열도 확인했다. Ready는 Guard와 비교하면 큰 차이가 없어 보여도, Idle의 천 면보다 밝았다. 같은 선택 영역에서 Ready 천 명도 중앙값108.18→92.32, Idle 비교 면91.61이다. RGB/명도 수치는 선택된 재료 유사색 영역의 보조 검사이며 완벽한 재료 분할이나 조명 독립 지표가 아니다. ValueComparison.json 참조.
- Guard는 AI가 그린 긴 칼날만 분리해 기존 칼뿌리·각도·기부 폭에 등록했다. 앞서 Idle/raised Ready에 사용한 같은 장검 제작 기준800 원화 픽셀로 맞췄다. 이전 노출 길이는 약312픽셀이었다. 원래 몸·방패·팔·손·손잡이 주변은 그대로이며 변경 칼날 영역 밖 픽셀은 동일하다. Guard-Join-Review.png에서 연결부를 확대 확인했다.
- 두 파일 모두2896×1304, 기존 메타/GUID와 데이터 참조 유지. 씬/프리팹 크기·위치, 시스템, 입력, 밸런스는 변경하지 않았다. After-Hashes.json에서 Ready-LeftToRight와 Guard만 바뀌었음을 확인했다.

Before/는 원본6자세/대상 메타, *-Generated.png는 내장 image_gen 출력, Prompts.json은 정확한 프롬프트, Registration.json은 보존 영역/길이/픽셀 검증 기록이다. Comparison.png와 Comparison-Dark.png는 원본→이전→수정 후를 같은 원화 배율로 표시한다. Ready의 어두운 면 구분과 Guard의 칼 축/투명 여백을 회색·어두운 바탕에서 확인했다. 생성한 그림 전체를 그대로 설치하지 않고 승인된 원본 요소를 보존하는 합성만 적용했다.

실제 적용 파일: Assets/Art/World/Angular/States/Heroes/Warrior-Ready-LeftToRight.png / Warrior-Guard.png. 원화만 수정했으며 Unity Play/시스템 검증은 실행하지 않았다. 최종 색감의 사용자 검토는 별도다.
