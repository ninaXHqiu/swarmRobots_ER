using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

// In the Unity editor, pressing Play always starts the Simulator, whichever scene is open.
// Also keeps both app scenes (Control and Simulator) in the build's scene list so the
// in-app buttons can switch between them.
[InitializeOnLoad]
static class PlayStartsSimulator
{
    static PlayStartsSimulator()
    {
        EditorApplication.delayCall += Apply;
    }

    public static void Run() { Apply(); EditorApplication.Exit(0); }

    public static void Apply()
    {
        var simulator = AssetDatabase.LoadAssetAtPath<SceneAsset>(CreateSimScene.ScenePath);
        if (simulator == null) { return; }
        EditorSceneManager.playModeStartScene = simulator;

        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == CreateSimScene.ScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(CreateSimScene.ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
