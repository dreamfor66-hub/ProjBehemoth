using UnityEngine;

namespace DungeonSlash
{
    public sealed class CombatSceneView : MonoBehaviour
    {
        [SerializeField] private RectTransform logicalArea;
        [SerializeField] private RectTransform playerAnchor;
        [SerializeField] private RectTransform monsterAnchor;
        [SerializeField] private float multiMonsterSpacing = 185f;

        private Vector2 logicalAreaRestPosition;
        private float shakeElapsed;
        private float shakeDuration;
        private float shakeMagnitude;
        private float shakePhase;
        private bool hasLogicalAreaRestPosition;

        public RectTransform LogicalArea => logicalArea;
        public Vector2 PlayerPosition => playerAnchor.anchoredPosition;
        public Vector2 MonsterPosition => monsterAnchor.anchoredPosition;
        public bool IsCameraShaking => shakeDuration > 0f;

        private void Awake()
        {
            CacheLogicalAreaRestPosition();
        }

        private void Update()
        {
            TickCameraShake(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            ResetCameraShake();
        }

        public void Configure(RectTransform area, RectTransform player, RectTransform monster)
        {
            logicalArea = area;
            playerAnchor = player;
            monsterAnchor = monster;
            CacheLogicalAreaRestPosition();
        }

        /// <summary>Briefly offsets the combat space without moving surrounding menus or popups.</summary>
        public void TriggerCameraShake(float magnitude, float duration)
        {
            if (logicalArea == null || magnitude <= 0f || duration <= 0f) return;
            if (!IsCameraShaking) CacheLogicalAreaRestPosition();

            shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
            shakeDuration = Mathf.Max(shakeDuration, duration);
            shakeElapsed = 0f;
            shakePhase = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        public void TickCameraShake(float deltaTime)
        {
            if (!IsCameraShaking || logicalArea == null) return;

            shakeElapsed += Mathf.Max(0f, deltaTime);
            if (shakeElapsed >= shakeDuration)
            {
                ResetCameraShake();
                return;
            }

            var fade = 1f - shakeElapsed / shakeDuration;
            var phase = shakePhase + shakeElapsed * 74f;
            var offset = new Vector2(Mathf.Sin(phase * 1.17f), Mathf.Cos(phase * .83f));
            logicalArea.anchoredPosition = logicalAreaRestPosition + offset * (shakeMagnitude * fade);
        }

        public void ResetCameraShake()
        {
            if (logicalArea != null && hasLogicalAreaRestPosition)
                logicalArea.anchoredPosition = logicalAreaRestPosition;

            shakeElapsed = 0f;
            shakeDuration = 0f;
            shakeMagnitude = 0f;
        }

        private void CacheLogicalAreaRestPosition()
        {
            if (logicalArea == null) return;
            logicalAreaRestPosition = logicalArea.anchoredPosition;
            hasLogicalAreaRestPosition = true;
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
