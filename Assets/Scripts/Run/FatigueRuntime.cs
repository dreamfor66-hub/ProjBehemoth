using UnityEngine;
namespace DungeonSlash
{
    public sealed class FatigueRuntime
    {
        public FatigueSettings Settings {get;private set;}
        public float Value {get;private set;}=100;
        public int GraceRemaining {get;private set;}
        public int Steps {get;private set;}
        public bool CampUsed {get;private set;}
        int floorGrace;
        public bool IsDead=>Value<=0;
        public int Expression=>Value>=100?0:Value>60?1:Value>10?2:3;
        public float AttackSpeedMultiplier=>Settings==null?1:Mathf.Clamp(Settings.attackSpeed.Evaluate(Value),.1f,1);
        public float FinalDamageMultiplier=>Settings==null?1:Mathf.Clamp01(Settings.finalDamage.Evaluate(Value));
        public void BeginFloor(FatigueSettings settings,DungeonGraph graph)
        { Settings=settings;Value=100;Steps=0;CampUsed=false;floorGrace=settings!=null?settings.GraceFor(graph):0;GraceRemaining=floorGrace; }
        public void Step()
        {
            if(Settings==null||IsDead)return;Steps++;
            if(GraceRemaining>0){GraceRemaining--;return;}
            Value=Mathf.Max(0,Value-Settings.costPerStep);
        }
        public bool Camp()
        {if(Settings==null||CampUsed||IsDead)return false;CampUsed=true;Value=100;GraceRemaining=floorGrace;return true;}
        public bool ShortRest()
        {if(Settings==null||IsDead||Value>=Settings.shortRestCeiling)return false;Value=Settings.shortRestCeiling;return true;}
        public void Recover(float amount,int grace=0)
        {if(IsDead)return;Value=Mathf.Clamp(Value+Mathf.Max(0,amount),0,100);GraceRemaining+=Mathf.Max(0,grace);}
    }
    public sealed partial class RunState
    {
        public FatigueRuntime Fatigue {get;}=new();
    }
}
