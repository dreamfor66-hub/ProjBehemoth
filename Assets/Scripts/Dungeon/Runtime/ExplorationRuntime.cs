using System.Collections.Generic;
using System.Linq;

namespace DungeonSlash
{
    public sealed class DungeonPassage
    {
        public DungeonRoom Origin { get; }
        public DungeonRoom Annex { get; }
        public bool Concealed { get; }
        public bool Discovered { get; private set; }
        public bool Locked { get; private set; }
        public int KeyToken => Annex.RoomId;
        public int ClueVariant { get; }
        public DungeonPassage(DungeonRoom origin, DungeonRoom annex, bool concealed, bool locked, int clueVariant = 0)
        { Origin = origin; Annex = annex; Concealed = concealed; Discovered = !concealed; Locked = locked; ClueVariant = clueVariant; }
        public void Discover() => Discovered = true;
        public bool Unlock(ISet<int> keys)
        {
            if (!Locked) return true;
            if (!keys.Remove(KeyToken)) return false;
            Locked = false; return true;
        }
        public DungeonRoom Other(DungeonRoom room) => room == Origin ? Annex : Origin;
        public bool Connects(DungeonRoom a, DungeonRoom b) => a == Origin && b == Annex || a == Annex && b == Origin;
    }

    public static class ExplorationPlacement
    {
        public static FacingDirection WallFor(DungeonGraph graph, DungeonRoom room, int seed)
        {
            var walls = Enumerable.Range(0, 4).Select(i => (FacingDirection)i).Where(d =>
            {
                var other = graph.At(room.Position + DirectionUtility.Offset(d));
                return other == null || !room.Connections.Contains(other.RoomId);
            }).ToArray();
            return walls.Length == 0 ? FacingDirection.North : walls[(seed & int.MaxValue) % walls.Length];
        }
        public static bool HasWall(DungeonGraph graph, DungeonRoom room) => room.Connections.Count < 4;
        public static int RelativeCorner(FacingDirection facing, int corner) => (corner - (int)facing + 4) % 4;
    }
}
