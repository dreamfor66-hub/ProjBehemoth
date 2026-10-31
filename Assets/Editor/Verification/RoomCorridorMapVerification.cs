using System.IO;
using DungeonSlash;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class RoomCorridorMapVerification
{
    const string Request = "Temp/RoomCorridorMap.request";
    static bool pending;
    static bool visitedMemory;
    static TestRunnerApi runner;
    static RoomCorridorMapVerification() => EditorApplication.update += Poll;
    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists(Request))
        {
            var command = File.ReadAllText(Request).Trim(); File.Delete(Request);
            if (command == "stop") { pending = false; EditorApplication.isPlaying = false; return; }
            if (command == "tests" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                runner = ScriptableObject.CreateInstance<TestRunnerApi>(); runner.RegisterCallbacks(new Results());
                runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] { "DungeonSlash.Tests.DungeonMapLayoutTests" } }));
                return;
            }
            if (command == "run" || command == "visited-memory")
            {
                if (!File.Exists(RoomCorridorMapPreparation.Folder + "Installed.txt")) return;
                visitedMemory = command == "visited-memory";
                pending = true; EditorApplication.isPlaying = true; EditorApplication.isPaused = false;
            }
            else if (command == "status") Object.FindFirstObjectByType<RoomCorridorMapLiveProbe>()?.Status();
            else if (command.StartsWith("capture:")) Object.FindFirstObjectByType<RoomCorridorMapLiveProbe>()?.CaptureNow(command.Substring(8));
        }
        if (pending && EditorApplication.isPlaying && Object.FindFirstObjectByType<CombatController>() != null)
        {
            pending = false;
            var probe = new GameObject("Scoped room/corridor map verification").AddComponent<RoomCorridorMapLiveProbe>();
            if (visitedMemory) probe.RunVisitedMemory(); else probe.Run();
        }
    }
    sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, RoomCorridorMapPreparation.Folder + "EditMode.xml");
            File.WriteAllText(RoomCorridorMapPreparation.Folder + "EditMode.txt", $"{result.TestStatus}: {result.PassCount} passed, {result.FailCount} failed\n");
        }
    }
}
