using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DungeonSlash.Tests
{
    public sealed class DungeonMapLayoutTests
    {
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void CorridorsRemainConnectedWithoutPackedBlocks(int floor)
        {
            var settings = ScriptableObject.CreateInstance<DungeonGenerationSettings>();
            try
            {
                foreach (var seed in new[] { 333, 5581, 8812, 9217, 20261031 })
                {
                    var state = new DungeonGenerator(settings).Generate(seed, floor);
                    Assert.That(DungeonGraphValidator.HasExpectedStructure(state, settings), Is.True, $"floor={floor}, seed={seed}");
                    Assert.That(state.Graph.Rooms.Count(r => r.Type == RoomEncounterType.Event), Is.EqualTo(settings.eventRoomCount));
                    foreach (var room in state.Graph.Rooms)
                    {
                        foreach (var offset in DirectionUtility.Offsets)
                        {
                            var adjacent = state.Graph.At(room.Position + offset);
                            Assert.That(adjacent == null || room.Connections.Contains(adjacent.RoomId), Is.True, "Separate corridors must not touch");
                        }
                        var p = room.Position;
                        Assert.That(state.Graph.HasRoom(p + new DungeonPosition(1, 0))
                            && state.Graph.HasRoom(p + new DungeonPosition(0, 1))
                            && state.Graph.HasRoom(p + new DungeonPosition(1, 1)), Is.False, "No packed 2x2 blocks");
                    }
                    if (floor == 1)
                    {
                        Assert.That(state.Graph.Rooms.Count(r => r.Connections.Count >= 3), Is.LessThanOrEqualTo(settings.firstActMaximumJunctions));
                        for (var i = 1; i <= settings.firstActOpeningCombats; i++)
                            Assert.That(state.Graph.At(new DungeonPosition(0, i)).Type, Is.EqualTo(RoomEncounterType.Combat));
                    }
                }
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [Test]
        public void RoomHierarchyIsAuthoredWithoutChangingEncounterState()
        {
            var style = ScriptableObject.CreateInstance<DungeonMapStyle>();
            try
            {
                var room = new DungeonRoom(8, new DungeonPosition(1, 2)) { Type = RoomEncounterType.Shop };
                Assert.That(style.SizeFor(room), Is.EqualTo(style.roomSize));
                room.Type = RoomEncounterType.Combat;
                Assert.That(style.SizeFor(room), Is.EqualTo(style.corridorEventSize));
                style.largeRoomTypes.Add(RoomEncounterType.Combat);
                Assert.That(style.SizeFor(room), Is.EqualTo(style.roomSize));
                Assert.That(room.IsVisited || room.IsRevealed || room.IsMajorRoom, Is.False);
            }
            finally { Object.DestroyImmediate(style); }
        }
    }
}
