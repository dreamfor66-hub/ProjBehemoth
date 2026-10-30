using System.Collections.Generic;
using UnityEngine;

namespace DungeonSlash
{
    /// <summary>Only rented items live in the layout. Returned icons live in a separate inactive pool.</summary>
    public sealed class RunHudIconPool : MonoBehaviour
    {
        public RunHudIconView prefab;
        public RectTransform content;
        public RectTransform storage;
        readonly List<RunHudIconView> active=new();
        readonly Stack<RunHudIconView> available=new();
        public IReadOnlyList<RunHudIconView> Active=>active;
        public int CreatedCount {get;private set;}
        public int AvailableCount=>available.Count;

        public void SetCount(int count)
        {
            if(content==null||prefab==null)return;
            count=Mathf.Max(0,count);
            if(storage==null)
            {
                var go=new GameObject("Pool",typeof(RectTransform));storage=(RectTransform)go.transform;
                storage.SetParent(transform,false);storage.gameObject.SetActive(false);
            }
            while(active.Count>count)
            {
                var index=active.Count-1;var icon=active[index];active.RemoveAt(index);
                icon.BindEmpty(null);icon.gameObject.SetActive(false);icon.transform.SetParent(storage,false);available.Push(icon);
            }
            while(active.Count<count)
            {
                RunHudIconView icon;
                if(available.Count>0)icon=available.Pop();
                else{icon=Instantiate(prefab,storage);icon.name="Item";CreatedCount++;}
                icon.transform.SetParent(content,false);icon.transform.SetAsLastSibling();
                icon.gameObject.SetActive(true);active.Add(icon);
            }
        }
    }
}
