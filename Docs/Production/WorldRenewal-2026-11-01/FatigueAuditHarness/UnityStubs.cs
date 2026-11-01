namespace UnityEngine
{
    public class ScriptableObject { }
    public class CreateAssetMenuAttribute:System.Attribute { public string menuName; }
    public class MinAttribute:System.Attribute { public MinAttribute(float value){} }
    public class RangeAttribute:System.Attribute { public RangeAttribute(float a,float b){} }
    public class TooltipAttribute:System.Attribute { public TooltipAttribute(string a){} }
    public class HeaderAttribute:System.Attribute { public HeaderAttribute(string a){} }
    public class SerializeField:System.Attribute { }
    public class HideInInspector:System.Attribute { }
    public readonly struct Keyframe {public readonly float time,value;public Keyframe(float t,float v){time=t;value=v;}}
    public class AnimationCurve
    {
        readonly Keyframe[] keys; public AnimationCurve(params Keyframe[] k){keys=k;}
        public float Evaluate(float t)
        { if(t<=keys[0].time)return keys[0].value;for(int i=1;i<keys.Length;i++)if(t<=keys[i].time)return keys[i-1].value+(keys[i].value-keys[i-1].value)*(t-keys[i-1].time)/(keys[i].time-keys[i-1].time);return keys[^1].value; }
    }
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);
        public static float Clamp01(float x)=>Math.Clamp(x,0,1);
    }
}
namespace UnityEngine.Serialization { public class FormerlySerializedAsAttribute:System.Attribute { public FormerlySerializedAsAttribute(string a){} } }
namespace DungeonSlash
{
    public class RoomObjectData { }
    public class RandomEventData { }
    public enum RoomEncounterType { Start,Combat,Reward,Fountain,PoisonFountain,Chest,Goddess,Shop,Elite,MajorReward,Boss,Empty,Event,Camp }
    public enum ChestContent { Gold,Relic,Mimic }
}
