using UnityEngine;
namespace DungeonSlash
{
    [CreateAssetMenu(menuName="Dungeon Slash/Fatigue Settings")]
    public sealed class FatigueSettings:ScriptableObject
    {
        [Min(.1f)] public float costPerStep=2;
        [Range(1,100), Tooltip("Percentage of the actual floor's derived fatigue capacity restored by a brief rest.")] public float shortRestCeiling=60;
        [Min(0)] public float campHeal=10;
        public bool moveNormalMonstersOnCamp=true;
        public AnimationCurve attackSpeed=new(new Keyframe(0,.35f),new Keyframe(10,.55f),new Keyframe(30,.7f),new Keyframe(50,.86f),new Keyframe(60,.95f),new Keyframe(70,1),new Keyframe(100,1));
        public AnimationCurve finalDamage=new(new Keyframe(0,.2f),new Keyframe(10,.65f),new Keyframe(30,.85f),new Keyframe(45,1),new Keyframe(100,1));
        public AnimationCurve peripheralHaze=new(new Keyframe(0,.45f),new Keyframe(40,.2f),new Keyframe(70,0),new Keyframe(100,0));
    }
}
