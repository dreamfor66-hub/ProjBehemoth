#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using DungeonSlash;
using UnityEngine;
using Object=UnityEngine.Object;

// Opt-in art-only checks in the existing Editor. No pacing/balance suite and no saved practice data.
public sealed class DarkArtLiveProbe : MonoBehaviour
{
    const string Folder="Docs/Production/CharacterRedesign-2026-10-25/";
    bool background;
    void Awake(){background=Application.runInBackground;Application.runInBackground=true;}
    void OnDestroy(){Application.runInBackground=background;}
    public void Run(string action)
    {
        Directory.CreateDirectory(Folder+"Screenshots");
        if(action=="menu")StartCoroutine(Menu());
        else if(action=="characters")StartCoroutine(Characters());
        else if(action=="combat")StartCoroutine(Combat());
        else if(action.StartsWith("capture:"))StartCoroutine(Frame(action.Substring(8)));
    }
    IEnumerator Frame(string name)
    {
        yield return null;yield return new WaitForEndOfFrame();
        var texture=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+"Screenshots/"+name+".png",texture.EncodeToPNG());
        var raster=Object.FindFirstObjectByType<ArcadeRasterCamera>();
        var palette=raster.presentation.darkPalette.Colors;
        var colors=new HashSet<Color32>();var unexpected=0;var dark=0;
        foreach(var pixel in texture.GetPixels32())
        {
            colors.Add(pixel);var matched=false;
            foreach(Color32 ink in palette)
                if(Mathf.Abs(ink.r-pixel.r)<=2&&Mathf.Abs(ink.g-pixel.g)<=2&&Mathf.Abs(ink.b-pixel.b)<=2){matched=true;break;}
            if(!matched)unexpected++;
            if(pixel.r<60&&pixel.g<60&&pixel.b<65)dark++;
        }
        File.AppendAllText(Folder+"Live.txt",$"{name}: {texture.width}x{texture.height}; viewport={raster.GetComponent<Camera>().pixelRect}; RGB colors={colors.Count}; outside palette (2-byte tolerance)={unexpected}; dark coverage={(float)dark/(texture.width*texture.height):P1}\n");
        Object.Destroy(texture);
    }
    IEnumerator Menu()
    {
        if(!File.Exists(Folder+"Live.txt"))File.WriteAllText(Folder+"Live.txt","Scoped live seven-ink review; final composited camera output including native TMP.\n");
        var menu=Object.FindFirstObjectByType<OutGameMenuView>();
        menu.ShowTitle();yield return Frame("01-Title");
        menu.ShowOptions();yield return Frame("02-Options");
        menu.ShowCharacters();
        for(var i=0;i<menu.selection.content.characters.Count;i++)
        {
            menu.selection.Select(i);yield return Frame("03-Selection-"+menu.selection.content.characters[i].character);
        }
        menu.selection.SetDetailsVisible(true);yield return Frame("04-Character-Details");
        menu.selection.SetDetailsVisible(false);menu.selection.Select(0);
        File.AppendAllText(Folder+"Live.txt","MENU COMPLETE\n");
    }
    IEnumerator Characters()
    {
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Character art practice").AddComponent<OctoberReviewLiveProbe>();
        foreach(var kind in new[]{CharacterKind.Warrior,CharacterKind.Rogue,CharacterKind.Barbarian,CharacterKind.Samurai,CharacterKind.Monk,CharacterKind.Wizard})
        {
            practice.Setup(kind,false);
            var combat=Object.FindFirstObjectByType<CombatController>();
            var view=Object.FindFirstObjectByType<PlayerView>();
            yield return Frame("Combat-"+kind+"-Idle");
            view.PlayAttack();
            // CPU screenshot/color readback may exceed the normal .3s pose window.
            // Hold only this editor probe's art state until its frame is recorded.
            var attackWindow=typeof(PlayerView).GetField("attackUntil",BindingFlags.Instance|BindingFlags.NonPublic);
            attackWindow.SetValue(view,Time.time+10f);
            yield return Frame("Combat-"+kind+"-Attack");
            var sprite=view.GetComponentInChildren<CombatSpriteAnimator>().GetComponent<UnityEngine.UI.Image>().sprite;
            File.AppendAllText(Folder+"Live.txt",(sprite==combat.Player.Data.combatAnimation.attack?"PASS":"FAIL")+" "+kind+" attack sprite\n");
            attackWindow.SetValue(view,0f);
            yield return new WaitForSeconds(.4f);
            combat.Player.SetKeyboardGuard(true);yield return Frame("Combat-"+kind+"-Guard");
            File.AppendAllText(Folder+"Live.txt",(combat.Player.IsGuarding?"PASS":"FAIL")+" "+kind+" guard art state\n");
            combat.Player.SetKeyboardGuard(false);
        }
        practice.Setup(CharacterKind.Warrior,false);
        File.AppendAllText(Folder+"Live.txt","CHARACTERS COMPLETE\n");
    }
    IEnumerator Combat()
    {
        var prior=File.Exists(Folder+"Live.txt")?File.ReadAllText(Folder+"Live.txt"):"";
        var menuEnd=prior.IndexOf("MENU COMPLETE\n",System.StringComparison.Ordinal);
        File.WriteAllText(Folder+"Live.txt",menuEnd>=0?prior.Substring(0,menuEnd+14):"Scoped final art runtime review.\n");
        var practice=Object.FindFirstObjectByType<OctoberReviewLiveProbe>()??new GameObject("Art practice clone").AddComponent<OctoberReviewLiveProbe>();
        practice.Setup(CharacterKind.Warrior,false);
        var combat=Object.FindFirstObjectByType<CombatController>();
        var scene=Object.FindFirstObjectByType<CombatSceneView>();
        var input=Object.FindFirstObjectByType<CombatInputController>();
        var enemy=combat.Enemies[0];
        foreach(var attack in enemy.Data.attacks)
        {
            attack.chargeTimeLimit=60;
            attack.chargeWeakPoints=new List<WeakPointDefinition>{new(){id="art-review",hitRadius=30,requiredChargeHits=1,localPosition=Vector2.zero}};
        }
        yield return Frame("05-Combat-Idle");
        enemy.StartCharge(scene.MonsterPosition);yield return Frame("06-Weakpoint-Charge");
        var point=enemy.WeakPoints[0];
        Vector2 ScreenAt(Vector2 p)=>RectTransformUtility.WorldToScreenPoint(scene.UiCamera,scene.LogicalArea.TransformPoint(p));
        void Contact(string method,params object[] args)=>typeof(CombatInputController).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input,args);
        var from=ScreenAt(point.HitShape.Center+Vector2.left*140);var to=ScreenAt(point.HitShape.Center+Vector2.right*140);
        combat.Player.ResetAttackCooldown();Contact("Begin",953,from);
        var elapsed=0f;
        while(elapsed<combat.Player.CurrentChargeDuration+.12f){yield return null;elapsed+=Time.deltaTime;Contact("Move",953,from);}
        yield return Frame("07-Player-Charged");
        // Full-frame readback/color audit stalls the Editor; start a fresh swipe sample window.
        // Keep the original held/charged contact and use the production Move/End path.
        var contacts=(System.Collections.IDictionary)typeof(CombatInputController).GetField("contacts",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(input);
        if(contacts.Contains(953))
        {
            var contact=contacts[953];
            var analyzer=(SwipeGestureAnalyzer)contact.GetType().GetField("Analyzer").GetValue(contact);
            analyzer.Reset((Vector2)contact.GetType().GetField("Latest").GetValue(contact),Time.unscaledTime);
        }
        Contact("Move",953,to);Contact("End",953,to,false);
        yield return Frame("08-Slash-And-Break");
        File.AppendAllText(Folder+"Live.txt",(point.RemainingHits==0&&enemy.IsStunned?"PASS":"FAIL")+$" production charged swipe breaks authored practice weak point (remaining={point.RemainingHits}, state={enemy.State})\n");
        yield return new WaitForSeconds(.12f);yield return Frame("09-Break-Followup");
        yield return new WaitForSeconds(.6f);
        combat.Player.SetKeyboardGuard(true);yield return Frame("10-Guard");
        File.AppendAllText(Folder+"Live.txt",(combat.Player.IsGuarding?"PASS":"FAIL")+" guard pose rendered during production guard state\n");
        combat.Player.SetKeyboardGuard(false);
        foreach(var kind in new[]{CharacterKind.Rogue,CharacterKind.Barbarian,CharacterKind.Samurai,CharacterKind.Monk,CharacterKind.Wizard})
        {
            practice.Setup(kind,false);yield return Frame("11-Combat-"+kind);
        }
        var backdrop=Object.FindFirstObjectByType<StageBackdropView>();
        var before=backdrop.background.sprite;
        var monsters=new[]{"Data_Monster_B1_Boss_MazeGuardian","Data_Monster_B2_Boss_FrostQueen","Data_Monster_B3_Boss_SoulEater"};
        for(var act=0;act<monsters.Length;act++)
        {
            practice.Setup(CharacterKind.Warrior,false,"Assets/Data/Combat/Monsters/"+monsters[act]+".asset");
            backdrop.background.sprite=backdrop.stages[Mathf.Min(act,backdrop.stages.Length-1)];
            yield return Frame("12-Stage-"+(act+1));
        }
        practice.Setup(CharacterKind.Warrior,false);backdrop.background.sprite=before;
        File.AppendAllText(Folder+"Live.txt","COMBAT COMPLETE\n");
    }
}
#endif
