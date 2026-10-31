using System;
using System.Collections.Generic;
using System.Linq;
namespace DungeonSlash
{
    public static class CampMonsterMovement
    {
        // Move living normal encounters only. No resurrection or destination-based encounter reroll.
        public static int Move(DungeonRunState state)
        {
            var random=new Random(unchecked(state.Seed+state.Floor*12671));
            var origins=state.Graph.Rooms.Where(r=>r.Type==RoomEncounterType.Combat&&!r.IsCleared&&!r.IsOptionalAnnex).OrderBy(_=>random.Next()).ToArray();
            var occupied=new HashSet<int>();var moves=new List<(DungeonRoom from,DungeonRoom to)>();
            foreach(var from in origins)
            {
                var to=state.Graph.Neighbors(from).Where(r=>r.Type==RoomEncounterType.Empty&&!r.IsOptionalAnnex&&r!=state.Player.CurrentRoom&&!occupied.Contains(r.RoomId)&&state.CanTravel(from,r)).OrderBy(_=>random.Next()).FirstOrDefault();
                if(to==null)continue;occupied.Add(to.RoomId);moves.Add((from,to));
            }
            foreach(var (from,to) in moves)
            {
                to.Type=RoomEncounterType.Combat;to.IsCleared=false;to.EncounterOriginId=from.EncounterOriginId;
                from.Type=RoomEncounterType.Empty;from.IsCleared=true;
            }
            return moves.Count;
        }
    }
}
