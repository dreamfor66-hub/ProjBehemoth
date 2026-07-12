using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public enum FacingDirection { North, East, South, West }
    public enum RelativeDirection { Forward, Left, Right, Back }

    [Serializable]
    public readonly struct DungeonPosition : IEquatable<DungeonPosition>
    {
        public int X { get; }
        public int Y { get; }
        public DungeonPosition(int x, int y) { X = x; Y = y; }
        public static DungeonPosition operator +(DungeonPosition a, DungeonPosition b) => new(a.X + b.X, a.Y + b.Y);
        public bool Equals(DungeonPosition other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is DungeonPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";
    }

    public static class DirectionUtility
    {
        public static readonly DungeonPosition[] Offsets = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };
        public static DungeonPosition Offset(FacingDirection direction) => Offsets[(int)direction];
        public static FacingDirection FromDelta(DungeonPosition delta)
        {
            if (delta.X > 0) return FacingDirection.East;
            if (delta.X < 0) return FacingDirection.West;
            return delta.Y >= 0 ? FacingDirection.North : FacingDirection.South;
        }
        public static FacingDirection ToAbsolute(FacingDirection facing, RelativeDirection relative)
        {
            var offset = relative switch { RelativeDirection.Forward => 0, RelativeDirection.Left => -1, RelativeDirection.Right => 1, _ => 2 };
            return (FacingDirection)(((int)facing + offset + 4) % 4);
        }
        public static RelativeDirection ToRelative(FacingDirection facing, FacingDirection target)
        {
            var delta = ((int)target - (int)facing + 4) % 4;
            return delta switch { 0 => RelativeDirection.Forward, 1 => RelativeDirection.Right, 2 => RelativeDirection.Back, _ => RelativeDirection.Left };
        }
    }

    public sealed class DungeonRoom
    {
        public int RoomId { get; }
        public DungeonPosition Position { get; }
        public RoomEncounterType Type { get; set; }
        public bool IsVisited { get; set; }
        public bool IsCleared { get; set; }
        public bool IsRevealed { get; set; }
        public bool IsMajorRoom { get; set; }
        public HashSet<int> Connections { get; } = new();
        public DungeonRoom(int id, DungeonPosition position) { RoomId = id; Position = position; }
    }

    public sealed class DungeonGraph
    {
        private readonly Dictionary<int, DungeonRoom> byId = new();
        private readonly Dictionary<DungeonPosition, DungeonRoom> byPosition = new();
        public IReadOnlyCollection<DungeonRoom> Rooms => byId.Values;
        public DungeonRoom StartRoom { get; private set; }

        public DungeonRoom AddRoom(DungeonPosition position)
        {
            var room = new DungeonRoom(byId.Count, position);
            byId.Add(room.RoomId, room); byPosition.Add(position, room);
            StartRoom ??= room;
            return room;
        }
        public bool HasRoom(DungeonPosition position) => byPosition.ContainsKey(position);
        public DungeonRoom At(DungeonPosition position) => byPosition.TryGetValue(position, out var room) ? room : null;
        public DungeonRoom ById(int id) => byId.TryGetValue(id, out var room) ? room : null;
        public void Connect(DungeonRoom a, DungeonRoom b) { if (a != null && b != null && a != b) { a.Connections.Add(b.RoomId); b.Connections.Add(a.RoomId); } }
        public IEnumerable<DungeonRoom> Neighbors(DungeonRoom room) => room.Connections.Select(ById).Where(roomValue => roomValue != null);
        public Dictionary<int, int> DistancesFrom(DungeonRoom origin)
        {
            var distances = new Dictionary<int, int> { [origin.RoomId] = 0 };
            var queue = new Queue<DungeonRoom>(); queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in Neighbors(current))
                {
                    if (distances.ContainsKey(neighbor.RoomId)) continue;
                    distances[neighbor.RoomId] = distances[current.RoomId] + 1;
                    queue.Enqueue(neighbor);
                }
            }
            return distances;
        }
    }

    public sealed class PlayerDungeonState
    {
        public DungeonRoom CurrentRoom { get; private set; }
        public FacingDirection Facing { get; private set; }
        public PlayerDungeonState(DungeonRoom start) { CurrentRoom = start; Facing = FacingDirection.North; start.IsVisited = true; start.IsRevealed = true; }
        public void MoveTo(DungeonRoom destination)
        {
            var delta = new DungeonPosition(destination.Position.X - CurrentRoom.Position.X, destination.Position.Y - CurrentRoom.Position.Y);
            Facing = DirectionUtility.FromDelta(delta);
            CurrentRoom = destination;
            destination.IsVisited = true; destination.IsRevealed = true;
        }
    }

    public sealed class DungeonRunState
    {
        public int Seed { get; }
        public DungeonGraph Graph { get; }
        public PlayerDungeonState Player { get; }
        private readonly HashSet<string> revealedConnections = new();
        public DungeonRunState(int seed, DungeonGraph graph) { Seed = seed; Graph = graph; Player = new PlayerDungeonState(graph.StartRoom); }
        public void RevealConnection(DungeonRoom from, DungeonRoom to) => revealedConnections.Add(ConnectionKey(from, to));
        public bool IsConnectionRevealed(DungeonRoom from, DungeonRoom to) => revealedConnections.Contains(ConnectionKey(from, to));
        private static string ConnectionKey(DungeonRoom a, DungeonRoom b) => a.RoomId < b.RoomId ? $"{a.RoomId}:{b.RoomId}" : $"{b.RoomId}:{a.RoomId}";
    }
}
