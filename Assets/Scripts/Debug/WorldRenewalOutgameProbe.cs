#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed partial class ExplorationDepthLiveProbe
    {
        public void RunOutgameRenewal()
        {outputFolder="Docs/Production/WorldRenewal-2026-11-01/";reportPrefix="OutgameRuntime";StartCoroutine(Guarded(OutgameRenewal()));}
        IEnumerator OutgameRenewal()
        {
            yield return Settle(.6f);var menu=FindFirstObjectByType<OutGameMenuView>();Require(menu!=null,"actual saved OutGameScene menu");
            Check(menu.Page==OutGamePage.Title,"title is the start state");yield return Shot("Outgame-Title");
            Click(menu.options);yield return Settle();Check(menu.Page==OutGamePage.Options,"actual options button opens options");
            Check(!FindObjectsByType<Image>(FindObjectsSortMode.None).Any(p=>p.gameObject.activeInHierarchy&&p.sprite!=null&&p.sprite.name=="TitleLabyrinth"),"options have no exposed legacy pixel background");
            yield return Shot("Outgame-Options");Click(menu.optionsBack);yield return Settle();Click(menu.newGame);yield return Settle();
            Require(menu.selection.content.characters.Count==6,"actual six active character choices");
            for(var i=0;i<menu.selection.characterButtons.Length;i++)
            {
                Click(menu.selection.characterButtons[i]);yield return Settle();
                var character=menu.selection.content.characters[i];
                Check(menu.selection.SelectedIndex==i&&menu.detailHeading.text==character.displayName,"actual active character selection "+character.name);
                Check(menu.selection.selectionSplash.sprite==character.selectionIllustration,"actual authored selection painting "+character.name);
                yield return Shot("Outgame-"+character.name);
            }
            Click(menu.back);yield return Settle();Check(menu.Page==OutGamePage.Title,"selection returns to title normally");
            log.AppendLine("Actual title/options/six character selection art review. No settings changes or new run.");
        }
    }
}
#endif
