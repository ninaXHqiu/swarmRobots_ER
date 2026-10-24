using UnityEngine;
using toio;

// The file name and class name must match.
public class BasicScene : MonoBehaviour
{
    float intervalTime = 0.05f;
    float elapsedTime = 0;
    Cube cube;

    // Asynchronous initialization
    // Use the async/await keyword, a standard C# feature, to wait for the end of each search and connection
    // async: asynchronous keyword
    // await: wait keyword
    async void Start()
    {
      	// Search for Bluetooth devices
        var peripheral = await new NearestScanner().Scan();
       	// Connect to devices and generate Cube variables
        cube = await new CubeConnecter(ConnectType.Real).Connect(peripheral);
    }

    void Update()
    {
        // Early return until Cube variable generation is complete
        if (null == cube) { return; }
        // Measure the elapsed time
        elapsedTime += Time.deltaTime;

      	// If more than 50 milliseconds have passed since the last command
        if (intervalTime < elapsedTime)
        {
            elapsedTime = 0.0f;
            // Left motor speed: 50, Right motor speed: -50, Control time: 200 msec
            cube.Move(50, -50, 200);
        }
    }
}