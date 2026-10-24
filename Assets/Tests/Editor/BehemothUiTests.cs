using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DungeonSlash.Tests
{
    public sealed class BehemothUiTests
    {
        private static GameContent Content => AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset");
        private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        internal static void Inside(RectTransform child, RectTransform container)
        {
            var corners = new Vector3[4]; child.GetWorldCorners(corners);
            foreach (var corner in corners)
            {
                var point = container.InverseTransformPoint(corner);
                Assert.That(point.x, Is.InRange(container.rect.xMin - 1, container.rect.xMax + 1), child.name + " x");
                Assert.That(point.y, Is.InRange(container.rect.yMin - 1, container.rect.yMax + 1), child.name + " y");
            }
        }

        [Test] public void Merchant_SixSlots_DoNotRerollAfterBuying_AndResetForNewRun()
        {
            var previous = RunSession.SelectedCharacter; RunSession.SelectedCharacter = null;
            var root = new GameObject("Fixed stock test");
            try
            {
                var run = root.AddComponent<RunController>();
                var balance = AssetDatabase.LoadAssetAtPath<RunBalanceSettings>("Assets/Data/Settings/RunBalanceSettings.asset");
                if (balance == null) balance = AssetDatabase.LoadAssetAtPath<RunBalanceSettings>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:RunBalanceSettings")[0]));
                run.Configure(Content.characters[0], balance, Content.perks, Content.equipment); run.BeginRun(); run.State.GainGold(1000);
                var inventory = run.GetShopInventory(31, 7); var before = inventory.ToArray();
                Assert.That(inventory.Count, Is.EqualTo(6)); Assert.That(inventory.Distinct().Count(), Is.EqualTo(6));
                var bought = inventory.First(item => item.IsRelic && item.grantedSpell == null);
                Assert.That(run.TryBuy(bought), Is.True);
                Assert.That(run.GetShopInventory(31, 7), Is.SameAs(inventory));
                CollectionAssert.AreEqual(before, inventory);
                Assert.That(run.Shop.CanPurchase(run.State, bought), Is.False);
                run.State.NormalCombatsInAct = 8;
                Assert.That(run.GetShopInventory(31, 7), Is.SameAs(inventory), "Progression also cannot restock an existing merchant.");
                run.BeginRun(); Assert.That(run.GetShopInventory(31, 7), Is.Not.SameAs(inventory));
            }
            finally { Object.DestroyImmediate(root); RunSession.SelectedCharacter = previous; }
        }

        [Test] public void MainScene_AllDecisionPanelsShareCentralLayerAboveSpellHud()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainGameScene.unity");
            var expansion = Object.FindFirstObjectByType<RunExpansionView>(FindObjectsInactive.Include);
            var layer = expansion.modal.transform.parent;
            Assert.That(layer.name, Is.EqualTo("CenterModalLayer"));
            Assert.That(layer.GetSiblingIndex(), Is.GreaterThan(expansion.transform.GetSiblingIndex()));
            var center = (RectTransform)layer;
            foreach (Transform panel in layer) { Inside((RectTransform)panel, center); Assert.That(((RectTransform)panel).rect.width, Is.EqualTo(510)); }
            var shop = Object.FindFirstObjectByType<ShopView>(FindObjectsInactive.Include);
            Assert.That(Field<System.Collections.Generic.List<ShopItemView>>(shop, "items").Count, Is.EqualTo(6));
            foreach (var item in Field<System.Collections.Generic.List<ShopItemView>>(shop, "items")) Inside((RectTransform)item.transform, (RectTransform)shop.transform);
            var hud = Object.FindFirstObjectByType<RunHudView>();
            Assert.That(Field<Image>(hud, "characterPortrait"), Is.Not.Null);
            Assert.That(Field<Image>(hud, "experienceFill"), Is.Not.Null);
            Assert.That(Field<TextMeshProUGUI>(hud, "runInfo").enabled, Is.False);
        }

        [Test] public void SelectionScene_AllControlsAreInsideCentralArea_AndRelicCanScroll()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity");
            var view = Object.FindFirstObjectByType<OutGameView>();
            var center = view.startButton.transform.parent as RectTransform;
            Assert.That(center.name, Is.EqualTo("SelectionCenter"));
            Assert.That(center.rect.size, Is.EqualTo(new Vector2(510, 820)));
            foreach (Transform child in center) Inside((RectTransform)child, center);
            Assert.That(view.startingRelicScroll, Is.Not.Null);
            for (var i = 0; i < view.characterButtons.Length; i++)
            {
                view.Select(i); Assert.That(view.characterName.text, Is.EqualTo(Content.characters[i].displayName));
                Assert.That(view.stats.text, Does.Contain("가드 피해 감소"));
                Assert.That(view.startingRelic.text, Does.Contain(Content.characters[i].startingRelic.displayName));
            }
        }
    }
}
