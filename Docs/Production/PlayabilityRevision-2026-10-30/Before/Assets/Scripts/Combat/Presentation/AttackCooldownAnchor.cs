using UnityEngine;

namespace DungeonSlash
{
    public enum AttackCooldownPlacement { Pointer, Monster, Authored }

    public sealed class AttackCooldownAnchor : MonoBehaviour
    {
        public CombatSceneView scene;
        public CombatController combat;
        public CombatInputController input;
        public AttackRecoveryGauge graphic;
        public AttackCooldownPlacement placement=AttackCooldownPlacement.Pointer;
        public Vector2 pointerOffset=new(0,-36);
        public Vector2 monsterOffset=new(260,80);
        public Vector2 authoredPosition;
        Vector2 lastTouchPosition;
        void LateUpdate()
        {
            if(graphic==null||scene==null)return;
            graphic.enabled=combat!=null&&combat.IsActive&&!combat.InputPaused;
            if(placement==AttackCooldownPlacement.Authored){graphic.rectTransform.anchoredPosition=authoredPosition;return;}
            if(placement==AttackCooldownPlacement.Monster){PlaceLogical(scene.MonsterPosition+monsterOffset);return;}
            if(Input.touchCount>0)
            {
                // The guard hand never steals the attack-hand indicator.
                if(input!=null&&input.TryGetAttackPointer(out var point))lastTouchPosition=point;
                PlaceLogical(lastTouchPosition);
            }
            else FollowScreenPoint(Input.mousePosition);
        }
        public void FollowScreenPoint(Vector2 screen)
        {
            if(scene==null||graphic==null)return;
            var parent=(RectTransform)graphic.transform.parent;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,scene.UiCamera,out var p))Place(p+pointerOffset);
        }
        void PlaceLogical(Vector2 logical)
        {
            var p=(Vector2)graphic.transform.parent.InverseTransformPoint(scene.LogicalArea.TransformPoint(logical));
            Place(p+(placement==AttackCooldownPlacement.Pointer?pointerOffset:Vector2.zero));
        }
        void Place(Vector2 p)
        {
            var rect=graphic.rectTransform;var bounds=((RectTransform)rect.parent).rect;var half=rect.rect.size*.5f;
            p.x=Mathf.Clamp(p.x,bounds.xMin+half.x+12,bounds.xMax-half.x-12);
            p.y=Mathf.Clamp(p.y,bounds.yMin+half.y+12,bounds.yMax-half.y-110);
            rect.anchoredPosition=p;
        }
    }
}
