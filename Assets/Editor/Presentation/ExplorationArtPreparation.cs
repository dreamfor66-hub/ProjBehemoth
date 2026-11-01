using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEditor;
using UnityEngine;

// Read-only geometry/lighting follows the editable PNGs automatically, including later designer edits.
[InitializeOnLoad]
internal static class ExplorationArtPreparation
{
    const string Report = "Docs/Production/Exploration-2026-10-31/IntegrationReview/";
    static double next;
    static string last;
    [Serializable] sealed class LayoutSet { public Layout[] floors; }
    [Serializable] sealed class Layout { public string asset; public Frame[] doorways; public Vector2[] walls, corners; }
    [Serializable] struct Frame { public float x, y, width, height; public Rect Rect => new(x, y, width, height); }
    static ExplorationArtPreparation() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 3;
        if (!Directory.Exists("Assets/Art/World/Exploration") || !Directory.Exists(Report)) return;
        const string layoutRequest = "Docs/Production/Exploration-2026-10-31/BackgroundRevision/Layout.request";
        if (File.Exists(layoutRequest))
        {
            var layout = JsonUtility.FromJson<LayoutSet>(File.ReadAllText(layoutRequest));
            foreach (var entry in layout.floors)
            {
                var data = AssetDatabase.LoadAssetAtPath<FloorArtwork>(entry.asset);
                if (data == null) throw new FileNotFoundException(entry.asset);
                data.openingPlacement = entry.doorways.Select(f => f.Rect).ToArray();
                if (entry.walls?.Length == 3) data.wallObjectSupports = entry.walls;
                if (entry.corners?.Length == 4) data.floorObjectCorners = entry.corners;
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets(); File.Delete(layoutRequest);
            File.WriteAllText("Docs/Production/Exploration-2026-10-31/BackgroundRevision/LayoutInstalled.txt", "Saved authored opening bounds and room composition sockets in FloorArtwork assets.\n");
        }
        var floors = AssetDatabase.FindAssets("t:FloorArtwork").Select(g => AssetDatabase.LoadAssetAtPath<FloorArtwork>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(f => f != null && f.walls != null && f.openings?.Length >= 3 && f.openings.All(s => s != null)).ToArray();
        var objects = AssetDatabase.FindAssets("t:RoomObjectData").Select(g => AssetDatabase.LoadAssetAtPath<RoomObjectData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        var paths = Directory.GetFiles("Assets/Art/World/Exploration", "*.png").Concat(floors.SelectMany(f => f.openings.Append(f.walls)).Select(AssetDatabase.GetAssetPath)).Distinct().OrderBy(p => p);
        var signature = "integration-v5\n" + string.Join("\n", paths.Select(p => p + ":" + File.GetLastWriteTimeUtc(p).Ticks + ":" + new FileInfo(p).Length))
            + "\n" + string.Join("\n", floors.Select(f => f.name + ":" + string.Join(";", (f.openingPlacement ?? Array.Empty<Rect>()).Select(r => r.x + "," + r.y + "," + r.width + "," + r.height))))
            + "\n" + string.Join("\n", objects.Select(o => o.name + ":" + string.Join(",", new[] { o.front, o.side, o.rear, o.usedFront, o.usedSide, o.usedRear }.Select(s => s == null ? "" : AssetDatabase.GetAssetPath(s)))));
        if (last == signature || File.Exists(Report + "DerivedSignature.txt") && File.ReadAllText(Report + "DerivedSignature.txt") == signature) { last = signature; return; }
        try
        {
            var log = new System.Text.StringBuilder();
            foreach (var data in objects)
            {
                var sprites = new[] { data.front, data.side, data.rear, data.usedFront, data.usedSide, data.usedRear }.Where(s => s != null).Distinct();
                data.SetDerivedGeometry(sprites.Select(Geometry).ToArray()); EditorUtility.SetDirty(data);
            }
            foreach (var floor in floors)
            {
                floor.openingContours = floor.openings.Select((s, i) => Contour(s, floor.openingPlacement != null && i < floor.openingPlacement.Length ? floor.openingPlacement[i] : default, floor.walls)).ToArray();
                floor.objectIllumination = Illumination(floor.walls); EditorUtility.SetDirty(floor);
                log.AppendLine(floor.name + " illumination=" + floor.objectIllumination);
                foreach (var c in floor.openingContours) log.AppendLine("  aperture: " + c.bounds + "; canvas=" + c.canvasScale + "/" + c.canvasOffset + "; exposure=" + c.exposure);
            }
            AssetDatabase.SaveAssets(); File.WriteAllText(Report + "DerivedGeometry.txt", log.ToString());
            File.WriteAllText(Report + "DerivedSignature.txt", signature); last = signature;
        }
        catch (Exception e) { last = signature; File.WriteAllText(Report + "DerivedError.txt", e.ToString()); Debug.LogException(e); }
    }
    static Texture2D Read(Sprite sprite)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)))) throw new IOException("Cannot read " + sprite.name);
        return texture;
    }
    internal static RoomObjectData.PaintingGeometry Geometry(Sprite sprite)
    {
        var t = Read(sprite);
        try
        {
            var p = t.GetPixels32(); var minX = t.width; var minY = t.height; var maxX = 0; var maxY = 0;
            for (var y = 0; y < t.height; y++) for (var x = 0; x < t.width; x++) if (p[y * t.width + x].a >= 180)
            { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
            // Flat dark baked shadows are not physical feet. Find the material resting on the ground.
            var supportY = maxY;
            for (var y = minY; y < minY + (maxY - minY) * .30f && supportY == maxY; y++) for (var x = minX; x <= maxX; x++)
            { var c = p[y * t.width + x]; if (c.a >= 180 && Math.Max(c.r, Math.Max(c.g, c.b)) > 55) { supportY = y; break; } }
            if (supportY == maxY) supportY = minY;
            var footMin = t.width; var footMax = 0;
            for (var y = supportY; y < supportY + (maxY - minY) * .22f; y++) for (var x = minX; x <= maxX; x++) if (p[y * t.width + x].a >= 180 && Math.Max(p[y * t.width + x].r, Math.Max(p[y * t.width + x].g, p[y * t.width + x].b)) > 55)
            { footMin = Math.Min(footMin, x); footMax = Math.Max(footMax, x); }
            if (footMin > footMax) { footMin = minX; footMax = maxX; }
            return new RoomObjectData.PaintingGeometry { sprite = sprite,
                bounds = Rect.MinMaxRect((float)minX / t.width, (float)minY / t.height, (maxX + 1f) / t.width, (maxY + 1f) / t.height),
                footprint = Rect.MinMaxRect((float)footMin / t.width, (float)supportY / t.height, (footMax + 1f) / t.width, (supportY + (maxY - minY) * .22f) / t.height) };
        }
        finally { UnityEngine.Object.DestroyImmediate(t); }
    }
    static Color Illumination(Sprite sprite)
    {
        var t = Read(sprite);
        try
        {
            var pixels = t.GetPixels32(); var sum = Vector3.zero; var count = 0;
            // Broad masonry midtones: exclude ink voids, torch highlights, and saturated trim.
            for (var y = (int)(t.height * .30f); y < t.height * .78f; y += 5)
                for (var x = (int)(t.width * .10f); x < t.width * .9f; x += 5)
                {
                    Color c = pixels[y * t.width + x]; var hi = Mathf.Max(c.r, c.g, c.b); var lo = Mathf.Min(c.r, c.g, c.b);
                    if (hi < .13f || hi > .65f || hi - lo > .20f) continue;
                    sum += new Vector3(c.r, c.g, c.b); count++;
                }
            var mean = sum / Mathf.Max(1, count); var maximum = Mathf.Max(mean.x, mean.y, mean.z);
            var exposure = Mathf.Clamp(maximum * 1.6f + .33f, .62f, .90f);
            var chroma = Vector3.Lerp(Vector3.one, mean / Mathf.Max(.01f, maximum), .85f) * exposure;
            return new Color(chroma.x, chroma.y, chroma.z, 1);
        }
        finally { UnityEngine.Object.DestroyImmediate(t); }
    }
    static FloorArtwork.OpeningContour Contour(Sprite sprite, Rect placement, Sprite walls)
    {
        var t = Read(sprite);
        try
        {
            var w = t.width; var h = t.height; var p = t.GetPixels32(); var stride = w + 1;
            var scale = Vector2.one; var offset = Vector2.zero;
            var exposure = 1f;
            if (placement.width > 0 && placement.height > 0)
            {
                var opaque = new List<int>();
                for (var i = 0; i < p.Length; i++) if (p[i].a >= 180) opaque.Add(i);
                if (opaque.Count == 0) throw new InvalidDataException("Empty doorway painting " + sprite.name);
                var lower = new Vector2(opaque.Min(i => i % w) / (float)w, opaque.Min(i => i / w) / (float)h);
                var upper = new Vector2((opaque.Max(i => i % w) + 1f) / w, (opaque.Max(i => i / w) + 1f) / h);
                scale = new Vector2(placement.width / (upper.x - lower.x), placement.height / (upper.y - lower.y));
                offset = placement.min - Vector2.Scale(lower, scale);
                // Match ambient exposure of the actual mounting wall; geometry/style remain authored paintings.
                var wall = Read(walls);
                try
                {
                    var room = wall.GetPixels32(); var roomValues = new List<float>(); var paintValues = new List<float>();
                    for (var y = Mathf.Max(0, (int)(placement.yMin * wall.height)); y < Mathf.Min(wall.height, placement.yMax * wall.height); y += 3)
                        for (var x = Mathf.Max(0, (int)(placement.xMin * wall.width)); x < Mathf.Min(wall.width, placement.xMax * wall.width); x += 3)
                            Sample(room[y * wall.width + x], roomValues);
                    foreach (var i in opaque.Where((_, n) => n % 3 == 0)) Sample(p[i], paintValues);
                    roomValues.Sort(); paintValues.Sort();
                    if (roomValues.Count > 0 && paintValues.Count > 0) exposure = Mathf.Clamp(roomValues[roomValues.Count / 2] / paintValues[paintValues.Count / 2], .45f, 1f);
                    void Sample(Color32 c, List<float> values)
                    { var value = Mathf.Max(c.r, c.g, c.b) / 255f; if (value > .12f && value < .65f) values.Add(value); }
                }
                finally { UnityEngine.Object.DestroyImmediate(wall); }
            }
            var integral = new int[(w + 1) * (h + 1)];
            for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
            {
                var c = p[y * w + x]; var ink = c.a > 240 && Math.Max(c.r, Math.Max(c.g, c.b)) < 38 ? 1 : 0;
                integral[(y + 1) * stride + x + 1] = ink + integral[y * stride + x + 1] + integral[(y + 1) * stride + x] - integral[y * stride + x];
            }
            const int r = 4; var mask = new bool[w * h]; var seen = new bool[w * h];
            for (var y = r; y < h - r; y++) for (var x = r; x < w - r; x++)
            {
                var a = (y - r) * stride + x - r; var b = (y + r + 1) * stride + x + r + 1;
                mask[y * w + x] = integral[b] - integral[(y - r) * stride + x + r + 1] - integral[(y + r + 1) * stride + x - r] + integral[a] == (2 * r + 1) * (2 * r + 1);
            }
            // Erosion disconnects fine ink contours from the broad dark interior of the aperture.
            var queue = new Queue<int>(); var component = new List<int>(); var largest = new List<int>();
            for (var seed = 0; seed < mask.Length; seed++)
            {
                if (!mask[seed] || seen[seed]) continue;
                component.Clear(); queue.Enqueue(seed); seen[seed] = true;
                while (queue.Count > 0)
                {
                    var at = queue.Dequeue(); component.Add(at);
                    Visit(at - 1); Visit(at + 1); Visit(at - w); Visit(at + w);
                }
                if (component.Count > largest.Count) largest = new List<int>(component);
            }
            if (largest.Count < 100) throw new InvalidDataException("No aperture interior in " + sprite.name);
            var rows = new SortedDictionary<int, Vector2Int>();
            foreach (var at in largest)
            {
                var x = at % w; var y = at / w;
                rows[y] = rows.TryGetValue(y, out var row) ? new Vector2Int(Math.Min(row.x, x), Math.Max(row.y, x)) : new Vector2Int(x, x);
            }
            var left = new List<Vector2>(); var right = new List<Vector2>();
            foreach (var pair in rows)
            {
                // One strip per pixel keeps the existing hand-drawn arch silhouette exact.
                left.Add(Vector2.Scale(new Vector2((pair.Value.x - 2f) / w, (float)pair.Key / h), scale) + offset);
                right.Add(Vector2.Scale(new Vector2((pair.Value.y + 3f) / w, (float)pair.Key / h), scale) + offset);
            }
            return new FloorArtwork.OpeningContour { left = left.ToArray(), right = right.ToArray(), canvasScale = scale, canvasOffset = offset, exposure = exposure,
                bounds = Rect.MinMaxRect(left.Min(v => v.x), left.Min(v => v.y), right.Max(v => v.x), right.Max(v => v.y)) };
            void Visit(int at) { if (at >= 0 && at < mask.Length && mask[at] && !seen[at]) { seen[at] = true; queue.Enqueue(at); } }
        }
        finally { UnityEngine.Object.DestroyImmediate(t); }
    }
}
