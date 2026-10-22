using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// One-time, non-destructive migration. The Power enum is only an old asset lookup here.
// Gameplay evaluates the resulting editable abilities; it never dispatches item identities.
internal static partial class BehemothContentBuilder
{
    [InitializeOnLoadMethod] private static void ScheduleAbilityRevision() => EditorApplication.delayCall += AutoAbilityRevision;
    private static void AutoAbilityRevision()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += AutoAbilityRevision; return; }
        var data = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
        if (data == null || data.schemaVersion < 4 || data.schemaVersion >= 5) return;
        try { ApplyAbilityRevision(); } catch (Exception error) { Debug.LogException(error); }
    }
    internal static void ApplyAbilityRevision()
    {
        content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
        relics.Clear(); encounters.Clear();
        foreach (var item in content.equipment.Where(item => item != null && item.power != RelicPower.None)) relics[item.power] = item;
        foreach (var guid in AssetDatabase.FindAssets("t:EncounterData", new[] { Root }))
        { var item = AssetDatabase.LoadAssetAtPath<EncounterData>(AssetDatabase.GUIDToAssetPath(guid)); encounters[item.id] = item; }
        var dog = BuildDog();
        foreach (var item in content.equipment)
        {
            item.abilities.Clear();
            if (item.power != RelicPower.None) continue;
            var spell = item.grantedSpell;
            var old = item.effect;
            if (old.effectType == EffectType.Damage && !item.IsPotion) { old.scale = EffectScale.NormalWeapon; old.scaleRatio = old.effectMagnitude; old.effectMagnitude = 0; }
            var ability = new EquipmentAbility { trigger = item.trigger, target = item.targetType, effects = new() { old } };
            ability.trigger.resetCountEachCombat = true;
            if (item.condition.conditionType == ConditionType.EnemyStun) ability.conditions.Add(When(AbilityConditionKind.TargetStunned));
            if (item.condition.conditionType == ConditionType.OwnerCharge) ability.trigger.triggerAttackTypeFilter = TriggerAttackTypeFilter.ChargeAttack;
            item.abilities.Add(ability);
            foreach (var mod in item.additionalModifiers) Add(item, TriggerType.OnAcquire, TargetType.Owner, Stat(mod.kind, mod.value));
            if (spell != null) Add(item, TriggerType.OnAcquire, TargetType.Owner, Learn(spell));
            item.grantedSpell = null; item.additionalModifiers.Clear();
            EditorUtility.SetDirty(item);
        }
        EquipmentData R(RelicPower power) => relics[power];
        void Recipe(RelicPower power, string text, params EquipmentAbility[] abilities)
        { var item = R(power); item.abilities = abilities.ToList(); item.description = text; item.additionalModifiers.Clear(); item.grantedSpell = null; EditorUtility.SetDirty(item); }
        EquipmentAbility Passive(AbilityRule rule, float value, params AbilityCondition[] conditions) => Ability(TriggerType.Passive, TargetType.Owner, new[] { RuleEffect(rule, value) }, conditions);
        EquipmentAbility Hit(EquipmentEffect effect, TriggerAttackTypeFilter filter = TriggerAttackTypeFilter.All, params AbilityCondition[] conditions)
        { var result = Ability(TriggerType.OnHit, TargetType.Enemy, new[] { effect }, conditions); result.trigger.triggerAttackTypeFilter = filter; return result; }
        EquipmentAbility Acquire(params EquipmentEffect[] effects) => Ability(TriggerType.OnAcquire, TargetType.Owner, effects);
        SpellData S(string id) => content.spells.Single(item => item.id == id);
        FormData F(string id) => AssetDatabase.LoadAssetAtPath<FormData>(Root + "/Forms/" + id + ".asset");
        var enraged = When(AbilityConditionKind.Enraged);
        var stunned = When(AbilityConditionKind.TargetStunned);
        var horizontal = When(AbilityConditionKind.Horizontal);
        var drawn = When(AbilityConditionKind.ActionHasTag);
        var formUsed = When(AbilityConditionKind.HasForm);

        Recipe(RelicPower.ShieldBash, "가드로 소모한 게이지의 50%를 저장하여 다음 무기 적중에 더한다.", Ability(TriggerType.OnGuard, TargetType.Owner, new[] { Scaled(EffectType.StoreGuardDamage, EffectScale.GuardDamage, .5f) }));
        var perfect = Ability(TriggerType.OnPerfectGuard, TargetType.Owner, new[] { Stat(ModifierKind.AllDamageMultiplier, .3f, 10, group: "perfect") });
        Recipe(RelicPower.PerfectMending, "가드 시작 0.1초에 퍼펙트가드 가능. 성공 시 게이지를 쓰지 않고 10초 동안 피해 +30%. 갱신되며 중첩되지 않는다.", Passive(AbilityRule.PerfectGuardWindow, .1f), perfect);
        var hammer = Hit(E(EffectType.Interrupt, .55f)); hammer.trigger.triggerAttackWayFilter = TriggerAttackWayFilter.Downward;
        Recipe(RelicPower.Hammer, "내려치기 적중 시 일반공격 준비를 0.55초 지연한다.", hammer);
        var up = When(AbilityConditionKind.DirectionIs); up.direction = SwipeDirection.Up;
        var notUp = When(AbilityConditionKind.DirectionIsNot); notUp.direction = SwipeDirection.Up;
        Recipe(RelicPower.Spear, "올려치기 피해 +35%, 공격 쿨타임 -50%. 다른 방향 피해 -20%, 공격 쿨타임 +25%.", Passive(AbilityRule.WeaponDamageBonus, .35f, up), Passive(AbilityRule.AttackCooldownReduction, .5f, up), Passive(AbilityRule.WeaponDamageBonus, -.2f, notUp), Passive(AbilityRule.AttackCooldownReduction, -.25f, notUp));
        var edge = Passive(AbilityRule.FlatWeaponDamage, 0); var edgeFx = edge.effects[0]; edgeFx.scale = EffectScale.GuardCurrent; edgeFx.scaleRatio = .15f; edge.effects[0] = edgeFx;
        Recipe(RelicPower.ShieldEdge, "무기 적중에 현재 가드게이지의 15%를 추가한다.", edge);
        var nova = Ability(TriggerType.OnGuard, TargetType.Enemy, new[] { Scaled(EffectType.Damage, EffectScale.GuardMaximum, .4f, targets: AbilityTarget.AllEnemies) }); nova.trigger.triggerCount = 3;
        Recipe(RelicPower.GuardNova, "가드 3회마다 모든 적에게 최대 가드게이지의 40% 피해.", nova);
        Recipe(RelicPower.Paladin, "잿불 화살 습득. 가드 성공 시 주문의 남은 쿨타임 -1초.", Acquire(Learn(S("Ember"))), Ability(TriggerType.OnGuard, TargetType.Owner, new[] { E(EffectType.ReduceSpellCooldown, 1) }));
        Recipe(RelicPower.IronFortress, "최대 가드게이지 +25, 전투 중 자연회복 초당 +2.", Acquire(Stat(ModifierKind.ShieldMax, 25), Stat(ModifierKind.NormalShieldRegen, 2)));
        Recipe(RelicPower.ViperFang, "차지 적중 시 독을 전부 소모해 남은 중첩당 독 피해의 2배를 즉시 준다.", Hit(E(EffectType.DetonatePoison, 2), TriggerAttackTypeFilter.ChargeAttack));
        Recipe(RelicPower.TwinDaggers, "좌·우 일반공격을 연속하면 공격 쿨타임이 4%씩 감소, 최대 40%. 상·하 공격으로 초기화.", Passive(AbilityRule.HorizontalCooldownPerHit, .04f), Passive(AbilityRule.HorizontalCooldownCap, .4f));
        var spread = E(EffectType.SpreadStatus, 2, 8); spread.scale = EffectScale.PoisonStacks; spread.scaleRatio = .5f; spread.statusDamage = 1; spread.statusCap = 8;
        Recipe(RelicPower.VenomSpread, "처치한 적의 독 절반에 2중첩을 더해 다른 적에게 전파한다. 독 피해는 중첩당 1, 최대 8중첩.", Ability(TriggerType.OnKill, TargetType.Enemy, new[] { spread }));
        Recipe(RelicPower.FirstStrike, "독 부여를 중단한다. 일반 피해 +25%. 각 적에게 첫 무기 적중 피해 +250%.", Acquire(Stat(ModifierKind.NormalDamage, .25f)), Passive(AbilityRule.BlockBuff, 1), Passive(AbilityRule.WeaponDamageBonus, 2.5f, When(AbilityConditionKind.FirstWeaponHit)));
        Recipe(RelicPower.PoisonLeech, "독 피해의 12%만큼 체력을 회복한다.", Ability(TriggerType.OnPoisonTick, TargetType.Owner, new[] { Scaled(EffectType.Heal, EffectScale.DamageDealt, .12f) }));
        Recipe(RelicPower.Executioner, "기절한 대상에게 무기 피해 +35%.", Passive(AbilityRule.WeaponDamageBonus, .35f, stunned));
        Recipe(RelicPower.DeadlyDose, "일반공격이 독을 1중첩 더 부여한다. 독 피해 1, 8초 지속, 최대 8중첩.", Hit(Poison(), TriggerAttackTypeFilter.NormalAttack));
        R(RelicPower.ShadowCounter).displayName = "독을 읽는 주문서";
        Recipe(RelicPower.ShadowCounter, "스파크 습득. 가드 성공 시 주문 쿨타임 -0.5초. 주문 발동 시 대상에게 독 2중첩.", Acquire(Learn(S("Spark"))), Ability(TriggerType.OnGuard, TargetType.Owner, new[] { E(EffectType.ReduceSpellCooldown, .5f) }), Ability(TriggerType.OnSpellCast, TargetType.Enemy, new[] { Poison(2) }));

        Recipe(RelicPower.DeferredPain, "분노 중 체력 피해를 5초에 나누어 받는다. 분노 종료 시 남은 피해를 받고, 전투 승리 시 사라진다.", Passive(AbilityRule.DeferredDamageSeconds, 5, enraged));
        Recipe(RelicPower.RageLeech, "분노 중 직접 무기 피해의 10%만큼 체력을 회복한다.", Ability(TriggerType.OnHit, TargetType.Owner, new[] { Scaled(EffectType.Heal, EffectScale.DamageDealt, .1f) }, enraged));
        var sustain = Ability(TriggerType.OnAttack, TargetType.Owner, new[] { E(EffectType.AddResource, 6) }, enraged); sustain.trigger.triggerAttackTypeFilter = TriggerAttackTypeFilter.NormalAttack;
        Recipe(RelicPower.LongRage, "분노 소모가 초당 8 감소한다. 분노 중 일반공격 시 격노 +6.", Passive(AbilityRule.ResourceDrainReduction, 8), sustain);
        var rageHit = Passive(AbilityRule.ChargeDamageBonus, 0, enraged); var rageFx = rageHit.effects[0]; rageFx.scale = EffectScale.Rage; rageFx.scaleRatio = .01f; rageHit.effects[0] = rageFx;
        var rageCost = Ability(TriggerType.OnAttackResolved, TargetType.Owner, new[] { E(EffectType.AddResource, -35) }, enraged); rageCost.trigger.triggerAttackTypeFilter = TriggerAttackTypeFilter.ChargeAttack;
        Recipe(RelicPower.RageCharge, "분노 중 차지 피해가 현재 격노 1당 1% 증가한다. 해당 차지 후 격노 35 소모.", rageHit, rageCost);
        Recipe(RelicPower.ExecuteRage, "분노 중 처치 시 격노 +30, 체력 +3.", Ability(TriggerType.OnKill, TargetType.Owner, new[] { E(EffectType.AddResource, 30), E(EffectType.Heal, 3) }, enraged));
        Recipe(RelicPower.Unstoppable, "차지는 방향 방벽을 무시한다. 차지 피해 +20%.", Passive(AbilityRule.IgnoreDirectionalGuard, 1), Acquire(Stat(ModifierKind.ChargeDamage, .2f)));
        R(RelicPower.BloodFrenzy).displayName = "피로 지핀 불꽃";
        var fire = When(AbilityConditionKind.ElementIs); fire.element = SpellElement.Fire;
        Recipe(RelicPower.BloodFrenzy, "잿불 화살 습득. 화염 주문 발동 시 격노 +15. 분노 중 화염 주문 피해 +30%.", Acquire(Learn(S("Ember"))), Ability(TriggerType.OnSpellCast, TargetType.Owner, new[] { E(EffectType.AddResource, 15) }, fire), Passive(AbilityRule.SpellDamageBonus, .3f, enraged, fire));
        var thunder = E(EffectType.Damage, 8); thunder.element = SpellElement.Lightning;
        var lightning = When(AbilityConditionKind.ElementIs); lightning.element = SpellElement.Lightning;
        Recipe(RelicPower.ThunderousCharge, "스파크 습득. 차지 적중 시 번개 피해 8 추가. 번개 주문 발동 시 가드게이지 +2.", Acquire(Learn(S("Spark"))), Hit(thunder, TriggerAttackTypeFilter.ChargeAttack), Ability(TriggerType.OnSpellCast, TargetType.Owner, new[] { E(EffectType.RestoreGuard, 2) }, lightning));

        Recipe(RelicPower.DrawMastery, "발도 피해 보정이 +50%에서 +85%로 증가.", Passive(AbilityRule.ActionDamageBonus, .35f));
        var patience = Passive(AbilityRule.ActionDamageBonus, 0); var waitFx = patience.effects[0]; waitFx.scale = EffectScale.SheathTime; waitFx.scaleRatio = .3f; waitFx.maximumMagnitude = 1; patience.effects[0] = waitFx;
        Recipe(RelicPower.PatientSheath, "납도 유지 1초당 발도 피해 +30%, 최대 +100%.", patience);
        Recipe(RelicPower.PerfectRiposte, "납도패리 성공 시 공격한 적에게 차지 공격력의 50% 피해.", Ability(TriggerType.OnSheathParry, TargetType.Enemy, new[] { Scaled(EffectType.Damage, EffectScale.ChargeWeapon, .5f) }));
        Recipe(RelicPower.BloodlessDraw, "발도 적중 시 체력 2 회복.", Ability(TriggerType.OnHit, TargetType.Owner, new[] { E(EffectType.Heal, 2) }, drawn));
        Recipe(RelicPower.DoubleDraw, "발도가 한 번 더 적중한다. 약점에도 추가로 적중한다.", Passive(AbilityRule.ActionExtraHits, 1));
        var nextDraw = RuleEffect(AbilityRule.ActionDamageBonus, .4f, uses: 1, consume: TriggerType.OnAttack, group: "spell-draw");
        Recipe(RelicPower.SpellBlade, "에너지 볼트 습득. 납도 중 캐스팅 시간 -25%. 주문 발동 후 다음 발도 피해 +40%.", Acquire(Learn(S("EnergyBolt"))), Passive(AbilityRule.CastTimeReduction, .25f, When(AbilityConditionKind.Sheathed)), Ability(TriggerType.OnSpellCast, TargetType.Owner, new[] { nextDraw }));
        Recipe(RelicPower.ParryRecovery, "납도패리 성공 시 공격 쿨타임 즉시 초기화.", Ability(TriggerType.OnSheathParry, TargetType.Owner, new[] { E(EffectType.ResetAttackCooldown, 0) }));
        Recipe(RelicPower.ShadowSheath, "납도패리 판정 시간이 0.1초 늘어난다.", Passive(AbilityRule.BuffDurationBonus, .1f));

        var shelter = When(AbilityConditionKind.FormIs); shelter.form = F("Shelter");
        var multi = When(AbilityConditionKind.FormIs); multi.form = F("Multi");
        var flow = When(AbilityConditionKind.FormIs); flow.form = F("Flow");
        Recipe(RelicPower.ShelterMaster, "호신격 사용 시 최대 가드게이지의 30%를 추가 회복.", Passive(AbilityRule.FormGuardFraction, .3f, shelter));
        Recipe(RelicPower.ComboLeech, "초식 마무리 사용 시 체력 2 회복.", Ability(TriggerType.OnFormFinish, TargetType.Owner, new[] { E(EffectType.Heal, 2) }));
        Recipe(RelicPower.HundredFists, "백렬권이 2회 더 적중한다. 약점도 추가 타격한다.", Passive(AbilityRule.FormExtraHits, 2, multi));
        Recipe(RelicPower.PressurePoint, "기절한 대상에게 초식 마무리 피해 +35%.", Passive(AbilityRule.WeaponDamageBonus, .35f, formUsed, stunned));
        Recipe(RelicPower.FlowMaster, "연환의 가속을 2회 더 사용할 수 있으며 쿨타임 감소가 40%에서 60%가 된다.", Passive(AbilityRule.BuffExtraUses, 2, flow), Passive(AbilityRule.BuffPotencyBonus, .2f, flow));
        var tiger = E(EffectType.UnlockForm, 0); tiger.form = F("Tiger"); var dragon = E(EffectType.UnlockForm, 0); dragon.form = F("Dragon");
        Recipe(RelicPower.LongTiger, "↓↑↑ 후 차지로 맹호붕권 해금. 차지 피해 ×2.5. 긴 패턴이 우선한다.", Acquire(tiger));
        Recipe(RelicPower.LongDragon, "↑↓→ 후 차지로 유룡연타 해금. 차지 피해 ×0.6로 4회, 약점도 4회 타격.", Acquire(dragon));
        R(RelicPower.MindfulGuard).displayName = "주문을 잇는 호흡";
        Recipe(RelicPower.MindfulGuard, "스파크 습득. 초식 사용 시 주문 쿨타임 -1초. 주문 발동 시 가드게이지 +3.", Acquire(Learn(S("Spark"))), Ability(TriggerType.OnFormFinish, TargetType.Owner, new[] { E(EffectType.ReduceSpellCooldown, 1) }), Ability(TriggerType.OnSpellCast, TargetType.Owner, new[] { E(EffectType.RestoreGuard, 3) }));

        var ice = When(AbilityConditionKind.ElementIs); ice.element = SpellElement.Ice;
        Recipe(RelicPower.EmberMaster, "잿불 화살 습득. 화염 주문 피해 +30%.", Acquire(Learn(S("Ember"))), Passive(AbilityRule.SpellDamageBonus, .3f, fire));
        Recipe(RelicPower.IceFocus, "서리 파동 습득. 냉기 주문 피해 +20%, 둔화 지속시간 +0.5초.", Acquire(Learn(S("FrostRing"))), Passive(AbilityRule.SpellDamageBonus, .2f, ice), Passive(AbilityRule.SlowDurationBonus, .5f));
        var rod = E(EffectType.ApplyStatus, 1, 8); rod.status = StatusKind.LightningRod;
        Recipe(RelicPower.LightningRod, "스파크 습득. 무기 적중 대상에게 8초 동안 피뢰침을 남겨 번개가 우선한다.", Acquire(Learn(S("Spark"))), Hit(rod));
        var echo = Ability(TriggerType.OnSpellCast, TargetType.Enemy, new[] { E(EffectType.EchoSpell, .5f, .22f) }); echo.trigger.triggerCount = 3;
        Recipe(RelicPower.SpellEcho, "세 번째 주문마다 0.22초 후 같은 주문을 50% 피해로 한 번 더 발동한다.", echo);
        Recipe(RelicPower.QuickCasting, "주문 캐스팅 시간 -35%.", Passive(AbilityRule.CastTimeReduction, .35f));
        Recipe(RelicPower.ChainMaster, "연쇄 번개 습득. 연쇄 대상 수 +2.", Acquire(Learn(S("ChainLightning"))), Passive(AbilityRule.SpellChainTargets, 2));
        Recipe(RelicPower.ArcaneGuard, "주문 발동 시 가드게이지 +4.", Ability(TriggerType.OnSpellCast, TargetType.Owner, new[] { E(EffectType.RestoreGuard, 4) }));
        var conduit = Ability(TriggerType.OnHit, TargetType.Owner, new[] { RuleEffect(AbilityRule.CastTimeReduction, .6f, uses: 1, consume: TriggerType.OnSpellCast, group: "staff-cast"), RuleEffect(AbilityRule.SpellDamageBonus, .2f, uses: 1, consume: TriggerType.OnSpellCast, group: "staff-damage") }); conduit.trigger.triggerAttackTypeFilter = TriggerAttackTypeFilter.ChargeAttack;
        Recipe(RelicPower.StaffConduit, "일반 무기 피해 +15%. 지팡이 차지 적중 후 다음 주문은 캐스팅 시간 -60%, 피해 +20%. 약점 파괴 시 가드게이지 +3.", Acquire(Stat(ModifierKind.NormalDamage, .15f)), conduit, Ability(TriggerType.OnWeakPointBreak, TargetType.Owner, new[] { E(EffectType.RestoreGuard, 3) }));

        var summon = E(EffectType.SummonCompanion, 0); summon.companion = dog;
        var bite = Scaled(EffectType.Damage, EffectScale.Level, 1, 7, AbilityTarget.LowestHp);
        var companionAttack = Ability(TriggerType.OnTimer, TargetType.Enemy, new[] { bite }); companionAttack.trigger.intervalSeconds = 4;
        Recipe(RelicPower.StrayDog, "강아지가 곁에서 동행한다. 4초마다 체력이 가장 낮은 적에게 7 + 현재 레벨만큼 피해.", Acquire(summon), companionAttack);
        var wisp = E(EffectType.Damage, 6);
        Recipe(RelicPower.GuidingWisp, "차지 적중과 주문 발동에 위습이 피해 6을 더한다.", Hit(wisp, TriggerAttackTypeFilter.ChargeAttack), Ability(TriggerType.OnSpellCast, TargetType.Enemy, new[] { wisp }));
        Recipe(RelicPower.Parasite, "일반 무기 피해 +25%, 차지 피해 +20%.", Acquire(Stat(ModifierKind.NormalDamage, .25f), Stat(ModifierKind.ChargeDamage, .2f)));
        var mirror = Hit(Scaled(EffectType.Damage, EffectScale.DamageDealt, .4f)); mirror.trigger.triggerCount = 4;
        Recipe(RelicPower.MirrorShard, "매 4번째 무기 적중에 해당 피해의 40%를 더한다.", mirror);
        Recipe(RelicPower.BloodContract, "모든 피해 +25%.", Acquire(Stat(ModifierKind.AllDamageMultiplier, .25f)));
        var oath = Ability(TriggerType.OnGuard, TargetType.Owner, new[] { E(EffectType.RestoreGuard, 3) }); oath.trigger.triggerCount = 3;
        Recipe(RelicPower.PilgrimOath, "가드 3회마다 가드게이지 +3. 순례자의 인연을 증명한다.", oath);
        Recipe(RelicPower.Hourglass, "차지 준비시간과 주문 쿨타임 -12%.", Acquire(Stat(ModifierKind.ChargeDurationMultiplier, .12f)), Passive(AbilityRule.SpellCooldownReduction, .12f));
        Recipe(RelicPower.StormBottle, "차지 적중 시 번개 피해 8 추가.", Hit(thunder, TriggerAttackTypeFilter.ChargeAttack));
        var seed = Ability(TriggerType.OnCombatWon, TargetType.Owner, new[] { Stat(ModifierKind.MaxHp, 4) }); seed.trigger.triggerCount = 3; seed.trigger.resetCountEachCombat = false;
        Recipe(RelicPower.AmberSeed, "전투 3승마다 최대·현재 체력 +4.", seed);
        Recipe(RelicPower.GuardianBell, "최대 가드게이지 +10. 전투 시작 3초간 받는 체력 피해 -20%.", Acquire(Stat(ModifierKind.ShieldMax, 10)), Ability(TriggerType.OnCombatStart, TargetType.Owner, new[] { RuleEffect(AbilityRule.DamageReduction, .2f, time: 3) }));
        var banner = Passive(AbilityRule.WeaponDamageBonus, 0); var bannerFx = banner.effects[0]; bannerFx.scale = EffectScale.EnemyCount; bannerFx.scaleRatio = .08f; banner.effects[0] = bannerFx;
        Recipe(RelicPower.RaggedBanner, "살아 있는 적 하나당 무기 피해 +8%.", banner);
        Recipe(RelicPower.GoldenTooth, "전투 승리마다 골드 +10.", Ability(TriggerType.OnCombatWon, TargetType.Owner, new[] { Stat(ModifierKind.Gold, 10) }));

        TuneCharactersAndForms();
        foreach (var spell in content.spells)
        {
            spell.damage = spell.id switch { "Ember" => 17, "EnergyBolt" => 17, "Spark" => 13, "FireBloom" => 20, "FrostRing" => 12, _ => 14 };
            spell.cooldownSeconds = spell.id switch { "Ember" => 6, "EnergyBolt" => 6, "Spark" => 5.5f, "FireBloom" => 10, "FrostRing" => 9, _ => 10 };
            spell.openingCooldownSeconds = 4; EditorUtility.SetDirty(spell);
        }
        MoveTriggeredPerks();
        content.stages.RemoveAll(stage => stage.act == 1 && stage.name != "Maze");
        RebuildReferenceEvents();
        content.schemaVersion = 5; EditorUtility.SetDirty(content);
        foreach (var item in content.equipment) { BehemothTextEditor.SyncAsset(item); EditorUtility.SetDirty(item); }
        BehemothTextEditor.SyncAll(); AssetDatabase.SaveAssets();
        Debug.Log("BEHEMOTH_ABILITY_REVISION_READY");
    }
    private static EquipmentAbility Ability(TriggerType trigger, TargetType target, IEnumerable<EquipmentEffect> effects, params AbilityCondition[] conditions)
        => new() { trigger = new EquipmentTrigger { chancePercent = 100,  triggerType = trigger, triggerCount = 1, resetCountEachCombat = true, triggerAttackTypeFilter = TriggerAttackTypeFilter.All, triggerAttackWayFilter = TriggerAttackWayFilter.All }, target = target, effects = effects.ToList(), conditions = conditions.ToList() };
    private static EquipmentAbility Add(EquipmentData item, TriggerType trigger, TargetType target, params EquipmentEffect[] effects)
    { var result = Ability(trigger, target, effects); item.abilities.Add(result); return result; }
    private static EquipmentEffect E(EffectType kind, float value, float time = 0) => new() { effectType = kind, effectMagnitude = value, effectCount = 1, effectTime = time };
    private static EquipmentEffect Scaled(EffectType kind, EffectScale scale, float ratio, float value = 0, AbilityTarget targets = AbilityTarget.ContextTarget)
    { var effect = E(kind, value); effect.scale = scale; effect.scaleRatio = ratio; effect.targets = targets; return effect; }
    private static EquipmentEffect Stat(ModifierKind kind, float value, float time = 0, string group = null)
    { var effect = E(EffectType.StatIncrease, value, time); effect.effectStatType = kind; effect.buffGroup = group; return effect; }
    private static EquipmentEffect RuleEffect(AbilityRule kind, float value, float time = 0, int uses = 0, TriggerType consume = TriggerType.OnAttack, string group = null)
    { var effect = E(EffectType.RuleModifier, value, time); effect.rule = kind; effect.buffUses = uses; effect.consumeOn = consume; effect.buffGroup = group; return effect; }
    private static EquipmentEffect Learn(SpellData spell) { var effect = E(EffectType.LearnSpell, 0); effect.spell = spell; return effect; }
    private static AbilityCondition When(AbilityConditionKind kind) => new() { kind = kind };
    private static EquipmentEffect Poison(int stacks = 1) { var effect = E(EffectType.ApplyStatus, stacks, 8); effect.status = StatusKind.Poison; effect.statusDamage = 1; effect.statusCap = 8; return effect; }
    private static void TuneCharactersAndForms()
    {
        foreach (var player in content.characters)
        {
            if (player.character == CharacterKind.Warrior) continue; // User-tuned reference is immutable in this migration.
            switch (player.character)
            {
                case CharacterKind.Rogue: player.baseAttackDamage = 5.5f; player.baseChargeDamage = 17; player.attackCooldown = .48f; player.chargeDuration = .65f;
                    BehemothStealthRevision.Configure(player.startingRelic); break;
                case CharacterKind.Barbarian: player.baseAttackDamage = 14.5f; player.baseChargeDamage = 31; player.attackCooldown = 1.05f; player.chargeDuration = .95f;
                    player.startingRelic.description = "일반공격 격노 +12. 40 이상에서 차지하면 분노 100. 분노 중 무기 피해 +30%, 공격 쿨타임 -20%, 받는 체력 피해 -25%. 초당 격노 25 소모. 분노 중 일반공격은 격노를 채우지 않는다."; break;
                case CharacterKind.Samurai: player.baseAttackDamage = 9; player.baseChargeDamage = 20; player.attackCooldown = .82f; player.chargeDuration = .75f; break;
                case CharacterKind.Monk: player.baseAttackDamage = 7.5f; player.baseChargeDamage = 13; player.attackCooldown = .55f; player.chargeDuration = .75f;
                    Add(player.startingRelic, TriggerType.OnFormFinish, TargetType.Owner, E(EffectType.RestoreGuard, 2));
                    player.startingRelic.description = "마지막 일반공격 두 방향 → 방향 무관 차지, 사용 시 기록 초기화. 가드해도 연계 유지. 초식 사용 시 가드 +2.\n↑↓ 붕권: 차지 ×1.7.\n↑↑ 백렬권: 차지 ×0.55로 3회, 약점도 3회.\n↓→ 호신격: 차지 ×1, 가드 +3, 다음 체력 피해 -50%.\n→↑ 연환: 차지 ×1, 다음 일반공격 2회 쿨타임 -40%."; break;
                case CharacterKind.Wizard: player.baseAttackDamage = 13.5f; player.baseChargeDamage = 18; player.attackCooldown = .85f; player.chargeDuration = .8f; player.shieldMax = 20;
                    Add(player.startingRelic, TriggerType.OnSpellCast, TargetType.Owner, E(EffectType.RestoreGuard, 2));
                    player.startingRelic.description = "에너지 볼트로 시작. 주문 쿨타임 -25%. 주문 발동 시 가드 +2. 주문을 준비하며 가드하면 초당 가드 +0.75. 지팡이로 약점을 깨고 주문과 무기 공격을 번갈아 사용한다."; break;
            }
            EditorUtility.SetDirty(player); EditorUtility.SetDirty(player.startingRelic);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:FormData", new[] { Root }))
        {
            var form = AssetDatabase.LoadAssetAtPath<FormData>(AssetDatabase.GUIDToAssetPath(guid));
            form.requiresUnlock = form.pattern.Length > 2; form.effects.Clear();
            form.damageMultiplier = form.name switch { "Burst" => 1.7f, "Multi" => .55f, "Tiger" => 2.5f, "Dragon" => .6f, _ => 1f };
            if (form.name == "Shelter") { var shield = RuleEffect(AbilityRule.DamageReduction, .5f, uses: 1, consume: TriggerType.OnHurt, group: "shelter"); form.effects.Add(shield); form.effects.Add(E(EffectType.RestoreGuard, 3)); }
            if (form.name == "Flow") { var flow = RuleEffect(AbilityRule.AttackCooldownReduction, .4f, uses: 2, group: "flow"); flow.consumeAttackTypes = TriggerAttackTypeFilter.NormalAttack; form.effects.Add(flow); }
            EditorUtility.SetDirty(form);
        }
    }
    private static void MoveTriggeredPerks()
    {
        var triggered = new[] { ModifierKind.ShieldOnHit, ModifierKind.ShieldOnWeakPoint, ModifierKind.MonsterKillHeal };
        foreach (var perk in content.perks.ToArray())
        {
            if (!perk.modifiers.Any(mod => triggered.Contains(mod.kind))) continue;
            var item = Asset<EquipmentData>(Root + "/Relics/Technique_" + perk.id + ".asset");
            item.id = "Technique_" + perk.id; item.displayName = perk.displayName; item.description = perk.description; item.icon = perk.icon; item.scope = RelicScope.Shared; item.price = 65; item.abilities.Clear();
            foreach (var modifier in perk.modifiers)
            {
                var trigger = modifier.kind == ModifierKind.ShieldOnHit ? TriggerType.OnHit : modifier.kind == ModifierKind.ShieldOnWeakPoint ? TriggerType.OnWeakPointBreak : TriggerType.OnKill;
                Add(item, trigger, TargetType.Owner, E(modifier.kind == ModifierKind.MonsterKillHeal ? EffectType.Heal : EffectType.RestoreGuard, modifier.value));
            }
            content.equipment.Add(item); content.perks.Remove(perk); EditorUtility.SetDirty(item);
        }
    }
    private static CompanionData BuildDog()
    {
        Directory.CreateDirectory("Assets/Prefabs/Companions"); Directory.CreateDirectory(Root + "/Companions"); AssetDatabase.Refresh();
        var data = Asset<CompanionData>(Root + "/Companions/TrailDog.asset"); data.displayName = "길동무";
        var root = new GameObject("TrailDog", typeof(RectTransform), typeof(CompanionView)); var view = root.GetComponent<CompanionView>();
        RectTransform Part(string name, Transform parent, Vector2 position, Vector2 size, Color color, float angle = 0)
        { var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position; rect.localRotation = Quaternion.Euler(0, 0, angle); go.GetComponent<Image>().color = color; go.GetComponent<Image>().raycastTarget = false; return rect; }
        Part("Shadow", root.transform, new(0, -20), new(64, 7), new(0, 0, 0, .3f));
        view.body = Part("Body", root.transform, new(0, 0), new(45, 28), new(.72f, .43f, .21f));
        Part("BackPaw", view.body, new(-13, -17), new(10, 14), new(.5f, .28f, .14f)); Part("FrontPaw", view.body, new(15, -17), new(10, 14), new(.78f, .5f, .26f));
        Part("Chest", view.body, new(13, -1), new(13, 23), new(.95f, .85f, .65f));
        var head = Part("Head", view.body, new(23, 17), new(29, 29), new(.86f, .58f, .3f));
        Part("Ear", head, new(-10, 9), new(12, 19), new(.43f, .24f, .13f), 24); Part("Muzzle", head, new(12, -6), new(15, 13), new(.97f, .87f, .7f));
        Part("Eye", head, new(7, 4), new(4, 5), new(.08f, .07f, .06f)); Part("Nose", head, new(19, -3), new(5, 6), new(.12f, .09f, .07f));
        Part("Collar", head, new(0, -14), new(23, 5), new(.2f, .65f, .68f));
        view.tail = Part("Tail", view.body, new(-29, 8), new(24, 8), new(.72f, .43f, .21f), -35);
        ((RectTransform)root.transform).sizeDelta = new(80, 70);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Companions/TrailDog.prefab"); data.prefab = prefab.GetComponent<CompanionView>(); UnityEngine.Object.DestroyImmediate(root); EditorUtility.SetDirty(data); return data;
    }
}
