using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace DungeonSlash.Tests
{
    public sealed partial class BehemothPlaytests
    {
        [UnityTest] public IEnumerator Typography_AllEventsAndSixCharacterDescriptions_ReadableWithoutLegacyText()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity"); yield return new EnterPlayMode();
            var selection = Object.FindFirstObjectByType<OutGameView>();
            for (var i = 0; i < 6; i++)
            {
                selection.Select(i); yield return null; selection.startingRelic.ForceMeshUpdate();
                Assert.That(selection.startingRelic.font, Is.Not.Null);
                Assert.That(selection.startingRelic.fontSize, Is.GreaterThanOrEqualTo(18));
                Assert.That(selection.startingRelic.textBounds.size.y, Is.LessThanOrEqualTo(selection.startingRelic.rectTransform.rect.height + 1), selection.content.characters[i].displayName);
            }
            yield return new ExitPlayMode(); EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity"); yield return new EnterPlayMode(); SetupCharacter(CharacterKind.Warrior);
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
            var ui = Object.FindFirstObjectByType<RunExpansionView>();
            foreach (var story in Content.events)
            {
                ui.ShowModal(story.displayName, story.narrative, story.options.Select(option => new PresentationChoice { Title = option.title, Description = option.description }), story.illustration);
                yield return null; ui.modalNarrative.ForceMeshUpdate();
                Assert.That(ui.modalNarrative.textBounds.size.y, Is.LessThanOrEqualTo(ui.modalNarrative.rectTransform.rect.height + 1), story.id + " narrative");
                foreach (var choice in ui.choices.Where(choice => choice.button.gameObject.activeSelf))
                { choice.description.ForceMeshUpdate(); Assert.That(choice.description.textBounds.size.y, Is.LessThanOrEqualTo(choice.description.rectTransform.rect.height + 1), story.id + " choice"); }
            }
            ScreenCapture.CaptureScreenshot("Logs/Behemoth-EventTypography.png"); yield return null;
            Matrix("한국어 TMP: 6직업 시작 설명과 20이벤트 본문·선택지 높이 검사 / 레거시 Text 없음"); yield return new ExitPlayMode();
        }
    }
}
