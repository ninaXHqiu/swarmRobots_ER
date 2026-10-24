using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class home : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int ledColour = 1;
    int speed = 80;
    int phase = 0;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(9);
        cubes[0].buttonCallback.AddListener("EventScene", OnPressButton);
        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
        cubeManager.navigators[2].ClearOther();
        cubeManager.navigators[3].ClearOther();
        cubeManager.navigators[4].ClearOther();
        cubeManager.navigators[5].ClearOther();
        cubeManager.navigators[6].ClearOther();
        cubeManager.navigators[7].ClearOther();
        cubeManager.navigators[8].ClearOther();
        cubes[0].TurnLedOn(255,255,0,0); // -->yellow
        
    }
    void OnPressButton(Cube c)
    {
        if (c.isPressed)
        {
            switch (ledColour){
            case 1:
                cubes[0].TurnLedOn(255,0,0,0); // --> red
                ledColour = 2;
                phase = 1; // --> start position
                break;
            case 2:
                cubes[0].TurnLedOn(0,255,0,0); // --> green
                ledColour = 3;
                phase = 2;
                break;
            case 3:
                cubes[0].TurnLedOn(0,0,255,0); // --> blue
                ledColour = 1;
                phase = 3;
                break;
            }
            
        }
        
    }
    bool mc1 = false; bool mc2 = false; bool mc3 = false; bool mc4 = false; bool mc5 = false; bool mc6, mc7, mc8, mc9 = false; 
    bool r1 = false; bool r2 = false; bool r3 = false; bool r4 = false; bool r5 = false; bool r6, r7, r8, r9 = false;
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
        if (cubes[8].x != x && cubes[8].y != y){var mv2 = cubeManager.navigators[8].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv2.reached){r9 = true; mc9 = false;}}
    }
    int ipx1 = 140; int ipx2 = 200; int ipx3 = 250; int ipx4 = 300; int ipx5 = 345;
    int yTop = 165; int yBottom = 340;

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced){
            if (phase == 1 ){
                MoveC2(ipx1, yTop); 
                MoveC3(ipx2, yTop); 
                MoveC4(ipx3, yTop); 
                MoveC5(ipx4, yTop); 
                MoveC6(ipx1, yBottom);  
                MoveC7(ipx2, yBottom); 
                MoveC8(ipx3, yBottom); 
                MoveC9(ipx4, yBottom);
                 

            }
            if (phase == 2){
                MoveC2(150, 256);
                MoveC3(187, 235); 
                MoveC4(317, 242); 
                MoveC5(338, 266);
                MoveC6(218, 295);
                MoveC7(223, 257);
                MoveC8(276, 257);
                MoveC9(278, 295);




                // MoveC2(187, 235); 
                // MoveC3(223, 257); 
                // MoveC4(276, 257); 
                // MoveC5(317, 242); 
                // MoveC6(150, 256); 
                // MoveC7(218, 295); 
                // MoveC8(278, 295);
                // MoveC9(338, 266);
                
            }
            if (phase == 3){
                MoveC2(197, 223);
                MoveC3(223, 193); 
                MoveC4(271, 193); 
                MoveC5(301, 225);
                MoveC6(218, 295);
                MoveC7(223, 257);
                MoveC8(276, 257);
                MoveC9(278, 295);


                // MoveC2(223, 193); 
                // MoveC3(223, 257); 
                // MoveC4(276, 257); 
                // MoveC5(271, 193); 
                // MoveC6(197, 223); 
                // MoveC7(218, 295); 
                // MoveC8(278, 295);
                // MoveC9(301, 225);
                
            }

        }
    }
}
