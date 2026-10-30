#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class IllustratedOutgameLiveProbe:MonoBehaviour
{
    string Folder="Docs/Production/OutgameIllustrated-2026-10-29/";
    int failures;bool oldBackground;PlayerCombatData oldSelected;
    void Awake(){oldBackground=Application.runInBackground;Application.runInBackground=true;oldSelected=RunSession.SelectedCharacter;DontDestroyOnLoad(gameObject);}
    void OnDestroy(){Application.runInBackground=oldBackground;RunSession.SelectedCharacter=oldSelected;}
    void Check(bool ok,string text){File.AppendAllText(Folder+"Runtime.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)failures++;}
    public void Run()=>StartCoroutine(Review());
    public void RunDetails()=>StartCoroutine(ReviewDetails());
    public void RunArtwork()
    {
        Folder="Docs/Production/OutgameIllustrated-2026-10-29/CharacterCharmRevision/";
        Directory.CreateDirectory(Folder);StartCoroutine(ReviewArtwork());
    }
    public void RunGripArtwork()
    {
        Folder="Docs/Production/OutgameIllustrated-2026-10-29/BarbarianGripRevision/";
        Directory.CreateDirectory(Folder);StartCoroutine(ReviewArtwork(2));
    }
    IEnumerator ReviewArtwork(int onlyIndex=-1)
    {
        File.WriteAllText(Folder+"Runtime.txt","Scoped character artwork rendering in the existing Unity Editor. No combat, balance or broader menu suite.\n");
        SceneManager.LoadScene("OutGameScene");yield return null;yield return null;
        var menu=FindFirstObjectByType<OutGameMenuView>();var view=menu.selection;menu.ShowCharacters();
        Check(view.illustratedSelection&&view.characterButtons.Length==6,"saved illustrated selection contains six characters");
        for(var i=0;i<view.characterButtons.Length;i++)
        {
            if(onlyIndex>=0&&i!=onlyIndex)continue;
            view.characterButtons[i].onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();var data=view.content.characters[i];
            Check(view.SelectedIndex==i&&view.selectionSplash.sprite==data.selectionIllustration,data.character+" selects its installed illustration");
            Check(view.selectionSplash.sprite!=null&&view.selectionSplash.sprite.texture.width>=1600,data.character+" full-resolution artwork imported");
            Readable(view.characterName,data.character+" name");Readable(view.selectionStory,data.character+" story");
            yield return Capture("Selection-"+data.character+"-Live");
        }
        File.AppendAllText(Folder+"Runtime.txt","COMPLETE failures="+failures+"\n");UnityEditor.EditorApplication.isPlaying=false;
    }
    IEnumerator ReviewDetails()
    {
        File.WriteAllText(Folder+"Details-Verification.txt","Scoped details layout and pointer feedback follow-up in the same Unity Editor.\n");
        SceneManager.LoadScene("OutGameScene");yield return null;yield return null;
        var menu=FindFirstObjectByType<OutGameMenuView>();var view=menu.selection;menu.ShowCharacters();view.Select(5);
        var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        var feedback=view.detailsButton.GetComponent<InkUiFeedback>();
        ExecuteEvents.Execute(view.detailsButton.gameObject,data,ExecuteEvents.pointerEnterHandler);
        Check(feedback.surface.IsHovered,"details button hover reaches authored ink surface");
        ExecuteEvents.Execute(view.detailsButton.gameObject,data,ExecuteEvents.pointerDownHandler);
        Check(feedback.surface.IsPressed,"details button press feedback");
        ExecuteEvents.Execute(view.detailsButton.gameObject,data,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(view.detailsButton.gameObject,data,ExecuteEvents.pointerExitHandler);
        Check(!feedback.surface.IsPressed&&!feedback.surface.IsHovered,"pointer release/exit clears feedback");
        view.detailsButton.onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)view.detailsPanel.transform;
        Check(rect.anchoredPosition.x< -400&&rect.rect.height>=329,"details stays inside left introduction area");
        Check(view.extraStatsHover.gameObject!=view.detailsPanel,"extra-stat hover has an independent footer anchor");
        Readable(view.abilityStats,"left details statistics");yield return Capture("Details-Live");
        view.extraStatsHover.OnPointerEnter(data);yield return Capture("Details-Hover-Live");view.extraStatsHover.OnPointerExit(data);
        view.detailsButton.onClick.Invoke();Check(!view.DetailsVisible&&view.illustratedIntroduction.activeSelf,"introduction restores after details follow-up");
        File.AppendAllText(Folder+"Details-Verification.txt","COMPLETE failures="+failures+"; follow-up PASS/FAIL rows appended to Runtime.txt\n");UnityEditor.EditorApplication.isPlaying=false;
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForSecondsRealtime(.18f);yield return null;
        yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+name+".png",shot.EncodeToPNG());Destroy(shot);
        var raster=FindFirstObjectByType<ArcadeRasterCamera>();
        if(raster!=null&&raster.Frame!=null)
        {
            var old=RenderTexture.active;RenderTexture.active=raster.Frame;var frame=new Texture2D(raster.Frame.width,raster.Frame.height,TextureFormat.RGBA32,false);frame.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);frame.Apply();File.WriteAllBytes(Folder+name+"-ArtworkFrame.png",frame.EncodeToPNG());RenderTexture.active=old;Destroy(frame);
        }
    }
    void Readable(TMPro.TextMeshProUGUI text,string name)
    {if(text==null||!text.gameObject.activeInHierarchy)return;text.ForceMeshUpdate();Check(!text.isTextOverflowing,name+" fits its text rectangle ("+text.fontSize+")");}
    IEnumerator Review()
    {
        File.WriteAllText(Folder+"Runtime.txt","Existing Unity Editor, saved OutGameScene, actual UI event handlers and rendering. No combat/balance suite.\n");
        SceneManager.LoadScene("OutGameScene");yield return null;yield return null;
        var menu=FindFirstObjectByType<OutGameMenuView>();var view=menu.selection;Canvas.ForceUpdateCanvases();
        Check(menu.Page==OutGamePage.Title,"title page shown initially");yield return Capture("Title-Live");
        menu.newGame.onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
        Check(menu.Page==OutGamePage.Characters&&view.characterButtons.Length==6,"title opens six-character selection");
        Check(view.illustratedSelection&&view.selectionSplash.rectTransform.rect.width>=1535&&view.selectionSplash.rectTransform.rect.height>=863,"illustration covers whole selection canvas");
        for(var i=0;i<view.characterButtons.Length;i++)
        {
            view.characterButtons[i].onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();var data=view.content.characters[i];
            Check(view.SelectedIndex==i&&view.selectionSplash.sprite==data.selectionIllustration,data.character+" button updates full-screen art");
            Check(view.characterArtwork[i].sprite!=null&&view.characterLabels[i].gameObject.activeInHierarchy,data.character+" original-art portrait and label visible");
            var point=RectTransformUtility.WorldToScreenPoint(view.GetComponentInParent<Canvas>().worldCamera,((RectTransform)view.characterButtons[i].transform).TransformPoint(Vector3.zero));
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==view.characterButtons[i],data.character+" portrait hit target not blocked");
            Check(view.characterButtons[i].FindSelectableOnRight()==view.characterButtons[(i+1)%6],data.character+" right keyboard navigation linked");
            Readable(view.characterName,data.character+" name");Readable(view.selectionStory,data.character+" story");Readable(view.compactDescription,data.character+" ability");Readable(view.selectionRelic,data.character+" starting relic");
            yield return Capture("Selection-"+data.character+"-Live");
        }
        view.detailsButton.onClick.Invoke();yield return null;
        Check(view.DetailsVisible&&!view.illustratedIntroduction.activeSelf,"ability details hide introduction without overlap");Readable(view.abilityStats,"detail statistics");yield return Capture("Details-Live");
        view.detailsButton.onClick.Invoke();Check(!view.DetailsVisible&&view.illustratedIntroduction.activeSelf,"details close restores introduction");
        menu.back.onClick.Invoke();Check(menu.Page==OutGamePage.Title,"back returns to title");menu.options.onClick.Invoke();yield return null;
        Check(menu.Page==OutGamePage.Options,"options opens");menu.optionsBack.onClick.Invoke();Check(menu.Page==OutGamePage.Title,"options back returns to title");
        menu.newGame.onClick.Invoke();view.characterButtons[0].onClick.Invoke();var expected=view.content.characters[0];view.startButton.onClick.Invoke();
        for(var i=0;i<120&&SceneManager.GetActiveScene().name!="MainGameScene";i++)yield return null;
        Check(SceneManager.GetActiveScene().name=="MainGameScene"&&RunSession.SelectedCharacter==expected,"start enters main game with selected character");
        File.AppendAllText(Folder+"Runtime.txt","COMPLETE failures="+failures+"\n");UnityEditor.EditorApplication.isPlaying=false;
    }
}
#endif
