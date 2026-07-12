using UnityEngine;

namespace DungeonSlash
{
    public sealed class CombatSceneView : MonoBehaviour
    {
        [SerializeField] private RectTransform logicalArea;
        [SerializeField] private RectTransform playerAnchor;
        [SerializeField] private RectTransform monsterAnchor;
        [SerializeField] private float multiMonsterSpacing = 185f;

        public RectTransform LogicalArea => logicalArea;
        public Vector2 PlayerPosition => playerAnchor.anchoredPosition;
        public Vector2 MonsterPosition => monsterAnchor.anchoredPosition;

        public void Configure(RectTransform area, RectTransform player, RectTransform monster)
        {
            logicalArea = area;
            playerAnchor = player;
            monsterAnchor = monster;
        }

        public Vector2 GetMonsterFormationPosition(int index, int count)
        {
            if (count <= 1) return MonsterPosition;
            var centerOffset = index - (count - 1) * .5f;
            var depthOffset = Mathf.Abs(centerOffset) * 18f;
            return MonsterPosition + new Vector2(centerOffset * multiMonsterSpacing, -depthOffset);
        }

        public bool TryScreenToLogical(Vector2 screenPoint, Camera uiCamera, out Vector2 position)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(logicalArea, screenPoint, uiCamera, out position))
                return RectTransformUtility.RectangleContainsScreenPoint(logicalArea, screenPoint, uiCamera);
            return false;
        }
    }
}
