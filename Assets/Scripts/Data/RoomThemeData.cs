using System;
using UnityEngine;

namespace DungeonSlash
{
    /// <summary>Authored room architecture, with fixed world walls rather than visit-random scenery.</summary>
    [CreateAssetMenu(menuName="Dungeon Slash/Presentation/Room Theme")]
    public sealed class RoomThemeData : ScriptableObject
    {
        [Serializable] public sealed class ActPainting
        {
            [Tooltip("A rotationally symmetric chamber may share one painting. Use the directional paintings for asymmetric architecture.")]
            public Sprite walls;
            [Tooltip("Optional complete views facing North / East / South / West. Never rerolled on re-entry.")]
            public Sprite[] directionalWalls = new Sprite[4];
        }
        [Serializable] public sealed class WallMark
        {
            public FacingDirection wall;
            public Sprite painting;
        }
        public ActPainting[] acts = new ActPainting[3];
        [Tooltip("Flat architectural marks fixed to compass walls. Only intact visible walls display them.")]
        public WallMark[] wallMarks = Array.Empty<WallMark>();
        [Tooltip("Neutral stone paintings can take the floor's common scene lighting.")]
        public bool useFloorLighting;

        public Sprite Resolve(int act, FacingDirection facing)
        {
            if(acts==null||acts.Length==0)return null;
            var art=acts[Mathf.Clamp(act-1,0,acts.Length-1)];if(art==null)return null;
            var i=(int)facing;
            return art.directionalWalls!=null&&i<art.directionalWalls.Length&&art.directionalWalls[i]!=null?art.directionalWalls[i]:art.walls;
        }
    }
}
