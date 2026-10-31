#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using Object=UnityEngine.Object;

// Opt-in coverage of the two repaired relics and the event exclusion only.
public sealed class ContentReviewLiveProbe:MonoBehaviour
{
    const string Log="Logs/ContentReview-Live.txt";
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)Debug.LogError("Content review: "+text);}
    public void Run()=>StartCoroutine(Verify());
    float Hit(RunState run,CombatController combat,TriggerAttackType kind)
    {
        var target=combat.Enemies[0];var before=target.CurrentHp;
        var context=new EquipmentTriggerContext(default,kind,TriggerAttackWay.Horizontal,false,false,target,damage:100);
        run.EmitEvent(TriggerType.OnHit,context);
        typeof(CombatController).GetMethod("ResolveQueuedAbilities",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(combat,new object[]{0});
        return before-target.CurrentHp;
    }
    IEnumerator Verify()
    {
        File.WriteAllText(Log,"Scoped elemental relic damage and excluded random events, existing live Unity instance.\n");
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Content review practice").AddComponent<OctoberReviewLiveProbe>();
        foreach(var id in new[]{"StormBottle","ThunderousCharge"})
        {
            practice.Setup(CharacterKind.Barbarian,false);yield return null;
            var controller=Object.FindFirstObjectByType<RunController>();var run=controller.State;var combat=Object.FindFirstObjectByType<CombatController>();combat.InputPaused=true;
            var relic=combat.Content.equipment.Single(e=>e.id==id);Check(controller.TryGrantRelic(relic),id+" real relic can be equipped");
            Check(Mathf.Abs(Hit(run,combat,TriggerAttackType.Normal))<.02f,id+" does not add lightning to normal hits");
            Check(Mathf.Abs(Hit(run,combat,TriggerAttackType.Charge)-8)<.02f,id+" base charged-hit bonus remains8");
            var weapon=run.Player.ChargeAttackDamage;var lightning=combat.Content.perks.Single(p=>p.id=="LightningMastery");
            controller.TryGrantPerk(lightning);Check(Mathf.Abs(Hit(run,combat,TriggerAttackType.Charge)-8.8f)<.02f,id+" lightning perk +10% gives8.8");
            controller.TryGrantPerk(lightning);Check(Mathf.Abs(Hit(run,combat,TriggerAttackType.Charge)-9.6f)<.02f,id+" two lightning perk stacks give9.6");
            controller.TryGrantPerk(combat.Content.perks.Single(p=>p.id=="FireMastery"));Check(Mathf.Abs(Hit(run,combat,TriggerAttackType.Charge)-9.6f)<.02f,id+" unrelated fire perk does not scale lightning");
            Check(Mathf.Approximately(weapon,run.Player.ChargeAttackDamage),id+" elemental perks leave physical charge base unchanged");
            controller.TryGrantRelic(combat.Content.equipment.Single(e=>e.id=="BloodContract"));Check(Mathf.Abs(Hit(run,combat,TriggerAttackType.Charge)-12)<.02f,id+" all-damage25% and lightning20% each apply once");
        }
        var content=Object.FindFirstObjectByType<CombatController>().Content;
        var excluded=content.events.Where(e=>e.excludedFromRandomPool).ToArray();
        var expected=new[]{"Symbiosis","Hourglass","Bell","Forge","MasterTrial"};
        Check(excluded.Select(e=>e.id).OrderBy(s=>s).SequenceEqual(expected.OrderBy(s=>s)),"only the five exchange events are excluded, authored pages retained");
        Check(content.events.Single(e=>e.id=="Delivery").excludedFromRandomPool==false&&content.events.Single(e=>e.id=="Training").excludedFromRandomPool==false,"gold conversion and ordinary training stay available");
        var hadHistory=PlayerPrefs.HasKey("Behemoth.EventHistory.v1");var history=PlayerPrefs.GetString("Behemoth.EventHistory.v1");
        var clone=Object.Instantiate(content);clone.events=excluded.ToList();var none=true;var active=true;
        try
        {
            var controller=Object.FindFirstObjectByType<RunController>();
            for(var act=1;act<=3;act++)for(var seed=0;seed<128;seed++)
            {
                var run=new RunState(controller.State.Player.Data,DungeonSlash.DesignerAssetPaths.Load<RunBalanceSettings>("Assets/Data/Settings/RunBalanceSettings.asset")){Seed=seed,NormalCombatsInAct=99};
                none&=RandomEventSystem.Draw(clone,run,act,7)==null;
                var draw=RandomEventSystem.Draw(content,run,act,7);active&=draw!=null&&!draw.excludedFromRandomPool;
            }
        }
        finally
        {
            if(hadHistory)PlayerPrefs.SetString("Behemoth.EventHistory.v1",history);else PlayerPrefs.DeleteKey("Behemoth.EventHistory.v1");
            Object.Destroy(clone);
        }
        Check(none,"disabled-only pool yields no event in B1/B2/B3");Check(active,"384 real-pool draws keep active events and never choose excluded events");
        File.AppendAllText(Log,"COMPLETE\n");
    }
}
#endif
