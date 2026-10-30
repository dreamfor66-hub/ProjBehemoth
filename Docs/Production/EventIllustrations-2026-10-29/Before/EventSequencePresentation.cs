using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class GameFlowController
    {
        private Sprite activeStoryIllustration;
        private void ShowEventConclusion(RandomEventData story,string text,int paragraph=0)
        {
            var paragraphs=SplitEventParagraphs(text);
            expansionView.ShowModal(story.displayName,paragraphs.Length>0?paragraphs[paragraph]:"",new[]{new PresentationChoice
            {
                Title=GameText.Get("ui.event.continue"),Selected=()=>
                {
                    if(paragraph+1<paragraphs.Length)ShowEventConclusion(story,text,paragraph+1);
                    else {roomTransitionView?.RevealScene();AfterRoomResolution();}
                }
            }},activeStoryIllustration!=null?activeStoryIllustration:story.illustration);
        }
        private void ShowEventSequence(DungeonRoom room, RandomEventData story)
        {
            var route = story.entryRoutes.FirstOrDefault(r => r.requirements.All(c => c.Matches(runController.State)));
            ShowEventPage(room, story, route?.page ?? 0);
        }
        private void ShowEventPage(DungeonRoom room, RandomEventData story, int index, int lineIndex = 0)
        {
            if (index < 0 || index >= story.pages.Count) { room.IsCleared = true; AfterRoomResolution(); return; }
            var page = story.pages[index];
            if (lineIndex == 0)
            {
                var route = page.routes.FirstOrDefault(r => r.requirements.All(c => c.Matches(runController.State)));
                if (route != null && route.page != index) { ShowEventPage(room, story, route.page); return; }
            }
            var lines = SplitEventParagraphs(page.narrative);
            var illustration = page.illustration != null ? page.illustration : story.illustration;
            activeStoryIllustration = illustration;
            if (lineIndex < lines.Length - 1)
            {
                expansionView.ShowModal(story.displayName, lines[lineIndex], new[] { new PresentationChoice { Title = GameText.Get("ui.event.continue"), Selected = () => ShowEventPage(room, story, index, lineIndex + 1) } }, illustration);
                return;
            }
            var choices = page.options.Select(option =>
            {
                var unavailable = RandomEventSystem.UnavailableReason(option, runController.State);
                var costs = new List<string>();
                if (option.goldCost > 0) costs.Add(GameText.Format("ui.gameflowexpansion.77b4b9fc", option.goldCost));
                if (option.hpCost > 0) costs.Add(GameText.Format("ui.gameflowexpansion.bd8c797", option.hpCost));
                if (option.maxHpCost > 0) costs.Add(GameText.Format("ui.gameflowexpansion.5b6889a1", option.maxHpCost));
                if (option.consumeHealingPotion) costs.Add(GameText.Get("ui.gameflowexpansion.ffbb9952"));
                if (option.supplyCost > 0) costs.Add(option.supply.displayName + " −" + option.supplyCost);
                return new PresentationChoice
                {
                    Title = option.title, Description = option.description, Requirements=string.Join(" · ",new[]{string.Join(" · ",costs),option.failurePage<0?unavailable:null}.Where(s=>!string.IsNullOrWhiteSpace(s))),
                    PreviewItem=option.effects.Select(e=>e.relic).FirstOrDefault(r=>r!=null),
                    Enabled = unavailable == null || option.failurePage >= 0,
                    Selected = () =>
                    {
                        if (unavailable != null && option.failurePage >= 0) { ShowEventPage(room, story, option.failurePage); return; }
                        if (!RandomEventSystem.Pay(option, runController.State)) { ShowEventPage(room, story, index, lineIndex); return; }
                        if (option.nextPage >= 0) { ShowEventPage(room, story, option.nextPage); return; }
                        room.IsCleared = true;
                        var messages = new List<string>(); if (!string.IsNullOrWhiteSpace(option.resultText)) messages.Add(option.resultText);
                        ApplyEventEffects(story, option.effects, 0, messages);
                    }
                };
            }).ToArray();
            if (choices.Length == 0) choices = new[] { new PresentationChoice { Title = GameText.Get("ui.event.leave"), Selected = () => { room.IsCleared = true; AfterRoomResolution(); } } };
            expansionView.ShowModal(story.displayName, lines.LastOrDefault() ?? "", choices, illustration);
        }

        private static string[] SplitEventParagraphs(string text) =>
            (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n')
                .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
    }
}
