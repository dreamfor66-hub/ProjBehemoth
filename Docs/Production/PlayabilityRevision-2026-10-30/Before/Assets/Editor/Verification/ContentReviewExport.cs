using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

// Read-only snapshot of the catalog AND saved playable pools for design review.
internal static class ContentReviewExport
{
    [Serializable] sealed class Snapshot
    {
        public string generated;
        public List<Item> perks=new(), equipment=new(), spells=new();
        public List<Story> events=new();
        public List<Character> characters=new();
        public string[] playablePerks,playableEquipment;
    }
    [Serializable] sealed class Character { public string name,kind,startingRelic,raw; }
    [Serializable] sealed class Item
    {
        public string id,name,description,path,scope,character,spell,raw;
        public int maxStacks,price;
    }
    [Serializable] sealed class Story
    {
        public string id,name,path,requiredFlag,forbiddenFlag,raw;
        public int minAct,maxAct,minCombats;
        public List<Option> options=new();
        public List<Page> pages=new();
        public List<Route> entryRoutes=new();
    }
    [Serializable] sealed class Page { public int index; public List<Option> options=new(); public List<Route> routes=new(); }
    [Serializable] sealed class Route { public int page; public List<Requirement> requirements=new(); }
    [Serializable] sealed class Option
    {
        public string title,description,requiredFlag,requiredRelic,requiredPerk,raw;
        public int gold,nextPage,failurePage;
        public float hp,maxHp;
        public List<Effect> effects=new();
        public List<Requirement> requirements=new();
    }
    [Serializable] sealed class Effect { public string kind,relic,spell,flag,stat;public float amount; }
    [Serializable] sealed class Requirement { public string kind,spell,relic,flag; public float amount;public bool invert; }
    static string Path(Object o)=>o==null?null:AssetDatabase.GetAssetPath(o);
    static Requirement Read(EventRequirement r)=>new(){kind=r.kind.ToString(),spell=Path(r.spell),relic=Path(r.relic),flag=r.flag,amount=r.amount,invert=r.invert};
    static Route Read(EventEntryRoute r)=>new(){page=r.page,requirements=r.requirements.Select(Read).ToList()};
    static Option Read(EventOption o)=>new()
    {
        title=o.title,description=o.description,requiredFlag=o.requiredFlag,requiredRelic=Path(o.requiredRelic),requiredPerk=o.requiredPerk,
        gold=o.goldCost,hp=o.hpCost,maxHp=o.maxHpCost,nextPage=o.nextPage,failurePage=o.failurePage,raw=JsonUtility.ToJson(o),
        effects=o.effects.Select(e=>new Effect{kind=e.kind.ToString(),relic=Path(e.relic),spell=Path(e.spell),flag=e.flag,stat=e.stat.ToString(),amount=e.amount}).ToList(),
        requirements=o.requirements.Select(Read).ToList()
    };
    internal static void Export()
    {
        var content=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");
        var run=Object.FindFirstObjectByType<RunController>();
        if(run==null)throw new InvalidOperationException("Open MainGameScene before exporting its playable pools.");
        var snapshot=new Snapshot{generated=DateTime.Now.ToString("s"),playablePerks=run.AvailablePerks.Select(Path).ToArray(),playableEquipment=run.Equipment.Select(Path).ToArray()};
        foreach(var p in content.perks.Where(p=>p!=null))snapshot.perks.Add(new Item{id=p.id,name=p.displayName,description=p.description,path=Path(p),scope=p.scope.ToString(),character=p.exclusiveCharacter.ToString(),maxStacks=p.maxStacks,raw=JsonUtility.ToJson(p)});
        foreach(var e in content.equipment.Where(e=>e!=null))snapshot.equipment.Add(new Item{id=e.id,name=e.displayName,description=e.description,path=Path(e),scope=e.scope.ToString(),character=e.exclusiveCharacter.ToString(),price=e.price,spell=Path(e.grantedSpell),raw=JsonUtility.ToJson(e)});
        foreach(var s in content.spells.Where(s=>s!=null))snapshot.spells.Add(new Item{id=s.id,name=s.displayName,description=s.description,path=Path(s),raw=JsonUtility.ToJson(s)});
        foreach(var c in content.characters.Where(c=>c!=null))snapshot.characters.Add(new Character{name=c.displayName,kind=c.character.ToString(),startingRelic=Path(c.startingRelic),raw=JsonUtility.ToJson(c)});
        foreach(var e in content.events.Where(e=>e!=null))snapshot.events.Add(new Story{id=e.id,name=e.displayName,path=Path(e),minAct=e.minAct,maxAct=e.maxAct,minCombats=e.minNormalCombatsInAct,requiredFlag=e.requiredFlag,forbiddenFlag=e.forbiddenFlag,raw=JsonUtility.ToJson(e),options=e.options.Select(Read).ToList(),entryRoutes=e.entryRoutes.Select(Read).ToList(),pages=e.pages.Select((p,i)=>new Page{index=i,options=p.options.Select(Read).ToList(),routes=p.routes.Select(Read).ToList()}).ToList()});
        Directory.CreateDirectory("Docs/Production/ContentReview");File.WriteAllText("Docs/Production/ContentReview/Snapshot.json",JsonUtility.ToJson(snapshot,true));
        Debug.Log("Content review exported from saved catalog and playable pools.");
    }
}
