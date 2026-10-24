using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class connectAndSpin : MonoBehaviour
{
    //public ConnectType connectType;

    // CubeManager cm;
    // //Cube cube;
    // //Cube[] cubes;
    // async void Start()
    // {
    //     // ConnectType.Auto - ビルド対象に応じて内部実装が自動的に変わる
    //     // ConnectType.Simulator - ビルド対象に関わらずシミュレータのキューブで動作する
    //     // ConnectType.Real - ビルド対象に関わらずリアル(現実)のキューブで動作する
    //     // cm = new CubeManager(connectType);
    //     // await cm.MultiConnect(2);
    //     ///var peripherals = new CubeScanner(ConnectType.Real).NearScan(1, 5.0f);
    //     ///cubes = await new CubeConnecter().Connect(peripherals[0]);
    //     // cm = new CubeManager(ConnectType.Real);
    //     // cube = await cm.SingleConnect();
    //     //cube = cubes[0];
    //     var peripheral = await new NearScanner(2).Scan();
    //     var cubes = await new CubeConnecter(ConnectType.Real).Connect(peripheral);
    // }


    // void Update()
    // {
    //     // foreach(var cube in cm.syncCubes)
    //     // {
    //     //     cube.Move(50, -50, 100);
    //     // }
    //     //cube.Move(50, -50, 100);
    // }
    int move = 1;
    bool shouldWait = true;
    Cube firstCube;
    Cube sndCube;
    Cube[] cubes;
    CubeManager cubeManager;
    async void Start()
    {
        Debug.Log("start");
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(2);
        //StartCoroutine(DelayedUpdateCoroutine());
        if (cubes.Length > 1)
        {
            firstCube = cubes[0];
            sndCube = cubes[1];
            // Now you can work with the first cube
            // ...
        }
        if (move == 1){
            cubes[1].Move(30, 30, 100);
            move = 0;
        }
        cubes[1].Move(30, 30, 100);
        int x1 = cubes[0].x;
        int y1 = cubes[0].y;
        int x2 = cubes[1].x;
        int y2 = cubes[1].y;
        
        Debug.Log($"at start cube1 pos x = {x1} y = {y1}");
        Debug.Log($"at start cube2 pos x = {x2} y = {y2}");
        
    }
    int x1 = 0;
    int y1 = 0;
    int x2 = 0;
    int y2 = 0;

    

    int moveCube1 = 1;
    void Update()
    {
        Debug.Log("update");
        // if (firstCube != null && sndCube != null)
        // {
        //     firstCube.Move(250, 250, 100);
        //     //sndCube.TurnLedOn(255,0,0,0);
        //     sndCube.Move(100, 100, 100);
            
        // }
        // if (cubeManager.synced)
        // {

        //     if (moveCube1 == 1){
        //         var mv = cubeManager.navigators[0].Navi2Target(150, 150, maxSpd:150).Exec();
        //         if (mv.reached){
        //             moveCube1 = 0;
        //             // var rawX = cubes[0].x;
        //             // var rawY = cubes[0].y;
        //             // string message2 = $"Phase 1 X: {rawX}, Y: {rawY}";
        //             // Debug.Log(message2);
        //         } 
        //     }

        // }
        // foreach (var navigator in cubeManager.navigators)
        // {
        //     // The timing (frame) at which it becomes controllable is per Cube. In other words, it is asynchronous.
        //     if (cubeManager.IsControllable(navigator))
        //     {
        //         navigator.Update(); // For the asynchronous version, be sure to call this method.
        //         navigator.handle.MoveRaw(-50, 50, 1000);
        //     }
        // }
        if (cubeManager.synced){
            if (move == 0){
                x1 = cubes[0].x;
                y1 = cubes[0].y;
                x2 = cubes[1].x;
                y2 = cubes[1].y;
                
                
                // define a circumference of radius 1
                
                Debug.Log($"cube1 pos x = {x1} y = {y1}");
                Debug.Log($"cube2 pos x = {x2} y = {y2}");
            }
             
            
        }
    }
    // private IEnumerator DelayedRequestSensor(Cube cube)
    //     {
    //         yield return new WaitForSecondsRealtime(0.05f);
    //         cube.RequestMotionSensor();
    //     }
}
