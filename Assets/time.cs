using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

// Time Join Best Money Medical Help Wrong Question People(also in randomToPos)

public class time : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    bool r = true;
    bool b = false;
    bool g = false;
    int ledColour = 1;
    bool haptic = true;
    bool sound = false;
    int speed = 80;
    int phase = 0;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(8);
        cubes[0].buttonCallback.AddListener("EventScene", OnPressButton);
        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
        cubeManager.navigators[2].ClearOther();
        cubeManager.navigators[3].ClearOther();
        cubeManager.navigators[4].ClearOther();
        cubeManager.navigators[5].ClearOther();
        cubeManager.navigators[6].ClearOther();
        cubeManager.navigators[7].ClearOther();
        // cubeManager.navigators[8].ClearOther();
        // cubeManager.navigators[9].ClearOther();
        // cubeManager.navigators[10].ClearOther();
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
                phase = 0;
                break;
            }
            
        }
        
    }

    bool mc1 = false; bool mc2 = false; bool mc3 = false; bool mc4 = false; bool mc5 = false; bool mc6, mc7, mc8, mc9, mc10, mc11 = false; 
    bool r1 = false; bool r2 = false; bool r3 = false; bool r4 = false; bool r5 = false; bool r6, r7, r8, r9, r10, r11 = false;
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
    void MoveC10(int x, int y){
        if (cubes[9].x != x && cubes[9].y != y){var mv2 = cubeManager.navigators[9].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv2.reached){r10 = true; mc10 = false;}}
    }
    void MoveC11(int x, int y){
        if (cubes[10].x != x && cubes[10].y != y){var mv2 = cubeManager.navigators[10].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv2.reached){r11 = true; mc11 = false;}}
    }
    int ipx1 = 140; int ipx2 = 200; int ipx3 = 250; int ipx4 = 300; int ipx5 = 345;
    int yTop = 165; int yBottom = 340;

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced){
            if (phase == 1 ){
                // 11 robots
                // MoveC2(ipx1, yTop); 
                // MoveC3(ipx2, yTop); 
                // MoveC4(ipx3, yTop); 
                // MoveC5(ipx4, yTop); 
                // MoveC6(ipx5, yTop);  
                // MoveC7(ipx1, yBottom); 
                // MoveC8(ipx2, yBottom); 
                // MoveC9(ipx3, yBottom); 
                // MoveC10(ipx4, yBottom);
                // MoveC11(ipx5, yBottom);

                // 10 robots
                // MoveC2(ipx1, yTop); 
                // MoveC3(ipx2, yTop); 
                // MoveC4(ipx3, yTop); 
                // MoveC5(ipx4, yTop); 
                // MoveC6(ipx5, yTop);  
                // MoveC7(ipx1, yBottom); 
                // MoveC8(ipx2, yBottom); 
                // MoveC9(ipx3, yBottom); 
                // MoveC10(ipx4, yBottom);

                // 9 robots
                // MoveC2(ipx1, yTop); 
                // MoveC3(ipx2, yTop); 
                // MoveC4(ipx3, yTop); 
                // MoveC5(ipx4, yTop); 
                // MoveC6(ipx1, yBottom);  
                // MoveC7(ipx2, yBottom); 
                // MoveC8(ipx3, yBottom); 
                // MoveC9(ipx4, yBottom); 

                // 8 robots
                MoveC2(ipx1, yTop); 
                MoveC3(ipx2, yTop); 
                MoveC4(ipx3, yTop); 
                MoveC5(ipx4, yTop); 
                MoveC6(ipx1, yBottom);  
                MoveC7(ipx2, yBottom); 
                MoveC8(ipx3, yBottom); 

                // 6 robots
                // MoveC2(ipx1, yTop); 
                // MoveC3(ipx3, yTop); 
                // MoveC4(ipx5, yTop); 
                // MoveC5(ipx1, yBottom); 
                // MoveC6(ipx5, yBottom);
 

            }
            if (phase == 2){
                // People --> 11 robots
                // MoveC2(140, 250); 
                // MoveC3(190, 220); 
                // MoveC4(210, 185); 
                // MoveC5(290, 210); 
                // MoveC6(340, 210); 
                // MoveC7(190, 280); 
                // MoveC8(210, 320); 
                // MoveC9(240, 250); 
                // MoveC10(290, 290); 
                // MoveC11(340, 290);

                // Wrong --> 10 robots
                // MoveC2(190, 190); 
                // MoveC3(220, 220); 
                // MoveC4(245, 250); 
                // MoveC5(280, 220); 
                // MoveC6(310, 190); 
                // MoveC7(190, 310); 
                // MoveC8(220, 280); 
                // MoveC9(280, 280); 
                // MoveC10(310, 310);

                // best --> 9 robots total
                // MoveC2(190, 250); 
                // MoveC3(225, 250); 
                // MoveC4(255, 250); 
                // MoveC5(295, 210); 
                // MoveC6(140, 245); 
                // MoveC7(165, 290); 
                // MoveC8(295, 290);
                // MoveC9(295, 245);

                // Money --> 9 robots total
                // MoveC2(185, 210); 
                // MoveC3(210, 245); 
                // MoveC4(245, 275); 
                // MoveC5(285, 210); 
                // MoveC6(140, 250); 
                // MoveC7(170, 305); 
                // MoveC8(285, 305);
                // MoveC9(330, 260);

                // Question --> 8 robots total
                // MoveC2(185, 200); 
                // MoveC3(225, 220); 
                // MoveC4(250, 250); 
                // MoveC5(285, 250); 
                // MoveC6(140, 250); 
                // MoveC7(170, 305); 
                // MoveC8(350, 250);

                // Help --> 8 robots total 
                MoveC2(160, 230); 
                MoveC3(190, 230);
                MoveC4(230, 230);
                MoveC5(300, 250); 
                MoveC6(160, 270); 
                MoveC7(190, 270); 
                MoveC8(230, 270); 
                

                // Medical --> 6 robots total
                // MoveC2(215, 250); 
                // MoveC3(250, 215);
                // MoveC4(250, 250); 
                // MoveC5(250, 285); 
                // MoveC6(285, 250);
                
                // Time --> 5 robots total
                // MoveC2(250, 290);
                // MoveC3(250, 260);
                // MoveC4(250, 220);
                // MoveC5(250, 180);

                // Join
                // MoveC2(320, 220); 
                
            }

        }
        
    }
}
