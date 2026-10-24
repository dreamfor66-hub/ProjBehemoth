#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Opt-in reward-selection checks in the existing live Editor. No encounters or pacing comparisons.
public sealed class ManaOfferLiveProbe:MonoBehaviour
{
    const string Log="Logs/ManaOffer-Live.txt";
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    void Check(bool ok,string text){File.AppendAllText(Log,(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)Debug.LogError("Mana offer: "+text);}
    public void Run()=>StartCoroutine(Verify());
    IEnumerator Verify()
    {
        File.WriteAllText(Log,"Scoped MP reward selection, existing Unity Editor.\n");
        var controller=Object.FindFirstObjectByType<RunController>();var content=Object.FindFirstObjectByType<CombatController>().Content;
        var balance=AssetDatabase.LoadAssetAtPath<RunBalanceSettings>("Assets/Data/DungeonSlashPrototype/Settings/RunBalanceSettings.asset");
        var group=AssetDatabase.LoadAssetAtPath<PerkOfferGroup>("Assets/Data/Behemoth/PerkOfferGroups/MP.asset");
        var members=controller.AvailablePerks.Where(p=>p.offerGroup==group).ToArray();
        Check(members.Length==2&&group.weight==1,"two saved MP perks share one ordinary offer weight");
        Check(members.Any(p=>p.modifiers.Any(m=>m.kind==ModifierKind.MaxMp&&m.value==3))&&members.Any(p=>p.modifiers.Any(m=>m.kind==ModifierKind.MpRegen&&m.value==.05f))&&members.All(p=>p.maxStacks==99),"MP values and stack caps unchanged");
        var catalog=new PerkSystem(controller.AvailablePerks);var noDoubles=true;var valid=true;var beforeSpells=true;var repeatable=true;
        IReadOnlyList<PerkData> example=null;RunState exampleRun=null;
        foreach(var character in content.characters)
        {
            var state=new RunState(character,balance);var counts=new int[2];var combined=0;
            for(var seed=0;seed<1000;seed++)
            {
                var choices=catalog.GetChoices(state,seed);var mana=choices.Where(p=>p.offerGroup==group).ToArray();
                noDoubles&=mana.Length<=1;valid&=choices.Count==3&&choices.Distinct().Count()==3&&choices.All(p=>PerkSystem.IsAvailable(p,state));
                repeatable&=choices.SequenceEqual(catalog.GetChoices(state,seed));
                if(mana.Length>0){combined++;counts[Array.IndexOf(members,mana[0])]++;if(example==null&&state.Spellbook.Count==0){example=choices;exampleRun=state;}}
            }
            beforeSpells&=counts.All(c=>c>0);
            File.AppendAllText(Log,$"{character.character}: MP group {combined}/1000; members {counts[0]}/{counts[1]}; starting spells {state.Spellbook.Count}\n");
        }
        Check(noDoubles,"six class reward pools never offer both MP perks together");Check(valid,"normal and class eligibility and three distinct options preserved");
        Check(repeatable,"same seed and state produce identical choices");Check(beforeSpells&&example!=null,"both MP choices remain available without learning a spell");
        var testRun=new RunState(content.characters[0],balance);
        var ordinary=Enumerable.Range(0,3).Select(i=>{var p=ScriptableObject.CreateInstance<PerkData>();p.maxStacks=1;return p;}).ToArray();
        var comparison=new PerkSystem(ordinary.Concat(members));var hits=new int[4];var memberHits=new int[2];
        for(var seed=0;seed<4000;seed++)foreach(var choice in comparison.GetChoices(testRun,seed))
            if(choice.offerGroup==group){hits[3]++;memberHits[Array.IndexOf(members,choice)]++;}else hits[Array.IndexOf(ordinary,choice)]++;
        Check(hits.All(n=>n>2800&&n<3200)&&memberHits.All(n=>n>1300&&n<1700),"MP pair has one bucket's frequency rather than two independent perk weights");
        File.AppendAllText(Log,"Four equal buckets, 4000 offers: "+string.Join("/",hits)+"; MP member split "+string.Join("/",memberHits)+"\n");
        var capped=new PerkRuntime(members[0]);for(var i=1;i<members[0].maxStacks;i++)capped.AddStack();testRun.ActivePerks.Add(capped);
        var onlyPair=new PerkSystem(members);
        Check(Enumerable.Range(0,50).All(seed=>onlyPair.GetChoices(testRun,seed).SequenceEqual(new[]{members[1]})),"one capped MP member leaves the other eligible and never fills extra slots");
        var second=new PerkRuntime(members[1]);for(var i=1;i<members[1].maxStacks;i++)second.AddStack();testRun.ActivePerks.Add(second);
        Check(onlyPair.GetChoices(testRun,11).Count==0,"fully capped group is removed from offers");
        foreach(var p in ordinary)Object.Destroy(p);
        var view=Object.FindFirstObjectByType<PerkChoiceView>(FindObjectsInactive.Include);var battle=Object.FindFirstObjectByType<CombatController>();battle.InputPaused=true;
        var chosen=false;view.Show(example,p=>{catalog.Apply(exampleRun,p);chosen=true;view.Hide();battle.InputPaused=false;});yield return null;yield return new WaitForEndOfFrame();
        var cards=view.GetComponentsInChildren<PerkChoiceItem>();var manaIndex=example.ToList().FindIndex(p=>p.offerGroup==group);
        cards[manaIndex].GetComponent<Button>().onClick.Invoke();yield return null;
        Check(view.focusedDescription.text.Contains(example[manaIndex].displayName),"reward UI previews grouped MP perk normally");
        view.confirm.onClick.Invoke();yield return null;
        Check(chosen&&exampleRun.ActivePerks.Any(p=>p.Data==example[manaIndex]&&p.StackCount==1),"reward confirm acquires selected perk through ordinary apply path");
        File.AppendAllText(Log,"COMPLETE\n");
    }
}
#endif
