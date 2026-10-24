using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System.Threading.Tasks;

public class returnToLocation : MonoBehaviour
{
    // Start is called before the first frame update
    CubeManager cubeManager;
    Cube cube;
    bool start = false;
    int x = 0;
    int y = 0;
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cube = await cubeManager.SingleConnect();
        await Task.Delay(100);
        x = cube.x;
        y = cube.y;
        string message = $"X: {x}, Y: {y}";
        start = true;
        Debug.Log(message);
    }

    // Update is called once per frame
    int phase = 0;
    void Update()
    {
        if (cubeManager.synced && start)
        {
            if (cube.x != x || cube.y != y){
                string message = $"Current cube location X: {cube.x}, Y: {cube.y}";
                Debug.Log(message);
                phase = 1;
                
            }
            if (phase == 1) {
                var mv = cubeManager.navigators[0].Navi2Target(x, y, maxSpd:150).Exec();
                if (mv.reached){
                    var rawX = cube.x;
                    var rawY = cube.y;
                    string message2 = $"Phase 1 X: {rawX}, Y: {rawY}";
                    Debug.Log(message2);
                    phase = 0;
                }

            }
        }
    }
}
