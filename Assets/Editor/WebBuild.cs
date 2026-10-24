using UnityEditor;
using UnityEditor.Build.Reporting;

// Builds the swarm control app for the web from the command line:
//   Unity -batchmode -quit -projectPath <swarm> -executeMethod WebBuild.Build
// Output goes to SwarmControl/, which GitHub Pages serves.
public static class WebBuild
{
    const string Scene = "Assets/development/SingleCubeInteraction.unity";
    const string Output = "SwarmControl";

    // Web build of the routine simulator, output to SimPreview/
    public static void BuildSimulator()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { CreateSimScene.ScenePath },
            locationPathName = "SimPreview",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    public static void Build()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = Output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
