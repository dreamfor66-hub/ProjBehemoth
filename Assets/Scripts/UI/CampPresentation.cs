using UnityEngine;
namespace DungeonSlash
{
    public sealed partial class GameFlowController
    {
        public Sprite campIllustration;
        Sprite CampArt=>UnityEngine.Object.FindFirstObjectByType<ArcadeRasterCamera>()?.presentation?.arcade==false?null:campIllustration;
        private void ShowCamp(DungeonRoom room)
        {
            State=GameFlowState.RoomEvent;var fatigue=runController.State.Fatigue;
            expansionView.ShowModal(GameText.Get("ui.camp.title"),GameText.Get("ui.camp.intro"),new[]{
                new PresentationChoice{Title=GameText.Get(fatigue.CampUsed?"ui.camp.used":"ui.camp.full"),Description=GameText.Format("ui.camp.full.detail",fatigue.Capacity,content.fatigue.campHeal),Summary=GameText.Format("ui.camp.full.summary",fatigue.Capacity,content.fatigue.campHeal),Enabled=!fatigue.CampUsed,Selected=()=>
                {
                    if(!fatigue.Camp())return;runController.State.Player.Heal(content.fatigue.campHeal);
                    var moved=content.fatigue.moveNormalMonstersOnCamp?CampMonsterMovement.Move(dungeonController.State):0;
                    RefreshHud();CampResult(GameText.Format(moved>0?"ui.camp.full.result.moved":"ui.camp.full.result",fatigue.Capacity,content.fatigue.campHeal));
                }},
                new PresentationChoice{Title=GameText.Get("ui.camp.short"),Description=GameText.Format("ui.camp.short.detail",Mathf.CeilToInt(fatigue.ShortRestCeiling)),Enabled=fatigue.Value<fatigue.ShortRestCeiling,Selected=()=>
                {if(!fatigue.ShortRest())return;RefreshHud();CampResult(GameText.Format("ui.camp.short.result",Mathf.CeilToInt(fatigue.ShortRestCeiling)));}},
                new PresentationChoice{Title=GameText.Get("ui.camp.leave"),Selected=ShowNavigation}
            },CampArt,true);
        }
        private void CampResult(string text)=>expansionView.ShowModal(GameText.Get("ui.camp.title"),text,new[]{new PresentationChoice{Title=GameText.Get("ui.event.continue"),Selected=ShowNavigation}},CampArt);
    }
}
