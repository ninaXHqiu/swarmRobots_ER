using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.Navigation;

public class boids : MonoBehaviour
{
    CubeManager cubeManager;
    bool started = false;
    Cube[] cubes;
    Cube firstCube;
    Cube sndCube;
    string front = "Front";
    string back = "Back";
    string left = "Left";
    string right = "Right";
    string currPose = "Up";
    int speed = 100;
    int connected = 0;
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(3);
        Debug.Assert(cubeManager.navigators.Count>1, "Need at least 2 cubes.");
        if (cubes.Length > 2)
        {
            // firstCube = cubes[0];
            // sndCube = cubes[1];
            connected = 1;
        }
        cubeManager.navigators[1].mode = CubeNavigator.Mode.BOIDS;
        cubeManager.navigators[2].mode = CubeNavigator.Mode.BOIDS;
        // cubeManager.navigators[3].mode = CubeNavigator.Mode.BOIDS;
        // cubeManager.navigators[4].mode = CubeNavigator.Mode.BOIDS;

        // Choose 1 cube not to be of boids
        //CubeNavigator navigatorNotBoids = cubeManager.navigators[0];

        // if (CubeScanner.actualTypeOfAuto == ConnectType.Simulator)
        //     foreach (var navigator in cubeManager.navigators)
        //         if (navigator.cube.localName == "Cube Not Boids")
        //             navigatorNotBoids = navigator;

        // Use LED color to distinguish cubes
        // foreach (var navigator in cubeManager.navigators)
        // {
        //     navigator.cube.TurnLedOn(0,255,0,0);
        //     // if (navigator == navigatorNotBoids) navigator.cube.TurnLedOn(255,0,0,0); // Red
        //     // else navigator.cube.TurnLedOn(0,255,0,0);  // Green
        // }

        // Set to BOIDS only mode, except navigatorNotBoids
        // foreach (var navigator in cubeManager.navigators)
        //     navigator.mode = CubeNavigator.Mode.BOIDS;
        //     // if (navigator != navigatorNotBoids) navigator.mode = CubeNavigator.Mode.BOIDS;

        // By default, all navigators are in one group of boids
        // here, separate Red cube from the group
        // navigatorNotBoids.SetRelation(cubeManager.navigators, CubeNavigator.Relation.NONE);
        // foreach (var navigator in cubeManager.navigators)
        //     navigator.SetRelation(navigatorNotBoids, CubeNavigator.Relation.NONE);

        started = true;
    }
    int moveCube = 0; int moveCubeX = 0;
    void Update()
    {
        if (!started) return;
        // ------ Sync ------
        // if (move1 == 1){
        //     foreach (var navigator in cubeManager.syncNavigators)
        //     {
        //         var mv = navigator.Navi2Target(250, 250, maxSpd:50).Exec();
        //         if (mv.reached){move1 = 0; move2 = 1;}
        //     }
        // }
        // if (move2 == 1){
        //     foreach (var navigator in cubeManager.syncNavigators)
        //     {
        //         var mv = navigator.Navi2Target(90, 120, maxSpd:50).Exec();
        //         if (mv.reached){move2 = 0; move3 = 1;}
        //     }
        // }
        // if (move3 == 1){
        //     foreach (var navigator in cubeManager.syncNavigators)
        //     {
        //         var mv = navigator.Navi2Target(350, 300, maxSpd:50).Exec();
        //         if (mv.reached){move3 = 0; move1 = 1;}
        //     }
        // }
        if (cubeManager.synced){
            if (connected == 1){
                cubes[0].TurnLedOn(0,255,0,0);
                int x1 = cubes[0].x;
                int y1 = cubes[0].y;
                // 4 possible cases:
                // 1. cube 1 x and y are less than cube 2 x and y
                // 2. cube 1 x smaller y bigger
                // 3. cube 1 x bigger y smaller
                // 4. cube 1 x and y are bigger than cube 2 x and y
                if(cubes[1].y != 0){
                    if (cubes[1].x - cubes[0].x > 30 && cubes[1].y - cubes[0].y > 30){moveCube = 1;}
                    else if (cubes[1].x - cubes[0].x > 30 && cubes[1].y - cubes[0].y <= 30 && cubes[0].x < 250 && cubes[0].y < 250){moveCube = 1;}
                    else if (cubes[1].y - cubes[0].y > 30 && cubes[1].x - cubes[0].x <= 30 && cubes[0].x < 250 && cubes[0].y < 250){moveCube = 1;}
                    else if (cubes[1].x - cubes[0].x > 30 && cubes[0].y - cubes[1].y > 30){moveCube = 2;}
                    else if (cubes[1].x - cubes[0].x > 30 && cubes[0].y - cubes[1].y <= 30 && cubes[0].x < 250 && cubes[0].y > 250){moveCube = 2;}
                    else if (cubes[0].y - cubes[1].y > 30 && cubes[1].x - cubes[0].x <= 30 && cubes[0].x < 250 && cubes[0].y > 250){moveCube = 2;}
                    else if (cubes[0].x - cubes[1].x > 30 && cubes[1].y - cubes[0].y > 30){moveCube = 3;}
                    else if (cubes[0].x - cubes[1].x > 30 && cubes[1].y - cubes[0].y <= 30 && cubes[0].x > 250 && cubes[0].y < 250){moveCube = 3;}
                    else if (cubes[1].y - cubes[0].y > 30 && cubes[0].x - cubes[1].x <= 30 && cubes[0].x > 250 && cubes[0].y < 250){moveCube = 3;}
                    else if (cubes[0].x - cubes[1].x > 30 && cubes[0].y - cubes[1].y > 30){moveCube = 4;}
                    else if (cubes[0].x - cubes[1].x > 30 && cubes[0].y - cubes[1].y <= 30 && cubes[0].x > 250 && cubes[0].y > 250){moveCube = 4;}
                    else if (cubes[0].y - cubes[1].y > 30 && cubes[0].x - cubes[1].x <= 30 && cubes[0].x > 250 && cubes[0].y > 250){moveCube = 4;}
                    if (moveCube == 1){
                        foreach (var navigator in cubeManager.syncNavigators)
                        {
                            if (navigator.cube.id != "Cube (1)"){navigator.Navi2Target(cubes[0].x + 25, cubes[0].y + 25, maxSpd:speed).Exec();}
                        }
                    }
                    if (moveCube == 2){
                        foreach (var navigator in cubeManager.syncNavigators)
                        {
                            if (navigator.cube.id != "Cube (1)"){navigator.Navi2Target(cubes[0].x + 25, cubes[0].y - 25, maxSpd:speed).Exec();}
                        }
                    }
                    if (moveCube == 3){
                        foreach (var navigator in cubeManager.syncNavigators)
                        {
                            if (navigator.cube.id != "Cube (1)"){navigator.Navi2Target(cubes[0].x - 25, cubes[0].y + 25, maxSpd:speed).Exec();}
                        }
                    }
                    if (moveCube == 4){
                        foreach (var navigator in cubeManager.syncNavigators)
                        {
                            if (navigator.cube.id != "Cube (1)"){navigator.Navi2Target(cubes[0].x - 25, cubes[0].y - 25, maxSpd:speed).Exec();}
                        }
                    }
                }
            }
        }
    }
}
