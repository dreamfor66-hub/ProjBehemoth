using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace DungeonSlash.Tests
{
    public sealed class UiFinishTests
    {
        bool previousRunInBackground;
        bool changedRunInBackground;
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);

        [Test] public void SavedFrames_HaveFourEqualEdges_AndLayoutMargins()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
            var panels=Object.FindObjectsByType<CrispPanelImage>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Assert.That(panels.Length,Is.GreaterThan(20));
            foreach(var name in new[]{"StoryPanel","ShopPanel","RoomEventPanel","RunResultPanel"})
            {
                var panel=panels.Single(item=>item.name==name);
                Assert.That(panel.rectTransform.anchorMin,Is.EqualTo(Vector2.zero),name);
                Assert.That(panel.rectTransform.anchorMax,Is.EqualTo(Vector2.one),name);
                Assert.That(panel.rectTransform.sizeDelta,Is.EqualTo(new Vector2(-96,-96)),name);
                Assert.That(panel.color.a,Is.EqualTo(1),name+" has an opaque reading surface");
            }
            var example=panels.Single(item=>item.name=="ArcadeStatus");
            Assert.That(example.rectTransform.anchorMin.x,Is.Zero);
            Assert.That(example.rectTransform.anchorMax.x,Is.EqualTo(1));
            Assert.That(example.rectTransform.sizeDelta.x,Is.Zero);
            Assert.That(example.borderThickness,Is.Zero,"HUD background is borderless");
            Assert.That(example.color,Is.EqualTo(Color.black));
            example=panels.Single(item=>item.name=="StoryPanel");
            Canvas.ForceUpdateCanvases();
            using(var vertices=new VertexHelper())
            {
                typeof(CrispPanelImage).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null).Invoke(example,new object[]{vertices});
                var mesh=new Mesh();
                try
                {
                    vertices.FillMesh(mesh);var points=mesh.vertices;
                    Assert.That(points.Length,Is.EqualTo(20));
                    Assert.That(Mathf.Abs(points[5].y-points[4].y),Is.EqualTo(4).Within(.01));
                    Assert.That(Mathf.Abs(points[9].y-points[8].y),Is.EqualTo(4).Within(.01));
                    Assert.That(Mathf.Abs(points[14].x-points[12].x),Is.EqualTo(4).Within(.01));
                    Assert.That(Mathf.Abs(points[18].x-points[16].x),Is.EqualTo(4).Within(.01));
                    Assert.That(example.mainTexture,Is.EqualTo(Texture2D.whiteTexture));
                }
                finally {Object.DestroyImmediate(mesh);}
            }
            var guard=Object.FindFirstObjectByType<SlantedGuardGauge>(FindObjectsInactive.Include);
            Assert.That(guard.border,Is.EqualTo(4));
            Assert.That(guard.fillColor.r,Is.EqualTo(guard.fillColor.g).Within(.001));
            Assert.That(guard.fillColor.g,Is.EqualTo(guard.fillColor.b).Within(.001));
            var hud=Object.FindFirstObjectByType<RunHudView>(FindObjectsInactive.Include);
            var health=Field<TextMeshProUGUI>(hud,"healthText");var gold=Field<TextMeshProUGUI>(hud,"goldText");
            Assert.That(health.rectTransform.anchoredPosition.y,Is.EqualTo(gold.rectTransform.anchoredPosition.y));
            Assert.That(health.textWrappingMode,Is.EqualTo(TextWrappingModes.NoWrap));
            Assert.That(gold.textWrappingMode,Is.EqualTo(TextWrappingModes.NoWrap));
        }

        [UnityTest] public IEnumerator Tutorials_HoldFirstBattleClocks_UntilConfirmation_AndShowOncePerRun()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
            yield return new EnterPlayMode();
            previousRunInBackground=Application.runInBackground;changedRunInBackground=true;
            Application.runInBackground=true;
            // EnterPlayMode can return before MonoBehaviour.Start when the Editor is unfocused.
            yield return null;yield return null;
            var flow=Object.FindFirstObjectByType<GameFlowController>();
            var initializedBy=Time.realtimeSinceStartup+10;
            while(flow.State==GameFlowState.RunStart&&Time.realtimeSinceStartup<initializedBy)yield return null;
            Assert.That(flow.State,Is.EqualTo(GameFlowState.DungeonNavigation),"Run initialization has completed");
            var expansion=Object.FindFirstObjectByType<RunExpansionView>();
            var combat=Object.FindFirstObjectByType<CombatController>();
            var hud=Object.FindFirstObjectByType<ArcadeHudController>();
            Assert.That(GameText.Database.Contains("ui.tutorial.exploration.title"),Is.True);
            Assert.That(GameText.Database.Contains("ui.tutorial.combat.body"),Is.True);
            Assert.That(expansion.IsModalOpen,Is.True,"Exploration begins with a deliberate modal");
            Assert.That(expansion.modalTitle.text,Is.EqualTo(GameText.Get("ui.tutorial.exploration.title")));
            Assert.That(hud.hint.gameObject.activeSelf,Is.False,"No loose bottom/central hint remains");
            Assert.That(combat.InputPaused,Is.True);
            expansion.ActiveChoices[0].button.onClick.Invoke();yield return null;
            Assert.That(expansion.IsModalOpen,Is.False);
            var target=combat.GetDebugMonsterCatalog().First();
            Assert.That(flow.StartCheatBattle(target),Is.True);
            var timeout=Time.realtimeSinceStartup+10;
            while((!expansion.IsModalOpen||expansion.modalTitle.text!=GameText.Get("ui.tutorial.combat.title"))&&Time.realtimeSinceStartup<timeout)yield return null;
            Assert.That(expansion.modalTitle.text,Is.EqualTo(GameText.Get("ui.tutorial.combat.title")),"Flow="+flow.State+"; tutorialShown="+hud.CombatTutorialShown+"; paused="+UnityEditor.EditorApplication.isPaused);
            Assert.That(combat.IsActive,Is.False,"Encounter clocks have not been started");
            Assert.That(combat.InputPaused,Is.True);
            var enemyTimer=combat.Enemies[0].NormalAttackTimer;
            combat.Player.StartAttackCooldown();var cooldown=combat.Player.AttackCooldownRemaining;var hp=combat.Player.CurrentHp;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(combat.Enemies[0].NormalAttackTimer,Is.EqualTo(enemyTimer));
            Assert.That(combat.Player.AttackCooldownRemaining,Is.EqualTo(cooldown));
            Assert.That(combat.Player.CurrentHp,Is.EqualTo(hp));
            expansion.ActiveChoices[0].button.onClick.Invoke();yield return null;yield return null;
            Assert.That(combat.IsActive,Is.True);Assert.That(combat.InputPaused,Is.False);
            Assert.That(hud.CombatTutorialShown,Is.True);
            Assert.That(hud.ShowCombatTutorial().MoveNext(),Is.False,"Later encounters do not reopen the general tutorial");
            yield return new ExitPlayMode();
        }

        [UnityTearDown] public IEnumerator RestoreEditor()
        {
            if(changedRunInBackground){Application.runInBackground=previousRunInBackground;changedRunInBackground=false;}
            if(Application.isPlaying)yield return new ExitPlayMode();
        }
    }
}
