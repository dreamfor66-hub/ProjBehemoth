using UnityEngine;
using UnityEngine.UI;
namespace DungeonSlash
{
    public sealed class FatigueHudView:MonoBehaviour
    {
        public RunController run;
        public FatigueFaces faces;
        public Image face;
        public HoverTooltipTrigger hover;
        public RunExpansionView expansion;
        public FatigueHaze haze;
        RunState previous;int last=-1;
        public void Refresh()
        {
            var state=run.State;if(state==null)return;var f=state.Fatigue;
            face.gameObject.SetActive(expansion==null||!expansion.IsDecisionOpen);
            face.sprite=faces.Face(state.Player.Data.character,f.Expression);face.color=Color.white;
            if(faces.useStageTint)face.color=Color.Lerp(new Color(1f,.53f,.43f),Color.white,Mathf.Clamp01(f.Value/60f));
            if(haze!=null){haze.intensity=f.Settings==null?0:f.Settings.peripheralHaze.Evaluate(f.Value);haze.SetVerticesDirty();}
            var hash=Mathf.CeilToInt(f.Value)*10000+f.GraceRemaining;
            if(previous==state&&last==hash)return;previous=state;last=hash;
            var stage=GameText.Get(f.Value>=100?"ui.fatigue.normal":f.Value>60?"ui.fatigue.tired":f.Value>10?"ui.fatigue.exhausted":"ui.fatigue.danger");
            var body=GameText.Format("ui.fatigue.detail",Mathf.CeilToInt(f.Value),stage,Mathf.RoundToInt((1-f.AttackSpeedMultiplier)*100),Mathf.RoundToInt((1-f.FinalDamageMultiplier)*100));
            if(f.Value<=10)body+="\n\n"+GameText.Get("ui.fatigue.warning");
            hover.Bind(GameText.Get("ui.fatigue.title"),body);
        }
        void LateUpdate()=>Refresh();
    }
}
