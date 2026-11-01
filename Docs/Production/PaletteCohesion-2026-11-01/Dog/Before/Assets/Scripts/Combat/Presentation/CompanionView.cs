using UnityEngine;
namespace DungeonSlash
{
    public sealed class CompanionView : MonoBehaviour
    {
        public RectTransform body, tail;
        private CompanionData data;
        private float pulse;
        private CombatSpriteAnimator animator;
        private StageBackdropView backdrop;
        private GameFlowController flow;
        private ExplorationView exploration;
        private CombatController combat;
        private DungeonRoom layoutRoom;
        private bool rightSide=true, combatLayout;
        private CombatSpriteAnimation ActiveAnimation => rightSide && data.oppositeAnimation != null ? data.oppositeAnimation : data.combatAnimation;
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
            var scale=(stage!=null?stage.rect.height/1000f:1)*data.displayHeight
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
            var left=new Vector2(Mathf.Clamp(nearLeft.x-.08f,.19f,.27f),Mathf.Max(.04f,nearLeft.y-.10f));
            var right=new Vector2(Mathf.Clamp(nearRight.x-.035f,.80f,.86f),Mathf.Max(.04f,nearRight.y-.05f));
            var room=backdrop.dungeon?.State?.Player?.CurrentRoom;var fighting=combat!=null&&combat.HasPreparedMonsters;
            if(layoutRoom!=room||combatLayout!=fighting)
            {
                layoutRoom=room;combatLayout=fighting;
                // The combat gauges occupy the foreground left. In navigation, reserve
                // the free foreground support using actual prop footprints, once per room.
                rightSide=data.oppositeAnimation!=null&&(fighting||FloorOverlap(right)<=FloorOverlap(left));
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
                var dogWidth=data.displayHeight*g.footprint.width*idle.rect.width/Mathf.Max(1,g.bounds.height*idle.rect.height)/Mathf.Max(1,stage.rect.width)*stage.rect.height/1000f;
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
    }
}
