using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System.Threading.Tasks;

public class twoCubesMove : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    // Cube cube;
    // Start is called before the first frame update
    async void Start()
    {
        // cubeManager = new CubeManager(ConnectType.Real);
        // cube = await cubeManager.SingleConnect();
        // var rawX = cube.x;
        // var rawY = cube.y;
        // string message = $"X: {rawX}, Y: {rawY}";
        // Debug.Log(message);

        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(2);

        // By default, each navigator is able to see all others
        // But you can also manually make a navigator "blind"
        cubeManager.navigators[0].ClearOther();

        cubeManager.navigators[1].usePred = true;
        
    }

    // int navigator_phase = 0;
    int navigator0_phase = 0; int navigator1_phase = 0;

    //public virtual Movement Navi2Target(double x, double y, int maxSpd=70, int rotateTime=250, double tolerance=20);
    // Update is called once per frame
    void Update()
    {
        // if (cube != null)
        // {
        //     //await Task.Delay(1000);
        //     int rawX = cube.x;
        //     int rawY = cube.y;
        //     string message = $"after update X: {rawX}, Y: {rawY}";
        //     Debug.Log(message);
        //     // Movement mv = cubeNavigator.Navi2Target(rawX + 50, rawY + 50);
        //     // // Execution
        //     // mv.Exec();
        //     if (cubeManager.synced){
        //         if (navigator_phase == 0){
        //             var mv = cubeManager.navigators[0].Navi2Target(0, 0, maxSpd:50).Exec();
        //             if (mv.reached) {
        //                 navigator_phase = 1;
        //                 Debug.Log("reached");
        //             }
        //             //await Task.Delay(10000);
        //         }
        //     }
        //     // else if (navigator_phase == 1){
        //     //     var mv = cubeManager.navigators[0].handle.Navi2Target(rawX + 50, rawY + 50, maxSpd:50, rotateTime:250, tolerance:300).Exec();
        //     //     if (mv.reached) navigator_phase = 0;
        //     //     await Task.Delay(10000);
        //     // }
            
        // }
        if (cubeManager.synced)
        {
            // navigator 0
            {
                if (navigator0_phase == 0){
                    var mv = cubeManager.navigators[0].Navi2Target(200, 200, maxSpd:50).Exec();
                    
                    if (mv.reached){
                        var rawX = cubes[0].x;
                        var rawY = cubes[0].y;
                        string message = $"Navigator 0 X: {rawX}, Y: {rawY}";
                        Debug.Log(message);
                        navigator0_phase = 1;
                    } 
                }
                else if (navigator0_phase == 1){
                    var mv = cubeManager.navigators[0].Navi2Target(350, 350, maxSpd:50).Exec();
                    if (mv.reached){
                        var rawX = cubes[0].x;
                        var rawY = cubes[0].y;
                        string message = $"Navigator 0 X: {rawX}, Y: {rawY}";
                        Debug.Log(message);
                        navigator0_phase = 0;
                    } 
                }
            }

            // navigator 1
            {
                if (navigator1_phase == 0){
                    var mv = cubeManager.navigators[1].Navi2Target(180, 350).Exec();
                    if (mv.reached){
                        var rawX = cubes[1].x;
                        var rawY = cubes[1].y;
                        string message = $"Navigator 1 X: {rawX}, Y: {rawY}";
                        Debug.Log(message);
                        navigator1_phase = 1;
                    } 
                }
                else if (navigator1_phase == 1){
                    var mv = cubeManager.navigators[1].Navi2Target(330, 180).Exec();
                    if (mv.reached){
                        var rawX = cubes[1].x;
                        var rawY = cubes[1].y;
                        string message = $"Navigator 1 X: {rawX}, Y: {rawY}";
                        Debug.Log(message);
                        navigator1_phase = 0;
                    }
                }
            }
        }


    }
}
