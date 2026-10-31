using UnityEngine;
namespace DungeonSlash
{
    // These feedback layers never move actor paintings or alter hit/guard timing.
    public sealed class CombatPressureFeedback : MonoBehaviour
    {
        public CombatController combat;
        public RunController run;
        public CombatSceneView scene;
        public MonsterImpactGraphic impact;
        public HealthDangerGraphic danger;
        PlayerCombatRuntime previous;
        float lastHp, hurtUntil;
        public float RecentDamage => Mathf.Clamp01((hurtUntil-Time.unscaledTime)/.8f);
        void Update()
        {
            var player=run!=null?run.State?.Player:null;
            if(player!=previous){previous=player;lastHp=player?.CurrentHp??0;hurtUntil=0;}
            if(player==null){if(danger!=null)danger.SetIntensity(0);return;}
            if(player.CurrentHp<lastHp)hurtUntil=Time.unscaledTime+.8f;
            lastHp=player.CurrentHp;
            var low=player.IsAlive?Mathf.Clamp01((.35f-player.CurrentHp/Mathf.Max(1,player.MaxHp))/.30f):0;
            var pulse=.75f+.25f*Mathf.Sin(Time.unscaledTime*4);
            if(danger!=null)danger.SetIntensity(Mathf.Max(RecentDamage*.82f,low*pulse*.68f));
        }
        public void MonsterAttack(Vector2 origin,bool charged,DamageResult result)
        {
            // Legacy callers can still clear the removed impact; health-edge feedback is driven by HP.
            if(impact!=null)impact.Clear();
        }
        public void ResetFeedback(){hurtUntil=0;if(impact!=null)impact.Clear();if(danger!=null)danger.SetIntensity(0);}
    }
}
