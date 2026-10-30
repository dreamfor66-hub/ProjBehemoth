using TMPro;
using UnityEngine;

namespace DungeonSlash
{
    public sealed partial class RunExpansionView
    {
        [Header("Illustrated event popup")]
        public ExpansionChoiceButton[] portraitChoices;
        [Range(.5f, 1f)] public float eventIllustrationAspect = .75f;
        [Min(0)] public float eventMargin = 48, eventColumnGap = 48;
        [Min(1)] public float eventBodySize = 28, eventChoiceTitleSize = 30, eventChoiceDescriptionSize = 24;
        bool portraitEvent, framingCached, artPreserveAspect;
        Vector2 artPosition, artSize, artAnchorMin, artAnchorMax, artPivot, titlePosition, titleSize;
        TextAlignmentOptions titleAlignment;
        float bodySize;

        void RestoreStandardFraming()
        {
            var art = modalIllustration.rectTransform;
            if (!framingCached)
            {
                artPosition=art.anchoredPosition; artSize=art.sizeDelta; artAnchorMin=art.anchorMin; artAnchorMax=art.anchorMax; artPivot=art.pivot;artPreserveAspect=modalIllustration.preserveAspect;
                titlePosition=modalTitle.rectTransform.anchoredPosition; titleSize=modalTitle.rectTransform.sizeDelta; titleAlignment=modalTitle.alignment;
                bodySize=modalNarrative.GetComponent<TypographyRoleBinding>()?.designSize??modalNarrative.fontSize;
                framingCached=true;
            }
            art.anchorMin=artAnchorMin; art.anchorMax=artAnchorMax; art.pivot=artPivot; art.anchoredPosition=artPosition; art.sizeDelta=artSize;
            modalIllustration.preserveAspect=artPreserveAspect;
            modalTitle.rectTransform.anchoredPosition=titlePosition; modalTitle.rectTransform.sizeDelta=titleSize; modalTitle.alignment=titleAlignment;
            SetEventFont(modalNarrative,bodySize);
        }

        static void SetEventFont(TextMeshProUGUI text,float size)
        {
            text.enableAutoSizing=false; text.fontSize=size;
            var binding=text.GetComponent<TypographyRoleBinding>();
            if(binding!=null){binding.designSize=size;binding.Apply();}
        }

        void LayoutPortraitEvent(int count,out float narrativeHeight,out float narrativeCenter)
        {
            var panel=(RectTransform)modalTitle.transform.parent;
            var availableHeight=panel.rect.height-eventMargin*2;
            var artHeight=availableHeight;
            var artWidth=artHeight*eventIllustrationAspect;
            var left=-panel.rect.width*.5f+eventMargin;
            var right=panel.rect.width*.5f-eventMargin;
            var textLeft=left+artWidth+eventColumnGap;
            var textWidth=right-textLeft;
            var textX=(textLeft+right)*.5f;
            var art=modalIllustration.rectTransform;
            art.anchorMin=art.anchorMax=art.pivot=Vector2.one*.5f;
            art.anchoredPosition=new Vector2(left+artWidth*.5f,0);
            art.sizeDelta=new Vector2(artWidth,artHeight);
            // Filled image permits ratio comparison without stretching the painting.
            modalIllustration.type=UnityEngine.UI.Image.Type.Simple;
            modalIllustration.preserveAspect=true;
            modalTitle.rectTransform.anchoredPosition=new Vector2(textX,panel.rect.height*.5f-eventMargin-36);
            modalTitle.rectTransform.sizeDelta=new Vector2(textWidth,72);
            modalTitle.alignment=TextAlignmentOptions.MidlineLeft;
            var top=panel.rect.height*.5f-eventMargin-104;
            var bottom=-panel.rect.height*.5f+eventMargin;
            for(var i=count-1;i>=0;i--)
            {
                var row=portraitChoices[i]; var r=(RectTransform)row.button.transform;
                r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
                SetEventFont(row.title,eventChoiceTitleSize); SetEventFont(row.description,eventChoiceDescriptionSize);
                var titleHeight=Mathf.Max(36,row.title.GetPreferredValues(row.title.text,textWidth-48,Mathf.Infinity).y);
                var hasDescription=!string.IsNullOrWhiteSpace(row.description.text);
                var descriptionHeight=hasDescription?row.description.GetPreferredValues(row.description.text,textWidth-48,Mathf.Infinity).y:0;
                var rowHeight=titleHeight+(hasDescription?descriptionHeight+6:0)+16;
                r.sizeDelta=new Vector2(textWidth,rowHeight); r.anchoredPosition=new Vector2(textX,bottom+rowHeight*.5f);
                row.title.rectTransform.sizeDelta=new Vector2(textWidth-48,titleHeight);
                row.title.rectTransform.anchoredPosition=new Vector2(0,rowHeight*.5f-8-titleHeight*.5f);
                row.description.gameObject.SetActive(hasDescription);
                row.description.rectTransform.sizeDelta=new Vector2(textWidth-48,descriptionHeight);
                row.description.rectTransform.anchoredPosition=new Vector2(0,rowHeight*.5f-8-titleHeight-6-descriptionHeight*.5f);
                bottom+=rowHeight+(i>0?8:0);
            }
            SetEventFont(modalNarrative,eventBodySize);
            var rect=(RectTransform)narrativeScroll.transform;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,textWidth);
            rect.anchoredPosition=new Vector2(textX,rect.anchoredPosition.y);
            narrativeHeight=Mathf.Max(96,top-bottom-28);
            narrativeCenter=top-narrativeHeight*.5f;
        }
    }
}
