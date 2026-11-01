using UnityEngine;
namespace DungeonSlash
{
    public sealed class FatigueRuntime
    {
        public FatigueSettings Settings {get;private set;}
        public float Value {get;private set;}=100;
        public int Capacity {get;private set;}=100;
        public FatigueFloorBudget FloorBudget {get;private set;}
        // Curves remain designer-authored on a 0–100 condition scale. A larger
        // floor extends the walking budget, not the strength of a combat penalty.
        public float Percent=>100f*Value/Mathf.Max(1,Capacity);
        public float ShortRestCeiling=>Capacity*(Settings==null ? .6f : Settings.shortRestCeiling/100f);
        // Compatibility for historical verification records; there is no hidden reserve.
        public int GraceRemaining => 0;
        public int Steps {get;private set;}
        public bool CampUsed {get;private set;}
        public bool IsDead=>Value<=0;
        public bool IsAtEase=>AttackSpeedMultiplier>=.99999f&&FinalDamageMultiplier>=.99999f;
        public int Expression=>IsAtEase?0:FinalDamageMultiplier>=.99999f?1:Percent>10?2:3;
        public float AttackSpeedMultiplier=>Settings==null?1:Mathf.Clamp(Settings.attackSpeed.Evaluate(Percent),.1f,1);
        public float FinalDamageMultiplier=>Settings==null?1:Mathf.Clamp01(Settings.finalDamage.Evaluate(Percent));
        public void BeginFloor(FatigueSettings settings,DungeonGraph graph,int act=0)
        { Settings=settings;FloorBudget=FatigueFloorBudget.For(graph,settings==null?2:settings.costPerStep);Capacity=FloorBudget.Capacity;Value=Capacity;Steps=0;CampUsed=false; }
        public void Step()
        {
            if(Settings==null||IsDead)return;Steps++;
            Value=Mathf.Max(0,Value-Settings.costPerStep);
        }
        public bool Camp()
        {if(Settings==null||CampUsed||IsDead)return false;CampUsed=true;Value=Capacity;return true;}
        public bool ShortRest()
        {if(Settings==null||IsDead||Value>=ShortRestCeiling)return false;Value=ShortRestCeiling;return true;}
        public void Recover(float amount,int grace=0)
        {if(IsDead)return;Value=Mathf.Clamp(Value+Mathf.Max(0,amount),0,Capacity);}
    }
    public sealed partial class RunState
    {
        public FatigueRuntime Fatigue {get;}=new();
    }
}
