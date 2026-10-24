using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Explicitly requested QA only. Saved production content is never regenerated.
// Results are written under Logs; it is never part of a player build.
[InitializeOnLoad]
internal static class BehemothVerification
{
    private const string Request = "Temp/BehemothVerification.request";
    private static TestRunnerApi runner;
    static BehemothVerification() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
        string command;
        try { command = File.ReadAllText(Request).Trim(); File.Delete(Request); }
        catch (IOException) { return; } // A request may still be held by the file writer this frame.
        Directory.CreateDirectory("Logs");
        if (command == "ashen-tests")
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
            File.WriteAllText("Logs/Behemoth-TestCases.txt", "");
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] { "DungeonSlash.Tests.AshenVowContentTests" } }));
        }
        else if (command == "tests" || command == "expansion-tests")
            RunTests(command == "expansion-tests");
        else if (command == "functional-tests")
        {
            BehemothTextEditor.PrepareForVerification();
            runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
            File.WriteAllText("Logs/Behemoth-TestCases.txt", ""); File.WriteAllText("Logs/Behemoth-PlaytestMatrix.txt", "");
            // The two long balance comparisons have a separate recorded matrix; run every functional case.
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
                groupNames = new[] { @"!^DungeonSlash\.Tests\.BehemothPlaytests\.Balance_" } }));
        }
        else if (command == "unit-tests")
        {
            runner=ScriptableObject.CreateInstance<TestRunnerApi>();runner.RegisterCallbacks(new Results());
            File.WriteAllText("Logs/Behemoth-TestCases.txt","");
            runner.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=new[]{"DungeonSlash.Tests.BehemothBuffTests","DungeonSlash.Tests.BehemothBuffUnificationTests","DungeonSlash.Tests.BehemothAbilityTests","DungeonSlash.Tests.BehemothExpansionTests","DungeonSlash.Tests.BehemothFeedbackTests","DungeonSlash.Tests.DungeonSlashCoreTests"}}));
        }
        else if (command == "stealth-tests")
        {
            BehemothTextEditor.PrepareForVerification();
            runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
            File.WriteAllText("Logs/Behemoth-TestCases.txt", ""); File.WriteAllText("Logs/Behemoth-PlaytestMatrix.txt", "");
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] {
                "DungeonSlash.Tests.BehemothStealthTests",
                "DungeonSlash.Tests.BehemothPlaytests.Stealth_PhysicalChargeAndLongCast_FreezePackUntilImpact_NoDoubleAction",
                "DungeonSlash.Tests.BehemothPlaytests.Stealth_PotionsNonDamageSpellGuardAndInvalidInput_RollOnlyCompletedActions",
                "DungeonSlash.Tests.BehemothPlaytests.Stealth_OtherFiveClasses_ImmediateTimersAndOriginalUI_NoConcealmentDelay"
            } }));
        }
        else if (command == "ui-tests")
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
            File.WriteAllText("Logs/Behemoth-TestCases.txt", ""); File.WriteAllText("Logs/Behemoth-PlaytestMatrix.txt", "");
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode,
                testNames = new[] { "DungeonSlash.Tests.BehemothUiTests", "DungeonSlash.Tests.BehemothPlaytests.UI_CentralSelectionAndSummary_ModalPriority_FixedMerchant",
                    "DungeonSlash.Tests.BehemothAbilityTests.SpellIntroduction_AllRewardSources_NoEarlyBooks_LateActOneRare_ActTwoNormal" } }));
        }
        else if (command == "selection") EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity");
        else if (command == "texts-export") BehemothTextEditor.ExportDefault();
        else if (command == "balance-tests")
        {
            runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] { "DungeonSlash.Tests.BehemothPlaytests.Balance_AuthoredElites_ThreeActs_MatchedProgression_DefenseAndFollowup", "DungeonSlash.Tests.BehemothPlaytests.Balance_SixCoreLoops_SustainedDpsProbe_AndVisibleDog" } }));
        }
    }
    private static void RunTests(bool expansionOnly)
    {
        BehemothTextEditor.PrepareForVerification();
        runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
        var filter = new Filter { testMode = TestMode.EditMode };
        if (expansionOnly) filter.testNames = new[] { "DungeonSlash.Tests.BehemothExpansionTests", "DungeonSlash.Tests.BehemothPlaytests" };
        File.WriteAllText("Logs/Behemoth-TestCases.txt", ""); File.WriteAllText("Logs/Behemoth-PlaytestMatrix.txt", "");
        runner.Execute(new ExecutionSettings(filter));
    }
    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { File.WriteAllText("Logs/Behemoth-TestStatus.txt", "Running requested test selection"); }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, "Logs/Behemoth-TestResults.xml");
            File.WriteAllText("Logs/Behemoth-TestStatus.txt", $"{result.TestStatus}: passed {result.PassCount}, failed {result.FailCount}, skipped {result.SkipCount}\n{result.Message}");
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity"); };
        }
        public void TestStarted(ITestAdaptor test) { File.WriteAllText("Logs/Behemoth-TestCurrent.txt", test.FullName); }
        public void TestFinished(ITestResultAdaptor result)
        { if (result.Test.TestCaseCount == 1) File.AppendAllText("Logs/Behemoth-TestCases.txt", $"{result.Test.FullName}: {result.TestStatus}\n{result.Message}\n"); }
    }
}
