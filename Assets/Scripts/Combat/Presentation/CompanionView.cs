using UnityEngine;
namespace DungeonSlash
{
    public sealed class CompanionView : MonoBehaviour, ICanvasRaycastFilter
    {
        public RectTransform body, tail;
        private CompanionData data;
        private float pulse;
        private CombatSpriteAnimator animator;
        private StageBackdropView backdrop;
        private GameFlowController flow;
        private ExplorationView exploration;
        private CombatController combat;
        private PlayerView player;
        private CombatSpriteAnimator playerPainting;
        private readonly Vector3[] playerCorners=new Vector3[4];
        private DungeonRoom layoutRoom;
        private bool rightSide=true, combatLayout;
        private CombatSpriteAnimation ActiveAnimation => data.forwardAnimation != null ? data.forwardAnimation
            : rightSide && data.oppositeAnimation != null ? data.oppositeAnimation : data.combatAnimation;
        [SerializeField] private RoomContactShadow contact;
        private UnityEngine.UI.Button talk;
        public ArcadePresentation presentation;
        public CombatVisualState State=>pulse>0?CombatVisualState.Attack:CombatVisualState.Idle;
        public void Configure(CompanionData definition)
        {
            pulse=0;
            data = definition; ((RectTransform)transform).anchoredPosition = presentation!=null&&presentation.arcade?Vector2.zero:data.playerOffset;
            backdrop = FindFirstObjectByType<StageBackdropView>();
            flow = FindFirstObjectByType<GameFlowController>(); exploration = FindFirstObjectByType<ExplorationView>();
            combat=FindFirstObjectByType<CombatController>();rightSide=true;layoutRoom=null;
            if (body != null && data.combatSprite != null)
            {
                var art=body.GetComponent<UnityEngine.UI.Image>();art.sprite=data.combatSprite;art.enabled=true;art.color=Color.white;art.preserveAspect=true;
                foreach(var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))if(graphic!=art)graphic.enabled=false;
                body.sizeDelta=presentation!=null&&presentation.arcade?new Vector2(288,320):new Vector2(126,106);if(tail!=null)tail.gameObject.SetActive(false);
                if(presentation!=null&&presentation.arcade&&data.combatAnimation!=null)
                {
                    animator=art.GetComponent<CombatSpriteAnimator>()??art.gameObject.AddComponent<CombatSpriteAnimator>();
                    animator.presentation=presentation;animator.Play(ActiveAnimation,CombatVisualState.Idle);
                    FramePainting(art.sprite);
                    talk = art.GetComponent<UnityEngine.UI.Button>() ?? art.gameObject.AddComponent<UnityEngine.UI.Button>();
                    talk.transition = UnityEngine.UI.Selectable.Transition.None; talk.targetGraphic = art;
                    var nav = talk.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.None; talk.navigation = nav;
                    talk.onClick.RemoveAllListeners(); talk.onClick.AddListener(() =>
                    { if (flow != null && flow.State == GameFlowState.DungeonNavigation && !flow.IsPlayRecordInspection) exploration?.Say(data.narrationKey); });
                    if (contact == null)
                    {
                        contact = new GameObject("Companion floor contact", typeof(RectTransform), typeof(CanvasRenderer), typeof(RoomContactShadow)).GetComponent<RoomContactShadow>();
                        contact.transform.SetParent(transform,false); contact.transform.SetAsFirstSibling(); contact.raycastTarget=false;
                    }
                }
            }
            else if(body!=null)
            {var art=body.GetComponent<UnityEngine.UI.Image>();art.sprite=null;art.color=new Color(.62f,.42f,.23f);body.sizeDelta=new Vector2(68,40);foreach(var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.enabled=true;if(tail!=null)tail.gameObject.SetActive(true);}
        }
        public void Attack(Vector2 target) { pulse = 1; }
        public bool IsRaycastLocationValid(Vector2 screenPoint,Camera eventCamera)
        {
            if(body==null||data==null)return true;
            var art=body.GetComponent<UnityEngine.UI.Image>();
            if(art==null||art.sprite==null)return true;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(body,screenPoint,eventCamera,out var local))return false;
            var b=data.Geometry(art.sprite).bounds;var r=body.rect;
            // A rear painting is narrow inside its GUID-preserving source canvas.
            // Its transparent padding must not cover nearby exploration controls.
            var hit=new Rect(r.xMin+b.x*r.width,r.yMin+b.y*r.height,b.width*r.width,b.height*r.height);
            return hit.Contains(local);
        }
        private void Update()
        {
            if (data == null || body == null) return;
            pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * 4);
            if(presentation!=null&&presentation.arcade&&data.combatAnimation!=null)
            {
                if(animator!=null)animator.Play(ActiveAnimation,State);
                FramePainting(body.GetComponent<UnityEngine.UI.Image>().sprite);
                return;
            }
            body.anchoredPosition = new Vector2(Mathf.Sin(pulse * Mathf.PI) * data.attackLungeDistance, Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3)) * data.idleBobHeight);
            if (tail != null) tail.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 9) * 20);
        }
        private void FramePainting(Sprite painting)
        {
            if(painting==null||data.combatSprite==null)return;
            var idle=ActiveAnimation!=null&&ActiveAnimation.idle!=null?ActiveAnimation.idle:data.combatSprite;
            var reference=data.Geometry(idle); var geometry=data.Geometry(painting);
            var stage=backdrop!=null&&backdrop.background!=null?backdrop.background.rectTransform:null;
            var playerScale=PlayerPaintingFrame(stage,out var playerFrame)?playerFrame.width/640f:stage!=null?stage.rect.height/1000f:1;
            var scale=playerScale*data.displayHeight
                /Mathf.Max(1,idle.rect.height*reference.bounds.height);
            body.anchorMin=body.anchorMax=new Vector2(.5f,0);body.pivot=new Vector2(.5f,0);
            body.sizeDelta=painting.rect.size*scale;
            body.anchoredPosition=new Vector2((.5f-geometry.footprint.center.x)*body.sizeDelta.x,-geometry.footprint.yMin*body.sizeDelta.y);
            if(contact!=null)
            {
                contact.enabled=true;
                var rect=contact.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0);
                rect.anchoredPosition=Vector2.zero;var width=geometry.footprint.width*body.sizeDelta.x;
                rect.sizeDelta=new Vector2(width*1.06f,Mathf.Clamp(width*.11f,8,30));contact.color=new Color(.015f,.015f,.015f,.65f);
            }
        }
        private void LateUpdate()
        {
            if(data==null||body==null||backdrop?.background==null||presentation==null||!presentation.arcade)return;
            var stage=backdrop.background.rectTransform;
            var nearLeft=new Vector2(.315f,.17f);var nearRight=new Vector2(.88f,.12f);
            var index=backdrop.run?.State!=null?Mathf.Clamp(backdrop.run.State.CurrentAct-1,0,2):0;
            if(backdrop.artwork?.Length>index&&backdrop.artwork[index]?.floorObjectCorners?.Length==4)
            {nearLeft=backdrop.artwork[index].floorObjectCorners[3];nearRight=backdrop.artwork[index].floorObjectCorners[2];}
            var left=new Vector2(Mathf.Clamp(nearLeft.x+.025f,.28f,.33f),.025f);
            var right=new Vector2(Mathf.Clamp(nearRight.x-.10f,.73f,.79f),.025f);
            if(PlayerPaintingFrame(stage,out var playerFrame))
            {
                // Share the actual player's near camera plane and zoom. Its image is
                // waist-cropped, so the companion's lower legs may also leave the frame.
                var flank=playerFrame.width*.46f;
                var pan=(Vector2)stage.InverseTransformVector(player.CameraFramingDisplacement);
                var navigationSize=playerFrame.size/Mathf.Max(.01f,player.CameraFramingZoom);
                var navigationBottom=playerFrame.center.y-pan.y-navigationSize.y*.5f;
                var baseGround=Mathf.Clamp((navigationBottom-stage.rect.yMin)/stage.rect.height,-.06f,.01f);
                var groundY=stage.rect.yMin+baseGround*stage.rect.height+pan.y;
                // One ground for every authored pose: follow the player pan, then
                // retain the upper 30% of the shortest live painting if deeply cropped.
                // This never moves the actor differently for Idle versus Attack.
                var minimumInkHeight=MinimumLivePoseHeight(playerFrame.width/640f);
                var canvas=GetComponentInParent<Canvas>().rootCanvas;
                var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(stage,new Vector2(Screen.width*.5f,0),camera,out var bottom))
                    groundY=Mathf.Max(groundY,bottom.y-minimumInkHeight*.70f);
                var ground=(groundY-stage.rect.yMin)/stage.rect.height;
                left=new Vector2((playerFrame.center.x-flank-stage.rect.xMin)/stage.rect.width,ground);
                right=new Vector2((playerFrame.center.x+flank-stage.rect.xMin)/stage.rect.width,ground);
            }
            var room=backdrop.dungeon?.State?.Player?.CurrentRoom;var fighting=combat!=null&&combat.HasPreparedMonsters;
            if(layoutRoom!=room||combatLayout!=fighting)
            {
                layoutRoom=room;combatLayout=fighting;
                // The combat gauges occupy the foreground left. In navigation, reserve
                // the free foreground support using actual prop footprints, once per room.
                rightSide=(data.forwardAnimation!=null||data.oppositeAnimation!=null)&&(fighting||FloorOverlap(right)<=FloorOverlap(left));
            }
            var support=rightSide?right:left;
            transform.position=stage.TransformPoint(stage.rect.min+Vector2.Scale(stage.rect.size,support));
            var art=body.GetComponent<UnityEngine.UI.Image>();
            if(animator!=null)animator.Play(ActiveAnimation,State);
            art.raycastTarget=flow!=null&&flow.State==GameFlowState.DungeonNavigation&&!flow.IsPlayRecordInspection;
            if(talk!=null)talk.interactable=art.raycastTarget;
            FramePainting(art.sprite);
            art.enabled=flow==null||!flow.IsPlayRecordInspection;if(contact!=null)contact.enabled=art.enabled;

            float FloorOverlap(Vector2 candidate)
            {
                if(exploration?.slots==null)return 0;var score=0f;
                var idle=ActiveAnimation!=null?ActiveAnimation.idle:data.combatSprite;var g=data.Geometry(idle);
                var cameraScale=PlayerPaintingFrame(stage,out var frame)?frame.width/640f:stage.rect.height/1000f;
                var dogWidth=data.displayHeight*g.footprint.width*idle.rect.width/Mathf.Max(1,g.bounds.height*idle.rect.height)/Mathf.Max(1,stage.rect.width)*cameraScale;
                foreach(var slot in exploration.slots)
                {
                    if(!slot.gameObject.activeInHierarchy||slot.BoundData==null||slot.BoundData.wallMounted||slot.BoundData.roomInterior)continue;
                    var p=(Vector2)stage.InverseTransformPoint(slot.transform.position);p=Vector2.Scale(p-stage.rect.min,new Vector2(1/stage.rect.width,1/stage.rect.height));
                    if(Mathf.Abs(p.y-candidate.y)>.065f)continue;
                    var propWidth=((RectTransform)slot.transform).rect.width/stage.rect.width;
                    score+=Mathf.Max(0,(dogWidth+propWidth)*.5f+.015f-Mathf.Abs(p.x-candidate.x));
                }
                return score;
            }
        }
        private float MinimumLivePoseHeight(float cameraScale)
        {
            var library=ActiveAnimation;
            var idle=library!=null&&library.idle!=null?library.idle:data.combatSprite;
            if(idle==null)return 0;
            var reference=data.Geometry(idle);
            var pixelScale=cameraScale*data.displayHeight/Mathf.Max(1,idle.rect.height*reference.bounds.height);
            float Height(Sprite painting)
            {
                var g=data.Geometry(painting);
                return Mathf.Max(0,g.bounds.yMax-g.footprint.yMin)*painting.rect.height*pixelScale;
            }
            var height=Height(idle);
            if(library!=null&&library.attack!=null)height=Mathf.Min(height,Height(library.attack));
            return height;
        }
        private bool PlayerPaintingFrame(RectTransform stage,out Rect frame)
        {
            frame=default;if(stage==null)return false;
            if(player==null)player=FindFirstObjectByType<PlayerView>();
            if(playerPainting==null&&player!=null&&player.Root!=null)
                playerPainting=player.Root.GetComponentInChildren<CombatSpriteAnimator>();
            if(playerPainting==null)return false;
            ((RectTransform)playerPainting.transform).GetWorldCorners(playerCorners);
            var min=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            var max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var corner in playerCorners)
            {var p=(Vector2)stage.InverseTransformPoint(corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
            frame=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return frame.width>0&&frame.height>0;
        }
    }
}
