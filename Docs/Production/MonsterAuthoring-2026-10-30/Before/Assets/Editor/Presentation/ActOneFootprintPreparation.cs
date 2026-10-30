using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class ActOneFootprintPreparation
{
    const string Folder = "Docs/Production/ActOneExperience-2026-10-28/";
    const string Root = "Assets/Data/Presentation/ActOne/";
    const string Atlas = "Assets/Art/World/ActOne/StoneSphinx-Attack.png";
    static bool busy; static double next;
    static ActOneFootprintPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next
            || File.Exists(Folder + "FootprintsInstalled.txt") || !File.Exists(Folder + "GuardRoleInstalled.txt") || !File.Exists(Atlas)) return;
        next = EditorApplication.timeSinceStartup + 5; busy = true;
        try { Prepare(); File.WriteAllText(Folder + "FootprintsInstalled.txt", "Species-authored footprints; never shrink groups.\n"); }
        catch (Exception e) { File.WriteAllText(Folder + "Footprints-Error.txt", e.ToString()); Debug.LogException(e); }
        finally { busy = false; }
    }
    static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static void Save(Object item) { EditorUtility.SetDirty(item); AssetDatabase.SaveAssetIfDirty(item); }
    static T Make<T>(string path, Func<T> create) where T : Object
    { var value = Asset<T>(path); if (value == null) { value = create(); AssetDatabase.CreateAsset(value, path); } return value; }
    static void Backup(Object asset)
    {
        var path = AssetDatabase.GetAssetPath(asset); var destination = Folder + "BeforeFootprints/" + path;
        if (File.Exists(destination)) return; Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(path, destination);
    }
    static Color32[] Pixels(Texture2D texture)
    {
        var previous = RenderTexture.active; var render = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(texture, render); RenderTexture.active = render;
        var read = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        read.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); read.Apply();
        var pixels = read.GetPixels32(); Object.DestroyImmediate(read); RenderTexture.active = previous; RenderTexture.ReleaseTemporary(render); return pixels;
    }
    static Rect Bounds(Texture2D texture, Rect source)
    {
        var pixels = Pixels(texture); int left = (int)source.xMax, right = (int)source.xMin, bottom = (int)source.yMax, top = (int)source.yMin;
        for (var y = (int)source.yMin; y < source.yMax; y++) for (var x = (int)source.xMin; x < source.xMax; x++)
            if (pixels[y * texture.width + x].a > 24) { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
        return new Rect(Math.Max(0, left - 3), Math.Max(0, bottom - 3), Math.Min(texture.width - Math.Max(0, left - 3), right - left + 7), Math.Min(texture.height - Math.Max(0, bottom - 3), top - bottom + 7));
    }
    static void CropPoses(CombatSpriteAnimation animation, string id)
    {
        Backup(animation);
        var poses = new[] { animation.idle, animation.ready, animation.attack, animation.dead };
        var bounds = poses.Select(s => s == null ? default : Bounds(s.texture, s.rect)).ToArray();
        var minimum = bounds.Where(r => r.width > 0).Min(r => r.xMin); var maximum = bounds.Max(r => r.xMax);
        for (var i = 0; i < poses.Length; i++)
        {
            if (poses[i] == null) continue;
            var rect = new Rect(minimum, bounds[i].yMin, maximum - minimum, bounds[i].height);
            var sprite = Make(Root + "Sprites/" + id + "-" + i + ".asset", () => Sprite.Create(poses[i].texture, rect, new Vector2(.5f, 0), 100, 0, SpriteMeshType.FullRect));
            sprite.name = id + "-" + i; Save(sprite);
            if (i == 0) animation.idle = sprite; else if (i == 1) animation.ready = sprite; else if (i == 2) animation.attack = sprite; else animation.dead = sprite;
        }
        animation.groundContactY = 0; animation.artworkTopY = 1; animation.tightPoseFrames = true; Save(animation);
    }
    static void Prepare()
    {
        Directory.CreateDirectory(Root + "Sprites"); AssetDatabase.Refresh();
        var entries = AngularWorldPreparation.Entries().Where(e => e.floor == 1).ToArray();
        var widths = new[] { 270f, 500, 360, 330, 430, 540, 460, 620, 820, 540, 700, 220 };
        var heights = new[] { 330f, 330, 420, 300, 360, 650, 560, 530, 630, 690, 590, 230 };
        var bodies = new[] { new Vector2(140,170), new Vector2(360,160), new Vector2(200,260), new Vector2(220,160), new Vector2(290,170),
            new Vector2(340,400), new Vector2(250,340), new Vector2(400,270), new Vector2(520,340), new Vector2(330,420), new Vector2(420,310), new Vector2(140,130) };
        var log = "Authored frame sizes (not normalized to a common square); union sprite crop retains proportions.\n";
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i]; var monster = Asset<MonsterData>("Assets/Data/Combat/Monsters/" + entry.baseline + ".asset"); Backup(monster);
            monster.useAuthoredFootprint = true; monster.combatScale = 1; monster.bodySize = bodies[i]; monster.artworkSize = new Vector2(widths[i], heights[i]);
            monster.weakPointLayoutScale = new Vector2(i >= 5 && i != 11 ? 2.5f : 1.5f, i >= 5 && i != 11 ? 1.8f : 1.3f);
            CropPoses(monster.combatAnimation, entry.id); Save(monster);
            log += $"{monster.displayName}: artwork={monster.artworkSize}, body={monster.bodySize}, weakPointLayout={monster.weakPointLayoutScale}\n";
        }
        var stage = Asset<StageSetData>("Assets/Data/Dungeon/Stages/Maze.asset");
        foreach (var encounter in stage.earlyCombats.Concat(stage.hardCombats).Concat(stage.elites).Concat(stage.bosses).Distinct())
        {
            Backup(encounter);
            for (var i = 0; i < encounter.members.Count; i++)
            {
                var member = encounter.members[i]; member.scale = 1;
                if (encounter.members.Count == 2) member.offset.x = i == 0 ? -300 : 300;
                if (encounter.members.Count >= 3) member.offset.x = i == 0 ? 0 : i % 2 == 1 ? -380 : 380;
            }
            Save(encounter);
        }
        PrepareSphinx();
        File.WriteAllText(Folder + "Footprints.txt", log);
    }
    static void PrepareSphinx()
    {
        var monster = Asset<MonsterData>("Assets/Data/Combat/Monsters/Data_Monster_B1_Boss_MazeGuardian.asset");
        var animation = monster.combatAnimation;
        var sprites = new Sprite[3];
        var states = new[] { "Idle", "Ready", "Attack" };
        for (var i = 0; i < states.Length; i++)
        {
            var source = "Assets/Art/World/ActOne/StoneSphinx-" + states[i] + ".png";
            AssetDatabase.ImportAsset(source, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(source);
            importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096; importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
            var texture = Asset<Texture2D>(source); var rect = Bounds(texture, new Rect(0, 0, texture.width, texture.height));
            sprites[i] = Make(Root + "Sprites/StoneSphinx-Spread-" + states[i] + ".asset", () => Sprite.Create(texture, rect, new Vector2(.5f, 0), 100, 0, SpriteMeshType.FullRect));
        }
        animation.idle = sprites[0]; animation.ready = sprites[1]; animation.attack = sprites[2]; animation.dead = sprites[0];
        animation.illustratedLayout = animation.tightPoseFrames = true;
        animation.illustratedMotion = animation.emphasizePoses = false;
        animation.groundContactY = 0; animation.artworkTopY = 1; animation.canvasScale = 1; animation.combatPositionOffset = Vector2.zero;
        animation.readyTilt = 0; animation.attackTilt = 0; animation.readyOffset = animation.attackOffset = Vector2.zero;
        animation.readyScale = animation.strikeScale = Vector2.one; Save(animation);
        monster.portrait = sprites[0]; monster.useAuthoredFootprint = true; monster.combatScale = 1;
        monster.artworkSize = new Vector2(1120, 500); monster.bodySize = new Vector2(1000, 500); monster.weakPointLayoutScale = Vector2.one;
        monster.bodyLobes = new List<MonsterBodyLobe> { new() { center = new Vector2(0, 0), radius = 205 },
            new() { center = new Vector2(-365, 115), radius = 155 }, new() { center = new Vector2(365, 115), radius = 155 },
            new() { center = new Vector2(-170, -170), radius = 95 }, new() { center = new Vector2(170, -170), radius = 95 } };
        monster.encounterHint = "펼친 양 날개와 두 앞발의 약점을 이어 베어 거체의 강타를 끊자.";
        foreach (var attack in monster.GetAttacks(MonsterAttackType.Charge))
        {
            Backup(attack);
            attack.chargeWeakPoints = new List<WeakPointDefinition> {
                new() { id = "left-wing", localPosition = new Vector2(-385, 100), hitRadius = 36, requiredChargeHits = 1 },
                new() { id = "left-forepaw", localPosition = new Vector2(-170, -205), hitRadius = 36, requiredChargeHits = 1 },
                new() { id = "right-forepaw", localPosition = new Vector2(170, -205), hitRadius = 36, requiredChargeHits = 1 },
                new() { id = "right-wing", localPosition = new Vector2(385, 100), hitRadius = 36, requiredChargeHits = 1 } };
            Save(attack);
        }
        Save(monster);
        BehemothTextEditor.SyncAsset(monster); Save(monster);
        var csv = File.ReadAllText(BehemothTextEditor.CsvPath); var serialized = new SerializedObject(monster);
        foreach (var binding in monster.textKeys)
        {
            var value = serialized.FindProperty(binding.propertyPath)?.stringValue ?? "";
            var previous = binding.propertyPath == "encounterHint" ? "필수 약점들을 연결해 베어 강타를 끊자." : value;
            csv = BehemothLocaleEditing.PatchCsv(csv, binding.key, previous, value, AssetDatabase.GetAssetPath(monster) + ":" + binding.propertyPath);
        }
        Save(BehemothTextEditor.Table); File.WriteAllText(BehemothTextEditor.CsvPath, csv, new System.Text.UTF8Encoding(true)); AssetDatabase.ImportAsset(BehemothTextEditor.CsvPath);
        File.WriteAllText(Folder + "SphinxFootprint.txt", "Large nearby boss, not elongated body. 1120 x 500 art; four weak points on two wings and two forepaws. Group member count never changes species size.\n");
    }
}
