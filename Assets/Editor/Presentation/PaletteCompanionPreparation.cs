using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Explicit scoped production installation. The root owns Editor refresh and invocation.
internal static class PaletteCompanionPreparation
{
    public static void Install()
    {
        var path=AssetDatabase.GUIDToAssetPath("c6d66f4a1e280544b805c94668f5f3e7");
        var dog=AssetDatabase.LoadAssetAtPath<CompanionData>(path);
        if(dog==null||dog.combatAnimation==null||dog.combatAnimation.idle==null||dog.combatAnimation.attack==null)
            throw new System.IO.InvalidDataException("Missing active recovery-hound paintings.");
        dog.forwardAnimation=dog.combatAnimation;
        dog.displayHeight=360;
        var animations=new[]{dog.combatAnimation,dog.oppositeAnimation,dog.forwardAnimation}.Where(a=>a!=null).Distinct();
        var paintings=animations.SelectMany(a=>new[]{a.idle,a.ready,a.attack,a.guard,a.dead,a.stunned}).Append(dog.combatSprite).Where(s=>s!=null).Distinct();
        dog.SetDerivedGeometry(paintings.Select(ExplorationArtPreparation.Geometry).ToArray());
        EditorUtility.SetDirty(dog);AssetDatabase.SaveAssets();
        System.IO.Directory.CreateDirectory("Docs/Production/PaletteCohesion-2026-11-01/Dog");
        System.IO.File.WriteAllText("Docs/Production/PaletteCohesion-2026-11-01/Dog/Installed.txt",
            "Installed active hound's authored forward pair, editable height 360 and PNG-derived geometry.\n");
    }
}
