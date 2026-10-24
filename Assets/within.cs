using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class within : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int phase = 0;
    int speed = 150;
    float lastTime = 0;
    bool delayMove = false;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(1);
    }

    int x1 = 202; int y1 = 213;
    int x2 = 324; int y2 = 214;
    int x3 = 227; int y3 = 309;

    int xOut1 = 180; int yOut1 = 185;
    int xOut2 = 180; int yOut2 = 320;
    int x = 265; int y = 250;

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced){
            /// Within 1

            if (phase == 0 && cubes[0].x != x1 && cubes[0].y != y1){
                var mv = cubeManager.navigators[0].Navi2Target(x1, y1, maxSpd:150).Exec();
                if (mv.reached){phase = 1;}
            }
            if (phase == 1 && cubes[0].x != x2 && cubes[0].y != y2){
                var mv = cubeManager.navigators[0].Navi2Target(x2, y2, maxSpd:150).Exec();
                if (mv.reached){phase = 2;}
            }
            if (phase == 2 && cubes[0].x != x3 && cubes[0].y != y3){
                var mv = cubeManager.navigators[0].Navi2Target(x3, y3, maxSpd:150).Exec();
                if (mv.reached){phase = 0;}
            }

            // Within 2
            // if (Time.time - lastTime>10.9f){
            //     if (phase == 0){
            //         var mv1 = cubeManager.navigators[0].Navi2Target(xOut1, yOut1, maxSpd:150).Exec();
            //         if (mv1.reached){phase ++;}
            //         var mv2 = cubeManager.navigators[1].Navi2Target(xOut2, yOut2, maxSpd:150).Exec();
            //         if (mv1.reached){phase ++;}
            //     }
            //     if (phase == 2){
            //         var mv1 = cubeManager.navigators[2].Navi2Target(x, y, maxSpd:150).Exec();
            //         if (mv1.reached){phase = 3;}
            //     }
            //     if (phase == 3){
            //         var mv1 = cubeManager.navigators[0].Navi2Target(xOut1, yOut1+40, maxSpd:150).Exec();
            //         if (mv1.reached){phase ++;}
            //         var mv2 = cubeManager.navigators[1].Navi2Target(xOut2, yOut2-40, maxSpd:150).Exec();
            //         if (mv1.reached){phase ++;}
            //     }
            // }

        }
    }
}
