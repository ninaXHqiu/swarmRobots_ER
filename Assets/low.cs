using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

// Home Low Go

public class low : MonoBehaviour
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
        cubes = await cubeManager.MultiConnect(5);
        cubes[0].buttonCallback.AddListener("EventScene", OnPressButton);
        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
        cubeManager.navigators[2].ClearOther();
        cubes[0].TurnLedOn(255,255,0,0);
        
    }

    void OnPressButton(Cube c)
    {
        if (c.isPressed)
        {
            switch (ledColour){
            case 1:
                cubes[0].TurnLedOn(255,0,0,0);
                ledColour = 2;
                phase = 1;
                break;
            case 2:
                cubes[0].TurnLedOn(0,255,0,0);
                ledColour = 3;
                phase = 2;
                break;
            case 3:
                cubes[0].TurnLedOn(0,0,255,0);
                ledColour = 1;
                phase = 0;
                break;
            }
            
        }
        

        // Home
        cubes[1].Move(60, 60, 350);
        cubes[2].Move(60, 60, 350);
        cubes[3].Move(60, 60, 350);
        cubes[4].Move(60, 60, 350);
        
    }

    bool mc1 = false; bool mc2 = false; bool mc3 = false;
    bool r1 = false; bool r2 = false; bool r3 = false;

    void MoveC2(int x, int y){
        if (cubes[1].x != x && cubes[1].y != y){var mv1 = cubeManager.navigators[1].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv1.reached){r2 = true; mc2 = false;}}
    }
    void MoveC3(int x, int y){
        if (cubes[2].x != x && cubes[2].y != y){var mv2 = cubeManager.navigators[2].Navi2Target(x, y, maxSpd:speed).Exec(); if(mv2.reached){r3 = true; mc3 = false;}}
    }

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced){
            // Low
            // if (phase == 1 ){
            //     MoveC2(295, 320);
            // }
            // if (phase == 2){
            //     MoveC2(300, 320);
            //     MoveC3(260, 320);
            // }
            
            // Go
            // if (phase == 1 ){
            //     //MoveC2(330, 180);
            // }
            // if (phase == 2){
            //     MoveC2(330, 180);
            //     MoveC3(300, 170);
            // }

        }
    }
}
