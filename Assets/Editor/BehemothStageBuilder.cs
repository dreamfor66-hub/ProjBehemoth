using System;
using System.Collections.Generic;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

internal static partial class BehemothContentBuilder
{
    private static void BuildStages()
    {
        content.stages.Clear(); monsters.Clear(); encounters.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:MonsterData", new[] { "Assets/Data/DungeonSlashPrototype/Monsters" }))
        { var monster = AssetDatabase.LoadAssetAtPath<MonsterData>(AssetDatabase.GUIDToAssetPath(guid)); monsters[monster.name] = monster; }
        var bat = Old("B1_Minion_Bat"); var skeleton = Old("B1_Minion_Skeleton"); var leech = Old("B1_Minion_Leech");
        var frostBat = Old("B2_Minion_FrostBat"); var wraith = Old("B2_Minion_IceWraith"); var yeti = Old("B2_Minion_Yeti");
        var ashHound = Old("B3_Minion_AshHound"); var shade = Old("B3_Minion_ShadeLancer"); var voidMaw = Old("B3_Minion_VoidMaw");
        var rat = Monster("TunnelRat", "굴쥐", 36, 5, 1, .75f, 2.7f, 10, 1);
        var caller = Monster("AlarmHound", "경보 사냥개", 65, 6, 1, .9f, 3.2f, 8, 1);
        caller.encounterHint = "울음의 약점을 깨거나 먼저 처치하면 굴쥐의 합류를 막는다.";
        caller.GetAttack(MonsterAttackType.Charge).chargeSummonMechanics.summonOnFailedCharge = rat;
        var cinder = Monster("CinderImp", "잿불 도깨비", 52, 5, 2, .8f, 3f, 14, 1);
        var chainling = Monster("Chainling", "쇠사슬 하인", 72, 7, 1, 1f, 3.2f, 16, 2);
        var furnace = Monster("FurnaceMite", "용광로 진드기", 44, 4, 3, .95f, 3f, 12, 1);
        var needle = Monster("IceNeedle", "빙침 정찰병", 100, 8, 2, .8f, 3.4f, 24, 2);
        var snowbell = Monster("SnowBell", "눈종 정령", 110, 11, 1, 1.2f, 3.8f, 27, 2);
        var ashCrow = Monster("AshCrow", "재까마귀", 125, 8, 3, 1f, 3.6f, 30, 2);
        var soulLamp = Monster("SoulLamp", "혼불 등잔", 145, 13, 1, 1.1f, 4f, 33, 2);
        var mirror = Monster("MirrorKnight", "거울기사", 185, 9, 2, 1.05f, 3.8f, 28, 2, MonsterPattern.MirrorCounter);
        mirror.encounterHint = "같은 방향의 무기 공격을 연속으로 맞으면 반격한다. 방향을 바꾸거나 0.7초 반격 준비에 가드로 대응하자.";
        var stitcher = Monster("ArmoredExecutioner", "철갑 집행관", 215, 7, 3, 1f, 4.4f, 32, 2);
        stitcher.encounterHint = "기본 약점을 깨면 강타를 취소한다. 세 겹의 보조 약점을 먼저 부수면 기절이 길어진다.";
        var bonus = stitcher.GetAttack(MonsterAttackType.Charge).chargeWeakPoints.Last(); bonus.requiredChargeHits = 3; bonus.optional = true;
        var furnaceGuard = Monster("FurnaceGuard", "용광로 파수꾼", 205, 10, 1, 1.15f, 4f, 30, 2);
        Guard(furnaceGuard, TriggerAttackWayFilter.Upward, 48);
        var chainBrother = Monster("ChainBrother", "사슬 형제", 120, 7, 2, .85f, 4.2f, 24, 1);
        var cinderPriest = Monster("CinderPriest", "불씨 사제", 175, 8, 1, 1.1f, 4f, 28, 2, MonsterPattern.Summoner);
        cinderPriest.GetAttack(MonsterAttackType.Charge).chargeSummonMechanics.summonOnFailedCharge = cinder;
        cinderPriest.GetAttack(MonsterAttackType.Normal).combatMechanics.healOnUnguardedHit = 10;
        var iceMirror = Monster("IceMirror", "얼음 거울기사", 300, 12, 2, .9f, 4f, 42, 2, MonsterPattern.MirrorCounter);
        Guard(iceMirror, TriggerAttackWayFilter.Downward, 65);
        var paleWisp = Monster("PaleWisp", "창백한 위습", 145, 9, 2, .85f, 4f, 28, 1);
        var bloodExecutioner = Monster("BloodExecutioner", "피의 집행자", 420, 16, 2, 1f, 4.2f, 50, 3, MonsterPattern.BloodFury);
        bloodExecutioner.encounterHint = "무기 적중 5회마다 다음 공격이 빨라진다. 약점 파괴로 확보한 기절에 피해를 집중하자.";
        var lampPriest = Monster("LampPriest", "검은 등불 사제", 290, 13, 1, 1.1f, 4.2f, 44, 2);
        lampPriest.GetAttack(MonsterAttackType.Charge).chargeSummonMechanics.summonOnFailedCharge = soulLamp;
        var bellKeeper = Monster("BellKeeper", "종지기", 370, 6, 3, 1.3f, 4.8f, 30, 3);
        bellKeeper.encounterHint = "연속 종소리 뒤 긴 강타. 여러 약점을 한 궤적으로 파괴할 기회를 찾자.";
        var brood = Monster("BroodKeeper", "회전 둥지의 주인", 330, 11, 1, 1.1f, 4.5f, 32, 2);
        var orbitBat = Monster("OrbitBat", "선회 박쥐", 65, 5, 1, 1f, 3.5f, 13, 1, MonsterPattern.Orbit); orbitBat.diesWithLeader = true;
        var leftHeart = Heart("LeftHeart", "좌심장 · 연타", "left_heart", 80);
        var rightHeart = Heart("RightHeart", "우심장 · 강타", "right_heart", 80);
        var twinHeart = Monster("TwinHeart", "쌍심장 포식자", 380, 9, 1, 1.1f, 4.8f, 30, 2, MonsterPattern.LinkedHeart);
        LinkHeartAttacks(twinHeart, 25, 7, 4);
        var judge = Monster("ClockworkJudge", "태엽 재판관", 420, 6, 4, 1.25f, 4.5f, 35, 2);
        Guard(judge, TriggerAttackWayFilter.Horizontal, 60);
        AddCharge(judge, "정지 후 내려찍기", 4.8f, 42, 1, 3, .3f);
        var slag = Monster("SlagTitan", "슬래그 거인", 460, 15, 1, 1.4f, 4.8f, 38, 2, MonsterPattern.BloodFury);
        slag.GetAttack(MonsterAttackType.Normal).combatMechanics.healOnUnguardedHit = 12;
        var spider = Monster("CrystalSpider", "수정 거미", 620, 9, 4, 1f, 5f, 50, 3);
        Guard(spider, TriggerAttackWayFilter.Downward, 85);
        var stormMatriarch = Monster("StormMatriarch", "폭풍의 모체", 580, 15, 2, 1.2f, 4.8f, 48, 2);
        var stormOrb = Monster("StormOrb", "선회 뇌구", 100, 7, 2, .95f, 4f, 25, 1, MonsterPattern.Orbit); stormOrb.diesWithLeader = true;
        var eclipse = Monster("EclipseBeast", "일식의 짐승", 850, 18, 2, 1.2f, 5.2f, 60, 3, MonsterPattern.LinkedHeart);
        LinkHeartAttacks(eclipse, 55, 12, 5);
        var elderLeft = Heart("ElderLeftHeart", "일식의 좌심장", "left_heart", 150);
        var elderRight = Heart("ElderRightHeart", "일식의 우심장", "right_heart", 150);
        var general = Monster("AshGeneral", "재의 군단장", 900, 9, 5, 1.4f, 5.3f, 62, 3, MonsterPattern.Summoner);
        general.GetAttack(MonsterAttackType.Charge).chargeSummonMechanics.summonOnFailedCharge = ashCrow;
        AddCharge(general, "군단의 맹세", 4.7f, 24, 3, 2, .45f);
        var trial = Monster("TrialGuardian", "봉인된 시련의 수호자", 240, 9, 2, 1.1f, 4.5f, 30, 2);
        var deepTrial = Monster("DeepTrialGuardian", "심층 시련의 수호자", 460, 15, 2, 1.1f, 5f, 48, 3);

        var maze = Stage("Maze", "잊힌 미궁", 1);
        maze.earlyCombats.AddRange(new[] { E("MazeBat", "길목의 박쥐", RoomEncounterType.Combat, bat), E("MazeRat", "굴쥐 두 마리", RoomEncounterType.Combat, rat, rat), E("MazeSkeleton", "미궁의 해골", RoomEncounterType.Combat, skeleton), E("MazeCaller", "경보 사냥개", RoomEncounterType.Combat, caller) });
        maze.hardCombats.AddRange(new[] { E("MazePair", "해골과 박쥐", RoomEncounterType.Combat, skeleton, bat), E("MazeLeeches", "흡혈 무리", RoomEncounterType.Combat, leech, leech, rat), E("MazeAmbush", "경보의 매복", RoomEncounterType.Combat, caller, bat) });
        maze.elites.AddRange(new[] { E("Minotaur", "미노타우루스", RoomEncounterType.Elite, Old("B1_Elite_Minotaur")), E("MirrorKnight", "거울기사", RoomEncounterType.Elite, mirror), E("Executioner", "철갑 집행관", RoomEncounterType.Elite, stitcher) });
        maze.bosses.AddRange(new[] { E("MazeGuardian", "미궁의 수호자", RoomEncounterType.Boss, Old("B1_Boss_MazeGuardian")), E("BellKeeper", "종지기", RoomEncounterType.Boss, bellKeeper), E("BroodKeeper", "회전 둥지의 주인", RoomEncounterType.Boss, brood, orbitBat, orbitBat) });
        var forge = Stage("Forge", "불씨의 용광로", 1);
        forge.earlyCombats.AddRange(new[] { E("ForgeImp", "잿불 도깨비", RoomEncounterType.Combat, cinder), E("ForgeChain", "쇠사슬 하인", RoomEncounterType.Combat, chainling), E("ForgeMites", "진드기 무리", RoomEncounterType.Combat, furnace, furnace) });
        forge.hardCombats.AddRange(new[] { E("ForgePair", "불꽃과 쇠사슬", RoomEncounterType.Combat, chainling, cinder), E("ForgeTrio", "용광로의 군집", RoomEncounterType.Combat, furnace, cinder, furnace), E("ForgeAlarm", "경보와 불씨", RoomEncounterType.Combat, caller, cinder) });
        forge.elites.AddRange(new[] { E("FurnaceGuard", "용광로 파수꾼", RoomEncounterType.Elite, furnaceGuard), E("ChainBrothers", "사슬 형제", RoomEncounterType.Elite, chainBrother, chainBrother), E("CinderPriest", "불씨의 의식", RoomEncounterType.Elite, cinderPriest, furnace, furnace) });
        forge.bosses.AddRange(new[] { E("TwinHeart", "쌍심장 포식자", RoomEncounterType.Boss, twinHeart, leftHeart, rightHeart), E("ClockworkJudge", "태엽 재판관", RoomEncounterType.Boss, judge), E("SlagTitan", "슬래그 거인", RoomEncounterType.Boss, slag) });
        var frost = Stage("Frost", "서리의 성소", 2);
        frost.earlyCombats.AddRange(new[] { E("FrostBat", "서리 박쥐", RoomEncounterType.Combat, frostBat), E("FrostNeedle", "빙침 정찰병", RoomEncounterType.Combat, needle), E("FrostWraith", "얼음 귀신", RoomEncounterType.Combat, wraith) });
        frost.hardCombats.AddRange(new[] { E("FrostPair", "빙침과 서리", RoomEncounterType.Combat, needle, frostBat), E("FrostYeti", "설인과 눈종", RoomEncounterType.Combat, yeti, snowbell), E("FrostTrio", "떠도는 서리", RoomEncounterType.Combat, wraith, frostBat, frostBat) });
        frost.elites.AddRange(new[] { E("FrostOgre", "서리 거인", RoomEncounterType.Elite, Old("B2_Elite_FrostOgre")), E("IceMirror", "얼음 거울기사", RoomEncounterType.Elite, iceMirror), E("WispChoir", "창백한 합창", RoomEncounterType.Elite, paleWisp, paleWisp, paleWisp) });
        frost.bosses.AddRange(new[] { E("FrostQueen", "서리 여왕", RoomEncounterType.Boss, Old("B2_Boss_FrostQueen")), E("CrystalSpider", "수정 거미", RoomEncounterType.Boss, spider), E("StormMatriarch", "폭풍의 모체", RoomEncounterType.Boss, stormMatriarch, stormOrb, stormOrb) });
        var ash = Stage("Ash", "영혼의 잿더미", 3);
        ash.earlyCombats.AddRange(new[] { E("AshHound", "재 사냥개", RoomEncounterType.Combat, ashHound), E("AshShade", "영혼 창병", RoomEncounterType.Combat, shade), E("AshCrow", "재까마귀", RoomEncounterType.Combat, ashCrow) });
        ash.hardCombats.AddRange(new[] { E("AshPair", "사냥개와 창병", RoomEncounterType.Combat, ashHound, shade), E("AshMaw", "공허의 사냥", RoomEncounterType.Combat, voidMaw, ashCrow), E("AshLamps", "혼불의 행렬", RoomEncounterType.Combat, soulLamp, ashHound, ashHound) });
        ash.elites.AddRange(new[] { E("SoulKnight", "영혼 기사", RoomEncounterType.Elite, Old("B3_Elite_SoulKnight")), E("BloodExecutioner", "피의 집행자", RoomEncounterType.Elite, bloodExecutioner), E("LampPriest", "검은 등불의 의식", RoomEncounterType.Elite, lampPriest, ashCrow, ashCrow) });
        ash.bosses.AddRange(new[] { E("SoulEater", "영혼 포식자", RoomEncounterType.Boss, Old("B3_Boss_SoulEater")), E("EclipseBeast", "일식의 짐승", RoomEncounterType.Boss, eclipse, elderLeft, elderRight), E("AshGeneral", "재의 군단장", RoomEncounterType.Boss, general) });
        E("Trial", "봉인된 시련", RoomEncounterType.Event, trial);
        E("DeepTrial", "심층 시련", RoomEncounterType.Event, deepTrial, needle);
        foreach (var stage in content.stages) EditorUtility.SetDirty(stage);
        foreach (var monster in monsters.Values.Where(item => AssetDatabase.GetAssetPath(item).StartsWith(Root))) EditorUtility.SetDirty(monster);
        foreach (var guid in AssetDatabase.FindAssets("t:MonsterAttackData", new[] { Root + "/Attacks" })) EditorUtility.SetDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
    }
    private static MonsterData Old(string suffix) => monsters["Data_Monster_" + suffix];
    private static StageSetData Stage(string id, string title, int act)
    {
        var stage = Asset<StageSetData>(Root + "/Stages/" + id + ".asset"); stage.id = id; stage.displayName = title; stage.act = act; stage.easyCombatCount = 3;
        stage.earlyCombats.Clear(); stage.hardCombats.Clear(); stage.elites.Clear(); stage.bosses.Clear(); content.stages.Add(stage); return stage;
    }
    private static MonsterData Monster(string id, string title, float hp, float normalDamage, int normalHits, float windup, float exposure, float chargeDamage, int weakPoints, MonsterPattern pattern = MonsterPattern.Standard)
    {
        var monster = Asset<MonsterData>(Root + "/Monsters/" + id + ".asset"); monsters[id] = monster;
        monster.displayName = title; monster.maxHp = hp; monster.normalAttackInterval = 2.9f; monster.chargeInterval = 6.8f;
        monster.stunDuration = 3f; monster.stunDamageMultiplier = 1.5f; monster.bodyHitRadius = 95f;
        monster.experienceReward = Mathf.RoundToInt(14f + hp * .08f); monster.goldReward = Mathf.RoundToInt(8f + hp * .035f);
        monster.bodyColor = Color.HSVToRGB((Mathf.Abs(id.GetHashCode()) % 100) / 100f, .4f, .8f); monster.pattern = pattern;
        monster.attacks.Clear();
        var normal = Attack(id + "_Normal", "견제", MonsterAttackType.Normal, normalDamage, windup, normalHits);
        monster.attacks.Add(normal);
        AddCharge(monster, "강타", exposure, chargeDamage, 1, weakPoints, .3f);
        return monster;
    }
    private static MonsterAttackData Attack(string id, string title, MonsterAttackType type, float damage, float duration, int hits)
    {
        var attack = Asset<MonsterAttackData>(Root + "/Attacks/" + id + ".asset");
        attack.type = type; attack.displayName = title; attack.damage = damage; attack.shieldDamage = damage * 1.6f; attack.windupDuration = duration;
        attack.guardable = true; attack.combatMechanics = new MonsterCombatMechanics { hitCount = hits, followupDelay = .4f };
        attack.chargeWeakPoints.Clear(); attack.chargeSummonMechanics = new ChargeSummonMechanics(); EditorUtility.SetDirty(attack); return attack;
    }
    private static MonsterAttackData AddCharge(MonsterData monster, string title, float exposure, float damage, int hits, int points, float delay)
    {
        var attack = Attack(monster.name + "_Charge_" + monster.attacks.Count, title, MonsterAttackType.Charge, damage, 1f, hits);
        attack.chargeTimeLimit = exposure; attack.combatMechanics.followupDelay = delay;
        for (var i = 0; i < points; i++) attack.chargeWeakPoints.Add(new WeakPointDefinition { id = "lock_" + i, normalizedPosition = new Vector2((i - (points - 1) * .5f) * 85f, points > 2 && i % 2 == 1 ? 50 : 0), hitRadius = 29, requiredChargeHits = 1 });
        monster.attacks.Add(attack); return attack;
    }
    private static void Guard(MonsterData monster, TriggerAttackWayFilter direction, float amount)
    { monster.GetAttack(MonsterAttackType.Normal).combatMechanics.directionalGuard = new DirectionalGuardData { enabled = true, breakByAttackWay = direction, maxGuard = amount, rebuildDelay = 6f, rebuildPerSecond = 12f }; }
    private static MonsterData Heart(string id, string title, string support, float hp)
    {
        var heart = Asset<MonsterData>(Root + "/Monsters/" + id + ".asset"); monsters[id] = heart; heart.displayName = title; heart.maxHp = hp;
        heart.bodyHitRadius = 65f; heart.normalAttackInterval = heart.chargeInterval = 99999; heart.attacks.Clear(); heart.supportId = support;
        heart.diesWithLeader = true; heart.pattern = MonsterPattern.LinkedHeart; heart.experienceReward = heart.goldReward = 0; heart.bodyColor = new(.95f, .3f, .45f); return heart;
    }
    private static void LinkHeartAttacks(MonsterData monster, float heavy, float flurry, int hits)
    {
        monster.encounterHint = "왼심장을 부수면 연타, 오른심장을 부수면 강타를 차단한다. 두 심장을 모두 부수면 본체가 기절한다.";
        monster.attacks.RemoveAll(attack => attack.type == MonsterAttackType.Charge);
        AddCharge(monster, "좌심장 · 연속 맥동", 4.5f, flurry, hits, 2, .4f).requiredSupportId = "left_heart";
        AddCharge(monster, "우심장 · 파열", 4.8f, heavy, 1, 2, .4f).requiredSupportId = "right_heart";
        AddCharge(monster, "본체 · 포식", 4.5f, heavy * .65f, 1, 2, .4f);
    }
    private static EncounterData E(string id, string title, RoomEncounterType category, params MonsterData[] members)
    {
        var encounter = Asset<EncounterData>(Root + "/Encounters/" + id + ".asset"); encounters[id] = encounter;
        encounter.id = id; encounter.displayName = title; encounter.category = category;
        encounter.hint = members.FirstOrDefault()?.encounterHint ?? ""; encounter.members.Clear();
        for (var i = 0; i < members.Length; i++)
        {
            var offset = members.Length == 1 ? Vector2.zero : members.Length == 2 ? new Vector2(i == 0 ? -135 : 135, i == 0 ? 35 : -30) : new Vector2(i == 0 ? 0 : i == 1 ? -190 : 190, i == 0 ? 80 : -55);
            encounter.members.Add(new EncounterMember { monster = members[i], offset = offset, scale = members.Length == 1 ? 1f : i == 0 && category == RoomEncounterType.Boss ? .9f : .68f, actionDelay = i * .85f });
        }
        EditorUtility.SetDirty(encounter); return encounter;
    }
}
