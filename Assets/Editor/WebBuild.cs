using UnityEditor;
using UnityEditor.Build.Reporting;

// Builds the web app from the command line (tools/run-simulator.sh runs this):
//   Unity -batchmode -quit -projectPath <swarm> -executeMethod WebBuild.BuildSimulator
// Opens on the Simulator, with the Control screen reachable from it. Output goes to SimPreview/.
public static class WebBuild
{
    const string Scene = "Assets/development/SingleCubeInteraction.unity";

    public static void BuildSimulator()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { CreateSimScene.ScenePath, Scene }, // Simulator first, Control reachable from it
            locationPathName = "SimPreview",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
