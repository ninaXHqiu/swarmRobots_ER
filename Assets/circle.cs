using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System;

public class circle : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    // initial x and y coordinates of the 5 cubes
    int x0 = 334;
    int y0 = 229;
    int x1 = 316;
    int y1 = 312;
    int x2 = 176;
    int y2 = 228;
    int x3 = 257;
    int y3 = 173;
    int x4 = 199;
    int y4 = 311;
    // Start is called before the first frame update
    int rad = 70;
    int cX = 250;
    int cY = 250;
    int points = 5;
    
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(5);

        double circumference = 2 * Math.PI * rad;
        double angleIncrement = 2*Math.PI / points;

        double angle1 = 0*angleIncrement;
        double angle2 = 1*angleIncrement;
        double angle3 = 2*angleIncrement;
        double angle4 = 3*angleIncrement;
        double angle5 = 4*angleIncrement;
        x0 = (int)(cX + rad * Math.Cos(angle1));
        y0 = (int)(cY + rad * Math.Sin(angle1));
        x1 = (int)(cX + rad * Math.Cos(angle2));
        y1 = (int)(cY + rad * Math.Sin(angle2));
        x2 = (int)(cX + rad * Math.Cos(angle3));
        y2 = (int)(cY + rad * Math.Sin(angle3));
        x3 = (int)(cX + rad * Math.Cos(angle4));
        y3 = (int)(cY + rad * Math.Sin(angle4));
        x4 = (int)(cX + rad * Math.Cos(angle5));
        y4 = (int)(cY + rad * Math.Sin(angle5));


        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
        cubeManager.navigators[2].ClearOther();
        cubeManager.navigators[3].ClearOther();
        cubeManager.navigators[4].ClearOther();
    }


    int moveCube0 = 0; int moveCube1 = 0; int moveCube2 = 0; int moveCube3 = 0; int moveCube4 = 0; int moveCube = 0; int nav = 0;
    // Update is called once per frame
    
    void Update()
    {
        
        if (cubeManager.synced)
        {
            if (cubes[0].x != x0 || cubes[0].y != y0 ){
                moveCube0 = 1;
            }
            if (cubes[1].x != x1 || cubes[1].y != y1){
                moveCube1 = 1;
            }
            if (cubes[2].x != x2 || cubes[2].y != y2){
                moveCube2 = 1;
            }
            if (cubes[3].x != x3 || cubes[3].y != y3){
                moveCube3 = 1;
            }
            if (cubes[4].x != x4 || cubes[4].y != y4){
                moveCube4 = 1;
            }
            if (moveCube0 == 1){
                var mv = cubeManager.navigators[0].Navi2Target(x0, y0, maxSpd:150).Exec();
                if (mv.reached){
                    moveCube1 = 0;
                } 
            }

            if (moveCube1 == 1){
                var mv = cubeManager.navigators[1].Navi2Target(x1, y1, maxSpd:150).Exec();
                if (mv.reached){
                    moveCube1 = 0;
                } 
            }

            if (moveCube2 == 1){
                var mv = cubeManager.navigators[2].Navi2Target(x2, y2, maxSpd:150).Exec();
                if (mv.reached){
                    moveCube2 = 0;
                } 
            }

            if (moveCube3 == 1){
                var mv = cubeManager.navigators[3].Navi2Target(x3, y3, maxSpd:150).Exec();
                if (mv.reached){
                    moveCube3 = 0;
                } 
            }

            if (moveCube4 == 1){
                var mv = cubeManager.navigators[4].Navi2Target(x4, y4, maxSpd:150).Exec();
                if (mv.reached){
                    moveCube4 = 0;
                } 
            }
        }
    }
}
