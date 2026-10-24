using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class AshenVowGameplayPreparation
{
    internal const string Root = "Assets/Data/Behemoth";
    private static GameContent content;
    static AshenVowGameplayPreparation() => EditorApplication.delayCall += Prepare;
    internal static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Prepare; return; }
        content = AssetDatabase.LoadAssetAtPath<GameContent>(Root + "/GameContent.asset");
        if (content == null || content.schemaVersion < 21 || content.gameplayRevision >= 1) return;
        try
        {
            foreach (var dir in new[] { "Perks", "PerkAbilities", "Buffs/AshenVow" }) Directory.CreateDirectory(Root + "/" + dir);
            AssetDatabase.Refresh();
            foreach (var buff in content.characters.SelectMany(c => c.attackBuffs).Where(b => b != null).Distinct())
            { buff.attackRole = buff.Units.SelectMany(u => u.effects).Any(e => e.effectType == EffectType.ScaleActionDamage && e.effectMagnitude > 1 || e.effectType == EffectType.ActionExtraHits && e.effectMagnitude > 0) ? AttackBuffRole.Offense : AttackBuffRole.Utility; EditorUtility.SetDirty(buff); }
            var shelter = Existing("Shelter"); if (shelter != null) { shelter.attackRole = AttackBuffRole.Defense; EditorUtility.SetDirty(shelter); }
            BuildPerks(); BuildSpells();
            content.gameplayRevision = 1;
            EditorUtility.SetDirty(content); BehemothTextEditor.SyncAll(); BehemothTextEditor.ExportDefault(); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/AshenVow-GameplayPreparation.txt", "39 stackable perks; six exclusive pools; MP; utility and advanced spells; all content available without meta unlocks.");
        }
        catch (Exception error) { Debug.LogException(error); }
    }
    internal static T Asset<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path); if (existing != null) return existing;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
    }
    private static BuffData Existing(string id) => AssetDatabase.LoadAssetAtPath<BuffData>(Root + "/Buffs/" + id + ".asset");
    private static BuffTagData Tag(string id) => Asset<BuffTagData>(Root + "/BuffTags/" + id + ".asset");
    internal static EquipmentEffect E(EffectType type, float amount = 0) => new() { effectType = type, effectMagnitude = amount, effectCount = 1 };
    internal static EquipmentAbility U(TriggerType trigger, TargetType target, EquipmentEffect effect, params AbilityCondition[] conditions) => new()
    { trigger = new EquipmentTrigger { triggerType = trigger, triggerCount = 1, triggerAttackTypeFilter = TriggerAttackTypeFilter.All, triggerAttackWayFilter = TriggerAttackWayFilter.All, chancePercent = 100, resetCountEachCombat = true }, target = target, effects = new() { effect }, conditions = conditions.ToList() };
    private static AbilityCondition C(AbilityConditionKind kind, BuffData buff = null, float value = .5f, Comparator compare = Comparator.GreaterOrEqual) => new() { kind = kind, buff = buff, threshold = value, comparator = compare };
    private static EquipmentEffect Rule(AbilityRule rule, float value) { var e = E(EffectType.RuleModifier, value); e.rule = rule; return e; }
    private static EquipmentEffect Variable(EquipmentEffect e, EffectScale scale, float ratio, BuffData buff = null)
    { e.valueVariables = new() { new() { type = scale, multiplier = ratio, buff = buff } }; return e; }
    private static EquipmentEffect Add(BuffData buff, float count = 1) { var e = E(EffectType.AddBuff, count); e.buff = buff; return e; }
    private static EquipmentAbility Filter(EquipmentAbility unit, TriggerAttackTypeFilter attacks) { var t = unit.trigger; t.triggerAttackTypeFilter = attacks; unit.trigger = t; return unit; }
    private static EquipmentAbility Passive(EquipmentEffect effect, params AbilityCondition[] conditions) => U(TriggerType.Passive, TargetType.OwnerStat, effect, conditions);
    private static BuffData Ready(string id, string name, float damage, TriggerAttackTypeFilter attacks = TriggerAttackTypeFilter.All, SpellElement? element = null)
    {
        var buff = Asset<BuffData>(Root + "/Buffs/AshenVow/" + id + ".asset");
        buff.displayName = name; buff.showInHud = true;
        buff.ReleaseData = new() { Type = BuffReleaseType.Stage, Count = 1 };
        buff.DuplicationData = new() { Type = BuffDuplicationType.UseNew, MaxStackCount = 999 };
        var effect = Variable(E(element.HasValue ? EffectType.ElementDamage : EffectType.Damage), EffectScale.BuffStacks, damage);
        if (element.HasValue) effect.element = element.Value;
        buff.Units = new() { Filter(U(TriggerType.OnHit, TargetType.Enemy, effect), attacks), Filter(U(TriggerType.OnHit, TargetType.ThisBuff, E(EffectType.RemoveBuff)), attacks) };
        EditorUtility.SetDirty(buff); return buff;
    }
    private static PerkData P(string id, string name, string description, CharacterKind? character, int icon, params EquipmentAbility[] abilities)
    {
        var perk = Asset<PerkData>(Root + "/Perks/" + id + ".asset"); perk.id = id; perk.displayName = name; perk.description = description; perk.maxStacks = 99;
        perk.scope = character.HasValue ? RelicScope.Exclusive : RelicScope.Shared; perk.exclusiveCharacter = character ?? CharacterKind.Warrior;
        perk.modifiers = new();
        var source = Asset<EquipmentData>(Root + "/PerkAbilities/" + id + ".asset"); source.id = id; source.displayName = name; source.description = description; source.abilities = abilities.ToList(); source.additionalModifiers.Clear();
        perk.abilitySource = source; EditorUtility.SetDirty(source); EditorUtility.SetDirty(perk); content.perks.Add(perk); return perk;
    }
    private static PerkData Stat(string id, string name, string description, CharacterKind? character, int icon, ModifierKind kind, float value)
    { var p = P(id, name, description, character, icon); p.modifiers.Add(new() { kind = kind, value = value }); EditorUtility.SetDirty(p); return p; }
    private static void BuildPerks()
    {
        content.perks.Clear();
        Stat("Vitality", "활력", "최대 체력 +10", null, 30, ModifierKind.MaxHp, 10);
        Stat("SharpWeapon", "날 선 무기", "일반공격 피해 +2", null, 31, ModifierKind.FlatNormalDamage, 2);
        Stat("HeavyWeapon", "묵직한 무기", "차지 공격 피해 +4", null, 32, ModifierKind.FlatChargeDamage, 4);
        Stat("Exploit", "빈틈 공략", "기절한 적에게 주는 피해 +15%", null, 33, ModifierKind.StunDamageMultiplier, .15f);
        Stat("ManaVessel", "마력 그릇", "최대 MP +3", null, 34, ModifierKind.MaxMp, 3);
        Stat("ManaFlow", "마력 순환", "초당 MP 회복량 +0.05", null, 35, ModifierKind.MpRegen, .05f);
        Stat("FireMastery", "화염 숙련", "화염 피해 +10%", null, 36, ModifierKind.FireDamage, .1f);
        Stat("IceMastery", "냉기 숙련", "냉기 피해 +10%", null, 37, ModifierKind.IceDamage, .1f);
        Stat("LightningMastery", "번개 숙련", "번개 피해 +10%", null, 38, ModifierKind.LightningDamage, .1f);
        var counter = Ready("Counter", "받아치기", 6);
        P("Counter", "받아치기", "가드에 성공하면 다음 무기 공격에 물리 피해 6을 추가한다.", CharacterKind.Warrior, 0, U(TriggerType.OnGuard, TargetType.Owner, Add(counter)));
        P("Pressure", "압박", "공격을 준비 중인 적에게 일반공격을 적중시키면 물리 피해 3을 추가한다.", CharacterKind.Warrior, 1,
            Passive(Rule(AbilityRule.FlatWeaponDamage, 3), C(AbilityConditionKind.TargetPreparing), new AbilityCondition { kind = AbilityConditionKind.Rule, rule = AbilityRule.NormalDamageBonus, invert = false, threshold = -100 }));
        // The normal-attack filter is evaluated on the passive unit as well as on trigger units.
        content.perks.Last().abilitySource.abilities[0].trigger.triggerAttackTypeFilter = TriggerAttackTypeFilter.NormalAttack;
        var shieldDamage = E(EffectType.Damage); shieldDamage.scale = EffectScale.GuardCurrent; shieldDamage.scaleRatio = .1f;
        P("ShieldImpact", "방패 중격", "차지 공격에 현재 실드의 10%만큼 물리 피해를 추가한다.", CharacterKind.Warrior, 2, Filter(U(TriggerType.OnHit, TargetType.Enemy, shieldDamage), TriggerAttackTypeFilter.ChargeAttack));
        P("ForwardGuard", "전진 방어", "일반공격 적중 시 실드를 0.5 회복한다.", CharacterKind.Warrior, 3, Filter(U(TriggerType.OnHit, TargetType.Owner, E(EffectType.RestoreGuard, .5f)), TriggerAttackTypeFilter.NormalAttack));
        Stat("Reform", "재정비", "기본 실드 재생량이 초당 0.4 증가한다.", CharacterKind.Warrior, 4, ModifierKind.NormalShieldRegen, .4f);
        var poison = CombatStatusCatalog.Instance.poison;
        P("PoisonBlade", "독 바른 칼", "일반공격 적중 시 독을 1중첩 부여한다.", CharacterKind.Rogue, 5, Filter(U(TriggerType.OnHit, TargetType.Enemy, Add(poison)), TriggerAttackTypeFilter.NormalAttack));
        P("DeepInjection", "깊은 주입", "차지 공격 적중 시 독을 2중첩 부여한다.", CharacterKind.Rogue, 6, Filter(U(TriggerType.OnHit, TargetType.Enemy, Add(poison, 2)), TriggerAttackTypeFilter.ChargeAttack));
        Stat("ConcentratedVenom", "농축 독액", "독 피해 +15%", CharacterKind.Rogue, 7, ModifierKind.PoisonDamage, .15f);
        P("Assassination", "암살", "전투의 첫 무기 공격에 물리 피해 12를 추가한다.", CharacterKind.Rogue, 8, Passive(Rule(AbilityRule.FlatWeaponDamage, 12), C(AbilityConditionKind.FirstWeaponAction)));
        var poisonedCharge = Passive(Rule(AbilityRule.ChargeDamageBonus, .12f), C(AbilityConditionKind.TargetPoisoned));
        P("VenomStrike", "독살자의 일격", "중독된 적에게 주는 차지 공격 피해 +12%", CharacterKind.Rogue, 9, poisonedCharge);
        var draw = new AbilityCondition { kind = AbilityConditionKind.ActionHasTag, buffTag = Tag("DrawAttack") };
        var parry = Ready("ParryEcho", "패리 잔격", 0);
        var echo = E(EffectType.Damage); echo.scale = EffectScale.DamageDealt; echo.scaleRatio = .2f; echo.multiplyByBuffStacks = true;
        parry.Units[0].effects[0] = echo; parry.Units[0].conditions.Add(draw); parry.Units[1].conditions.Add(draw); EditorUtility.SetDirty(parry);
        P("ParryEcho", "패리 잔격", "납도 패리에 성공하면 다음 발도가 피해의 20%만큼 추가 타격한다.", CharacterKind.Samurai, 10, U(TriggerType.OnSheathParry, TargetType.Owner, Add(parry)));
        var patience = Variable(Rule(AbilityRule.FlatWeaponDamage, 0), EffectScale.ActionStartBuffElapsed, 6, Existing("Sheathed"));
        P("PatientDraw", "축적 일섬", "납도를 유지한 시간 1초당 다음 발도에 물리 피해 6을 추가한다.", CharacterKind.Samurai, 11, Passive(patience, draw));
        P("RapidDraw", "연속 발도", "발도 후 2초 안에 다시 발도하면 물리 피해 4의 잔격이 발생한다.", CharacterKind.Samurai, 12, U(TriggerType.OnHit, TargetType.Enemy, E(EffectType.Damage, 4), draw, new AbilityCondition { kind = AbilityConditionKind.SinceActionTag, buffTag = Tag("DrawAttack"), threshold = 2, comparator = Comparator.LessOrEqual }));
        var swordRecovery = Ready("SwordRecovery", "검세 회수", 5); swordRecovery.DuplicationData.Type = BuffDuplicationType.StackCount;
        swordRecovery.Units[0].conditions.Add(draw); swordRecovery.Units[1].conditions.Add(draw); EditorUtility.SetDirty(swordRecovery);
        P("SwordRecovery", "검세 회수", "발도 후 일반공격을 적중시킬 때마다 다음 발도에 물리 피해 5를 추가한다.", CharacterKind.Samurai, 13, Filter(U(TriggerType.OnHit, TargetType.Owner, Add(swordRecovery), new AbilityCondition { kind = AbilityConditionKind.SinceActionTag, buffTag = Tag("DrawAttack"), threshold = 999999, comparator = Comparator.Less }), TriggerAttackTypeFilter.NormalAttack));
        P("LongBlade", "긴 칼날", "발도 공격 범위 +8%", CharacterKind.Samurai, 14, Passive(Rule(AbilityRule.DrawReachBonus, .08f)));
        var offensive = C(AbilityConditionKind.HasActionBuff, value: (int)AttackBuffRole.Offense);
        P("OneSchool", "일문전수", "같은 공격형 초식을 연속 사용하면 두 번째부터 물리 피해 6을 추가한다.", CharacterKind.Monk, 15, Passive(Rule(AbilityRule.FlatWeaponDamage, 6), offensive, C(AbilityConditionKind.PreviousAttackBuffSame)));
        var change = Ready("ChangingForms", "변초", 18); change.Units[0].conditions.Add(offensive); change.Units[1].conditions.Add(offensive); EditorUtility.SetDirty(change);
        P("ChangingForms", "변초", "서로 다른 초식을 3회 연속 사용하면 다음 공격형 초식에 물리 피해 18을 추가한다.", CharacterKind.Monk, 17, U(TriggerType.OnAttackResolved, TargetType.Owner, Add(change), C(AbilityConditionKind.HasActionBuff, value: 0), C(AbilityConditionKind.NextDistinctTechniques, value: 3)));
        var flowState = Existing("HorizontalFlow");
        P("FlowFinish", "연환의 끝", "연환으로 강화된 일반공격을 모두 적중시키면 마지막 공격에 물리 피해 10을 추가한다.", CharacterKind.Monk, 18, Filter(U(TriggerType.OnHit, TargetType.Enemy, E(EffectType.Damage, 10), C(AbilityConditionKind.OwnerBuffRemainingUses, flowState, 1, Comparator.LessOrEqual)), TriggerAttackTypeFilter.NormalAttack));
        var shelterState = Existing("Shelter").Units.SelectMany(u => u.effects).Where(e => e.effectType == EffectType.AddBuff && e.buff != null).Select(e => e.buff).FirstOrDefault();
        var shelterCounter = Ready("ShelterCounter", "호신경", 10); shelterCounter.Units[0].conditions.Add(offensive); shelterCounter.Units[1].conditions.Add(offensive); EditorUtility.SetDirty(shelterCounter);
        P("ShelterCounter", "호신경", "호신격으로 공격을 받아내면 다음 공격형 초식에 물리 피해 10을 추가한다.", CharacterKind.Monk, 19, U(TriggerType.OnGuard, TargetType.Owner, Add(shelterCounter), C(AbilityConditionKind.OwnerHasBuff, shelterState)));
        P("InnerStrength", "내공", "공격형 초식 피해 +10%", CharacterKind.Monk, 20, Passive(Rule(AbilityRule.WeaponDamageBonus, .1f), offensive));
        P("ElementFocus", "원소 집중", "직전에 시전한 주문과 같은 속성의 공격 주문을 시전하면 피해가 15% 증가한다.", CharacterKind.Wizard, 21, Passive(Rule(AbilityRule.SpellDamageBonus, .15f), C(AbilityConditionKind.PreviousSpellElementSame)));
        P("ThreeElements", "삼원 공명", "서로 다른 세 속성의 주문을 연속 시전하면 MP 4를 회복한다.", CharacterKind.Wizard, 22, U(TriggerType.OnSpellCast, TargetType.Owner, E(EffectType.RestoreMp, 4), C(AbilityConditionKind.NextDistinctElements, value: 3)));
        P("ManaBurn", "마력 연소", "주문 사용 후 MP가 20% 이하이면 해당 주문의 피해가 25% 증가한다.", CharacterKind.Wizard, 23, Passive(Rule(AbilityRule.SpellDamageBonus, .25f), C(AbilityConditionKind.MpFraction, value: .2f, compare: Comparator.LessOrEqual)));
        var enchant = Tag("WeaponEnchantment");
        // Three data recipes share the same accumulation primitive; their element is authored.
        var battlemage = P("Spellblade", "마검 방출", "인챈트된 일반공격을 적중시킬 때마다 다음 차지 공격에 해당 속성 피해 4를 추가한다.", CharacterKind.Wizard, 24);
        foreach (SpellElement element in Enum.GetValues(typeof(SpellElement)))
        {
            var elementTag = Tag("Enchant" + element); var ready = Ready("Spellblade" + element, "마검 방출", 4, TriggerAttackTypeFilter.ChargeAttack, element); ready.DuplicationData.Type = BuffDuplicationType.StackCount; EditorUtility.SetDirty(ready);
            battlemage.abilitySource.abilities.Add(Filter(U(TriggerType.OnHit, TargetType.Owner, Add(ready), new AbilityCondition { kind = AbilityConditionKind.OwnerHasBuff, buffTag = elementTag }), TriggerAttackTypeFilter.NormalAttack));
        }
        EditorUtility.SetDirty(battlemage.abilitySource);
        P("ShortIncantation", "축약 영창", "주문 시전 시간 -5%", CharacterKind.Wizard, 25, Passive(Rule(AbilityRule.CastTimeReduction, .05f)));
        var fury = Existing("Fury"); var rage = Existing("Rage");
        P("BoilingBlood", "끓는 피", "일반공격으로 얻는 격노 +2", CharacterKind.Barbarian, 26, Filter(U(TriggerType.OnActionPreparing, TargetType.Owner, Add(fury, 2), new AbilityCondition { kind = AbilityConditionKind.OwnerHasBuff, buff = rage, invert = true }), TriggerAttackTypeFilter.NormalAttack));
        var conversion = Variable(E(EffectType.Damage), EffectScale.ActionStartBuffStacks, .1f, fury);
        P("ConversionImpact", "전환 충격", "분노에 진입하는 차지 공격에 전환한 격노 10당 물리 피해 1을 추가한다.", CharacterKind.Barbarian, 27, Filter(U(TriggerType.OnHit, TargetType.Enemy, conversion, C(AbilityConditionKind.OwnerBuffElapsed, rage, .1f, Comparator.LessOrEqual)), TriggerAttackTypeFilter.ChargeAttack));
        P("FrenziedFist", "광폭한 주먹", "분노 중 일반공격에 물리 피해 3을 추가한다.", CharacterKind.Barbarian, 28, Filter(U(TriggerType.OnHit, TargetType.Enemy, E(EffectType.Damage, 3), C(AbilityConditionKind.OwnerHasBuff, rage)), TriggerAttackTypeFilter.NormalAttack));
        P("RageWeight", "분노의 무게", "분노 중 차지 공격에 남은 분노의 10%만큼 물리 피해를 추가한다.", CharacterKind.Barbarian, 29, Filter(U(TriggerType.OnHit, TargetType.Enemy, Variable(E(EffectType.Damage), EffectScale.BuffStacks, .1f, rage), C(AbilityConditionKind.OwnerHasBuff, rage)), TriggerAttackTypeFilter.ChargeAttack));
        P("Embers", "잔불", "분노가 끝나면 격노를 4 얻는다.", CharacterKind.Barbarian, 30, U(TriggerType.OnBuffEnd, TargetType.Owner, Add(fury, 4), C(AbilityConditionKind.ContextBuffIs, rage)));
    }
    private static SpellData Spell(string id, string title, string text, SpellElement element, SpellShape shape, SwipeDirection slot, float mp, float cooldown, float damage, int tier = 0)
    {
        var spell = Asset<SpellData>(Root + "/Spells/" + id + ".asset");
        spell.id = id; spell.displayName = title; spell.description = text; spell.element = element; spell.shape = shape; spell.defaultSlot = slot;
        spell.mpCost = mp; spell.cooldownSeconds = cooldown; spell.damage = damage; spell.castingSeconds = tier >= 2 ? .6f : .4f; spell.openingCooldownSeconds = 0; spell.tier = tier; spell.abilities.Clear();
        if (!content.spells.Contains(spell)) content.spells.Add(spell); EditorUtility.SetDirty(spell); return spell;
    }
    private static void Book(SpellData spell)
    {
        var book = Asset<EquipmentData>(Root + "/Relics/Tome_" + spell.id + ".asset"); book.id = "Tome_" + spell.id; book.displayName = spell.displayName + "의 마도서"; book.description = spell.description; book.scope = RelicScope.Shared; book.price = 38;
        var learn = E(EffectType.LearnSpell); learn.spell = spell; book.abilities = new() { U(TriggerType.OnAcquire, TargetType.Owner, learn) };
        if (!content.equipment.Contains(book)) content.equipment.Add(book); EditorUtility.SetDirty(book);
    }
    private static void BuildSpells()
    {
        foreach (var spell in content.spells.Where(s => s != null)) { spell.mpCost = 8; spell.castingSeconds = .4f; spell.openingCooldownSeconds = 0; EditorUtility.SetDirty(spell); }
        var bolt = Spell("Ember", "파이어볼트", "화염탄을 날려 적을 관통한다.", SpellElement.Fire, SpellShape.Pierce, SwipeDirection.Right, 8, 4, 36); bolt.width = 25;
        var bloom = content.spells.FirstOrDefault(s => s.id == "FireBloom"); if (bloom != null) { bloom.displayName = "화염폭발"; bloom.mpCost = 12; bloom.tier = 1; EditorUtility.SetDirty(bloom); }
        var poisonMist = Spell("PoisonMist", "독안개", "10초 동안 독안개를 펼친다. 2초마다 독을 1중첩 부여하고 적의 행동을 늦춘다.", SpellElement.Ice, SpellShape.PersistentArea, SwipeDirection.Left, 10, 12, 0);
        poisonMist.duration = 10; poisonMist.pulseInterval = 2; poisonMist.radius = 210; poisonMist.slowSeconds = 2.2f; poisonMist.abilities.Add(U(TriggerType.OnSpellCast, TargetType.Enemy, Add(CombatStatusCatalog.Instance.poison))); Book(poisonMist);
        var sharedEnchant = Tag("WeaponEnchantment");
        foreach (SpellElement element in Enum.GetValues(typeof(SpellElement)))
        {
            var name = element == SpellElement.Fire ? "파이어" : element == SpellElement.Ice ? "아이스" : "라이트닝";
            var buff = Asset<BuffData>(Root + "/Buffs/AshenVow/Enchant" + element + ".asset"); buff.displayName = "인챈트 " + name; buff.showInHud = true; buff.Tags = new() { sharedEnchant, Tag("Enchant" + element) };
            buff.ReleaseData = new() { Type = BuffReleaseType.Time, Time = 10 }; buff.DuplicationData = new() { Type = BuffDuplicationType.UseNew, ConsiderTagIsDuplicate = sharedEnchant };
            var hit = E(EffectType.ElementDamage, 4); hit.element = element;
            buff.Units = new() { U(TriggerType.OnHit, TargetType.Enemy, hit) };
            if (element == SpellElement.Fire) buff.Units.Add(U(TriggerType.OnHit, TargetType.Enemy, Add(CombatStatusCatalog.Instance.burn)));
            if (element == SpellElement.Ice) buff.Units.Add(U(TriggerType.OnHit, TargetType.Enemy, Add(CombatStatusCatalog.Instance.slow)));
            if (element == SpellElement.Lightning) { var chain = E(EffectType.ElementDamage, 2); chain.element = element; chain.targets = AbilityTarget.OtherEnemies; buff.Units.Add(U(TriggerType.OnHit, TargetType.Enemy, chain)); }
            EditorUtility.SetDirty(buff);
            var text = element == SpellElement.Fire ? "10초 동안 무기 공격에 화염 피해 4와 화상을 추가한다." : element == SpellElement.Ice ? "10초 동안 무기 공격에 냉기 피해 4와 둔화를 추가한다." : "10초 동안 무기 공격에 번개 피해 4를 추가하고 주변 적에게 번개 피해 2를 준다.";
            var spell = Spell("Enchant" + element, "인챈트 " + name, text, element, SpellShape.Utility, SwipeDirection.Down, 8, 10, 0); spell.duration = 10; spell.abilities.Add(U(TriggerType.OnSpellCast, TargetType.Owner, Add(buff))); Book(spell);
        }
        var cleanse = Spell("Cleanse", "정화", "해로운 효과를 제거한다.", SpellElement.Ice, SpellShape.Utility, SwipeDirection.Up, 8, 10, 0); cleanse.abilities.Add(U(TriggerType.OnSpellCast, TargetType.Owner, E(EffectType.Cleanse))); Book(cleanse);
        var mark = Asset<BuffData>(Root + "/Buffs/AshenVow/LightningMark.asset"); mark.displayName = "벼락표식"; mark.ReleaseData = new() { Type = BuffReleaseType.Time, Time = 7 }; mark.DuplicationData = new() { Type = BuffDuplicationType.UseNew };
        var spread = E(EffectType.ElementDamage); spread.scale = EffectScale.DamageDealt; spread.scaleRatio = .3f; spread.element = SpellElement.Lightning; spread.targets = AbilityTarget.OtherEnemies;
        mark.Units = new() { U(TriggerType.OnHit, TargetType.Enemy, spread), U(TriggerType.OnHit, TargetType.ThisBuff, E(EffectType.RemoveBuff)) }; EditorUtility.SetDirty(mark);
        var lightningMark = Spell("LightningMark", "벼락표식", "7초 동안 적에게 표식을 남긴다. 다음 무기 공격 피해의 30%를 주변 적에게 번개로 전달한다.", SpellElement.Lightning, SpellShape.Utility, SwipeDirection.Left, 8, 8, 0); lightningMark.abilities.Add(U(TriggerType.OnSpellCast, TargetType.Enemy, Add(mark))); Book(lightningMark);
        var meteor = Spell("Meteor", "메테오", "거대한 운석을 떨어뜨려 넓은 범위에 화염 피해 120을 준다.", SpellElement.Fire, SpellShape.Burst, SwipeDirection.Left, 18, 20, 120, 2); meteor.radius = 270;
        var vortex = Spell("VortexLightning", "볼텍스 라이트닝", "6초 동안 번개의 소용돌이를 펼친다. 0.75초마다 번개 피해 14를 준다.", SpellElement.Lightning, SpellShape.PersistentArea, SwipeDirection.Left, 18, 20, 14, 2); vortex.duration = 6; vortex.pulseInterval = .75f; vortex.radius = 230;
        var shower = Spell("IcicleShower", "아이시클 샤워", "고드름을 6차례 쏟아 각각 냉기 피해 16을 주고 적의 행동을 늦춘다.", SpellElement.Ice, SpellShape.Rain, SwipeDirection.Left, 16, 18, 16, 2); shower.pulses = 6; shower.pulseInterval = .5f; shower.radius = 240; shower.slowSeconds = 1.5f;
        var inferno = Spell("CrimsonInferno", "홍염", "8초 동안 홍염을 펼친다. 매초 화염 피해 12를 주고 화상을 남긴다.", SpellElement.Fire, SpellShape.PersistentArea, SwipeDirection.Left, 18, 20, 12, 2); inferno.duration = 8; inferno.radius = 240; inferno.abilities.Add(U(TriggerType.OnSpellCast, TargetType.Enemy, Add(CombatStatusCatalog.Instance.burn)));
        var wizard = content.characters.First(c => c.character == CharacterKind.Wizard).startingRelic;
        var restore = U(TriggerType.OnTimer, TargetType.Owner, E(EffectType.RestoreMp, .09f), C(AbilityConditionKind.GuardHeld)); restore.trigger.intervalSeconds = .1f; wizard.abilities.Add(restore);
        wizard.description += "\n가드 중 MP가 초당 0.9 추가 회복된다."; EditorUtility.SetDirty(wizard);
        foreach (var status in new[] { CombatStatusCatalog.Instance.poison, CombatStatusCatalog.Instance.slow, CombatStatusCatalog.Instance.burn }) { status.harmful = true; EditorUtility.SetDirty(status); }
        BehemothTextEditor.Table.Set("ui.mana.insufficient", "MP 부족", "AshenVow");
        BehemothTextEditor.Table.Set("ui.mana.cost", " · MP {0:0}", "AshenVow");
        BehemothTextEditor.Table.Set("ui.mana.gauge", "MP {0:0}/{1:0}", "AshenVow");
    }
}
