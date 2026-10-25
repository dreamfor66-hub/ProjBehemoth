using System.IO;
using System.Linq;
using System.Text;
using DungeonSlash;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

// Explicit, scoped requests in the user's existing Editor. Never starts a background suite.
[InitializeOnLoad]
internal static class NativeTextVerification
{
    const string Request="Temp/NativeText.request";
    const string Folder="Docs/Production/NativeText/";
    static NativeTextVerification()=>EditorApplication.update+=Poll;
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
        string command;try{command=File.ReadAllText(Request).Trim();File.Delete(Request);}catch(IOException){return;}
        if(!EditorApplication.isPlaying)return;
        var menu=Object.FindFirstObjectByType<OutGameMenuView>();
        if(command=="event-font-audit")AuditEventFonts();
        if(command=="mixed-font")
        {
            const string output="Docs/Production/Typography/";
            Directory.CreateDirectory(output);
            var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Art/Fonts/Typography/DosStory.asset");
            var text=new StringBuilder("Scoped mixed-font runtime audit / "+Screen.width+"x"+Screen.height+"\n");
            var source=AssetDatabase.GetAssetPath(font.sourceFontFile);
            text.AppendLine("Source: "+source+" / family: "+font.faceInfo.familyName);
            text.AppendLine("Mixed source loaded: "+source.EndsWith("/DOSStoryGothicLatin.ttf"));
            foreach(var label in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(t=>t.font==font&&t.text.Any(c=>c is >= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z')))
            {
                label.GetComponent<TypographyRoleBinding>()?.Apply();label.ForceMeshUpdate();
                text.AppendLine(label.name+" / "+label.text.Replace('\n',' ')+" / overflow="+label.isTextOverflowing+" / size="+label.fontSize);
            }
            var probe=new GameObject("Scoped mixed-font glyph probe",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI));
            try
            {
                probe.transform.SetParent(Object.FindFirstObjectByType<Canvas>().transform,false);
                var label=probe.GetComponent<TMPro.TextMeshProUGUI>();label.font=font;
                label.rectTransform.anchoredPosition=new Vector2(10000,10000);
                label.rectTransform.sizeDelta=new Vector2(1000,100);label.fontSize=32;
                label.text="Az09 · 잊힌 미궁 궤";label.ForceMeshUpdate();
                text.AppendLine("Probe: "+label.text);
                foreach(var character in label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c=>!char.IsWhiteSpace(c.character)))
                    text.AppendLine(character.character+" / "+character.fontAsset.name+" / glyph="+character.textElement.glyphIndex);
                text.AppendLine("Dynamic new Hangul available: "+font.HasCharacter('궤',false,true));
            }
            finally {Object.DestroyImmediate(probe);}
            File.WriteAllText(output+"MixedFontRuntime.txt",text.ToString());
        }
        if(command=="wizard")
        {
            menu.ShowCharacters();menu.selection.Select(menu.selection.content.characters.FindIndex(c=>c.character==CharacterKind.Wizard));
        }
        if(command=="tooltip")menu.selection.startingRelicHover.OnPointerEnter(new PointerEventData(EventSystem.current));
        if(command=="tooltip-hide")menu.selection.startingRelicHover.OnPointerExit(new PointerEventData(EventSystem.current));
        if(command=="details")menu.selection.ToggleDetails();
        if(command=="title")menu.ShowTitle();
        if(command=="scroll")
        {
            var view=Object.FindFirstObjectByType<RunExpansionView>();
            var narrative=string.Join("\n\n",Enumerable.Repeat(GameText.Get("data.randomeventdata.hourglass.narrative"),10));
            view.ShowModal(GameText.Get("data.randomeventdata.hourglass.displayname"),narrative,new[]{new PresentationChoice {Title=GameText.Get("ui.event.continue"),Selected=view.HideModal}});
        }
        if(command=="damage")
        {
            Object.FindFirstObjectByType<RunExpansionView>().HideModal();
            var combat=Object.FindFirstObjectByType<CombatController>();
            typeof(CombatController).GetMethod("ShowHitDamage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(combat,new object[]{Vector2.zero,123f,true});
            EditorApplication.delayCall+=()=>{Directory.CreateDirectory(Folder+"Screenshots");ScreenCapture.CaptureScreenshot(Folder+"Screenshots/damage-fullhd.png");};
        }
        if(command.StartsWith("capture:"))
        {
            Directory.CreateDirectory(Folder+"Screenshots");
            var label=command.Substring(8);
            ScreenCapture.CaptureScreenshot(Folder+"Screenshots/"+label+".png");
            var text=new StringBuilder(label+" / "+Screen.width+"x"+Screen.height+"\n");
            foreach(var raster in Object.FindObjectsByType<ArcadeRasterCamera>(FindObjectsSortMode.None))
            {
                text.AppendLine("Art: "+raster.presentation.width+"x"+raster.presentation.height+"; palette="+raster.quantizeColors+"; native UI draws="+raster.TextRenderer.Draws.Count);
                foreach(var draw in raster.TextRenderer.Draws.Where(d=>d.material.shader.name.StartsWith("TextMeshPro")))text.AppendLine("Native text: "+draw.material.name+" / "+draw.mesh.vertexCount+" vertices / clip="+draw.clip);
            }
            File.WriteAllText(Folder+label+".txt",text.ToString());
        }
    }

    static void AuditEventFonts()
    {
        const string output="Docs/Production/Typography/EventFontRuntime.txt";
        var report=new StringBuilder();var passed=true;
        void Check(bool condition,string description){passed&=condition;report.AppendLine((condition?"PASS: ":"FAIL: ")+description);}
        var view=Object.FindFirstObjectByType<RunExpansionView>();
        if(view==null){File.WriteAllText(output,"PENDING: MainGameScene is not loaded.");return;}
        var profile=AssetDatabase.LoadAssetAtPath<TypographyProfile>(TypographyPreparation.ProfilePath);
        var trial=Object.Instantiate(profile);
        var rows=view.choices.Concat(view.compactChoices??System.Array.Empty<ExpansionChoiceButton>()).ToArray();
        var labels=new[]{view.modalTitle,view.modalNarrative}.Concat(rows.SelectMany(row=>new[]{row.title,row.description})).ToArray();
        var bindings=labels.Select(label=>label.GetComponent<TypographyRoleBinding>()).ToArray();
        var originals=bindings.Select(binding=>binding!=null?binding.profile:null).ToArray();
        var narrowProbe=new GameObject("Scoped trial rollback probe",typeof(RectTransform),typeof(TMPro.TextMeshProUGUI),typeof(TypographyRoleBinding));
        narrowProbe.transform.SetParent(view.modal.transform,false);
        var probeLabel=narrowProbe.GetComponent<TMPro.TextMeshProUGUI>();
        probeLabel.rectTransform.anchoredPosition=new Vector2(10000,10000);probeLabel.rectTransform.sizeDelta=new Vector2(1,64);
        probeLabel.text="ABCDE123";
        var probeBinding=narrowProbe.GetComponent<TypographyRoleBinding>();
        probeBinding.profile=trial;probeBinding.role=TextRole.Button;probeBinding.designSize=32;probeBinding.snapSize=false;
        try
        {
            Check(bindings.All(binding=>binding!=null),"Event labels have saved semantic bindings");
            Check(bindings[1]!=null&&bindings[1].role==TextRole.Narrative,"Narrative uses Narrative role");
            foreach(var row in rows)Check(row.title.GetComponent<TypographyRoleBinding>()?.role==TextRole.Button&&row.description.GetComponent<TypographyRoleBinding>()?.role==TextRole.Description,"Choice title/description roles: "+row.button.name);
            foreach(var binding in bindings)if(binding!=null)binding.profile=trial;
            var story=AssetDatabase.LoadAssetAtPath<GameContent>("Assets/Data/GameContent.asset").events.First(item=>item.options.Count>=2);
            foreach(var enabled in new[]{true,false,true})
            {
                trial.useTangbaForCondensed=enabled;
                var expected=enabled?trial.tangba:trial.galmuri11Condensed;
                Check(expected!=null,"Trial face is imported: "+enabled);
                probeBinding.Apply();
                Check(probeLabel.font==expected,"Cached narrow-label alternative follows toggle without text/width changes: "+enabled);
                view.ShowModal(story.displayName,story.narrative,story.options.Select(option=>new PresentationChoice{Title=option.title,Description=option.description}),story.illustration);
                foreach(var binding in bindings)binding?.Apply();
                Check(view.modalNarrative.font==expected,"Narrative follows trial toggle: "+enabled);
                foreach(var row in view.choices.Where(row=>row.button.gameObject.activeSelf))
                {
                    row.description.ForceMeshUpdate();
                    Check(row.description.font==expected,"Visible description follows trial toggle: "+enabled);
                    Check(row.description.textBounds.size.y<=row.description.rectTransform.rect.height+2,"Visible description fits row: "+enabled);
                }
                // Both serialized choice layouts must resolve correctly, even when one is inactive.
                Check(rows.All(row=>row.description.font==expected),"Both choice layouts follow trial toggle: "+enabled);
            }
        }
        catch(System.Exception exception){passed=false;report.AppendLine(exception.ToString());}
        finally
        {
            for(var i=0;i<bindings.Length;i++)if(bindings[i]!=null){bindings[i].profile=originals[i];bindings[i].Apply();}
            view.HideModal();Object.DestroyImmediate(narrowProbe);Object.DestroyImmediate(trial);
        }
        File.WriteAllText(output,(passed?"PASS":"FAIL")+" / scoped event typography and trial rollback\n"+report);
    }
}
