using UnityEngine;
using UnityEngine.UI;

namespace DungeonSlash
{
    // Item capacity comes from the run; icon size/spacing comes from the authored item grid.
    public sealed class PotionBagView : MonoBehaviour
    {
        public RunHudIconPool items;
        public Image artwork, slotTemplate;
        public RectTransform slotBackgrounds;
        public RectOffset padding = new();
        int capacity = -1;

        public void Bind(RunState state)
        {
            gameObject.SetActive(state!=null);
            if(state!=null&&capacity!=state.PotionSlotCapacity)PrepareSlots(state.PotionSlotCapacity);
        }

        public void PrepareSlots(int count)
        {
            if(items==null||items.content==null||slotTemplate==null)return;
            var grid=items.content.GetComponent<GridLayoutGroup>();
            if(grid==null)return;
            capacity=count;
            var height=count*grid.cellSize.y+Mathf.Max(0,count-1)*grid.spacing.y;
            ((RectTransform)transform).sizeDelta=new Vector2(padding.horizontal+grid.cellSize.x,padding.vertical+height);
            foreach(var r in new[]{items.content,slotBackgrounds})
            {
                r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
                r.anchoredPosition=new Vector2(padding.left,-padding.top);
                r.sizeDelta=new Vector2(grid.cellSize.x,height);
            }
            while(slotBackgrounds.childCount<count)
            {
                var slot=Instantiate(slotTemplate,slotBackgrounds);slot.name="Potion Slot "+(slotBackgrounds.childCount);
            }
            for(var i=0;i<slotBackgrounds.childCount;i++)
            {
                var slot=(RectTransform)slotBackgrounds.GetChild(i);slot.gameObject.SetActive(i<count);
                slot.anchorMin=slot.anchorMax=slot.pivot=new Vector2(0,1);
                slot.anchoredPosition=new Vector2(0,-i*(grid.cellSize.y+grid.spacing.y));slot.sizeDelta=grid.cellSize;
            }
        }
    }
}
