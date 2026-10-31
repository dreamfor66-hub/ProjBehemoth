using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonSlash
{
    public sealed class DungeonGenerator
    {
        private readonly DungeonGenerationSettings settings;
        public DungeonGenerator(DungeonGenerationSettings generationSettings) => settings = generationSettings;

        public DungeonRunState Generate(int seed, int floor = 1)
        {
            for (var attempt = 0; attempt < settings.maxGenerationAttempts; attempt++)
            {
                var random = new Random(seed + attempt * 7919);
                var graph = CreateGraph(random, floor);
                if (TryAssignRooms(graph, random, floor))
                { ExplorationGeneration.Populate(graph, settings.exploration, seed, floor); return new DungeonRunState(seed, graph, floor); }
            }
            throw new InvalidOperationException("Unable to create a dungeon that satisfies the configured distance constraints.");
        }

        private DungeonGraph CreateGraph(Random random, int floor)
        {
            var graph = new DungeonGraph();
            graph.AddRoom(new DungeonPosition(0, 0));
            var targetCount = random.Next(settings.minRooms, settings.maxRooms + 1);
            var opening = floor == 1 ? Math.Min(settings.firstActOpeningCombats, targetCount - 2) : 0;
            var tip = graph.StartRoom;
            for (var step = 0; step < opening; step++)
            {
                var next = graph.AddRoom(new DungeonPosition(0, step + 1));
                graph.Connect(tip, next); tip = next;
            }
            var corridorStepsRemaining = 2;
            while (graph.Rooms.Count < targetCount)
            {
                // Extend short runs before choosing another branch. Unconnected corridors never
                // touch side by side: an adjacent occupied cell must be a deliberate, valid loop.
                var candidates = graph.Rooms.OrderBy(room => corridorStepsRemaining > 0 && room == tip ? 0 : 1)
                    .ThenBy(_ => random.Next()).ToList();
                var placed = false;
                foreach (var parent in candidates)
                {
                    if (floor == 1 && !CanAddExit(graph, parent, opening)) continue;
                    var offsetStart = random.Next(4);
                    for (var i = 0; i < 4; i++)
                    {
                        var position = parent.Position + DirectionUtility.Offsets[(offsetStart + i) % 4];
                        if (graph.HasRoom(position)) continue;
                        var touching = DirectionUtility.Offsets.Select(offset => graph.At(position + offset)).Where(room => room != null).ToList();
                        if (touching.Count > 2) continue;
                        var other = touching.FirstOrDefault(room => room != parent);
                        if (other != null)
                        {
                            // Reject tight 2x2 blocks and reserve loops for genuinely separated routes.
                            if (graph.DistancesFrom(parent)[other.RoomId] < 4) continue;
                            if (floor == 1 && (!CanAddExit(graph, other, opening)
                                || graph.Rooms.Count(r => r.Connections.Count >= 3)
                                + (parent.Connections.Count == 2 ? 1 : 0)
                                + (other.Connections.Count == 2 ? 1 : 0) > settings.firstActMaximumJunctions)) continue;
                            if (random.NextDouble() > (floor == 1 ? settings.firstActLoopChance : settings.loopChance)) continue;
                        }
                        var next = graph.AddRoom(position);
                        graph.Connect(parent, next);
                        if (other != null) graph.Connect(next, other);
                        if (parent != tip || corridorStepsRemaining <= 0) corridorStepsRemaining = random.Next(2, 4);
                        corridorStepsRemaining--; tip = next;
                        placed = true;
                        break;
                    }
                    if (placed) break;
                }
                // Degree limits can leave every endpoint spatially blocked. Retry another graph instead of hanging.
                if (!placed) break;
            }

            return graph;
        }

        private bool CanAddExit(DungeonGraph graph, DungeonRoom room, int opening)
        {
            if (opening > 0 && room.Position.X == 0 && room.Position.Y >= 0 && room.Position.Y < opening) return false;
            return room.Connections.Count < settings.firstActMaximumExits
                && (room.Connections.Count < 2 || graph.Rooms.Count(r => r.Connections.Count >= 3) < settings.firstActMaximumJunctions);
        }

        private bool TryAssignRooms(DungeonGraph graph, Random random, int floor)
        {
            var start = graph.StartRoom;
            if (graph.Rooms.Count < settings.minRooms) return false;
            var distances = graph.DistancesFrom(start);
            if (distances.Count != graph.Rooms.Count) return false;
            foreach (var room in graph.Rooms) { room.Type = RoomEncounterType.Empty; room.IsMajorRoom = false; room.IsRevealed = false; room.ChestContent = ChestContent.Relic; }
            start.Type = RoomEncounterType.Start; start.IsRevealed = true;

            var boss = SelectAtDistance(graph, distances, settings.minimumBossDistance, random);
            if (boss == null) return false;
            AssignMajor(boss, RoomEncounterType.Boss);
            var unassigned = graph.Rooms.Where(room => room != start && room != boss).ToList();
            if (floor == 1)
                foreach (var room in unassigned.Where(r => distances[r.RoomId] <= settings.firstActOpeningCombats).ToList())
                { room.Type = RoomEncounterType.Combat; unassigned.Remove(room); }

            // The former major-gold room is now an ordinary-looking chest at a prominent location.
            // Floor one guarantees one gold chest here, while later floors roll a hidden chest outcome.
            for (var i = 0; i < settings.chestRoomCount; i++)
            {
                var candidate = unassigned.Where(room => distances[room.RoomId] >= settings.minimumChestDistance).OrderBy(_ => random.Next()).FirstOrDefault();
                if (candidate == null) return false;
                AssignMajor(candidate, RoomEncounterType.Chest);
                candidate.ChestContent = floor == 1 ? ChestContent.Gold : RollChestContent(random);
                unassigned.Remove(candidate);
            }
            // Elite routes become a meaningful map decision after B1: B1=1, B2=2, B3=3.
            for (var i = 0; i < Math.Max(1, floor); i++)
            {
                var candidate = unassigned.OrderByDescending(room => distances[room.RoomId]).ThenBy(_ => random.Next()).FirstOrDefault();
                if (candidate == null) return false;
                AssignMajor(candidate, RoomEncounterType.Elite); unassigned.Remove(candidate);
            }

            if (floor == 1)
            {
                if (!AssignDistant(RoomEncounterType.Camp, 1, settings.firstActCampDistance)
                    || !AssignDistant(RoomEncounterType.Reward, settings.minimumRewardRooms, settings.firstActServiceDistance)
                    || !AssignDistant(RoomEncounterType.Fountain, settings.minimumFountainRooms, settings.firstActServiceDistance)
                    || !AssignDistant(RoomEncounterType.Shop, 1, settings.firstActServiceDistance)
                    || !AssignDistant(RoomEncounterType.Goddess, settings.minimumGoddessRooms, settings.firstActServiceDistance)
                    || !AssignDistant(RoomEncounterType.Event, settings.eventRoomCount, settings.firstActServiceDistance)) return false;
            }
            else
            {
                if (!AssignDistant(RoomEncounterType.Camp, 1, 1)
                    || !AssignDistant(RoomEncounterType.Fountain, settings.minimumFountainRooms, 1)
                    || !AssignDistant(RoomEncounterType.Shop, settings.minimumShopRooms, 1)
                    || !AssignDistant(RoomEncounterType.Goddess, settings.minimumGoddessRooms, 1)
                    || !AssignDistant(RoomEncounterType.Reward, settings.minimumRewardRooms, 1)
                    || !AssignDistant(RoomEncounterType.Event, settings.eventRoomCount, 1)) return false;
            }
            var combatCount = Math.Max(1, (int)Math.Ceiling(graph.Rooms.Count * (settings.combatRoomRatioMin + settings.combatRoomRatioMax) * .5f));
            AssignMinimum(unassigned, RoomEncounterType.Combat, Math.Min(Math.Max(0, combatCount - graph.Rooms.Count(r => r.Type == RoomEncounterType.Combat)), unassigned.Count), random);
            foreach (var room in unassigned) room.Type = random.NextDouble() < .70 ? RoomEncounterType.Combat : RoomEncounterType.Empty;
            AddFloorEventVariants(graph, random, floor);
            // A wall-mounted shrine cannot occupy a four-exit junction.
            foreach (var shrine in graph.Rooms.Where(r => r.Connections.Count == 4
                && r.Type is RoomEncounterType.Goddess or RoomEncounterType.Fountain or RoomEncounterType.PoisonFountain).ToArray())
            {
                var substitute = graph.Rooms.Where(r => !r.IsMajorRoom && r.Connections.Count < 4
                    && (r.Type == RoomEncounterType.Empty || r.Type == RoomEncounterType.Event)
                    && (floor != 1 || distances[r.RoomId] >= settings.firstActServiceDistance)).OrderBy(_ => random.Next()).FirstOrDefault();
                if (substitute == null) return false;
                var serviceType = shrine.Type; shrine.Type = substitute.Type; substitute.Type = serviceType;
            }
            return FacilitiesSeparated(graph) && graph.Rooms.Where(r => r.Type == RoomEncounterType.Camp).All(r => r.Connections.Count == 1);

            bool AssignDistant(RoomEncounterType type, int count, int minimumDistance)
            {
                for (var i = 0; i < count; i++)
                {
                    var candidate = unassigned.Where(r => distances[r.RoomId] >= minimumDistance
                        && (type != RoomEncounterType.Camp || r.Connections.Count == 1)
                        && CanPlaceFacility(graph, r, type)
                        && (type != RoomEncounterType.Event || !graph.Neighbors(r).Any(n => n.Type == RoomEncounterType.Event)))
                        .OrderBy(_ => random.Next()).FirstOrDefault();
                    if (candidate == null) return false;
                    candidate.Type = type; unassigned.Remove(candidate);
                }
                return true;
            }
        }

        public static RoomEncounterType FacilityFamily(RoomEncounterType type) => type == RoomEncounterType.PoisonFountain ? RoomEncounterType.Fountain : type;
        private bool CanPlaceFacility(DungeonGraph graph, DungeonRoom room, RoomEncounterType type)
        {
            var family = FacilityFamily(type);
            if (family is not (RoomEncounterType.Shop or RoomEncounterType.Goddess or RoomEncounterType.Fountain)) return true;
            var distances = graph.DistancesFrom(room);
            return graph.Rooms.Where(r => r != room && FacilityFamily(r.Type) == family)
                .All(r => distances[r.RoomId] >= settings.minimumRepeatedFacilityDistance);
        }
        private bool FacilitiesSeparated(DungeonGraph graph) => graph.Rooms.All(r => CanPlaceFacility(graph, r, r.Type));

        private void AddFloorEventVariants(DungeonGraph graph, Random random, int floor)
        {
            // Every floor receives a second visible chest. Floor one therefore has exactly two: gold and relic.
            var candidates = graph.Rooms.Where(room => !room.IsMajorRoom && (room.Type is RoomEncounterType.Reward or RoomEncounterType.Empty)).OrderBy(_ => random.Next()).ToList();
            if (candidates.Count > 0)
            {
                candidates[0].Type = RoomEncounterType.Chest;
                candidates[0].ChestContent = floor == 1 ? ChestContent.Relic : RollChestContent(random);
                candidates[0].IsRevealed = true;
            }
            if (floor < 2) return;
            if (random.NextDouble() > .65) return;
            var toxicCandidate = graph.Rooms.Where(room => !room.IsMajorRoom && room.Type == RoomEncounterType.Empty
                && CanPlaceFacility(graph, room, RoomEncounterType.PoisonFountain)).OrderBy(_ => random.Next()).FirstOrDefault();
            if (toxicCandidate != null) toxicCandidate.Type = RoomEncounterType.PoisonFountain;
        }

        private static ChestContent RollChestContent(Random random)
        {
            var roll = random.NextDouble();
            // Across the two floor-2+ chests, this keeps the old mix close to one gold chest and
            // one relic-or-mimic chest, without revealing which chest carries which result.
            return roll < .5d ? ChestContent.Gold : roll < .825d ? ChestContent.Relic : ChestContent.Mimic;
        }

        // The boss occupies a dedicated chamber. Its altar/throne wall must not become
        // a through corridor whose doorway paints over the room's focal architecture.
        private static DungeonRoom SelectAtDistance(DungeonGraph graph, IReadOnlyDictionary<int, int> distances, int minDistance, Random random) => graph.Rooms.Where(room => distances[room.RoomId] >= minDistance && room.Connections.Count==1).OrderBy(_ => random.Next()).FirstOrDefault();
        private static void AssignMajor(DungeonRoom room, RoomEncounterType type) { room.Type = type; room.IsMajorRoom = true; room.IsRevealed = true; }
        private static void AssignMinimum(List<DungeonRoom> rooms, RoomEncounterType type, int count, Random random)
        {
            for (var i = 0; i < count && rooms.Count > 0; i++)
            {
                var index = random.Next(rooms.Count); rooms[index].Type = type; rooms.RemoveAt(index);
            }
        }
    }

    public static class DungeonGraphValidator
    {
        public static bool HasExpectedStructure(DungeonRunState state, DungeonGenerationSettings settings)
        {
            var graph = state.Graph;
            var distances = graph.DistancesFrom(graph.StartRoom);
            var baseCount = graph.Rooms.Count(r => !r.IsOptionalAnnex);
            return baseCount >= settings.minRooms && baseCount <= settings.maxRooms &&
                   distances.Count == graph.Rooms.Count &&
                   graph.Rooms.Any(room => room.Type == RoomEncounterType.Boss && distances[room.RoomId] >= settings.minimumBossDistance) &&
                   graph.Rooms.Count(room => room.Type == RoomEncounterType.Chest) >= settings.chestRoomCount + 1;
        }
    }
}
