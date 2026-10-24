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
                new PresentationChoice{Title=GameText.Get(fatigue.CampUsed?"ui.camp.used":"ui.camp.full"),Description=GameText.Get("ui.camp.full.detail"),Summary=GameText.Get("ui.camp.full.summary"),Enabled=!fatigue.CampUsed,Selected=()=>
                {
                    if(!fatigue.Camp())return;runController.State.Player.Heal(content.fatigue.campHeal);
                    var moved=content.fatigue.moveNormalMonstersOnCamp?CampMonsterMovement.Move(dungeonController.State):0;
                    RefreshHud();CampResult(GameText.Get(moved>0?"ui.camp.full.result.moved":"ui.camp.full.result"));
                }},
                new PresentationChoice{Title=GameText.Get("ui.camp.short"),Description=GameText.Get("ui.camp.short.detail"),Enabled=fatigue.Value<content.fatigue.shortRestCeiling,Selected=()=>
                {if(!fatigue.ShortRest())return;RefreshHud();CampResult(GameText.Get("ui.camp.short.result"));}},
                new PresentationChoice{Title=GameText.Get("ui.camp.leave"),Selected=ShowNavigation}
            },CampArt,true);
        }
        private void CampResult(string text)=>expansionView.ShowModal(GameText.Get("ui.camp.title"),text,new[]{new PresentationChoice{Title=GameText.Get("ui.event.continue"),Selected=ShowNavigation}},CampArt);
    }
}
