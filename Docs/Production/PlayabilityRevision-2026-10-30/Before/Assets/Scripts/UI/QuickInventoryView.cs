using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace DungeonSlash
{
    public sealed class QuickInventoryView : MonoBehaviour
    {
        public RunController run;
        public RunHudIconPool relicPool,potionPool;
        public ItemTooltipView tooltip;
        public PotionActionView potionActions;
        [FormerlySerializedAs("relics"),SerializeField,HideInInspector] RunHudIconView[] legacyRelics=Array.Empty<RunHudIconView>();
        [FormerlySerializedAs("potions"),SerializeField,HideInInspector] RunHudIconView[] legacyPotions=Array.Empty<RunHudIconView>();
        public IReadOnlyList<RunHudIconView> relics=>relicPool!=null?relicPool.Active:legacyRelics;
        public IReadOnlyList<RunHudIconView> potions=>potionPool!=null?potionPool.Active:legacyPotions;
        RunState bound;
        EquipmentData[] shownRelics=Array.Empty<EquipmentData>(),shownPotions=Array.Empty<EquipmentData>();
        public void ConfigurePools(RunHudIconPool newRelics,RunHudIconPool newPotions)
        {
            relicPool=newRelics;potionPool=newPotions;
            legacyRelics=legacyPotions=Array.Empty<RunHudIconView>();bound=null;
        }
        void LateUpdate()=>Refresh();
        public void Refresh()
        {
            var state=run!=null?run.State:null;
            if(state==null){relicPool?.SetCount(0);potionPool?.SetCount(0);bound=null;return;}
            var equipment=state.EquippedItems.Where(e=>!e.IsPotion);
            if(bound==state&&shownRelics.SequenceEqual(equipment)&&shownPotions.SequenceEqual(state.Potions))return;
            bound=state;shownRelics=equipment.ToArray();shownPotions=state.Potions.ToArray();
            relicPool?.SetCount(shownRelics.Length);potionPool?.SetCount(shownPotions.Length);
            for(var i=0;i<relics.Count;i++)
            {
                var visible=i<shownRelics.Length;relics[i].gameObject.SetActive(visible);
                if(visible)relics[i].Bind(shownRelics[i],tooltip);
            }
            for(var i=0;i<potions.Count;i++)
            {
                var visible=i<shownPotions.Length;potions[i].gameObject.SetActive(visible);if(!visible)continue;
                var item=shownPotions[i];var anchor=(RectTransform)potions[i].transform;
                potions[i].Bind(item,tooltip,potionActions==null?null:()=>potionActions.Open(item,anchor));
            }
        }
    }
}
