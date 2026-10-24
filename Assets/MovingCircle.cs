using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System;

public class MovingCircle : MonoBehaviour
{
    Cube[] cubes;
    CubeManager cm;
    bool started = false;
    int phase = -1;
    float lastTime = 0;
    Cube c1; Cube c2; Cube c3; Cube c4;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cubes = await cm.MultiConnect(4);
        //var peripherals = await new NearScanner(2).Scan();
        //cubes = await new CubeConnecter(ConnectType.Real).Connect(peripherals);

        foreach (var cube in cubes)
        {
            cube.targetMoveCallback.AddListener("Sample_Motor", TargetMoveCallback);
        }
        started = true;
    }

    void TargetMoveCallback(Cube cube, int configID, Cube.TargetMoveRespondType response)
    {
        Debug.LogFormat("Cube#{0}'s TargetMove ends with response code: {1}", configID, (byte)response);
        phase ++;
    }

    // Update is called once per frame
    void Update()
    {
        if (!started) return;
        if (cubes.Length==0) return;

        if (Time.time-lastTime < 0.05f) return;
        lastTime = Time.time;

        switch (phase)
        {
            case -1:    // Start TargetMove
            {
                Debug.Log("====== Start TargetMove ======");
                if (cubes.Length==1)
                    cubes[0].TargetMove(targetX:250, targetY:250, targetAngle:270, configID:0);              // up positions: x = 250, y = 230
                if (cubes.Length == 2)                                                                                         // down x = 250 , y = 270
                {
                    if (cubes[0].x > cubes[1].x)    // cubes[0] on the right, so goes to right target
                    {
                        cubes[0].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        cubes[1].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                    }
                    else                            // cubes[0] on the left, so goes to left target
                    {
                        cubes[1].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        cubes[0].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                    }
                }
                if (cubes.Length == 3){
                    if (cubes[0].x > cubes[1].x && cubes[0].x > cubes[2].x){
                        cubes[0].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        if (cubes[1].x > cubes[2].x){
                            cubes[1].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:1);
                            cubes[2].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                        }
                        else{
                            cubes[1].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            cubes[2].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:1);
                        }
                    }
                    if (cubes[1].x > cubes[0].x && cubes[1].x > cubes[2].x){
                        cubes[1].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        if (cubes[0].x > cubes[2].x){
                            cubes[0].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:1);
                            cubes[2].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                        }
                        else{
                            cubes[0].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            cubes[2].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:1);
                        }
                    }
                    else{
                        cubes[2].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        if (cubes[0].x > cubes[1].x){
                            cubes[0].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:1);
                            cubes[1].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                        }
                        else{
                            cubes[0].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            cubes[1].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:1);
                        }
                    }
                }
                if (cubes.Length == 4){
                    if (cubes[0].x > cubes[1].x && cubes[0].x > cubes[2].x && cubes[0].x > cubes[3].x){
                        Debug.Log("0 biggest x");
                        cubes[0].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        if (cubes[1].x <= cubes[2].x && cubes[1].x <= cubes[3].x){
                            cubes[1].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[2].y < cubes[3].y){
                                cubes[2].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[3].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[3].y < cubes[2].y){
                                cubes[3].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[2].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                        if (cubes[2].x <= cubes[1].x && cubes[2].x <= cubes[3].x){
                            cubes[2].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[1].y < cubes[3].y){
                                cubes[1].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[3].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[3].y < cubes[1].y){
                                cubes[3].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[1].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                        if (cubes[3].x <= cubes[1].x && cubes[3].x <= cubes[2].x){
                            cubes[3].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[1].y < cubes[2].y){
                                cubes[1].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[2].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[2].y < cubes[1].y){
                                cubes[2].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[1].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                    }
                    if (cubes[1].x > cubes[0].x && cubes[1].x > cubes[2].x && cubes[1].x > cubes[3].x){
                        Debug.Log("1 biggest x");
                        cubes[1].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        if (cubes[0].x <= cubes[2].x && cubes[0].x <= cubes[3].x){
                            cubes[0].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[2].y < cubes[3].y){
                                cubes[2].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[3].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[3].y < cubes[2].y){
                                cubes[3].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[2].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                        if (cubes[2].x <= cubes[0].x && cubes[2].x <= cubes[3].x){
                            cubes[2].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[0].y < cubes[3].y){
                                cubes[0].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[3].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[3].y < cubes[0].y){
                                cubes[3].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[0].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                        if (cubes[3].x <= cubes[0].x && cubes[3].x <= cubes[2].x){
                            cubes[3].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[0].y < cubes[2].y){
                                cubes[0].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[2].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[2].y < cubes[0].y){
                                cubes[2].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[0].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                    }
                    if (cubes[2].x > cubes[0].x && cubes[2].x > cubes[1].x && cubes[2].x > cubes[3].x){
                        Debug.Log("2 biggest x");
                        cubes[2].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        if (cubes[0].x <= cubes[1].x && cubes[0].x <= cubes[3].x){
                            cubes[0].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[1].y < cubes[3].y){
                                cubes[1].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[3].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[3].y < cubes[1].y){
                                cubes[3].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[1].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                        if (cubes[1].x <= cubes[0].x && cubes[1].x <= cubes[3].x){
                            cubes[1].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[0].y < cubes[3].y){
                                cubes[0].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[3].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[3].y < cubes[0].y){
                                cubes[3].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[0].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                        if (cubes[3].x <= cubes[0].x && cubes[3].x <= cubes[1].x){
                            cubes[3].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[1].y < cubes[0].y){
                                cubes[1].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[0].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[0].y < cubes[1].y){
                                cubes[0].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[1].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                    }
                    if (cubes[3].x > cubes[0].x && cubes[3].x > cubes[1].x && cubes[3].x > cubes[2].x) {
                        Debug.Log("3 biggest x");
                        cubes[3].TargetMove(targetX:290, targetY:250, targetAngle:270, configID:0);
                        if (cubes[0].x <= cubes[1].x && cubes[0].x <= cubes[2].x){
                            Debug.Log("0 smallest x");
                            cubes[0].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[1].y < cubes[2].y){
                                Debug.Log("1 smallest y");
                                cubes[1].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[2].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[2].y < cubes[1].y){
                                Debug.Log("2 smallest y");
                                cubes[2].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[1].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                        if (cubes[1].x <= cubes[0].x && cubes[1].x <= cubes[2].x){
                            Debug.Log("1 smallest x");
                            cubes[1].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            if (cubes[0].y < cubes[2].y){
                                Debug.Log("0 smallest y");
                                cubes[0].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[2].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[2].y < cubes[0].y){
                                Debug.Log("2 smallest y");
                                cubes[2].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[0].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                        if (cubes[2].x <= cubes[0].x && cubes[2].x <= cubes[1].x){
                            cubes[2].TargetMove(targetX:210, targetY:250, targetAngle:90, configID:1);
                            Debug.Log("2 smallest x");
                            if (cubes[0].y < cubes[1].y){
                                Debug.Log("0 smallest y");
                                cubes[0].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[1].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                            if (cubes[1].y < cubes[0].y){
                                Debug.Log("1 smallest y");
                                cubes[1].TargetMove(targetX:250, targetY:210, targetAngle:180, configID:2);
                                cubes[0].TargetMove(targetX:250, targetY:290, targetAngle:0, configID:3);
                            }
                        }
                    }
                }
                phase = 0;
                break;
            }
            case 1: case 2: case 3: case 4: // TargetMove ends & Start AccelerationMove
            {
                if (phase==cubes.Length)    // if all cubes' TargetMove end.
                {
                    foreach (var cube in cubes){
                        // Immediately set speed to 30 by setting acceleration to 0
                        cube.AccelerationMove(targetSpeed:15, acceleration:0, rotationSpeed:-50, controlTime:0);}
                    phase = 5;
                }
                break;
            }
            case 5:     // Start AccelerationMove
            {
                Debug.Log("====== Start AccelerationMove ======");
                foreach (var cube in cubes)
                    // Accelerate from speed 30 to 115
                    cube.AccelerationMove(targetSpeed:30, acceleration:2, rotationSpeed:-40, controlTime:0);
                phase ++;
                break;
            }
            case 6:     // End AccelerationMove
            {
                //bool allOut = false;
                foreach (var cube in cubes)
                {
                    //if ( (cube.x-250)*(cube.x-250)+(cube.y-250)*(cube.y-250) > 140*140 ) allOut = true;
                    if ( (cube.x-250)*(cube.x-250)+(cube.y-250)*(cube.y-250) > 140*140 ) phase = -1;
                }
                //if (allOut) phase = -1;
                break;
            }
            case 7:
            {
                Debug.Log("====== Start decelerationMove ======");
                foreach (var cube in cubes)
                    // Accelerate from speed 30 to 115
                    cube.AccelerationMove(targetSpeed:30, acceleration:2, rotationSpeed:30, controlTime:0);
                phase = 5;
                break;
            }
        }
    }
}
