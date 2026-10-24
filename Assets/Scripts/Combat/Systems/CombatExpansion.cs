using System;
using System.Collections;
using System.Linq;
using UnityEngine;
namespace DungeonSlash
{
    public sealed partial class CombatController
    {
        [SerializeField] private GameContent content;
        [SerializeField] private RunExpansionView expansionView;
        private bool externalInputPaused;
        private bool potionInputBlocked;
        private int potionReleaseFrame = -1;
        public bool PotionInputBlocked { get => potionInputBlocked; set { if(potionInputBlocked&&!value)potionReleaseFrame=Time.frameCount; potionInputBlocked=value; } }
        public bool InputPaused { get => externalInputPaused || PotionInputBlocked || Time.frameCount==potionReleaseFrame; set => externalInputPaused = value; }
        public RunState Run => run;
        public System.Collections.Generic.IReadOnlyList<MonsterRuntime> Enemies => activeMonsters.Select(m => m.Runtime).ToArray();
        public GameContent Content => content;
        public float CastingSeconds => input != null ? input.CastingSeconds : 0;
        public string EncounterHint { get; private set; }
        private CharacterAttack currentAttack;
        private ActiveMonster lastStaffTarget;
        private float lastStaffDistance;
        private int encounterSerial;
        private System.Random spellRandom = new(7919);
        private System.Random criticalRandom = new(104729);
        private EncounterPlanner encounterPlanner;
        private int plannerSeed;
        private EncounterData pendingEncounter;
        private readonly System.Collections.Generic.HashSet<MonsterRuntime> reportedKills = new();
        private void ReportKill(MonsterRuntime monster, EquipmentTriggerContext context)
        { if (monster != null && !monster.IsAlive && reportedKills.Add(monster)){activeMonsters.FirstOrDefault(m=>m.Runtime==monster)?.View?.PlayDead();run.EmitEvent(TriggerType.OnKill, context);} }
        public string StageName(int act) { EnsurePlanner(); return encounterPlanner?.Stage(act)?.displayName ?? GameText.Format("ui.combatexpansion.674bdc24", act); }
        private void EnsurePlanner()
        {
            if (content != null && run != null && (encounterPlanner == null || plannerSeed != run.Seed))
            { plannerSeed = run.Seed; encounterPlanner = new EncounterPlanner(content, plannerSeed); trackedFloor = -1; }
        }
        public void PrepareFixedEncounter(RunState state, EncounterData encounter, int act)
        {
            pendingEncounter = encounter;
            PrepareEncounter(state, encounter.members.Select(member => member.monster).ToArray(), act);
        }
        public static SwipeDirection ResolveDirection(Vector2 vector) => Mathf.Abs(vector.x) >= Mathf.Abs(vector.y)
            ? vector.x >= 0 ? SwipeDirection.Right : SwipeDirection.Left : vector.y >= 0 ? SwipeDirection.Up : SwipeDirection.Down;

        private float ModifyDirectDamage(ActiveMonster target, AttackSegment segment, bool charged, float damage)
        {
            var context = BuildAttackContext(segment, charged ? TriggerAttackType.Charge : TriggerAttackType.Normal, charged, target);
            damage *= 1 + Player.Rule(AbilityRule.WeaponDamageBonus, context) + Player.Rule(charged ? AbilityRule.ChargeDamageBonus : AbilityRule.NormalDamageBonus, context);
            damage += Player.Rule(AbilityRule.FlatWeaponDamage, context);
            if (Player.StoredGuardDamage > 0) { damage += Player.StoredGuardDamage; Player.StoredGuardDamage = 0; }
            return damage;
        }
        private void OnExpandedDirectHit(ActiveMonster target, AttackSegment segment, bool charged, float damage)
        {
            var monster = target.Runtime;
            var distance = Vector2.Distance(segment.End, target.Position);
            if (distance <= lastStaffDistance) { lastStaffTarget = target; lastStaffDistance = distance; }
            monster.HasTakenDirectHit = true;
            var direction = ResolveDirection(segment.End - segment.Start);
            if (monster.Data.pattern == MonsterPattern.MirrorCounter && monster.LastDirectDirection == direction && monster.IsAlive && !monster.IsStunned && monster.CounterAt < 0)
            { monster.CounterAt = Player.CombatTime + .7f; target.View.ShowAttackName(GameText.Get("ui.combatexpansion.a49c9ee9"), .7f); }
            monster.LastDirectDirection = direction;
            if (monster.Data.pattern == MonsterPattern.BloodFury && monster.IsAlive && ++monster.FuryHits % 5 == 0)
            { monster.Hasten(.5f); target.View.ShowAttackName(GameText.Get("ui.combatexpansion.4b4fc6cb"), .65f); }
        }
        private void TickExpandedCombat(float dt)
        {
            foreach (var monster in activeMonsters)
            {
                var runtime = monster.Runtime;
                runtime.LivingSupports.Clear();
                foreach (var support in activeMonsters.Where(item => item.Runtime.IsAlive && !string.IsNullOrEmpty(item.Runtime.Data.supportId))) runtime.LivingSupports.Add(support.Runtime.Data.supportId);
                var poisonDamage = runtime.ConsumePoisonDamage();
                if (poisonDamage > 0)
                { ShowDamage(monster.Position, GameText.Format("ui.combatexpansion.b419b64", poisonDamage), new Color(.6f, 1f, .25f)); run.EmitEvent(TriggerType.OnPoisonTick, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, runtime, damage: poisonDamage)); }
                if (!runtime.IsAlive) { ReportKill(runtime, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, runtime)); continue; }
                if (runtime.IsUnaware) continue;
                if (runtime.Data.pattern == MonsterPattern.LinkedHeart && string.IsNullOrEmpty(runtime.Data.supportId))
                {
                    if (runtime.CurrentChargeAttack != null && !string.IsNullOrEmpty(runtime.CurrentChargeAttack.requiredSupportId) && !runtime.LivingSupports.Contains(runtime.CurrentChargeAttack.requiredSupportId))
                    { runtime.EndCharge(); ClearWeakPointViews(monster); monster.View.ShowAttackName(GameText.Get("ui.combatexpansion.a17c6ce1"), 1f); }
                    if (!runtime.HeartsBroken && !runtime.LivingSupports.Any())
                    { runtime.HeartsBroken = true; runtime.EnterStun(); ClearWeakPointViews(monster); monster.View.ShowAttackName(GameText.Get("ui.combatexpansion.32dafe8b"), 1.5f); }
                }
                if (runtime.Data.diesWithLeader && !activeMonsters.Any(item => item != monster && item.Runtime.IsAlive && !item.Runtime.Data.diesWithLeader)) { runtime.TakeDamage(runtime.CurrentHp); continue; }
                if (runtime.Data.pattern == MonsterPattern.Orbit)
                {
                    var angle = Player.CombatTime * runtime.Data.orbitSpeed + activeMonsters.IndexOf(monster) * Mathf.PI;
                    var next = sceneView.MonsterPosition + new Vector2(Mathf.Cos(angle) * runtime.Data.orbitRadius, Mathf.Sin(angle) * 65f);
                    runtime.MoveWeakPoints(next - monster.Position); monster.Position = next; monster.View.Root.anchoredPosition = next - sceneView.MonsterPosition;
                }
                if (runtime.CounterAt > 0 && Player.CombatTime >= runtime.CounterAt)
                {
                    runtime.CounterAt = -1;
                    if (!runtime.IsStunned) { var result = Player.ReceiveDamage(new DamageRequest(DamageType.MonsterAttack, 9f, 10f, true, runtime)); ShowPlayerDamage(result); ResolveDamageTriggers(result, monster); }
                }
            }
            ResolveQueuedAbilities(0); RefreshCompanions();
            expansionView?.RefreshCombat(this);
            if (activeMonsters.Count > 0 && activeMonsters.All(monster => !monster.Runtime.IsAlive)) Finish(true);
        }
        public bool TryCastSpell(SwipeDirection slot, float castingSeconds, AttackSegment? aim = null)
        {
            if (!IsActive || InputPaused || resolvingAttackSequence || !Player.CanNormalAttack()) return false;
            var spell = run.Slot(slot);
            if (spell == null) { ShowDamage(sceneView.PlayerPosition, GameText.Get("ui.combatexpansion.4520ed9e"), new Color(.7f, .75f, .8f)); return false; }
            var runtime = run.RuntimeFor(spell);
            if (runtime.Cooldown > 0) { ShowDamage(sceneView.PlayerPosition, GameText.Format("ui.combatexpansion.98b418ae", runtime.Cooldown), Color.gray); return false; }
            if (castingSeconds + .015f < spell.castingSeconds * Player.SpellCastingFor(spell)) { ShowDamage(sceneView.PlayerPosition, GameText.Get("ui.combatexpansion.411a9b44"), new Color(1f, .7f, .35f)); return false; }
            if (!activeMonsters.Any(monster => monster.Runtime.IsAlive)) return false;
            if (!Player.TrySpendMp(spell.mpCost)) { ShowDamage(sceneView.PlayerPosition, GameText.Get("ui.mana.insufficient"), new Color(.5f, .65f, 1)); return false; }
            runtime.Use(Player); run.SpellCasts++;
            var target = aim.HasValue&&spell.targeting!=SpellTargeting.Automatic?ChooseAimedTarget(spell,aim.Value):ChooseSpellTarget(spell);
            var context = new EquipmentTriggerContext(aim ?? default, TriggerAttackType.None, TriggerAttackWay.None, false, target?.Runtime.IsStunned ?? false, target?.Runtime, spell: spell, enemyCount: activeMonsters.Count(item => item.Runtime.IsAlive));
            run.StartAction(context); Player.StartAttackCooldown();playerView?.PlayAttack(); FireSpell(spell, 1, target, aim);
            run.EmitEvent(TriggerType.OnSpellCast, context); ResolveQueuedAbilities(0); run.CompleteAction(TriggerType.OnSpellCast, context);
            input?.ResetCasting();
            if (activeMonsters.All(monster => !monster.Runtime.IsAlive)) Finish(true);
            return true;
        }
        private IEnumerator EchoSpell(SpellData spell, float ratio, float delay, AttackSegment? aim=null) { yield return new WaitForSeconds(delay); if (IsActive) FireSpell(spell, ratio,null,aim); }
        private ActiveMonster ChooseSpellTarget(SpellData spell)
        {
            var living = activeMonsters.Where(monster => monster.Runtime.IsAlive).ToArray();
            if (living.Length == 0) return null;
            if (spell.element == SpellElement.Fire) return living.OrderBy(monster => monster.Runtime.CurrentHp / monster.Runtime.MaxHp).ThenBy(monster => monster.Runtime.CurrentHp).First();
            if (spell.element == SpellElement.Ice)
            {
                if (lastStaffTarget != null && lastStaffTarget.Runtime.IsAlive) return lastStaffTarget;
                var highest = living.Max(monster => monster.Runtime.CurrentHp); living = living.Where(monster => Mathf.Approximately(monster.Runtime.CurrentHp, highest)).ToArray();
            }
            else { var rods = living.Where(monster => monster.Runtime.LightningRodUntil > Player.CombatTime).ToArray(); if (rods.Length > 0) living = rods; }
            return living[spellRandom.Next(living.Length)];
        }
        private void FireSpell(SpellData spell, float multiplier, ActiveMonster primary = null, AttackSegment? aim = null)
        {
            if(aim.HasValue && spell.targeting!=SpellTargeting.Automatic){FireAimedSpell(spell,multiplier,aim.Value);return;}
            var target = primary ?? ChooseSpellTarget(spell); if (target == null) return;
            var damage = spell.damage * multiplier * Player.AllDamageMultiplier * Player.ElementMultiplier(spell.element);
            damage *= 1 + Player.Rule(AbilityRule.SpellDamageBonus, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, target.Runtime, spell: spell));
            var origin = sceneView.PlayerPosition + Vector2.up * 65f;
            ShowDamage(origin + Vector2.up * 65f, spell.displayName, spell.Color);
            if (spell.shape == SpellShape.Utility) { ApplySpellAbilities(spell, target); return; }
            if (spell.shape is SpellShape.PersistentArea or SpellShape.Rain) { StartCoroutine(SustainSpell(spell, target.Position, multiplier)); return; }
            if (spell.shape == SpellShape.Chain)
            {
                var visited = new System.Collections.Generic.HashSet<ActiveMonster>(); var current = target;
                var count = spell.chainTargets + Mathf.RoundToInt(Player.Rule(AbilityRule.SpellChainTargets));
                for (var i = 0; i < count && current != null; i++)
                {
                    ShowSpellPath(origin, current.Position, spell); visited.Add(current); HitSpell(current, spell, damage); origin = current.Position;
                    current = activeMonsters.Where(monster => monster.Runtime.IsAlive && !visited.Contains(monster) && Vector2.Distance(origin, monster.Position) <= spell.radius).OrderBy(monster => Vector2.Distance(origin, monster.Position)).FirstOrDefault();
                }
            }
            else if (spell.shape == SpellShape.Burst)
            {
                ShowSpellPath(origin, target.Position, spell);
                attackLine?.ShowSpellArea(target.Position, spell.radius, spell.Color);
                foreach (var monster in activeMonsters.Where(monster => monster.Runtime.IsAlive && Vector2.Distance(monster.Position, target.Position) <= spell.radius + monster.Runtime.Data.bodyHitRadius * monster.VisualScale).ToArray()) HitSpell(monster, spell, damage);
            }
            else
            {
                var direction = (target.Position - origin).normalized; var end = target.Position + direction * 650f;
                var segment = new AttackSegment(origin, end, spell.width); ShowSpellPath(origin, end, spell);
                foreach (var monster in activeMonsters.Where(monster => monster.Runtime.IsAlive).ToArray())
                    if (monster == target || SegmentHitResolver.Intersects(segment, new CircleHitShape(monster.Position, monster.Runtime.Data.bodyHitRadius * monster.VisualScale))) HitSpell(monster, spell, damage);
            }
        }
        private ActiveMonster ChooseAimedTarget(SpellData spell,AttackSegment aim)
        {
            var direction=(aim.End-aim.Start).normalized;var ray=new AttackSegment(aim.Start,aim.Start+direction*1800,spell.width);
            return activeMonsters.Where(m=>m.Runtime.IsAlive&&(spell.targeting==SpellTargeting.SwipeRay?SegmentHitResolver.Intersects(ray,new CircleHitShape(m.Position,m.Runtime.Data.bodyHitRadius*m.VisualScale)):Vector2.Distance(m.Position,aim.End)<=spell.radius+m.Runtime.Data.bodyHitRadius*m.VisualScale)).OrderBy(m=>Vector2.Distance(m.Position,spell.targeting==SpellTargeting.SwipeRay?aim.Start:aim.End)).FirstOrDefault();
        }
        private void FireAimedSpell(SpellData spell,float multiplier,AttackSegment aim)
        {
            var damage=spell.damage*multiplier*Player.AllDamageMultiplier*Player.ElementMultiplier(spell.element);
            var target=ChooseAimedTarget(spell,aim);
            damage*=1+Player.Rule(AbilityRule.SpellDamageBonus,new EquipmentTriggerContext(aim,TriggerAttackType.None,TriggerAttackWay.None,false,target?.Runtime.IsStunned??false,target?.Runtime,spell:spell));
            ShowDamage(aim.Start,spell.displayName,spell.Color);
            if(spell.targeting==SpellTargeting.SwipeRay)
            {
                var direction=(aim.End-aim.Start).normalized;
                var ray=new AttackSegment(aim.Start,aim.Start+direction*1800,spell.width);
                ShowSpellPath(ray.Start,ray.End,spell);
                foreach(var monster in activeMonsters.Where(m=>m.Runtime.IsAlive).ToArray())
                    if(SegmentHitResolver.Intersects(ray,new CircleHitShape(monster.Position,monster.Runtime.Data.bodyHitRadius*monster.VisualScale)))HitSpell(monster,spell,damage);
                return;
            }
            var point=aim.End;
            ShowSpellPath(point+Vector2.up*1200,point,spell);
            attackLine?.ShowSpellArea(point,spell.radius,spell.Color);
            var first=activeMonsters.Where(m=>m.Runtime.IsAlive&&Vector2.Distance(m.Position,point)<=spell.radius+m.Runtime.Data.bodyHitRadius*m.VisualScale).OrderBy(m=>Vector2.Distance(m.Position,point)).FirstOrDefault();
            if(first==null)return;
            if(spell.shape!=SpellShape.Chain){foreach(var m in activeMonsters.Where(m=>m.Runtime.IsAlive&&Vector2.Distance(m.Position,point)<=spell.radius+m.Runtime.Data.bodyHitRadius*m.VisualScale).ToArray())HitSpell(m,spell,damage);return;}
            var visited=new System.Collections.Generic.HashSet<ActiveMonster>();var current=first;
            var count=spell.chainTargets+Mathf.RoundToInt(Player.Rule(AbilityRule.SpellChainTargets));
            for(var i=0;i<count&&current!=null;i++)
            {
                visited.Add(current);HitSpell(current,spell,damage);var origin=current.Position;
                current=activeMonsters.Where(m=>m.Runtime.IsAlive&&!visited.Contains(m)&&Vector2.Distance(m.Position,origin)<=spell.radius).OrderBy(m=>Vector2.Distance(m.Position,origin)).FirstOrDefault();
                if(current!=null)ShowSpellPath(origin,current.Position,spell);
            }
        }
        private void HitSpell(ActiveMonster target, SpellData spell, float damage)
        {
            if (damage > 0 || spell.slowSeconds > 0) target.Runtime.NotifyAttacked();
            DealMagic(target, damage, spell.Color);
            if (spell.slowSeconds > 0) target.Runtime.Slow(spell.slowSeconds + Player.Rule(AbilityRule.SlowDurationBonus));
            ApplySpellAbilities(spell, target);
            // No weak-point resolution: spells cannot replace weapon/staff charge attacks.
        }
        private void ShowSpellPath(Vector2 from, Vector2 to, SpellData spell) => attackLine?.ShowSpell(new AttackSegment(from, to, spell.width), spell.Color);
        private void DealMagic(ActiveMonster target, float damage, Color color)
        {
            if (target == null || !target.Runtime.IsAlive) return;
            var result=damageResolver.ApplyToMonster(target.Runtime,damage,Player,criticalRandom,!InfiniteBattleEnabled);
            target.View.Bind(target.Runtime);if(result.Damage>0)ShowHitDamage(target.Position,result.Damage,result.IsCritical);
            if (!target.Runtime.IsAlive && Player.MonsterKillHeal > 0) Player.Heal(Player.MonsterKillHeal);
            ReportKill(target.Runtime, new EquipmentTriggerContext(default, TriggerAttackType.None, TriggerAttackWay.None, false, false, target.Runtime, spell: run.ActionContext.Spell, damage: result.Damage));
        }
    }
}
