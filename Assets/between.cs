using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class between : MonoBehaviour
{
    CubeManager cubeManager;
    Cube cube;
    bool r1 = false;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cube = await cubeManager.SingleConnect();
    }
    int phase = 0;
    void MoveC1(int x, int y){
        if (cube.x != x && cube.y != y){var mv0 = cubeManager.navigators[0].Navi2Target(x, y, maxSpd:80).Exec(); if(mv0.reached){r1 = true;}}
    }
    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced)
        {
            if (phase == 0){
                MoveC1(250, 250); 
            }
            if (r1 == true && phase == 0){phase = 1;}
            if (phase == 1){
                r1 = false;
                MoveC1(300, 250); 
            }
            if (r1 == true && phase == 1){phase = 2;}
            if (phase == 2){
                r1 = false;
                MoveC1(200, 250); 
            }
            if (r1 == true && phase == 2){phase = 1;}
        }
    }
}
