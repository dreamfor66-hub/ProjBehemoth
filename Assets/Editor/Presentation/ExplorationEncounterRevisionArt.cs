using System;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

internal static partial class ExplorationEncounterRevisionPreparation
{
    [Serializable] class ArtPlan { public SubjectArt[] objects; public string seedIcon; }
    [Serializable] class SubjectArt
    {
        public string name, front, side, rear, usedFront, usedSide, usedRear;
        public float height;
        public bool wall;
    }
    static void InstallArt()
    {
        var plan = JsonUtility.FromJson<ArtPlan>(File.ReadAllText(Folder + "ArtReady.json"));
        var paths = plan.objects.SelectMany(a => new[] { a.front, a.side, a.rear, a.usedFront, a.usedSide, a.usedRear }).Append(plan.seedIcon).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToArray();
        foreach (var path in paths)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Backup(path + ".meta");
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.spritePixelsPerUnit = 100; importer.SaveAndReimport();
        }
        Sprite Painting(string path) => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
        var objects = AssetDatabase.FindAssets("t:RoomObjectData").Select(Ref<RoomObjectData>).ToDictionary(a => a.name.Replace("Data_RoomObject_", ""));
        foreach (var a in plan.objects)
        {
            if (!objects.TryGetValue(a.name, out var subject))
            {
                subject = ScriptableObject.CreateInstance<RoomObjectData>();
                AssetDatabase.CreateAsset(subject, "Assets/Data/Presentation/Exploration/Data_RoomObject_" + a.name + ".asset"); objects.Add(a.name, subject);
            }
            Backup(AssetDatabase.GetAssetPath(subject));
            subject.front = Painting(a.front); subject.side = Painting(a.side); subject.rear = Painting(a.rear);
            subject.usedFront = Painting(a.usedFront); subject.usedSide = Painting(a.usedSide); subject.usedRear = Painting(a.usedRear);
            if (a.height > 0) subject.displayHeight = a.height;
            subject.wallMounted = a.wall; subject.wallSuspended = false; subject.roomInterior = false;
            subject.facePlayerOnFirstEncounter = false; subject.fitsDoorway = false; subject.includesDoorframe = false; subject.usedForm = null;
            subject.SetDerivedGeometry(new[] { subject.front, subject.side, subject.rear, subject.usedFront, subject.usedSide, subject.usedRear }.Where(s => s != null).Distinct().Select(ExplorationArtPreparation.Geometry).ToArray());
            EditorUtility.SetDirty(subject);
        }
        var safe = objects["저주받은금고"]; var pit = objects["급료고의빈배수구"];
        // Both images retain the same support/canvas. Scale is derived from those
        // actual alpha bounds, not a second designer-authored placement/size value.
        var initialPixels = safe.front.rect.height * safe.Geometry(safe.front).bounds.height;
        var remainingPixels = pit.front.rect.height * pit.Geometry(pit.front).bounds.height;
        pit.displayHeight = safe.displayHeight * remainingPixels / Mathf.Max(1, initialPixels);
        safe.usedForm = pit;
        pit.labelKey = "ui.encounter.pit.empty"; pit.resolvedTextKey = "ui.encounter.pit.empty";
        BehemothTextEditor.Table.Set(pit.labelKey, "금고가 떨어진 빈 배수구다. 아래에는 손이 닿지 않는다.", "Encounter reconstruction");
        EditorUtility.SetDirty(safe); EditorUtility.SetDirty(pit);
        var seed = Ref<EquipmentData>("2900393ef75c6e542881f42f9ee141ec"); Backup(AssetDatabase.GetAssetPath(seed)); seed.icon = Painting(plan.seedIcon); EditorUtility.SetDirty(seed);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Folder + "ArtInstalled.txt", paths.Length + " independent editable PNGs imported. Existing painting GUIDs preserved; alpha geometry and same-canvas aftermath supports rebuilt. Empty reading-pedestal views preserve the original support after book destruction. Live art acceptance is recorded by root verification.\n");
    }
}
