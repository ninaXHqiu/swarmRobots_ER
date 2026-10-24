using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates Assets/development/RoutineSimulator.unity: the toio simulator stage (mat, camera,
// lights) plus SimRoutinePlayer. Batch: -executeMethod CreateSimScene.Run
public static class CreateSimScene
{
    public const string ScenePath = "Assets/development/RoutineSimulator.unity";

    [MenuItem("Tools/Swarm/Create routine simulator scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var stage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/toio-sdk/Scripts/Simulator/Resources/Stage.prefab");
        PrefabUtility.InstantiatePrefab(stage);
        new GameObject("RoutineSimulator").AddComponent<SimRoutinePlayer>();
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    public static void Run()
    {
        Create();
        EditorApplication.Exit(0);
    }
}
