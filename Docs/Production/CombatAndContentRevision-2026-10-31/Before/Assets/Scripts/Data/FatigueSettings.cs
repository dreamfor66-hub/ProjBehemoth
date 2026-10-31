using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName="Dungeon Slash/Fatigue Settings")]
    public sealed class FatigueSettings:ScriptableObject
    {
        [Min(.1f)] public float costPerStep=2;
        [Range(1,100)] public float shortRestCeiling=60;
        [Min(0)] public float campHeal=10;
        public bool moveNormalMonstersOnCamp=true;
        public AnimationCurve attackSpeed=AnimationCurve.Linear(0,.35f,100,1);
        public AnimationCurve finalDamage=new(new Keyframe(0,.2f),new Keyframe(10,.3f),new Keyframe(40,.65f),new Keyframe(60,.85f),new Keyframe(75,1),new Keyframe(100,1));
        public AnimationCurve peripheralHaze=new(new Keyframe(0,.45f),new Keyframe(40,.2f),new Keyframe(70,0),new Keyframe(100,0));
    }
}
