using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class CombatSceneView : MonoBehaviour
    {
        [SerializeField] private RectTransform logicalArea;
        [SerializeField] private RectTransform playerAnchor;
        [SerializeField] private RectTransform monsterAnchor;
        [Header("Default monster formation (encounter offsets can override)")]
        [SerializeField] private float multiMonsterSpacing = 370f;
        [SerializeField] private float multiMonsterDepth = 65f;
        [SerializeField] private Vector2 twoMonsterOffset = new Vector2(270f, -35f);
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
        public RectTransform PlayerAnchor => playerAnchor;
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
            if (count == 2) return MonsterPosition + new Vector2(index == 0 ? -twoMonsterOffset.x : twoMonsterOffset.x, twoMonsterOffset.y);
            var centerOffset = index - (count - 1) * .5f;
            var depthOffset = Mathf.Abs(centerOffset) * multiMonsterDepth;
            return MonsterPosition + new Vector2(centerOffset * multiMonsterSpacing, -depthOffset);
        }

        public Vector2 FitMonsterPlacement(MonsterData data,Vector2 placement)
        {
            // Static framing derived from authored paintings, shared by all poses. Never resize a species in a group.
            var library=data.combatAnimation;var frames=library!=null?new[]{library.idle,library.ready,library.attack,library.stunned}:new[]{data.portrait};
            var minX=float.PositiveInfinity;var maxX=float.NegativeInfinity;var maxY=float.NegativeInfinity;
            foreach(var sprite in frames)
            {
                if(sprite==null||sprite.rect.width<=0)continue;
                var units=MonsterData.ArtworkUnitsPerWorldUnit/sprite.pixelsPerUnit*(library!=null?library.canvasScale:1)*data.combatScale;
                minX=Mathf.Min(minX,-sprite.pivot.x*units);maxX=Mathf.Max(maxX,(sprite.rect.width-sprite.pivot.x)*units);maxY=Mathf.Max(maxY,(sprite.rect.height-sprite.pivot.y)*units);
            }
            if(float.IsInfinity(maxY))return placement;
            var origin=data.BodyCenterFromPlacement(placement,data.combatScale);
            var ceiling=logicalArea.rect.yMax-190;
            if(origin.y+maxY>ceiling)placement.y-=origin.y+maxY-ceiling;
            var orbit=data.useOrbit?data.orbitRadius:0;
            var left=logicalArea.rect.xMin+40-minX+orbit;var right=logicalArea.rect.xMax-40-maxX-orbit;
            if(left<=right)placement.x=Mathf.Clamp(placement.x,left,right);
            return placement;
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
