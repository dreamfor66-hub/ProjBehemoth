using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName="Dungeon Slash/Fatigue Settings")]
    public sealed class FatigueSettings:ScriptableObject
    {
        [Min(1)] public int minimumGraceSteps=30;
        [Range(.1f,2)] public float surveyGraceFraction=.55f;
        [Min(.1f)] public float costPerStep=2;
        [Range(1,100)] public float shortRestCeiling=60;
        [Min(0)] public float campHeal=10;
        public bool moveNormalMonstersOnCamp=true;
        [Header("Act one exploration pressure (other acts retain their baseline)")]
        [Min(1)] public int firstActMinimumGraceSteps = 10;
        [Range(.1f, 2)] public float firstActSurveyGraceFraction = .22f;
        public AnimationCurve attackSpeed=AnimationCurve.Linear(0,.35f,100,1);
        public AnimationCurve finalDamage=new(new Keyframe(0,.2f),new Keyframe(10,.3f),new Keyframe(40,.65f),new Keyframe(60,.85f),new Keyframe(75,1),new Keyframe(100,1));
        public AnimationCurve peripheralHaze=new(new Keyframe(0,.45f),new Keyframe(40,.2f),new Keyframe(70,0),new Keyframe(100,0));
        public int GraceFor(DungeonGraph graph, int act = 0)
        {
            // A round-trip survey, not half of the number of distinct cells.
            var longest=0;foreach(var distance in graph.DistancesFrom(graph.StartRoom).Values)longest=Mathf.Max(longest,distance);
            var minimum = act == 1 ? firstActMinimumGraceSteps : minimumGraceSteps;
            var fraction = act == 1 ? firstActSurveyGraceFraction : surveyGraceFraction;
            return Mathf.Max(minimum,Mathf.CeilToInt((2*(graph.Rooms.Count-1)+longest)*fraction));
        }
    }
}
