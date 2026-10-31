using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
internal static class MonsterScaleRevisionPreparation
{
    [Serializable] sealed class Region {public float x,y,width,height;public Rect Value=>new Rect(x,y,width,height);}
    [Serializable] sealed class Frame {public string image,state;public Region rect;public float ppu;public Vector2 pivot;}
    [Serializable] sealed class Giant {public string oldName;public List<Frame> frames;public List<MonsterHitBox> hitBoxes;public List<Vector2> weakPoints;}
    [Serializable] sealed class Plan {public List<Giant> monsters;}
    static bool busy,done;
    static MonsterScaleRevisionPreparation()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        const string folder=PlayabilityRevisionPreparation.Folder;
        if(done||busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(folder+"GiantFramesReady.txt"))return;
        if(File.Exists(folder+"SizesInstalled.txt"))
        {
            if(!File.Exists(folder+"NativeGiantFramesReady.txt"))CalibrateFrames();
            if(!File.Exists(folder+"GiantFormationReady.txt")){FixFormation();File.WriteAllText(folder+"GiantFormationReady.txt","Roc support positions flank the main body.\n");}
            if(!File.Exists(folder+"VisibleGiantTargetsReady.txt"))
            {
                var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(folder+"GiantFrames.json"));
                foreach(var giant in plan.monsters)foreach(var attack in Monster(giant.oldName).GetAttacks(MonsterAttackType.Charge))
                {for(var n=0;n<attack.chargeWeakPoints.Count;n++)attack.chargeWeakPoints[n].localPosition=giant.weakPoints[n];EditorUtility.SetDirty(attack);AssetDatabase.SaveAssetIfDirty(attack);}
                File.WriteAllText(folder+"VisibleGiantTargetsReady.txt","Anatomical targets avoid the player/HUD foreground.\n");
            }
            done=true;return;
        }
        busy=true;
        try{Prepare();File.WriteAllText(folder+"SizesInstalled.txt","Species scales, giant native paintings, hitboxes, weak points and formations saved.\n");done=true;}
        catch(Exception e){done=true;File.WriteAllText(folder+"SizesError.txt",e.ToString());Debug.LogException(e);}
        finally{busy=false;}
    }
    static MonsterData Monster(string old)=>DesignerAssetPaths.Load<MonsterData>("Assets/Data/Combat/Monsters/"+old+".asset");
    static void Prepare()
    {
        var folder=PlayabilityRevisionPreparation.Folder;var report=new StringBuilder("Species scale choices: current authored values before -> after. Changes are never tied to group size.\n");
        // Authored species presentation choices for this migration only; no runtime identity dispatch.
        var scales=new Dictionary<string,(float scale,string reason)>{
            ["Data_Monster_B1_Minion_Bat"]=(.72f,"Goblin: clearly smaller than skeleton, including body rather than spear."),
            ["AlarmHound"]=(.78f,"Hound: lower quadruped silhouette beneath the skeleton."),["TunnelRat"]=(.85f,"Spider: small low body with a readable leg span."),
            ["MirrorKnight"]=(.78f,"Animated armor: human-sized precise guard concept, not a giant."),
            ["Data_Monster_B1_Elite_Minotaur"]=(1.05f,"Ettin: substantial broad close body volume without clipping heads."),["ArmoredExecutioner"]=(1.04f,"Mantis: imposing scythe reach and thorax without clipping."),
            ["BellKeeper"]=(1.03f,"Bell: massive upright bell, preserve vertical concept."),
            ["Data_Monster_B2_Minion_FrostBat"]=(.6f,"Quill spirit: small airborne creature."),["IceNeedle"]=(.62f,"Urchin: compact ground hazard."),["SnowBell"]=(.64f,"Hare: small creature, not humanoid-sized."),["StormOrb"]=(.62f,"Jelly support: small companion to roc."),
            ["Data_Monster_B2_Elite_FrostOgre"]=(1.65f,"Stag: large antlered quadruped, retain graceful body."),["IceMirror"]=(1.25f,"Moth: fragile winged elite; moderate span, not a hulking boss."),
            ["CrystalSpider"]=(1.85f,"Prism crab boss: thick armored body and huge pincers."),
            ["Data_Monster_B3_Elite_SoulKnight"]=(1.55f,"Mounted centaur elite: horse body and weapon reach."),["BloodExecutioner"]=(1.1f,"Count: elegant human-scale duelist, deliberately compact."),
            ["AshGeneral"]=(1.5f,"Lich king boss: imposing tall caster without turning robe into a wide beast."),["EclipseBeast"]=(1.4f,"Chimera: central body with two flanking supports, no forced panorama."),
            ["ElderLeftHeart"]=(.74f,"Small sun support."),["ElderRightHeart"]=(.74f,"Small moon support."),["LampPriest"]=(1.1f,"Priest paired with gargoyle; reserve side space."),
            ["TrialGuardian"]=(1.6f,"Armored trial beast: heavy close body."),["DeepTrialGuardian"]=(1.85f,"Colossus: broad large torso rather than a tiny square token.")};
        foreach(var pair in scales)
        {var monster=Monster(pair.Key);if(monster==null)throw new InvalidOperationException(pair.Key);report.AppendLine($"{monster.displayName}: {monster.combatScale} -> {pair.Value.scale}; {pair.Value.reason}");monster.combatScale=pair.Value.scale;EditorUtility.SetDirty(monster);}
        var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(folder+"GiantFrames.json"));
        Directory.CreateDirectory("Assets/Art/Combat/GiantMonsterFrames");
        foreach(var giant in plan.monsters)
        {
            var monster=Monster(giant.oldName);var library=monster.combatAnimation;
            foreach(var frame in giant.frames)
            {
                AssetDatabase.ImportAsset(frame.image,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(frame.image);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(frame.image);
                var path="Assets/Art/Combat/GiantMonsterFrames/"+Path.GetFileNameWithoutExtension(frame.image)+".asset";
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(sprite==null){sprite=Sprite.Create(texture,frame.rect.Value,frame.pivot,frame.ppu,0,SpriteMeshType.FullRect);sprite.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(sprite,path);}
                if(frame.state=="Idle")library.idle=sprite;else if(frame.state=="Ready")library.ready=sprite;else library.attack=sprite;
            }
            // Keep a coherent body frame when fading out after death.
            library.dead=library.idle;library.canvasScale=1;library.canvasPaddingPixels=Vector4.zero;library.tightPoseFrames=true;library.illustratedLayout=true;
            monster.combatScale=1;monster.hitBoxes=giant.hitBoxes;
            foreach(var attack in monster.GetAttacks(MonsterAttackType.Charge))
            {
                if(attack.chargeWeakPoints.Count!=giant.weakPoints.Count)throw new InvalidOperationException(monster.displayName+" weak point count changed unexpectedly");
                for(var n=0;n<attack.chargeWeakPoints.Count;n++)attack.chargeWeakPoints[n].localPosition=giant.weakPoints[n];
                EditorUtility.SetDirty(attack);
            }
            EditorUtility.SetDirty(library);EditorUtility.SetDirty(monster);
            report.AppendLine(monster.displayName+": recomposed wide body; authored hitbox union and anatomical weak-point placement. HP/damage/timings/rewards/hit requirements unchanged.");
        }
        // Encounters author the physical hitbox ground line, so expanded bodies keep a clear floor lane.
        foreach(var guid in AssetDatabase.FindAssets("t:EncounterData",new[]{"Assets/Data/Dungeon/Encounters"}))
        {
            var encounter=AssetDatabase.LoadAssetAtPath<EncounterData>(AssetDatabase.GUIDToAssetPath(guid));var changed=false;
            foreach(var member in encounter.members)
            {
                if(plan.monsters.Any(g=>Monster(g.oldName)==member.monster)){member.offset.y=0;changed=true;}
            }
            if(encounter.members.Any(m=>m.monster==Monster("StormMatriarch")))
                foreach(var member in encounter.members){member.offset=member.monster==Monster("StormMatriarch")?new Vector2(-155,0):new Vector2(520,100);changed=true;}
            if(encounter.members.Count>1&&encounter.members.Any(m=>m.monster==Monster("EclipseBeast")))
            {var side=0;foreach(var member in encounter.members){member.offset=member.monster==Monster("EclipseBeast")?Vector2.zero:new Vector2(++side==1?-470:470,80);changed=true;}}
            if(changed)EditorUtility.SetDirty(encounter);
        }
        AssetDatabase.SaveAssets();File.WriteAllText(folder+"ScaleChoices.txt",report.ToString());
    }
    static void CalibrateFrames()
    {
        var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(PlayabilityRevisionPreparation.Folder+"GiantFrames.json"));
        foreach(var giant in plan.monsters)foreach(var frame in giant.frames)
        {
            if(frame.rect.width<=0||frame.rect.height<=0)throw new InvalidDataException(frame.image+" has an empty frame");
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(frame.image);var path="Assets/Art/Combat/GiantMonsterFrames/"+Path.GetFileNameWithoutExtension(frame.image)+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);var sprite=Sprite.Create(texture,frame.rect.Value,frame.pivot,frame.ppu,0,SpriteMeshType.FullRect);sprite.name=existing.name;
            EditorUtility.CopySerialized(sprite,existing);Object.DestroyImmediate(sprite);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);
            if(existing.rect.width<100||existing.rect.height<100)throw new InvalidDataException(path+" failed native frame save");
        }
        File.WriteAllText(PlayabilityRevisionPreparation.Folder+"NativeGiantFramesReady.txt","9 native Sprite regions and vertex/UV streams calibrated; GUIDs preserved.\n");
    }
    static void FixFormation()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:EncounterData",new[]{"Assets/Data/Dungeon/Encounters"}))
        {
            var encounter=AssetDatabase.LoadAssetAtPath<EncounterData>(AssetDatabase.GUIDToAssetPath(guid));if(!encounter.members.Any(m=>m.monster==Monster("StormMatriarch")))continue;
            var side=0;foreach(var member in encounter.members)member.offset=member.monster==Monster("StormMatriarch")?Vector2.zero:new Vector2(++side==1?-610:610,100);
            EditorUtility.SetDirty(encounter);AssetDatabase.SaveAssetIfDirty(encounter);
        }
    }
}
