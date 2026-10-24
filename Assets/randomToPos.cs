using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
public class randomToPos : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int numCubes = 7;
    int x = 0;
    int y = 0;
    bool connected = false;
    int phase = 0;
    int speed = 150;
    float lastTime = 0;
    bool delayMove = false;

    // people

    // question

 int ipx1 = 160; int ipx2 = 205; int ipx3 = 250; int ipx4 = 295; int ipx5 = 335;
 int yTop = 165; int yBottom = 340;
 bool mc1 = false; bool mc2 = false; bool mc3 = false; bool mc4 = false; bool mc5 = false; bool mc6 = false; bool mc7 = false; bool mc8 = false; bool mc9, mc10 = false;
 bool r1 = false; bool r2 = false; bool r3 = false; bool r4 = false; bool r5 = false; bool r6, r7, r8, r9, r10 = false;
 
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
    void MoveC6(int x, int y){
        if (cubes[5].x != x && cubes[5].y != y){var mv0 = cubeManager.navigators[5].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv0.reached){r6 = true; mc6 = false;}}
    }
    void MoveC7(int x, int y){
        if (cubes[6].x != x && cubes[6].y != y){var mv1 = cubeManager.navigators[6].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv1.reached){r7 = true; mc7 = false;}}
    }
    void MoveC8(int x, int y){
        if (cubes[7].x != x && cubes[7].y != y){var mv2 = cubeManager.navigators[7].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv2.reached){r8 = true; mc8 = false;}}
    }
    void MoveC9(int x, int y){
        if (cubes[8].x != x && cubes[8].y != y){var mv3 = cubeManager.navigators[8].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv3.reached){r9 = true; mc9 = false;}}
    }
    void MoveC10(int x, int y){
        if (cubes[9].x != x && cubes[9].y != y){var mv4 = cubeManager.navigators[9].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv4.reached){r10 = true; mc10 = false;}}
    } 
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(numCubes);
        if (cubes.Length > numCubes)
        {
            
            connected = true;
        }
        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
        cubeManager.navigators[2].ClearOther();
        cubeManager.navigators[3].ClearOther();
        cubeManager.navigators[4].ClearOther();
        cubeManager.navigators[5].ClearOther();
        cubeManager.navigators[6].ClearOther();
        // cubeManager.navigators[7].ClearOther();
        // cubeManager.navigators[8].ClearOther();
        // cubeManager.navigators[9].ClearOther();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced)
        {
            if (phase == 0){
                MoveC1(ipx1, yTop); MoveC2(ipx2, yTop); MoveC3(ipx3, yTop); MoveC4(ipx4, yTop); MoveC5(ipx5, yTop); MoveC6(ipx1, yBottom);  MoveC7(ipx2, yBottom); //MoveC8(ipx3, yBottom); MoveC9(ipx4, yBottom); MoveC10(ipx5, yBottom);
            }
            if (r1 && r2 && r3 && r4 && r5 && r6 && r7){ lastTime = Time.time; phase = 1; }
            if (Time.time-lastTime > 14.9f){delayMove = true;}
            if (phase == 1 && delayMove == true) {
                
                //people
                //MoveC1(140, 250); MoveC2(190, 220); MoveC3(210,185); MoveC4(290, 210); MoveC5(340, 210); MoveC6(190, 280); MoveC7(210, 320); MoveC8(240, 250); MoveC9(290, 290); MoveC10(340, 290);

                //Help
                MoveC1(160, 230); MoveC2(190, 230); MoveC3(230,270); MoveC4(230,230); MoveC5(300, 250); MoveC6(160, 270); MoveC7(190, 270);
            }
        }
        
    }
}
