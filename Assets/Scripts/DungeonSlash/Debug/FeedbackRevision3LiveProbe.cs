#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class FeedbackRevision3LiveProbe:MonoBehaviour
{
    const string Log="Logs/FeedbackRevision3-Live.txt",Screens="Docs/Production/FeedbackRevision3/Screenshots/";
    OctoberReviewLiveProbe practice;
    void Check(bool ok,string message){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+message+"\n");if(!ok)Debug.LogError("Revision3: "+message);}
    public static void Capture(string name)
    {
        var frame=Object.FindFirstObjectByType<ArcadeRasterCamera>()?.Frame;if(frame==null)return;
        var old=RenderTexture.active;RenderTexture.active=frame;var texture=new Texture2D(frame.width,frame.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();Directory.CreateDirectory(Screens);File.WriteAllBytes(Screens+name+".png",texture.EncodeToPNG());Object.Destroy(texture);RenderTexture.active=old;
    }
    public void Run(string action)
    {
        if(action=="selection")StartCoroutine(Selection());
        else if(action=="combat")StartCoroutine(Combat());
        else if(action=="spellbook")StartCoroutine(Spellbook());
        else if(action=="critical")StartCoroutine(CriticalStats());
        else if(action=="book-open")
        {
            Setup(CharacterKind.Wizard,true);var battle=Object.FindFirstObjectByType<CombatController>();var controller=Object.FindFirstObjectByType<RunController>();
            battle.ResetEncounter();Object.FindFirstObjectByType<DungeonController>().RefreshNavigation();
            foreach(var spell in battle.Content.spells)controller.State.RememberSpell(spell);
            Object.FindFirstObjectByType<RunExpansionView>().ShowSpellbook();
        }
    }
    IEnumerator Selection()
    {
        File.WriteAllText(Log,"Revision3 focused live runtime verification.\n");
        var menu=Object.FindFirstObjectByType<OutGameMenuView>();menu.ShowCharacters();yield return null;
        for(var i=0;i<6;i++)
        {
            menu.selection.characterButtons[i].onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();var v=menu.selection;
            Check(v.selectionStory.text==v.content.characters[i].description&&!v.selectionStory.isTextOverflowing,"original description fits: "+v.content.characters[i].character);
            Check(v.characterButtons.All(b=>b.gameObject.activeInHierarchy&&b.GetComponentInChildren<ClassSymbolGraphic>()!=null),"six class symbols stay on one page");
            Check(v.selectionSplash.sprite==v.content.characters[i].selectionIllustration&&!v.portrait.gameObject.activeSelf,"expanded illustration selected without portrait crop");
            yield return new WaitForEndOfFrame();Capture("Selection-"+v.content.characters[i].character);
        }
        var hover=menu.selection.startingRelicHover;hover.OnPointerEnter(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));yield return null;
        Check(hover.view.IsVisible,"starter relic side popup remains available");yield return new WaitForEndOfFrame();Capture("RelicPopup");hover.OnPointerExit(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));File.AppendAllText(Log,"SELECTION COMPLETE\n");
    }
    void Setup(CharacterKind kind,bool spells=false){practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Focused revision3 practice").AddComponent<OctoberReviewLiveProbe>();practice.Setup(kind,spells);}
    IEnumerator Combat()
    {
        File.AppendAllText(Log,"\nCombat temporal checks\n");Setup(CharacterKind.Monk);yield return null;
        var battle=Object.FindFirstObjectByType<CombatController>();var area=Object.FindFirstObjectByType<CombatSceneView>();var c=area.MonsterPosition;
        var multi=battle.Player.Data.attackBuffs.First(b=>b.name=="Multi");
        foreach(var direction in multi.attackPattern)
        {
            var axis=direction==SwipeDirection.Left?Vector2.left:direction==SwipeDirection.Right?Vector2.right:direction==SwipeDirection.Up?Vector2.up:Vector2.down;
            battle.Player.ResetAttackCooldown();battle.ExecuteAttack(new AttackSegment(c-axis*180,c+axis*180,40),false);yield return new WaitForSeconds(.02f);
        }
        Check(battle.Player.PreparedAttackBuff==multi,"ordinary attack directions prepare Hundred Fists");
        var enemy=battle.Enemies[0];var hp=enemy.CurrentHp;var values=new List<float>();var times=new List<float>();var started=Time.time;
        battle.Player.ResetAttackCooldown();battle.ExecuteAttack(new AttackSegment(c+Vector2.left*180,c+Vector2.right*180,40),true);
        for(var elapsed=0f;elapsed<.5f;elapsed+=Time.deltaTime)
        {
            if(!Mathf.Approximately(hp,enemy.CurrentHp)){values.Add(hp-enemy.CurrentHp);times.Add(Time.time-started);hp=enemy.CurrentHp;}yield return null;
        }
        File.AppendAllText(Log,"HP steps: "+string.Join(", ",values.Select((d,i)=>$"{d:0.###}@{times[i]:0.000}s"))+"\n");
        Check(values.Count==3,"Hundred Fists causes three actual HP reductions");Check(times.Count==3&&times[1]-times[0]>=.09f&&times[2]-times[1]>=.09f,"three HP reductions are separated in time");
        yield return new WaitForSeconds(1.1f);Setup(CharacterKind.Warrior);yield return null;
        c=area.MonsterPosition;battle.Player.ResetAttackCooldown();battle.ExecuteAttack(new AttackSegment(c+Vector2.left*180,c+Vector2.right*180,40),false);yield return null;
        var normal=Object.FindObjectsByType<DamageNumberView>(FindObjectsSortMode.None).FirstOrDefault(v=>!v.IsCritical&&v.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>float.TryParse(t.text,out _)));
        Check(normal!=null&&normal.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.color==Color.white),"normal damage is white with black outline");yield return new WaitForEndOfFrame();Capture("NormalDamage");
        yield return new WaitForSeconds(1);battle.Enemies[0].EnterStun();battle.Player.ResetAttackCooldown();battle.ExecuteAttack(new AttackSegment(c+Vector2.left*180,c+Vector2.right*180,40),false);yield return null;
        var critical=Object.FindObjectsByType<DamageNumberView>(FindObjectsSortMode.None).FirstOrDefault(v=>v.IsCritical);
        Check(critical!=null&&critical.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text.EndsWith("!!")),"critical damage has !! suffix and orange text");Check(area.IsCriticalFlashing,"critical hit produces brief screen flash");
        yield return new WaitForEndOfFrame();Capture("Critical-Flash");var start=critical!=null?((RectTransform)critical.transform).anchoredPosition:Vector2.zero;
        yield return new WaitForSeconds(.3f);Check(critical!=null&&Mathf.Abs(((RectTransform)critical.transform).anchoredPosition.x-start.x)>20,"critical number jumps sideways");yield return new WaitForEndOfFrame();Capture("Critical-Jump");
        yield return new WaitForSeconds(.66f);Check(critical!=null&&critical.BounceCount==2,"critical number performs two floor bounces");Check(!area.IsCriticalFlashing,"screen flash ends promptly");yield return new WaitForEndOfFrame();Capture("Critical-Bounce");
        Setup(CharacterKind.Wizard,true);yield return new WaitForSeconds(6.2f);practice.Charge();yield return null;yield return new WaitForEndOfFrame();Capture("WeakPoint-Bat");
        var point=battle.Enemies[0].WeakPoints[0];var collision=point.HitShape.Radius;var reticle=Object.FindFirstObjectByType<WeakPointReticle>();Check(reticle!=null&&reticle.rectTransform.rect.width>=96,"weakpoint has prominent 24px bullseye");
        Check(Mathf.Approximately(collision,point.HitShape.Radius),"visual marker does not modify hit collision");
        practice.Setup(CharacterKind.Warrior,false,"Assets/Data/DungeonSlashPrototype/Monsters/Data_Monster_B1_Minion_Skeleton.asset");yield return new WaitForSeconds(6.2f);practice.Charge();yield return null;yield return new WaitForEndOfFrame();Capture("WeakPoint-Skeleton");
        File.AppendAllText(Log,"COMBAT COMPLETE\n");
    }
    sealed class FixedRandom:System.Random
    {
        readonly double value;public int Calls;
        public FixedRandom(double value){this.value=value;}
        public override double NextDouble(){Calls++;return value;}
    }
    IEnumerator CriticalStats()
    {
        File.AppendAllText(Log,"\nCritical rules and shared perks\n");Setup(CharacterKind.Warrior);yield return new WaitForSeconds(1.2f);
        var battle=Object.FindFirstObjectByType<CombatController>();var run=Object.FindFirstObjectByType<RunController>();var enemy=battle.Enemies[0];var area=Object.FindFirstObjectByType<CombatSceneView>();var c=area.MonsterPosition;
        var chance=battle.Content.perks.Single(p=>p.id=="KeenInstinct");var damage=battle.Content.perks.Single(p=>p.id=="DecisiveBlow");
        Check(run.AvailablePerks.Contains(chance)&&run.AvailablePerks.Contains(damage)&&chance.scope==RelicScope.Shared&&damage.scope==RelicScope.Shared,"both shared critical perks are in the playable reward pool");
        Check(chance.icon!=null&&damage.icon!=null,"critical perks have saved art references");
        Check(Mathf.Approximately(battle.Player.CriticalChance,0)&&Mathf.Approximately(battle.Player.CriticalDamageMultiplier,1.5f),"default critical stats are 0% / 150%");
        var hp=enemy.CurrentHp;var expected=battle.Player.ChargeAttackDamage;battle.Player.ResetAttackCooldown();battle.ExecuteAttack(new AttackSegment(c+Vector2.left*180,c+Vector2.right*180,40),true);yield return null;
        Check(Mathf.Abs(hp-enemy.CurrentHp-expected)<.01f&&!Object.FindObjectsByType<DamageNumberView>(FindObjectsSortMode.None).Any(v=>v.IsCritical),"charged attack is not automatically a critical hit");
        for(var i=0;i<5;i++){run.TryGrantPerk(chance);run.TryGrantPerk(damage);}
        Check(Mathf.Approximately(battle.Player.CriticalChance,.25f)&&Mathf.Approximately(battle.Player.CriticalDamageMultiplier,2.25f),"five stacked perks give 25% / 225%");
        Check(!run.TryGrantPerk(chance)&&!run.TryGrantPerk(damage),"critical perks respect their stack caps");
        var resolver=new DamageResolver();var low=new FixedRandom(.2499);var high=new FixedRandom(.25);
        var hit=resolver.ApplyToMonster(enemy,10,battle.Player,low,false);var miss=resolver.ApplyToMonster(enemy,10,battle.Player,high,false);
        Check(hit.IsCritical&&Mathf.Approximately(hit.Damage,22.5f)&&!miss.IsCritical&&Mathf.Approximately(miss.Damage,10),"critical chance uses the authored threshold and damage multiplier");
        var oldPerk=battle.Content.perks.First(p=>p.modifiers.Any(m=>m.kind==ModifierKind.StunDamageMultiplier));run.TryGrantPerk(oldPerk);var oldBonus=battle.Player.StunDamageMultiplier;
        enemy.EnterStun();var guaranteed=new FixedRandom(.999);var stunned=resolver.ApplyToMonster(enemy,10,battle.Player,guaranteed,false);
        Check(stunned.IsCritical&&stunned.WasStunned&&guaranteed.Calls==0,"stunned target guarantees critical without rolling chance");
        Check(Mathf.Approximately(stunned.Damage,10*2.25f*oldBonus)&&oldBonus>1,"existing stunned-target damage bonus stacks with critical damage");
        run.TryRemovePerk(chance);Check(Mathf.Approximately(battle.Player.CriticalChance,0),"removing chance perk removes every acquired stack");
        hp=enemy.CurrentHp;expected=battle.Player.NormalAttackDamage*battle.Player.CriticalDamageMultiplier*oldBonus;battle.Player.ResetAttackCooldown();battle.ExecuteAttack(new AttackSegment(c+Vector2.left*180,c+Vector2.right*180,40),false);yield return null;
        Check(Mathf.Abs(hp-enemy.CurrentHp-expected)<.02f&&Object.FindObjectsByType<DamageNumberView>(FindObjectsSortMode.None).Any(v=>v.IsCritical),"live weapon hit applies guaranteed critical and preserves stun bonus");yield return new WaitForEndOfFrame();Capture("GuaranteedCritical");
        var spell=battle.Content.spells.First(s=>s.id=="EnergyBolt");run.State.LearnSpell(spell,true);battle.Player.RefillMp();battle.Player.ResetAttackCooldown();run.State.RuntimeFor(spell).ResetCooldown();enemy.EnterStun();hp=enemy.CurrentHp;
        expected=spell.damage*battle.Player.AllDamageMultiplier*battle.Player.ElementMultiplier(spell.element)*battle.Player.CriticalDamageMultiplier*oldBonus;
        var cast=battle.TryCastSpell(spell.defaultSlot,10);yield return null;
        Check(cast&&Mathf.Abs(hp-enemy.CurrentHp-expected)<.02f,"damaging spells use the same critical resolver and stun multiplier");
        run.TryRemovePerk(damage);Check(Mathf.Approximately(battle.Player.CriticalDamageMultiplier,1.5f)&&Mathf.Approximately(battle.Player.StunDamageMultiplier,oldBonus),"removing critical perk leaves old stunned-target bonus intact");
        File.AppendAllText(Log,"CRITICAL COMPLETE\n");
    }
    IEnumerator Spellbook()
    {
        Setup(CharacterKind.Wizard,true);yield return null;
        var battle=Object.FindFirstObjectByType<CombatController>();var run=Object.FindFirstObjectByType<RunController>();var expansion=Object.FindFirstObjectByType<RunExpansionView>();
        battle.ResetEncounter();Object.FindFirstObjectByType<DungeonController>().RefreshNavigation();
        foreach(var spell in battle.Content.spells)run.State.RememberSpell(spell);
        expansion.ShowSpellbook();yield return null;var book=expansion.spellbookView;
        Check(book.gameObject.activeInHierarchy&&expansion.IsDecisionOpen&&battle.InputPaused,"dedicated spellbook opens and blocks combat input");
        for(var i=0;i<4;i++){book.slots[i].onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();Check(book.SelectedDirection==(SwipeDirection)i&&!book.description.isTextOverflowing,"direction slot previews readable description: "+i);}
        book.slots[0].onClick.Invoke();yield return null;var known=book.knownButtons.Where(b=>b.gameObject.activeSelf).ToArray();Check(known.Length>1,"same-direction learned spells remain accessible");
        foreach(var spell in battle.Content.spells){book.Preview(spell);yield return null;Canvas.ForceUpdateCanvases();Check(!book.description.isTextOverflowing&&!book.heading.isTextOverflowing,"spell detail fits: "+spell.displayName);}
        known.Last().onClick.Invoke();yield return null;var selected=book.Selected;var previous=run.State.Slot(selected.defaultSlot);Check(previous!=selected,"clicking a learned spell previews before preparing");book.equip.onClick.Invoke();yield return null;
        Check(run.State.Slot(selected.defaultSlot)==selected,"prepare swaps selected direction without closing book");yield return new WaitForEndOfFrame();Capture("Spellbook");
        book.close.onClick.Invoke();yield return null;Check(!expansion.IsModalOpen&&!battle.InputPaused,"close restores exploration input");File.AppendAllText(Log,"SPELLBOOK COMPLETE\n");
    }
}
#endif
