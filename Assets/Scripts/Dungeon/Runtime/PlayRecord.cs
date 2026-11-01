using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DungeonSlash
{
    [Serializable] public sealed class PlayRecord
    {
        public int version = 1;
        public string createdUtc, build, character, artRevision;
        public List<FloorRecord> floors = new();
    }
    [Serializable] public sealed class FloorRecord
    {
        public int floor, seed, currentRoom, facing;
        public List<RoomRecord> rooms = new();
        public List<PassageRecord> passages = new();
        public List<int> trail = new();
        public int[] keys;
    }
    [Serializable] public sealed class RoomRecord
    {
        public int id, x, y, type, chest, origin, corner, objectFacing, wall, lastEntryFacing = -1;
        public bool visited, cleared, revealed, major, annex, alcove, placed, storyPrepared, investigated, removed, used;
        public bool merchantPurchased, merchantDeparted, conversationStarted, conversationIgnored, canRearApproach;
        public string story, subject, aftermath, revisit, background, roomTheme;
        public string[] paintings, monsters;
        public int[] connections, keys;
        public List<int> entries = new();
        public List<string> choices = new();
    }
    [Serializable] public sealed class PassageRecord
    {
        public int origin, entrance, searches, clue;
        public bool concealed, discovered, locked;
    }

    public static class PlayRecordCodec
    {
        public const string Prefix = "BHM1-";
        const int MaxBytes = 2 * 1024 * 1024;
        public static string Encode(PlayRecord record)
        {
            var json = Encoding.UTF8.GetBytes(JsonUtility.ToJson(record));
            using var output = new MemoryStream();
            using (var zip = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, true)) zip.Write(json, 0, json.Length);
            var data = output.ToArray();
            return Prefix + Convert.ToBase64String(data).TrimEnd('=').Replace('+','-').Replace('/','_') + "." + Checksum(data);
        }
        public static bool TryDecode(string code, out PlayRecord record, out string errorKey)
        {
            record = null; errorKey = "ui.playrecord.invalid";
            try
            {
                code = (code ?? "").Trim();
                if (code.Length > MaxBytes || !code.StartsWith(Prefix, StringComparison.Ordinal)) return false;
                var split = code.LastIndexOf('.'); if (split < Prefix.Length || code.Length - split != 17) return false;
                var base64 = code.Substring(Prefix.Length, split - Prefix.Length).Replace('-','+').Replace('_','/');
                var data = Convert.FromBase64String(base64.PadRight((base64.Length + 3) / 4 * 4, '='));
                if (!string.Equals(Checksum(data), code.Substring(split + 1), StringComparison.OrdinalIgnoreCase)) return false;
                using var source = new MemoryStream(data); using var zip = new DeflateStream(source, CompressionMode.Decompress); using var target = new MemoryStream();
                var buffer = new byte[8192]; int count;
                while ((count = zip.Read(buffer,0,buffer.Length)) > 0)
                { if (target.Length + count > MaxBytes) return false; target.Write(buffer,0,count); }
                var value = JsonUtility.FromJson<PlayRecord>(Encoding.UTF8.GetString(target.ToArray()));
                if (value == null || value.version != 1 || value.floors == null || value.floors.Count == 0 || value.floors.Count > 3) return false;
                if (value.floors.Any(f=>f==null) || value.floors.Select(f => f.floor).Distinct().Count() != value.floors.Count) return false;
                foreach (var floor in value.floors)
                {
                    if (floor == null || floor.floor < 1 || floor.floor > 3 || floor.rooms == null || floor.rooms.Count < 1 || floor.rooms.Count > 128
                        || floor.passages == null || floor.passages.Count > 8 || floor.facing < 0 || floor.facing > 3
                        || floor.currentRoom < 0 || floor.currentRoom >= floor.rooms.Count || floor.trail==null
                        || floor.trail.Count>4096 || floor.trail.Any(id=>id<0||id>=floor.rooms.Count)) return false;
                    var positions = new HashSet<DungeonPosition>();
                    for (var i = 0; i < floor.rooms.Count; i++)
                    {
                        var room = floor.rooms[i];
                        if (room == null || room.id != i || Math.Abs((long)room.x) > 128 || Math.Abs((long)room.y) > 128
                            || !positions.Add(new DungeonPosition(room.x,room.y)) || room.connections == null || room.connections.Length > 4
                            || room.connections.Distinct().Count() != room.connections.Length
                            || !Enum.IsDefined(typeof(RoomEncounterType),room.type) || !Enum.IsDefined(typeof(ChestContent),room.chest)
                            || room.corner < 0 || room.corner > 3 || room.objectFacing < 0 || room.objectFacing > 3 || room.wall < 0 || room.wall > 3
                            || room.entries == null || room.entries.Count > 512 || room.entries.Any(e=>e<0||e>3)
                            || room.lastEntryFacing < -1 || room.lastEntryFacing > 3 || room.choices == null || room.choices.Count > 128
                            || room.choices.Any(c=>c==null||c.Length>8192) || room.paintings==null || room.paintings.Length>4
                            || room.keys==null || room.keys.Any(k=>k<0||k>=floor.rooms.Count)) return false;
                        foreach (var id in room.connections)
                        {
                            if (id < 0 || id >= floor.rooms.Count || id == i) return false;
                            var other = floor.rooms[id];
                            if (other?.connections == null || !other.connections.Contains(i)
                                || Math.Abs(other.x-room.x)+Math.Abs(other.y-room.y)!=1) return false;
                        }
                    }
                    var pairs = new HashSet<string>();
                    foreach (var p in floor.passages)
                        if (p == null || p.origin < 0 || p.origin >= floor.rooms.Count || p.entrance < 0 || p.entrance >= floor.rooms.Count
                            || !floor.rooms[p.origin].connections.Contains(p.entrance) || p.searches < 0 || p.searches > 100
                            || p.concealed && p.locked || !pairs.Add(Math.Min(p.origin,p.entrance)+":"+Math.Max(p.origin,p.entrance))) return false;
                }
                record = value; return true;
            }
            catch (Exception e) when (e is ArgumentException or FormatException or InvalidDataException or IOException or OverflowException)
            { return false; }
        }
        static string Checksum(byte[] data)
        { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(data),0,8).Replace("-",""); }

        public static DungeonRunState Restore(FloorRecord saved, IEnumerable<RandomEventData> stories, IEnumerable<RoomObjectData> objects)
        {
            var events = stories.Where(e=>e!=null).GroupBy(e=>e.id).ToDictionary(g=>g.Key,g=>g.First());
            var paintings = objects.Where(o=>o!=null).GroupBy(o=>o.name).ToDictionary(g=>g.Key,g=>g.First());
            var graph = new DungeonGraph();
            foreach (var item in saved.rooms)
            {
                var r = graph.AddRoom(new DungeonPosition(item.x,item.y));
                r.Type=(RoomEncounterType)item.type;r.EncounterOriginId=item.origin;r.ChestContent=(ChestContent)item.chest;
                r.IsVisited=item.visited;r.IsCleared=item.cleared;r.IsRevealed=item.revealed;r.IsMajorRoom=item.major;r.IsOptionalAnnex=item.annex;r.IsExplorationAlcove=item.alcove;
                r.ObjectCorner=item.corner;r.ObjectFacing=(FacingDirection)item.objectFacing;r.ObjectWall=(FacingDirection)item.wall;
                r.ObjectPlacementPrepared=item.placed;r.StoryPrepared=item.storyPrepared;r.StoryInvestigated=item.investigated;
                r.ObjectRemoved=item.removed;r.ObjectUsed=item.used;r.RevisitTextKey=item.revisit;
                if(!string.IsNullOrEmpty(item.story)&&events.TryGetValue(item.story,out var story))r.Story=story;
                if(!string.IsNullOrEmpty(item.aftermath)&&paintings.TryGetValue(item.aftermath,out var art))r.ObjectAftermath=art;
                if(item.keys!=null)r.KeyTokens.AddRange(item.keys);
            }
            foreach(var item in saved.rooms)foreach(var id in item.connections)graph.Connect(graph.ById(item.id),graph.ById(id));
            foreach(var item in saved.passages)
            {
                var p=new DungeonPassage(graph.ById(item.origin),graph.ById(item.entrance),item.concealed,item.locked,item.clue);
                for(var i=0;i<item.searches;i++)p.Search(int.MaxValue);
                if(item.discovered)p.Discover();graph.Passages.Add(p);
            }
            var state=new DungeonRunState(saved.seed,graph,saved.floor){Inspection=true};
            state.Player.Inspect(graph.ById(saved.currentRoom),(FacingDirection)saved.facing);
            if(saved.keys!=null)state.Keys.UnionWith(saved.keys);
            return state;
        }

        public static string SaveReadable(PlayRecord record, bool imported)
        {
            var dir=Path.Combine(Application.persistentDataPath,"PlayRecords");Directory.CreateDirectory(dir);
            var code=Encode(record);var id=code.Substring(code.LastIndexOf('.')+1);
            var path=Path.Combine(dir,(imported?"Imported-":"Run-")+id+".json");
            File.WriteAllText(path,JsonUtility.ToJson(record,true),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(dir,"Latest"+(imported?"Imported":"Run")+".txt"),path,new UTF8Encoding(false));
            return path;
        }
    }
}
