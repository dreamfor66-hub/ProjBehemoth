using TMPro;
using UnityEngine;

namespace DungeonSlash
{
    /// <summary>A saved semantic text style. Localized text and layout remain on the original TMP component.</summary>
    [ExecuteAlways,DisallowMultipleComponent,RequireComponent(typeof(TextMeshProUGUI))]
    [DefaultExecutionOrder(10000)]
    public sealed class TypographyRoleBinding : MonoBehaviour
    {
        public TypographyProfile profile;
        public TextRole role;
        [Min(1)] public float designSize=44;
        public bool snapSize=true;
        TextMeshProUGUI label;
        float appliedSize;
        string previousText;
        float previousWidth=-1,previousSize=-1;
        TMP_FontAsset previousPreferred,previousNarrow,selected;

        void OnEnable()=>Apply();
        void OnValidate(){previousWidth=-1;Apply();}
        void LateUpdate()=>Apply();

        public void Apply()
        {
            if(profile==null)return;
            if(label==null)label=GetComponent<TextMeshProUGUI>();
            if(label==null)return;
            var rule=profile.Rule(role);
            var preferred=profile.Font(rule?.face??Typeface.Base);
            if(rule!=null&&rule.numericOnly&&ContainsWords(label.text))preferred=profile.Font(Typeface.Base);
            if(preferred==null)return;

            // Respect runtime choices that intentionally change font size (e.g. compact event options).
            if(appliedSize>0&&!label.enableAutoSizing&&Mathf.Abs(label.fontSize-appliedSize)>.05f)
                designSize=label.fontSize/Mathf.Max(.01f,rule?.sizeMultiplier??1);
            var target=designSize*(rule?.sizeMultiplier??1);
            var scale=label.canvas!=null?label.canvas.scaleFactor:1;
            var candidateSize=label.enableAutoSizing?target:Snap(preferred,target,scale);
            var width=label.rectTransform.rect.width;
            var narrow=rule!=null&&rule.useNarrowFaceWhenNeeded?profile.Font(rule.narrowFace):null;
            if(previousText!=label.text||previousWidth!=width||previousPreferred!=preferred||previousNarrow!=narrow||previousSize!=candidateSize)
            {
                selected=preferred;
                if(label.font!=preferred)label.font=preferred;
                // A narrow action label may need Condensed even when a larger narrative does not.
                if(rule!=null&&rule.useNarrowFaceWhenNeeded&&width>0)
                {
                    var oldSize=label.fontSize;
                    label.fontSize=candidateSize;
                    var available=width-label.margin.x-label.margin.z;
                    if(label.GetPreferredValues(label.text).x>available)
                        selected=narrow??preferred;
                    label.fontSize=oldSize;
                }
                previousText=label.text;previousWidth=width;previousPreferred=preferred;previousNarrow=narrow;previousSize=candidateSize;
            }
            if(selected==null)selected=preferred;
            if(label.font!=selected)label.font=selected;
            if(label.enableAutoSizing)return; // Preserve authored autosizing and its minimum readable size.

            // Nested animated labels (damage numbers) keep their intended scale animation.
            target=Snap(selected,target,scale);
            if(Mathf.Abs(label.fontSize-target)>.01f)label.fontSize=target;
            appliedSize=target;
        }

        float Snap(TMP_FontAsset font,float size,float scale)
        {
            if(snapSize&&profile.snapToNativePixels&&scale>0&&size*scale>=font.faceInfo.pointSize)
            {
                var point=Mathf.Max(1,font.faceInfo.pointSize);
                return Mathf.Max(point,Mathf.Round(size*scale/point)*point)/scale;
            }
            return size;
        }

        static bool ContainsWords(string text)
        {
            if(string.IsNullOrEmpty(text))return false;
            for(var i=0;i<text.Length;i++)
            {
                var c=text[i];
                // The localized stack counter uses "x3" as a numeric multiplication mark.
                if((c=='x'||c=='X')&&i+1<text.Length&&char.IsDigit(text[i+1]))continue;
                if(char.IsLetter(c))return true;
            }
            return false;
        }
    }
}
