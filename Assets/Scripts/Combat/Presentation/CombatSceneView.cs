using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class CombatSceneView : MonoBehaviour
    {
        [SerializeField] private RectTransform logicalArea;
        [SerializeField] private RectTransform playerAnchor;
        [SerializeField] private RectTransform monsterAnchor;
        [SerializeField] private float multiMonsterSpacing = 185f;
        public Image criticalFlash;
        private float flashUntil;
        public bool IsCriticalFlashing => criticalFlash!=null&&criticalFlash.enabled;

        private Vector2 logicalAreaRestPosition;
        private float shakeElapsed;
        private float shakeDuration;
        private float shakeMagnitude;
        private float shakePhase;
        private bool hasLogicalAreaRestPosition;

        public RectTransform LogicalArea => logicalArea;
        public Camera UiCamera => logicalArea.GetComponentInParent<Canvas>().renderMode==RenderMode.ScreenSpaceOverlay?null:logicalArea.GetComponentInParent<Canvas>().worldCamera;
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
            if(criticalFlash!=null)
            {
                var remaining=flashUntil-Time.unscaledTime;
                criticalFlash.enabled=remaining>0;
                if(remaining>0)criticalFlash.color=new Color(1,1,1,remaining>.065f?.03f:remaining>.035f?0:.01f);
            }
        }

        private void OnDisable()
        {
            ResetCameraShake();
            if(criticalFlash!=null)criticalFlash.enabled=false;
        }

        public void TriggerCriticalFlash() { flashUntil=Time.unscaledTime+.1f; }

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
            if(!GamePreferences.ScreenShake)return;
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
            uiCamera=uiCamera!=null?uiCamera:UiCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(logicalArea, screenPoint, uiCamera, out position))
                return RectTransformUtility.RectangleContainsScreenPoint(logicalArea, screenPoint, uiCamera);
            return false;
        }
    }
}
