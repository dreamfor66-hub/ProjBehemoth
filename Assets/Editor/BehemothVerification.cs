using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Narrow local QA bridge: requests can only rebuild content or run the project's test suite.
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
        if (command == "build" || command == "build-tests")
        {
            BehemothContentBuilder.Build();
            if (command == "build-tests") EditorApplication.delayCall += () => RunTests(false);
        }
        else if (command == "tests" || command == "expansion-tests")
            RunTests(command == "expansion-tests");
        else if (command == "selection") EditorSceneManager.OpenScene("Assets/Scenes/OutGameScene.unity");
        else if (command == "texts-export") BehemothTextEditor.ExportDefault();
        else if (command == "typography") BehemothTypography.FinalizeScenes();
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
        public void RunStarted(ITestAdaptor testsToRun) { File.WriteAllText("Logs/Behemoth-TestStatus.txt", "Running " + testsToRun.TestCaseCount + " tests"); }
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
