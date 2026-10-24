using UnityEngine;
using UnityEngine.SceneManagement;

// Switches between the app's two parts: Control (real robots) and Simulator (routines on
// simulated robots). Both scenes must be in the build's scene list.
public class SceneSwitcher : MonoBehaviour
{
    public const string ControlScene = "SingleCubeInteraction";
    public const string SimulatorScene = "RoutineSimulator";

    // Wired to the Simulator button in the Control screen
    public void OpenSimulator() { SceneManager.LoadScene(SimulatorScene); }

    public static void OpenControl() { SceneManager.LoadScene(ControlScene); }
}
