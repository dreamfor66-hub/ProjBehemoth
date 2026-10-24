using UnityEngine;using UnityEngine.EventSystems;
namespace DungeonSlash{
public sealed class ChoicePreview:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler{
 private ItemTooltipView tooltip;private EquipmentData item;private string heading,body;
 public void Bind(ItemTooltipView view,EquipmentData data,string title,string description){tooltip=view;item=data;heading=title;body=description;}
 public void OnPointerEnter(PointerEventData e){if(tooltip!=null && (item!=null || !string.IsNullOrWhiteSpace(body)))tooltip.Show(item!=null?item.displayName:heading,item!=null?item.description:body,e.position);}
 public void OnPointerExit(PointerEventData e)=>tooltip?.Hide();private void OnDisable()=>tooltip?.Hide();
}}
