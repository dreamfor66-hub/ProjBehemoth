using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    /// <summary>Shared non-blocking description surface with cursor and anchored side placement.</summary>
    public sealed class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI description;
        [SerializeField] private RectTransform positionRoot;
        public bool expandToFit;
        [Min(100)] public float maximumWidth=960;
        private float preferredWidth;
        private Object owner;
        private readonly Vector3[] anchorCorners = new Vector3[4];
        public bool IsVisible => panel != null && panel.activeInHierarchy;
        public string VisibleTitle => title != null ? title.text : "";

        public void Configure(GameObject newPanel, TextMeshProUGUI newTitle, TextMeshProUGUI newDescription, RectTransform newPositionRoot)
        {
            panel = newPanel;
            title = newTitle;
            description = newDescription;
            positionRoot = newPositionRoot;
            preferredWidth=newPanel.transform is RectTransform rect?rect.rect.width:0;
            Hide();
        }

        public void Show(string itemName, string itemDescription, Vector2 screenPosition)
        {
            owner=null;
            Refresh(itemName,itemDescription);
            PositionAt(screenPosition);
        }

        public void ShowBeside(Object source,string itemName,string itemDescription,RectTransform anchor)
        {
            owner=source;
            Refresh(itemName,itemDescription);
            if(positionRoot==null||panel==null||anchor==null)return;
            anchor.GetWorldCorners(anchorCorners);
            var left=positionRoot.InverseTransformPoint(anchorCorners[0]).x;
            var right=positionRoot.InverseTransformPoint(anchorCorners[2]).x;
            var top=positionRoot.InverseTransformPoint(anchorCorners[2]).y;
            var box=(RectTransform)panel.transform;var size=box.rect.size;var bounds=positionRoot.rect;
            const float gap=24,margin=8;
            var rightSpace=bounds.xMax-right;var leftSpace=left-bounds.xMin;
            var onRight=rightSpace>=size.x+gap+margin||rightSpace>=leftSpace;
            var point=new Vector2(onRight?right+gap+size.x*.5f:left-gap-size.x*.5f,top-size.y*.5f);
            point.x=Mathf.Clamp(point.x,bounds.xMin+size.x*.5f+margin,bounds.xMax-size.x*.5f-margin);
            point.y=Mathf.Clamp(point.y,bounds.yMin+size.y*.5f+margin,bounds.yMax-size.y*.5f-margin);
            box.anchoredPosition=new Vector2(Mathf.Round(point.x/4),Mathf.Round(point.y/4))*4;
        }

        private void Refresh(string itemName,string itemDescription)
        {
            if (panel == null) return;
            if (title != null) title.text = itemName;
            if (description != null) description.text = itemDescription;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            var group=panel.GetComponent<CanvasGroup>();
            if(group!=null){group.blocksRaycasts=false;group.interactable=false;}
            if(title!=null)title.raycastTarget=false;
            if(description!=null)description.raycastTarget=false;
            if (description != null && panel.transform is RectTransform box)
            {
                var maxHeight = positionRoot != null ? positionRoot.rect.height - 16f : 700f;
                if(preferredWidth<=0)preferredWidth=box.rect.width;
                var width=preferredWidth;
                if(expandToFit)
                {
                    var limit=Mathf.Min(maximumWidth,positionRoot!=null?positionRoot.rect.width-16:maximumWidth);
                    while(width<limit&&description.GetPreferredValues(itemDescription,width-56,Mathf.Infinity).y+92>maxHeight)width=Mathf.Min(width+64,limit);
                    box.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);
                    description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-56);
                    if(title!=null)title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-56);
                }
                var contentHeight=description.GetPreferredValues(itemDescription,description.rectTransform.rect.width,Mathf.Infinity).y;
                var height = Mathf.Clamp(Mathf.Ceil((contentHeight+92)/8)*8,152,Mathf.Max(152,maxHeight));
                box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                var body = description.rectTransform;
                body.anchorMin = body.anchorMax = new Vector2(.5f, 1f); body.pivot = new Vector2(.5f, 1f); body.anchoredPosition = new Vector2(0, -72);
                body.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - 84f);
                if (title != null) { var heading = title.rectTransform; heading.anchorMin = heading.anchorMax = new Vector2(.5f, 1f); heading.pivot = new Vector2(.5f, 1f); heading.anchoredPosition = new Vector2(0, -12);heading.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,48); }
            }
        }

        public void Hide(Object source){if(owner==source)Hide();}

        public void Hide()
        {
            owner=null;
            if (panel != null) panel.SetActive(false);
        }

        private void PositionAt(Vector2 screenPosition)
        {
            if (positionRoot == null || panel == null) return;
            var canvas=positionRoot.GetComponentInParent<Canvas>();var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(positionRoot, screenPosition, camera, out var point)) return;
            var panelRect = panel.GetComponent<RectTransform>();
            if (panelRect == null) return;
            var size = panelRect.sizeDelta;
            var bounds = positionRoot.rect;
            var original=point;
            point += new Vector2(size.x*.5f+32f,-40f);
            if(point.x+size.x*.5f>bounds.xMax-8)point.x=original.x-size.x*.5f-32f;
            point.x = Mathf.Clamp(point.x, bounds.xMin + size.x * .5f + 8f, bounds.xMax - size.x * .5f - 8f);
            point.y = Mathf.Clamp(point.y, bounds.yMin + size.y * .5f + 8f, bounds.yMax - size.y * .5f - 8f);
            panelRect.anchoredPosition = new Vector2(Mathf.Round(point.x/4f),Mathf.Round(point.y/4f))*4f;
        }
    }
}
