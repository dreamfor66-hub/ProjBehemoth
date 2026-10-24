using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    public sealed class PixelDitherTransition : MaskableGraphic
    {
        private static readonly int[] Bayer = { 0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5 };
        private float coverage;
        private Coroutine transitionRoutine;
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        protected override void OnDisable()
        {
            if(transitionRoutine!=null)StopCoroutine(transitionRoutine);
            transitionRoutine=null;coverage=0;SetVerticesDirty();base.OnDisable();
        }
        public void Reveal()
        {
            if (!isActiveAndEnabled) return;
            transform.SetAsLastSibling();
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(Animate());
        }
        private IEnumerator Animate()
        {
            var start = Time.unscaledTime;
            do { coverage = 1 - Mathf.Clamp01((Time.unscaledTime-start)/.28f); SetVerticesDirty(); yield return null; } while(coverage>0);
            transitionRoutine = null;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (coverage <= 0) return;
            var rect = GetPixelAdjustedRect(); const float cell = 12;
            var columns = Mathf.CeilToInt(rect.width/cell); var rows = Mathf.CeilToInt(rect.height/cell);
            for (var y=0;y<rows;y++) for(var x=0;x<columns;x++)
            {
                if ((Bayer[(y%4)*4+x%4]+.5f)/16f > coverage) continue;
                var left=rect.xMin+x*cell;var bottom=rect.yMin+y*cell;var right=Mathf.Min(rect.xMax,left+cell);var top=Mathf.Min(rect.yMax,bottom+cell);var index=vh.currentVertCount;
                var tint=new Color32(12,11,16,255);vh.AddVert(new Vector3(left,bottom),tint,Vector2.zero);vh.AddVert(new Vector3(left,top),tint,Vector2.zero);vh.AddVert(new Vector3(right,top),tint,Vector2.zero);vh.AddVert(new Vector3(right,bottom),tint,Vector2.zero);vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
            }
        }
    }
}
