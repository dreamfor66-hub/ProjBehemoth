using UnityEngine;
namespace DungeonSlash{
public sealed class QuickInventoryView:MonoBehaviour{
 public RunController run;public RunHudIconView[] relics,potions;public ItemTooltipView tooltip;public PotionActionView potionActions;private RunState bound;private int signature;
 private void LateUpdate(){var state=run?.State;if(state==null)return;var hash=17;unchecked{foreach(var item in state.EquippedItems)hash=hash*31+item.GetInstanceID();foreach(var item in state.Potions)hash=hash*31+item.GetInstanceID();}
 if(bound==state && hash==signature)return;bound=state;signature=hash;
 var equipped=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(state.EquippedItems,e=>!e.IsPotion));
 for(var i=0;i<relics.Length;i++){relics[i].gameObject.SetActive(i<equipped.Length);if(i<equipped.Length)relics[i].Bind(equipped[i],tooltip);}
 for(var i=0;i<potions.Length;i++){potions[i].gameObject.SetActive(i<state.Potions.Count);if(i<state.Potions.Count){var potion=state.Potions[i];var anchor=(RectTransform)potions[i].transform;potions[i].Bind(potion,tooltip,()=>potionActions.Open(potion,anchor));}}
 }
}}

