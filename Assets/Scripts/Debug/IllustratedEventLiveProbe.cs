#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class IllustratedEventLiveProbe:MonoBehaviour
{
    const string Folder="Docs/Production/EventIllustrations-2026-10-29/";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    RunExpansionView view;int failures;
    public void Run()=>StartCoroutine(Review());
    void Check(bool ok,string label){if(!ok)failures++;File.AppendAllText(Folder+"Runtime.txt",(ok?"PASS ":"FAIL ")+label+"\n");}
    static Rect Bounds(RectTransform t,RectTransform relative){var p=new Vector3[4];t.GetWorldCorners(p);var a=relative.InverseTransformPoint(p[0]);var b=relative.InverseTransformPoint(p[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);}
    IEnumerator Capture(string label){yield return new WaitForSecondsRealtime(.65f);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Folder+label+".png");yield return null;}
    void Inspect(string label,int count)
    {
        Canvas.ForceUpdateCanvases();var panel=(RectTransform)view.modalTitle.transform.parent;
        var art=Bounds(view.modalIllustration.rectTransform,panel);var body=Bounds(view.narrativeScroll.GetComponent<RectTransform>(),panel);
        var rows=view.ActiveChoices.Where(r=>r.button.gameObject.activeInHierarchy).ToArray();
        Check(rows.Length==count,label+" all "+count+" choices visible");
        Check(!view.previousPage.gameObject.activeSelf&&!view.nextPage.gameObject.activeSelf,label+" no choice pagination");
        Check(body.xMin>=art.xMax+view.eventColumnGap-.2f,label+" narrative is right of art");
        Check(rows.All(r=>!Bounds((RectTransform)r.button.transform,panel).Overlaps(body)),label+" narrative and choices do not overlap");
        Check(rows.All(r=>Bounds((RectTransform)r.button.transform,panel).xMin>=art.xMax+view.eventColumnGap-.2f),label+" all choices right of art");
        Check(rows.All(r=>!r.title.isTextOverflowing&&!r.description.isTextOverflowing),label+" choice text fits");
        Check(rows.All(r=>Bounds((RectTransform)r.button.transform,panel).yMin>=panel.rect.yMin),label+" choices inside panel");
        Check(body.height>=180,label+" narrative has at least 180 canvas units for comfortable reading");
        File.AppendAllText(Folder+"Runtime.txt",label+" panel="+panel.rect+" art="+art+" body="+body+" content="+view.narrativeScroll.content.rect+"\n");
    }
    IEnumerator Review()
    {
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Runtime.txt","Scoped live Unity popup review. "+Screen.width+"x"+Screen.height+"\n");
        view=FindFirstObjectByType<RunExpansionView>();var flow=FindFirstObjectByType<GameFlowController>();var dungeon=FindFirstObjectByType<DungeonController>();
        if(view==null||flow==null){Check(false,"MainGameScene loaded");yield break;}
        foreach(var id in new[]{"Mirror","Pilgrim"})
        {
            var story=UnityEditor.AssetDatabase.LoadAssetAtPath<RandomEventData>("Assets/Data/Dungeon/Events/"+id+".asset");
            typeof(GameFlowController).GetMethod("ShowEventPage",Private).Invoke(flow,new object[]{dungeon.State.Player.CurrentRoom,story,0,0});
            yield return null;yield return null;
            Check(view.modalNarrative.text==story.pages[0].narrative,id+" complete narrative and choices presented together");
            Inspect(id,story.pages[0].options.Count);
            var options=(PresentationChoice[])typeof(RunExpansionView).GetField("currentChoices",Private).GetValue(view);
            var tex=story.illustration.texture;var w=tex.height*.5f;var crop=Sprite.Create(tex,new Rect((tex.width-w)*.5f,0,w,tex.height),Vector2.one*.5f,100);
            view.eventIllustrationAspect=.5f;view.ShowModal(story.displayName,story.pages[0].narrative,options,crop,illustratedEvent:true);
            yield return Capture(id+"-A-1x2");
            view.eventIllustrationAspect=.75f;view.ShowModal(story.displayName,story.pages[0].narrative,options,story.illustration,illustratedEvent:true);
            yield return Capture(id+"-B-3x4");Inspect(id+" final",options.Length);
            if(id=="Pilgrim")
            {
                Check(view.ActiveChoices.Any(c=>c.button.gameObject.activeSelf&&!c.button.interactable),"Pilgrim conditionally unavailable choice visible");
                Check(view.ActiveChoices.Where(c=>c.button.gameObject.activeSelf&&!c.button.interactable).All(c=>c.title.color.r<.6f),"unavailable choice text visibly dimmed");
                var wheel=new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-2)};
                ExecuteEvents.Execute(view.narrativeScroll.gameObject,wheel,ExecuteEvents.scrollHandler);yield return null;
                Check(view.narrativeScroll.verticalNormalizedPosition<.99f,"mouse wheel scrolls long narrative");
                view.narrativeScroll.verticalNormalizedPosition=0;yield return null;
                Check(view.narrativeScroll.verticalNormalizedPosition<.01f,"long narrative scroll reaches end");
                yield return Capture("Pilgrim-Scrolled");view.narrativeScroll.verticalNormalizedPosition=1;
            }
            else
            {
                var button=view.ActiveChoices[0].button;var feedback=button.GetComponent<InkUiFeedback>();var pointer=new PointerEventData(EventSystem.current);
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);Check(feedback!=null&&feedback.surface.IsHovered,"choice hover feedback");
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);Check(feedback!=null&&feedback.surface.IsPressed,"choice pressed feedback");
                ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);
                button.onClick.Invoke();yield return null;
                Check(view.modal.activeSelf&&view.modalNarrative.text==story.pages[1].narrative,"Mirror actual choice opens next authored page");
                yield return Capture("Mirror-NextPage");
            }
        }
        view.ShowModal(GameText.Get("ui.common.close"),GameText.Get("ui.event.continue"),new[]{new PresentationChoice{Title=GameText.Get("ui.common.close"),Selected=view.HideModal}});
        yield return null;Check(view.ActiveChoices==view.choices,"ordinary modal returns to standard rows");
        view.ActiveChoices[0].button.onClick.Invoke();Check(!view.modal.activeSelf,"ordinary modal action closes after portrait event");
        File.AppendAllText(Folder+"Runtime.txt","COMPLETE failures="+failures+"\n");
        UnityEditor.SessionState.SetBool("IllustratedEventFinished",true);
    }
}
#endif
