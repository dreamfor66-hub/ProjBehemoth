namespace DungeonSlash
{
    public interface IRoomEncounter
    {
        RoomEncounterType Type { get; }
        void Enter(DungeonRoom room, RunState context);
        bool IsComplete { get; }
    }

    public abstract class InstantRoomEncounter : IRoomEncounter
    {
        public abstract RoomEncounterType Type { get; }
        public bool IsComplete { get; protected set; }
        public virtual void Enter(DungeonRoom room, RunState context) { IsComplete = true; room.IsCleared = true; }
    }
    public sealed class EmptyRoomEncounter : InstantRoomEncounter { public override RoomEncounterType Type => RoomEncounterType.Empty; }
    public sealed class RewardRoomEncounter : InstantRoomEncounter { public override RoomEncounterType Type => RoomEncounterType.Reward; public override void Enter(DungeonRoom room, RunState context) { base.Enter(room, context); context.GainGold(24); } }
    public sealed class FountainRoomEncounter : InstantRoomEncounter { public override RoomEncounterType Type => RoomEncounterType.Fountain; public override void Enter(DungeonRoom room, RunState context) { base.Enter(room, context); context.Player.Heal(35f); } }
    public sealed class GoddessRoomEncounter : InstantRoomEncounter { public override RoomEncounterType Type => RoomEncounterType.Goddess; public override void Enter(DungeonRoom room, RunState context) { base.Enter(room, context); context.GainExperience(35); } }
    public sealed class MajorRewardRoomEncounter : InstantRoomEncounter { public override RoomEncounterType Type => RoomEncounterType.MajorReward; public override void Enter(DungeonRoom room, RunState context) { base.Enter(room, context); context.GainGold(48); } }
}
