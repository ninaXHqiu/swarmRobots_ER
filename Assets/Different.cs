using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class Different : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int phase = 0;
    int speed = 50;
    float lastTime = 0;
    bool delayMove = false;

    bool mc1 = false; bool mc2 = false; bool mc3 = false; bool mc4 = false; bool mc5 = false;

    void MoveC1(int x, int y){
        if (cubes[0].x != x && cubes[0].y != y){var mv0 = cubeManager.navigators[0].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv0.reached){r1 = true;}}
    }
    void MoveC2(int x, int y){
        if (cubes[1].x != x && cubes[1].y != y){var mv1 = cubeManager.navigators[1].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv1.reached){r2 = true; mc2 = false;}}
    }
    void MoveC3(int x, int y){
        if (cubes[2].x != x && cubes[2].y != y){var mv2 = cubeManager.navigators[2].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv2.reached){r3 = true; mc3 = false;}}
    }
    void MoveC4(int x, int y){
        if (cubes[3].x != x && cubes[3].y != y){var mv3 = cubeManager.navigators[3].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv3.reached){r4 = true; mc4 = false;}}
    }
    void MoveC5(int x, int y){
        if (cubes[4].x != x && cubes[4].y != y){var mv4 = cubeManager.navigators[4].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv4.reached){r5 = true; mc5 = false;}}
    } 

    int ipx1 = 145; int ipx2 = 200; int ipx3 = 250; int ipx4 = 315; int ipx5 = 355;
    int yTop = 165; int yBottom = 340;

    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(5);

        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
        cubeManager.navigators[2].ClearOther();
        cubeManager.navigators[3].ClearOther();
        cubeManager.navigators[4].ClearOther();
        
    }
    bool r1, r2, r3, r4, r5 = false;

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced){
            

            if (phase == 0){
                MoveC1(ipx1, yTop);
                MoveC2(ipx2, yTop);
                MoveC3(ipx3, yBottom);
                MoveC4(ipx4, yTop);
                MoveC5(ipx5, yTop);
            }
            if (r1 && r2 && r3 && r4 && r5 && phase == 0){ lastTime = Time.time; phase = 1;}
            if (Time.time-lastTime > 8.9f){delayMove = true;}
            if (phase == 1 && delayMove == true){
                //lastTime = Time.time;
                MoveC1(ipx1, yBottom);
                MoveC2(ipx2, yBottom);
                MoveC3(ipx3, yTop);
                MoveC4(ipx4, yBottom);
                MoveC5(ipx5, yBottom);
                // cubes[0].Move(-100,-100,speed);
                // cubes[1].Move(-100,-100,speed);
                // cubes[2].Move(100,100,speed);
                // cubes[3].Move(-100,-100,speed);
                // cubes[4].Move(-100,-100,speed);
            }
            if (r1 && r2 && r3 && r4 && r5 && phase == 1 && (Time.time-lastTime > 3.1)){
                phase = 2;
               
            }
            if (phase == 2){
                MoveC1(ipx1, yTop);
                MoveC2(ipx2, yTop);
                MoveC3(ipx3, yBottom);
                MoveC4(ipx4, yTop);
                MoveC5(ipx5, yTop);
            }
        }
        
    }
}
