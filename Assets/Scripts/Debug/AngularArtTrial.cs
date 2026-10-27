#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using DungeonSlash;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

// Only attached to the two disposable art-direction scenes. No production data writes.
[DefaultExecutionOrder(-500)]
public sealed class AngularArtTrial : MonoBehaviour
{
    public GameContent content;
    public string mainScene="Assets/Scenes/Trials/Angular01_Main.unity";
    public bool characterPage;
    public RoomChoiceButton[] navigation=new RoomChoiceButton[0];
    public Vector2[] navigationPositions={new(0,148),new(-440,-12),new(440,-12),new(-530,-265)};
    bool previousBackground;
    public const string Report="Docs/Production/AngularTrial-2026-10-25/";
    void Awake()
    {
        var kind=RunSession.SelectedCharacter!=null?RunSession.SelectedCharacter.character:CharacterKind.Warrior;
        RunSession.SelectedCharacter=content.characters.First(c=>c.character==kind);
        previousBackground=Application.runInBackground;Application.runInBackground=true;
    }
    void OnDestroy(){Application.runInBackground=previousBackground;}
    void LateUpdate()
    {
        for(var i=0;i<navigation.Length&&i<navigationPositions.Length;i++)
            if(navigation[i]!=null)((RectTransform)navigation[i].transform).anchoredPosition=navigationPositions[i];
    }
    IEnumerator Start()
    {
        yield return null;
        if(characterPage)
        {
            var menu=FindFirstObjectByType<OutGameMenuView>();menu.ShowCharacters();menu.selection.Select(0);
            menu.selection.startButton.onClick.RemoveAllListeners();
            menu.selection.startButton.onClick.AddListener(()=>
            {
                RunSession.SelectedCharacter=content.characters[menu.selection.SelectedIndex];
                EditorSceneManager.LoadSceneInPlayMode(mainScene,new LoadSceneParameters(LoadSceneMode.Single));
            });
        }
    }
    public void Capture(string name)=>StartCoroutine(Frame(name));
    IEnumerator Frame(string name)
    {
        yield return null;yield return new WaitForEndOfFrame();
        Directory.CreateDirectory(Report+"Screenshots");
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Report+"Screenshots/"+name+".png",image.EncodeToPNG());
        Destroy(image);
        File.AppendAllText(Report+"Live.txt",name+" rendered in "+SceneManager.GetActiveScene().name+"\n");
    }
    public void Review()=>StartCoroutine(ReviewSelection());
    IEnumerator ReviewSelection()
    {
        var menu=FindFirstObjectByType<OutGameMenuView>();menu.ShowCharacters();
        for(var i=0;i<content.characters.Count;i++)
        {
            menu.selection.Select(i);yield return Frame("Selection-"+content.characters[i].character);
            var ok=menu.selection.selectionSplash.sprite==content.characters[i].selectionIllustration;
            File.AppendAllText(Report+"Live.txt",(ok?"PASS":"FAIL")+" selected art "+content.characters[i].character+"\n");
        }
        menu.selection.Select(0);
        File.AppendAllText(Report+"Live.txt","SELECTION COMPLETE\n");
    }
    public void ContinueFirst()=>StartCoroutine(ContinueNavigation());
    IEnumerator ContinueNavigation()
    {
        var continueText=GameText.Get("ui.runexpansionview.781bae3a");
        var button=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text.Contains(continueText)));
        if(button==null){File.AppendAllText(Report+"Live.txt","FAIL start continuation not found\n");yield break;}
        button.onClick.Invoke();
        File.AppendAllText(Report+"Live.txt","PASS first-run Continue button\n");
        yield return Frame("Main-First-Navigation");
        var run=FindFirstObjectByType<RunController>();
        File.AppendAllText(Report+"Live.txt",(run.State.Player.Data==content.characters.First(c=>c.character==run.State.Player.Data.character)?"PASS":"FAIL")+" isolated trial character data\n");
    }
}
#endif
