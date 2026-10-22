using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DungeonSlash;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static partial class BehemothContentBuilder
{
    internal const string Root = "Assets/Data/Behemoth";
    internal const string ContentPath = Root + "/GameContent.asset";
    private static GameContent content;
    private static readonly Dictionary<RelicPower, EquipmentData> relics = new();
    private static readonly Dictionary<string, MonsterData> monsters = new();
    private static readonly Dictionary<string, EncounterData> encounters = new();
    private static readonly Color Ink = new(.035f, .055f, .085f);
    private static readonly Color Panel = new(.075f, .11f, .16f);
    private static readonly Color Gold = new(.96f, .77f, .4f);
    private static TMP_FontAsset font;

    [InitializeOnLoadMethod]
    private static void Schedule() => EditorApplication.delayCall += AutoBuild;
    private static void AutoBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) { EditorApplication.delayCall += AutoBuild; return; }
        var existing = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
        if (existing != null && existing.schemaVersion >= 1) return;
        if (!File.Exists("Assets/Scenes/GamePrototype.unity") && !File.Exists("Assets/Scenes/MainGameScene.unity")) return;
        try { Build(); } catch (Exception exception) { Debug.LogException(exception); }
    }
    internal static void Build()
    {
        Directory.CreateDirectory(Root);
        foreach (var folder in new[] { "Characters", "Relics", "Spells", "Forms", "Monsters", "Attacks", "Encounters", "Stages", "Events" }) Directory.CreateDirectory(Root + "/" + folder);
        AssetDatabase.Refresh();
        content = Asset<GameContent>(ContentPath);
        font = BehemothTypography.Font;
        var variables = Asset<GameVariables>(Root + "/GameVariables.asset");
        var oldPlayerPath = "Assets/Data/DungeonSlashPrototype/Player/PlayerCombatData.asset";
        if (File.Exists(oldPlayerPath))
        {
            var yaml = File.ReadAllText(oldPlayerPath);
            foreach (var field in typeof(GameVariables).GetFields().Where(field => field.FieldType == typeof(float)))
            {
                var match = Regex.Match(yaml, "(?m)^  " + field.Name + @":\s*([\d.]+)");
                if (match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)) field.SetValue(variables, value);
            }
        }
        content.variables = variables;
        content.equipment = AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/Data/DungeonSlashPrototype/Equipment" }).Select(guid => AssetDatabase.LoadAssetAtPath<EquipmentData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
        content.perks = AssetDatabase.FindAssets("t:PerkData", new[] { "Assets/Data/DungeonSlashPrototype/Perks" }).Select(guid => AssetDatabase.LoadAssetAtPath<PerkData>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
        BuildSpells(); BuildForms(); BuildCharacters(); BuildExclusiveRelics(); BuildEventRelics();
        BuildStages(); BuildEvents(); BuildScenes();
        content.schemaVersion = 1;
        EditorUtility.SetDirty(content); EditorUtility.SetDirty(variables);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("BEHEMOTH_CONTENT_READY: six characters, 48 exclusive relics, 20 events, four stage sets.");
    }
    private static T Asset<T>(string path) where T : ScriptableObject
    {
        var item = AssetDatabase.LoadAssetAtPath<T>(path);
        if (item != null) return item;
        item = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(item, path); return item;
    }
    private static Sprite Icon(string id) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/RunIcons/" + id + ".png");
    private static EquipmentData Relic(string id, string title, string text, RelicScope scope, RelicPower power, string icon = "Relic_CorePrism")
    {
        var item = Asset<EquipmentData>(Root + "/Relics/" + id + ".asset");
        item.id = id; item.displayName = title; item.description = text; item.scope = scope; item.power = power;
        item.icon = Icon(icon); item.itemKind = ShopItemKind.Relic; item.price = 80;
        item.trigger = new EquipmentTrigger { chancePercent = 100,  triggerType = TriggerType.OnAcquire, triggerCount = 1, triggerAttackTypeFilter = TriggerAttackTypeFilter.All, triggerAttackWayFilter = TriggerAttackWayFilter.All };
        item.effect = new EquipmentEffect { effectType = EffectType.StatIncrease, effectCount = 1, effectStatType = ModifierKind.NormalDamage, effectMagnitude = 0 };
        item.additionalModifiers.Clear();
        if (power != RelicPower.None) relics[power] = item;
        if (!content.equipment.Contains(item)) content.equipment.Add(item);
        EditorUtility.SetDirty(item); return item;
    }
    private static void Mod(EquipmentData relic, ModifierKind kind, float value)
    { relic.additionalModifiers.Add(new StatModifier { kind = kind, value = value }); EditorUtility.SetDirty(relic); }

    private static void BuildSpells()
    {
        content.spells.Clear();
        AddSpell("Ember", "잿불 화살", "체력 비율이 가장 낮은 적을 겨냥한다. 목표 너머의 적도 관통한다.", SpellElement.Fire, SpellShape.Pierce, SwipeDirection.Right, .45f, 4.5f, 36f, "Relic_PainFrenzy");
        AddSpell("EnergyBolt", "에너지 볼트", "마지막 무기 적중 대상 우선. 지정 대상이 없으면 현재 체력이 가장 높은 적을 겨냥한다. 관통 공격.", SpellElement.Ice, SpellShape.Pierce, SwipeDirection.Up, .65f, 4f, 38f, "Relic_AzureTalon");
        AddSpell("Spark", "스파크", "무작위 적 하나를 겨냥해 번개를 뻗는다. 피뢰침이 있으면 우선한다. 경로의 적도 적중한다.", SpellElement.Lightning, SpellShape.Pierce, SwipeDirection.Down, .3f, 3.5f, 30f, "Relic_ThunderWire");
        AddSpell("FireBloom", "화염 개화", "체력 비율이 가장 낮은 적 주위에 폭발한다. 반경 안의 적을 함께 공격한다.", SpellElement.Fire, SpellShape.Burst, SwipeDirection.Left, 1.1f, 8f, 48f, "Relic_CorePrism");
        var frost = AddSpell("FrostRing", "서리 파동", "마지막 무기 적중 대상 주위에 서리를 펼친다. 맞은 적의 행동이 3초간 느려진다.", SpellElement.Ice, SpellShape.Burst, SwipeDirection.Left, .9f, 7f, 27f, "Perk_CoreHarvest"); frost.slowSeconds = 3f;
        var chain = AddSpell("ChainLightning", "연쇄 번개", "무작위 대상에서 가까운 적으로 최대 3회 연결한다. 사선이 맞지 않아도 여러 적을 공격한다.", SpellElement.Lightning, SpellShape.Chain, SwipeDirection.Right, 1.15f, 8f, 31f, "Relic_ThunderWire"); chain.radius = 280f;
        foreach (var spell in content.spells)
        {
            var item = Relic("Tome_" + spell.id, spell.displayName + "의 마도서", "획득 즉시 주문 습득. " + spell.description, RelicScope.Shared, RelicPower.None, spell.element == SpellElement.Fire ? "Relic_PainFrenzy" : spell.element == SpellElement.Ice ? "Relic_AzureTalon" : "Relic_ThunderWire");
            item.grantedSpell = spell; item.price = 38; EditorUtility.SetDirty(item); EditorUtility.SetDirty(spell);
        }
    }
    private static SpellData AddSpell(string id, string title, string text, SpellElement element, SpellShape shape, SwipeDirection slot, float cast, float cooldown, float damage, string icon)
    {
        var spell = Asset<SpellData>(Root + "/Spells/" + id + ".asset");
        spell.id = id; spell.displayName = title; spell.description = text; spell.element = element; spell.shape = shape; spell.defaultSlot = slot;
        spell.castingSeconds = cast; spell.cooldownSeconds = cooldown; spell.damage = damage; spell.width = 25f; spell.radius = shape == SpellShape.Burst ? 180f : 145f; spell.icon = Icon(icon);
        content.spells.Add(spell); EditorUtility.SetDirty(spell); return spell;
    }
    private static void BuildForms()
    {
        Form("Burst", "붕권", new[] { SwipeDirection.Up, SwipeDirection.Down }, FormFinisher.Burst, 2.5f, 1);
        Form("Multi", "백렬권", new[] { SwipeDirection.Up, SwipeDirection.Up }, FormFinisher.MultiHit, .65f, 3);
        Form("Shelter", "호신격", new[] { SwipeDirection.Down, SwipeDirection.Right }, FormFinisher.Shelter, 1f, 1);
        Form("Flow", "연환", new[] { SwipeDirection.Right, SwipeDirection.Up }, FormFinisher.Flow, 1.15f, 1);
        Form("Tiger", "맹호붕권", new[] { SwipeDirection.Down, SwipeDirection.Up, SwipeDirection.Up }, FormFinisher.Burst, 4f, 1, RelicPower.LongTiger);
        Form("Dragon", "유룡연타", new[] { SwipeDirection.Up, SwipeDirection.Down, SwipeDirection.Right }, FormFinisher.MultiHit, .75f, 4, RelicPower.LongDragon);
    }
    private static void Form(string id, string title, SwipeDirection[] pattern, FormFinisher finisher, float damage, int hits, RelicPower power = RelicPower.None)
    {
        var form = Asset<FormData>(Root + "/Forms/" + id + ".asset");
        form.displayName = title; form.pattern = pattern; form.finisher = finisher; form.damageMultiplier = damage; form.hits = hits; form.requiredPower = power; EditorUtility.SetDirty(form);
    }
    private static void BuildCharacters()
    {
        content.characters.Clear();
        Character(CharacterKind.Warrior, "워리어", "검과 방패로 체력을 지키는 전사. 가드게이지가 체력 손실을 대신하고 전투 중 자연 회복한다.", StartingMechanic.WarriorGuard, "수호자의 방패", "전투 중 가드게이지가 초당 2.5 회복된다. 가드 성공 시 체력 피해를 받지 않는다.", 100, 35, 1f, 14, 24, .85f, .75f, new(.3f, .7f, .95f), "Relic_AegisBattery");
        Character(CharacterKind.Rogue, "로그", "빠른 공격으로 독을 쌓는다. 독은 2초마다 피해를 주고, 마지막 부여 후 8초간 유지된다.", StartingMechanic.RoguePoison, "독 묻은 칼날", "일반공격 적중 시 독 1중첩. 독은 중첩당 3 피해를 2초마다 준다.", 75, 18, .6f, 9, 20, .48f, .65f, new(.55f, .85f, .35f), "Relic_EdgeCharm");
        Character(CharacterKind.Barbarian, "바바리안", "일반공격으로 격노를 모은다. 40 이상에서 차지하면 분노에 돌입한다. 분노가 사라지기 전에 몰아붙이자.", StartingMechanic.BarbarianRage, "격노의 심장", "일반공격 격노 +12. 40 이상에서 차지 시 분노 100. 분노 중 피해 +50%, 공격 대기 -38%, 받는 피해 -30%. 초당 20 소모, 일반공격으로 6 유지.", 120, 20, .55f, 22, 45, 1.15f, 1f, new(.95f, .4f, .25f), "Relic_PainFrenzy");
        Character(CharacterKind.Samurai, "사무라이", "가드로 납도하고 다음 공격을 발도로 바꾼다. 납도 직후의 짧은 패리를 맞히면 차지 발도를 얻는다. 일반 가드에도 체력 피해가 있다.", StartingMechanic.SamuraiDraw, "고요한 칼집", "가드 진입 시 납도. 납도 패리 0.1초. 다음 발도 피해 +50%. 납도 패리 성공 시 다음 발도가 차지 판정을 얻는다.", 80, 20, .5f, 15, 29, .8f, .75f, new(.95f, .55f, .7f), "Perk_Execution");
        Character(CharacterKind.Monk, "몽크", "최근 일반공격 방향으로 다음 차지를 바꾼다. ↑↓ 붕권 / ↑↑ 백렬권 / ↓→ 호신격 / →↑ 연환. 가드해도 연계는 유지된다.", StartingMechanic.MonkForms, "초식의 두루마리", "마지막 두 일반공격 방향의 초식을 차지로 발동한다. 차지 방향은 자유. 긴 초식을 해금하면 가장 긴 일치 패턴을 우선한다. 차지 사용 시 기록 초기화.", 95, 25, .7f, 9, 24, .55f, .65f, new(.95f, .75f, .3f), "Perk_QuickHand");
        Character(CharacterKind.Wizard, "위자드", "에너지 볼트로 시작하는 저체력 주문 사용자. 가드를 유지해 주문을 준비하고, 약점은 지팡이 차지로 파괴한다.", StartingMechanic.WizardCasting, "집중의 수정", "주문 재사용 대기시간 50% 감소. 에너지 볼트로 시작한다. MP는 없으며 주문마다 캐스팅 시간과 쿨타임이 다르다.", 65, 15, .55f, 9, 18, .8f, .75f, new(.65f, .55f, 1f), "Relic_CorePrism");
    }
    private static void Character(CharacterKind kind, string title, string text, StartingMechanic mechanic, string relicTitle, string relicText, float hp, float shield, float reduction, float normal, float charge, float cooldown, float chargeTime, Color color, string icon)
    {
        var starter = Relic("Starter_" + kind, relicTitle, relicText, RelicScope.Starting, RelicPower.None, icon); starter.exclusiveCharacter = kind; EditorUtility.SetDirty(starter);
        var player = Asset<PlayerCombatData>(Root + "/Characters/" + kind + ".asset");
        player.character = kind; player.displayName = title; player.description = text; player.characterColor = color; player.portrait = Icon(icon); player.gameVariables = content.variables;
        player.startingRelic = starter; player.maxHp = hp; player.shieldMax = shield; player.guardDamageReduction = reduction;
        player.normalShieldRegen = kind == CharacterKind.Warrior ? 2.5f : 0; player.brokenShieldRegen = 5f;
        player.baseAttackDamage = normal; player.baseChargeDamage = charge; player.attackCooldown = cooldown; player.chargeDuration = chargeTime;
        player.startingSpells.Clear(); if (kind == CharacterKind.Wizard) player.startingSpells.Add(content.spells.First(spell => spell.id == "EnergyBolt"));
        player.forms.Clear(); if (kind == CharacterKind.Monk) player.forms.AddRange(AssetDatabase.FindAssets("t:FormData", new[] { Root + "/Forms" }).Select(guid => AssetDatabase.LoadAssetAtPath<FormData>(AssetDatabase.GUIDToAssetPath(guid))));
        content.characters.Add(player); EditorUtility.SetDirty(player);
    }
    private static void BuildExclusiveRelics()
    {
        Exclusive(CharacterKind.Warrior, new[] {
            (RelicPower.ShieldBash,"신성검의 서약","가드로 받은 게이지 피해의 70%를 다음 무기 적중에 추가한다."),
            (RelicPower.PerfectMending,"완벽한 수호","퍼펙트가드 시 체력 4, 가드게이지 8 회복."),
            (RelicPower.Hammer,"철퇴의 가호","내려치기 적중 시 적의 일반공격 준비를 0.55초 지연. 약점 파괴 기절과 별개로 작동한다."),
            (RelicPower.Spear,"천공창","올려치기 피해 +35%, 공격 대기 -50%. 다른 방향 피해 -20%, 공격 대기 +25%."),
            (RelicPower.ShieldEdge,"방패의 칼날","무기 적중에 현재 가드게이지의 35%를 추가 피해로 더한다."),
            (RelicPower.GuardNova,"수호의 파동","가드 3회마다 모든 적에게 최대 가드게이지의 65% 피해."),
            (RelicPower.Paladin,"성기사의 길","퍼펙트가드마다 모든 주문의 남은 쿨타임 -2초. 잿불 화살 습득."),
            (RelicPower.IronFortress,"불굴의 성채","최대 가드게이지 +25. 자연 회복 속도 +80%.") });
        Exclusive(CharacterKind.Rogue, new[] {
            (RelicPower.ViperFang,"독사의 송곳니","차지 적중 시 대상의 독을 전부 소모해 중첩당 독 피해의 2.5배를 즉시 준다."),
            (RelicPower.TwinDaggers,"쌍단도","좌·우 일반공격을 연속하면 대기시간이 점점 감소, 최대 55% 감소. 상·하 공격으로 초기화."),
            (RelicPower.VenomSpread,"번지는 맹독","직접 처치한 적의 독 절반, 최소 2중첩을 살아 있는 다른 적에게 전파."),
            (RelicPower.FirstStrike,"단 한 번의 암살","각 적에게 첫 무기 적중 피해 +250%. 일반 피해 +60%. 시작 유물의 독 부여를 중단한다."),
            (RelicPower.PoisonLeech,"맹독의 생명줄","독이 피해를 줄 때 해당 피해의 12%만큼 체력 회복."),
            (RelicPower.Executioner,"숨통 끊기","기절한 대상에게 무기 피해 +65%."),
            (RelicPower.DeadlyDose,"치사량","일반공격의 독 부여량이 2중첩으로 증가."),
            (RelicPower.ShadowCounter,"그림자 반격","퍼펙트가드 후 다음 무기 공격 피해 +150%.") });
        Exclusive(CharacterKind.Barbarian, new[] {
            (RelicPower.DeferredPain,"늦게 찾아오는 고통","분노 중 체력 피해를 5초에 걸쳐 받는다. 분노 종료 시 남은 피해를 한 번에 받으며, 전투 종료 시 남은 피해는 사라진다."),
            (RelicPower.RageLeech,"피의 연회","분노 중 직접 무기 피해의 12%를 체력으로 회복."),
            (RelicPower.LongRage,"끝나지 않는 격노","분노의 초당 소모량이 20에서 12로 감소."),
            (RelicPower.RageCharge,"분노의 일격","분노 중 차지는 현재 분노에 비례해 강화되고 분노 35를 소비한다."),
            (RelicPower.ExecuteRage,"사냥은 계속된다","분노 중 직접 처치 시 분노 +30, 체력 +3."),
            (RelicPower.Unstoppable,"방어 분쇄","차지 공격이 방향 방벽을 무시한다. 차지 피해 +25%."),
            (RelicPower.BloodFrenzy,"피로 쓰는 광기","체력 절반 미만에서 일반공격의 격노 획득량이 18로 증가."),
            (RelicPower.ThunderousCharge,"뇌성의 대검","차지 적중 시 대상에게 추가 번개 피해 20. 차지 피해 +20%.") });
        Exclusive(CharacterKind.Samurai, new[] {
            (RelicPower.DrawMastery,"일섬","발도 피해 보정이 +50%에서 +110%로 증가."),
            (RelicPower.PatientSheath,"기다림의 칼","납도를 유지한 시간에 따라 발도 피해가 추가 증가, 최대 +100%."),
            (RelicPower.PerfectRiposte,"되비치는 검광","납도 패리 성공 시 공격한 적에게 차지 공격력만큼 즉시 피해."),
            (RelicPower.BloodlessDraw,"피를 거두는 칼집","발도 적중마다 체력 3 회복."),
            (RelicPower.DoubleDraw,"이중 발도","발도가 같은 궤적을 두 번 타격한다. 차지 발도는 약점도 두 번 타격."),
            (RelicPower.SpellBlade,"주문검","납도 상태에서 주문 캐스팅 시간 -35%. 에너지 볼트 습득."),
            (RelicPower.ParryRecovery,"틈을 베다","퍼펙트가드 성공 시 무기 공격 대기시간 즉시 초기화."),
            (RelicPower.ShadowSheath,"찰나의 잔영","납도 패리 시간이 0.1초에서 0.2초로 증가.") });
        Exclusive(CharacterKind.Monk, new[] {
            (RelicPower.ShelterMaster,"금강 호신","호신격 발동 시 최대 가드게이지의 45%를 회복."),
            (RelicPower.ComboLeech,"생명의 초식","초식 마무리 후 첫 적중 시 체력 3 회복."),
            (RelicPower.HundredFists,"백렬의 극의","백렬권의 타격 횟수 +2. 약점에도 적용."),
            (RelicPower.PressurePoint,"기혈 파쇄","기절한 적에게 초식 마무리 피해 +75%."),
            (RelicPower.FlowMaster,"끊기지 않는 연환","연환이 다음 일반공격 4회에 적용되며 공격 대기시간을 60% 감소시킨다."),
            (RelicPower.LongTiger,"맹호의 두루마리","긴 초식 ↓↑↑ 해금. 방향 무관 차지로 강력한 맹호붕권 발동."),
            (RelicPower.LongDragon,"유룡의 두루마리","긴 초식 ↑↓→ 해금. 방향 무관 차지로 4회 타격하는 유룡연타 발동."),
            (RelicPower.MindfulGuard,"호신에서 파쇄로","호신격의 보호가 남아 있으면 다음 차지 준비시간 -35%.") });
        Exclusive(CharacterKind.Wizard, new[] {
            (RelicPower.EmberMaster,"잿불의 왕관","화염 주문 피해 +60%. 잿불 화살 습득."),
            (RelicPower.IceFocus,"겨울의 시선","얼음 주문 피해 +35%, 둔화 지속시간 +50%. 서리 파동 습득."),
            (RelicPower.LightningRod,"피뢰침 지팡이","무기 적중 대상에게 8초간 피뢰침을 남긴다. 번개 주문은 피뢰침 대상을 우선한다. 스파크 습득."),
            (RelicPower.SpellEcho,"주문의 잔향","주문 3회 발동마다 해당 주문이 50% 피해로 한 번 더 발동."),
            (RelicPower.QuickCasting,"축약 영창","모든 주문의 요구 캐스팅 시간 -35%."),
            (RelicPower.ChainMaster,"뻗어나가는 폭풍","연쇄 주문의 최대 타깃 수 +2. 연쇄 번개 습득."),
            (RelicPower.ArcaneGuard,"비전 방벽","주문 발동마다 가드게이지 5 회복."),
            (RelicPower.StaffConduit,"지팡이와 주문 사이","차지 무기 적중 이후 다음 주문의 캐스팅 시간 -85%, 피해 +30%.") });
        Mod(relics[RelicPower.IronFortress], ModifierKind.ShieldMax, 25);
        Mod(relics[RelicPower.FirstStrike], ModifierKind.NormalDamage, .6f);
        Mod(relics[RelicPower.Unstoppable], ModifierKind.ChargeDamage, .25f);
        Mod(relics[RelicPower.ThunderousCharge], ModifierKind.ChargeDamage, .2f);
        Grant(RelicPower.Paladin, "Ember"); Grant(RelicPower.SpellBlade, "EnergyBolt"); Grant(RelicPower.EmberMaster, "Ember");
        Grant(RelicPower.IceFocus, "FrostRing"); Grant(RelicPower.LightningRod, "Spark"); Grant(RelicPower.ChainMaster, "ChainLightning");
    }
    private static void Exclusive(CharacterKind kind, (RelicPower power, string title, string description)[] definitions)
    {
        var icons = new[] { "Relic_CorePrism", "Relic_ChargeCoil", "Relic_EchoSpur", "Relic_AzureTalon", "Relic_ThornBulwark", "Perk_Execution", "Perk_DeepBreath", "Relic_AegisBattery" };
        for (var i = 0; i < definitions.Length; i++)
        { var definition = definitions[i]; var item = Relic(definition.power.ToString(), definition.title, definition.description, RelicScope.Exclusive, definition.power, icons[i]); item.exclusiveCharacter = kind; EditorUtility.SetDirty(item); }
    }
    private static void Grant(RelicPower power, string spell) { relics[power].grantedSpell = content.spells.First(item => item.id == spell); EditorUtility.SetDirty(relics[power]); }
    private static void BuildEventRelics()
    {
        Relic("StrayDog", "작은 발자국", "구조한 강아지가 동료로 합류한다. 전투 중 4초마다 체력이 낮은 적에게 13 + 레벨×2 피해.", RelicScope.Event, RelicPower.StrayDog, "Relic_HunterLantern");
        Relic("GuidingWisp", "길잡이의 등불", "주문 발동과 차지 적중 때 동료 위습이 적에게 10 추가 피해.", RelicScope.Event, RelicPower.GuidingWisp, "Perk_CoreHarvest");
        Mod(Relic("Parasite", "공생하는 씨앗", "일반 피해 +40%, 차지 피해 +30%. 이 씨앗은 사건에서만 얻을 수 있다.", RelicScope.Event, RelicPower.Parasite, "Perk_Harvest"), ModifierKind.NormalDamage, .4f);
        Mod(relics[RelicPower.Parasite], ModifierKind.ChargeDamage, .3f);
        Relic("MirrorShard", "기억하는 거울 조각", "매 4번째 무기 공격 적중 시 피해의 40%를 추가로 반복한다.", RelicScope.Event, RelicPower.MirrorShard, "Relic_DoubleCharge");
        Mod(Relic("BloodContract", "붉은 계약서", "모든 피해 +35%. 계약의 대가는 사건에서 지불한다.", RelicScope.Event, RelicPower.BloodContract, "Relic_PainFrenzy"), ModifierKind.AllDamageMultiplier, .35f);
        Relic("PilgrimOath", "순례자의 서약", "퍼펙트가드 성공 시 체력 2 회복. 길 잃은 신도에게 다른 해결책을 제공한다.", RelicScope.Event, RelicPower.PilgrimOath, "Relic_ChargeWard");
        Mod(Relic("Hourglass", "깨어진 모래시계", "주문 쿨타임 -15%, 무기 차지 준비시간 -15%.", RelicScope.Event, RelicPower.Hourglass, "Relic_ChargeCoil"), ModifierKind.ChargeDurationMultiplier, .15f);
        Relic("StormBottle", "병 속의 폭풍", "무기 차지 적중 시 추가 번개 피해 20. 모든 직업이 활용할 수 있다.", RelicScope.Event, RelicPower.StormBottle, "Relic_ThunderWire");
        Relic("AmberSeed", "호박빛 싹", "전투 승리 3회마다 최대 체력과 현재 체력 +4.", RelicScope.Event, RelicPower.AmberSeed, "Perk_Vitality");
        Mod(Relic("GuardianBell", "수호의 종", "각 전투 시작 시 3초간 받는 피해 20% 감소. 최대 가드게이지 +10.", RelicScope.Event, RelicPower.GuardianBell, "Relic_AegisBattery"), ModifierKind.ShieldMax, 10);
        Relic("RaggedBanner", "이름 없는 깃발", "살아 있는 적 하나당 무기 피해 +12%. 다중 전투에서 가치가 커진다.", RelicScope.Event, RelicPower.RaggedBanner, "Relic_CrosswindCharm");
        Relic("GoldenTooth", "금빛 이빨", "전투 승리마다 골드 +10.", RelicScope.Event, RelicPower.GoldenTooth, "Relic_HunterLantern");
    }
}
