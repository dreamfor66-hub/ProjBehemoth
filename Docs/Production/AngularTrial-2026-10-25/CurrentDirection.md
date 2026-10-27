# Current character direction — all-six style correction

The latest user clarification is authoritative: reviewing style includes FIXING deviations found. Do not stop at a report or defer already-authorized corrections for another approval.

Current six-character image set: Assets/Art/Trials/Angular01/StyleCorrections03/. Roster.json lists all outputs. The current review board uses these corrected images.

- Warrior: 허름한 올리브 천, 투구, 초반 장비. Applied correction: 갑옷·칼·천의 잔반사면을 줄이고 큰 명암으로 통합.
- Barbarian: 승인된 리자드 머리·상체 디자인, 꼬리·도끼. Applied correction: 근육과 바지의 작은 명암 조각을 정리.
- Monk: 눈가리개, 손동작, 큰 염주. Applied correction: 팔 근육·바지의 면 분할과 허리 겹침을 줄임.
- Samurai: 원래 남자 장발 검사, 강조한 견갑. Applied correction: 사용자 선택 이미지 그대로 채택 · 얼굴·비율·의상 유지.
- Wizard: 온화한 노학자, 책·허리 단검·등 스태프. Applied correction: 반실사 얼굴을 단순한 카툰 얼굴로 다시 그리고 옷 명암 정리.
- Rogue: 원래 눈매·입꼬리·앞머리, 역수 쌍단검. Applied correction: 짧아진 몸 비율을 유지하고 옷 명암만 정리 · 원본 얼굴 고정.

Rogue's exact original face, eyes, mouth-corner angle, fringe and expression remain locked. The actual final file's protected 38,700 pixels were verified against original SourceRogue.png with zero differences. Correct the body/costume without repainting that face.

Primary style authority is the nine original user-supplied images under Assets/Art/References/AngularCharacterStyle/Images/, not the latest generated set. Follow Art/AGENTS.md and its STYLE_GUIDE.md for all subsequent generation and correction. Original Warrior/Monk/Barbarian remain the requested proportion comparison group. Do not spread the old elongated Rogue proportions to others.

Warrior remains the original worn olive cloth design with cleaned upper-front cloth, not the exposed-armor or white-mantle variants. Barbarian keeps the approved lizard head/upper-body identity and compact lower costume. Samurai remains male with strong shoulder armor; the female direction was rejected. Wizard remains a kindly elderly, well-kept tower scholar with book, sheathed waist dagger and back-carried staff.

Original generated canvases are retained: six 1024x1536 PNGs. No downsampling or RGB palette quantization was used. Generated color/detail is corrected through drawing; transparent edge cleanup is a separate preparation step. Earlier versions and rejected candidates remain in their versioned folders.

This pass updates character image files and the comparison board. Earlier scene/HUD feedback (rear Warrior head, face-only portraits, selection layout) is separate work.

Latest explicit selection: Samurai uses the user's codex-clipboard-6f73b7d1-8316-4bb9-900f-9059b993beaa.png exactly as supplied. No face, proportions, costume or alpha changes. The compact square redraw is historical and unselected. See StyleCorrections03/Samurai-Selection.json. Preserve the chosen artwork unless the user asks for a further change.

Latest rollback request: discard the entire subsequent weapon enlargement, shield grip, Rogue hand/dagger and Shaman iteration. Restore the approved six-character state shown immediately after Samurai selection. StyleCorrections03 remains authoritative; all six files match the pre-iteration SHA256 snapshot. WeaponGrip04 is excluded from Assets and retained only under Docs/Production/AngularTrial-2026-10-25/Discarded as rejected history. Do not resume its edits or add its Shaman without a new user request.

Latest UI scope correction: apply approved images ONLY. The redesigned CharacterSelectionLayout UI was rejected and removed from Assets. OutGameScene and OutGameMenuView match their before-task backups exactly. Six approved figures are fitted uniformly into the existing selection image canvas without moving UI, changing controls, text, or data balance. Use production Selection*.png; do not revive the discarded layout.
For the image-only application, native OutGame camera rendering preserves approved image colors and contours. This is an artwork rendering setting only; the UI layout, text, controls and menu code are exactly the original UX.
