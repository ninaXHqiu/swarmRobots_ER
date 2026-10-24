using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System;

public class MultiFlickAnyCube : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int ph = 0;
    int speed = 30;
    bool connected = false;
    int mvCube = 0;
    bool started = false;
    int phase = 0;
    int done1 = 0; int done2 = 0; int done3 = 0; int done4 = 0; int done5 = 0;
    bool called = false;
    float lastTime = 0;
    int range = 5;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(5);
        Debug.Assert(cubeManager.navigators.Count>1, "Need at least 2 cubes.");
        foreach (var cube in cubes)
        {
            cube.targetMoveCallback.AddListener("Sample_Motor", TargetMoveCallback);
        }
        started = true;
        //if (cubes.Length == 5){connected = true;}
    }
    void TargetMoveCallback(Cube cube, int configID, Cube.TargetMoveRespondType response)
    {
        //Debug.LogFormat("Cube#{0}'s TargetMove ends with response code: {1}", configID, (byte)response);
        if (response == 0 && configID == 0) done1 ++;
        if (response == 0 && configID == 1) done2 ++;
        if (response == 0 && configID == 2) done3 ++;
        if (response == 0 && configID == 3) done4 ++;
        if (response == 0 && configID == 4) done5 ++;
        //if ((byte)response == 2) done ++;
    }
    int checkpos = 260;
    // Star positions: 
    //int px1 = 170; int py1 = 355; int px2 = 252; int py2 = 117; int px3 = 320; int py3 = 355; int px4 = 120; int py4 = 210; int px5 = 370; int py5 = 210;

    int px1 = 330; int py1 = 275; int px2 = 236; int py2 = 284; int px3 = 154; int py3 = 289; int px4 = 151; int py4 = 209; int px5 = 239; int py5 = 212;
    // Update is called once per frame
    void Update()
    {
        if (!started) return;
        if (cubes.Length==0) return;

        if (Time.time-lastTime < 1.5f) return;
        lastTime = Time.time;
        if (done1 >= 1 && done2 >= 1 && done3 >= 1 && done4 >= 1 && done5 >= 1){
            // if (phase == 0){
            //     phase = 1;
            //     done = 0;
            // }
            if (phase == 5 || phase == 0){
                if (phase == 0){
                    phase = 2;
                }
                else{
                    phase = 1;
                }
                
                done1 = 0; done2 = 0; done3 = 0; done4 = 0; done5 = 0;
            }
            else{
                phase ++;
                done1 = 0; done2 = 0; done3 = 0; done4 = 0; done5 = 0;
            }
        }
        // if (phase == 0){
        //     if (cubes[0].x != px1 || cubes[1].x != px2 || cubes[2].x != px3 || cubes[3].x != px4 || cubes[4].x != px5){
        //         mvCube = 1;
        //     }
        //     if (mvCube == 1){
        //         var mv0 = cubeManager.navigators[1].Navi2Target(px1, py1, maxSpd:speed).Exec();
        //         var mv1 = cubeManager.navigators[2].Navi2Target(px2, py2, maxSpd:speed).Exec();
        //         var mv2 = cubeManager.navigators[3].Navi2Target(px3, py3, maxSpd:speed).Exec();
        //         var mv3 = cubeManager.navigators[4].Navi2Target(px4, py4, maxSpd:speed).Exec();
        //         var mv4 = cubeManager.navigators[0].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
        //         if (mv0.reached || mv1.reached || mv2.reached || mv3.reached){phase = 1;mvCube = 0;}
        //     }
        // }
        
        switch (phase)
        {
            case 0:
            {
                Debug.Log("phase0");
                Debug.Log($"done1 {done1} done2 {done2} done3 {done3} done4 {done4} done5 {done5} ");
                if (done1 == 0 || done2 == 0 || done3 == 0 || done4 == 0 || done5 == 0){
                    //if ((cubes[0].x != px1 || cubes[1].x != px2 || cubes[2].x != px3 || cubes[3].x != px4 || cubes[4].x != px5)){
                    if ((cubes[0].x <= px1 - range || cubes[0].x >= px1 + range || 
                        cubes[1].x <= px2 - range || cubes[1].x >= px2 + range || 
                        cubes[2].x <= px3 - range || cubes[2].x >= px3 + range ||
                        cubes[3].x <= px4 - range || cubes[3].x >= px4 + range ||
                        cubes[4].x <= px5 - range || cubes[4].x <= px5 + range)){
                            cubes[0].TargetMove(targetX:px1, targetY:py1, targetAngle:270, configID:0, maxSpd:speed);
                            cubes[1].TargetMove(targetX:px2, targetY:py2, targetAngle:0, configID:1, maxSpd:speed);
                            cubes[2].TargetMove(targetX:px3, targetY:py3, targetAngle:0, configID:2, maxSpd:speed);
                            cubes[3].TargetMove(targetX:px4, targetY:py4, targetAngle:90, configID:3, maxSpd:speed);
                            cubes[4].TargetMove(targetX:px5, targetY:py5, targetAngle:180, configID:4, maxSpd:speed);
                    }
                }
                break;
            }
            case 1:  // Start TargetMove
            {
                Debug.Log("phase1");
                Debug.Log($"done1 {done1} done2 {done2} done3 {done3} done4 {done4} done5 {done5} ");
                Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                if (done1 == 0 || done2 == 0 || done3 == 0 || done4 == 0 || done5 == 0){
                    //if ((cubes[0].x != px1 || cubes[1].x != px2 || cubes[2].x != px3 || cubes[3].x != px4 || cubes[4].x != px5)){
                    Debug.Log($"Before If c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                    if (cubes[0].x <= px2 - range || cubes[0].x >= px2 + range || 
                        cubes[1].x <= px3 - range || cubes[1].x >= px3 + range || 
                        cubes[2].x <= px4 - range || cubes[2].x >= px4 + range ||
                        cubes[3].x <= px5 - range || cubes[3].x >= px5 + range ||
                        cubes[4].x <= px1 - range || cubes[4].x >= px1 + range){
                            if (cubes[0].x <= px2 - range || cubes[0].x >= px2 + range) Debug.Log($"c1 {cubes[0].x}");
                            if (cubes[1].x <= px3 - range || cubes[1].x >= px3 + range) Debug.Log($"c2 {cubes[1].x}");
                            if (cubes[2].x <= px4 - range || cubes[2].x >= px4 + range) Debug.Log($"c3 {cubes[2].x}");
                            if (cubes[3].x <= px5 - range || cubes[3].x >= px5 + range) Debug.Log($"c4 {cubes[3].x}");
                            if (cubes[4].x <= px1 - range || cubes[4].x <= px1 + range) Debug.Log($"c5 {cubes[4].x}");
                    //if (cubes[0].x != px1 || cubes[1].x != px2 || cubes[2].x != px3 || cubes[3].x != px4 || cubes[4].x != px5){
                        Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                        cubes[0].TargetMove(targetX:px1, targetY:py1, targetAngle:270, configID:0);
                        cubes[1].TargetMove(targetX:px2, targetY:py2, targetAngle:0, configID:1);
                        cubes[2].TargetMove(targetX:px3, targetY:py3, targetAngle:0, configID:2);
                        cubes[3].TargetMove(targetX:px4, targetY:py4, targetAngle:90, configID:3);
                        cubes[4].TargetMove(targetX:px5, targetY:py5, targetAngle:180, configID:4);
                    }
                }
                break;
            }
            case 2:
            {
                Debug.Log("phase2");
                Debug.Log($"done1 {done1} done2 {done2} done3 {done3} done4 {done4} done5 {done5} ");
                Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                if (done1 == 0 || done2 == 0 || done3 == 0 || done4 == 0 || done5 == 0){
                    if (cubes[0].x <= px1 - range || cubes[0].x >= px1 + range || 
                        cubes[1].x <= px2 - range || cubes[1].x >= px2 + range || 
                        cubes[2].x <= px3 - range || cubes[2].x >= px3 + range ||
                        cubes[3].x <= px4 - range || cubes[3].x >= px4 + range ||
                        cubes[4].x <= px5 - range || cubes[4].x >= px5 + range){
                    //if (cubes[0].x != px1 || cubes[1].x != px2 || cubes[2].x != px3 || cubes[3].x != px4 || cubes[4].x != px5){
                        Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                    //if (cubes[0].x != px5 || cubes[1].x != px1 || cubes[2].x != px2 || cubes[3].x != px3 || cubes[4].x != px4){
                        cubes[0].TargetMove(targetX:px5, targetY:py5, targetAngle:180, configID:0);
                        cubes[1].TargetMove(targetX:px1, targetY:py1, targetAngle:270, configID:1);
                        cubes[2].TargetMove(targetX:px2, targetY:py2, targetAngle:0, configID:2);
                        cubes[3].TargetMove(targetX:px3, targetY:py3, targetAngle:0, configID:3);
                        cubes[4].TargetMove(targetX:px4, targetY:py4, targetAngle:90, configID:4);
                    }
                }
                break;
            }
            case 3:
            {
                Debug.Log("phase3");
                Debug.Log($"done1 {done1} done2 {done2} done3 {done3} done4 {done4} done5 {done5} ");
                Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                if (done1 == 0 || done2 == 0 || done3 == 0 || done4 == 0 || done5 == 0){
                    if (cubes[0].x <= px5 - range || cubes[0].x >= px5 + range || 
                        cubes[1].x <= px1 - range || cubes[1].x >= px1 + range || 
                        cubes[2].x <= px2 - range || cubes[2].x >= px2 + range ||
                        cubes[3].x <= px3 - range || cubes[3].x >= px3 + range ||
                        cubes[4].x <= px4 - range || cubes[4].x >= px4 + range){
                    //if (cubes[0].x != px5 || cubes[1].x != px1 || cubes[2].x != px2 || cubes[3].x != px3 || cubes[4].x != px4){
                        Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                    //if (cubes[0].x != px4 || cubes[1].x != px5 || cubes[2].x != px1 || cubes[3].x != px2 || cubes[4].x != px3){
                        cubes[0].TargetMove(targetX:px4, targetY:py4, targetAngle:90, configID:0);
                        cubes[1].TargetMove(targetX:px5, targetY:py5, targetAngle:180, configID:1);
                        cubes[2].TargetMove(targetX:px1, targetY:py1, targetAngle:270, configID:2);
                        cubes[3].TargetMove(targetX:px2, targetY:py2, targetAngle:0, configID:3);
                        cubes[4].TargetMove(targetX:px3, targetY:py3, targetAngle:0, configID:4);
                    }
                }
                break;
            }
            case 4:
            {
                Debug.Log("phase4");
                Debug.Log($"done1 {done1} done2 {done2} done3 {done3} done4 {done4} done5 {done5} ");
                Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                if (done1 == 0 || done2 == 0 || done3 == 0 || done4 == 0 || done5 == 0){
                    if (cubes[0].x <= px4 - range || cubes[0].x >= px4 + range || 
                        cubes[1].x <= px5 - range || cubes[1].x >= px5 + range || 
                        cubes[2].x <= px1 - range || cubes[2].x >= px1 + range ||
                        cubes[3].x <= px2 - range || cubes[3].x >= px2 + range ||
                        cubes[4].x <= px3 - range || cubes[4].x >= px3 + range){
                    //if (cubes[0].x != px4 || cubes[1].x != px5 || cubes[2].x != px1 || cubes[3].x != px2 || cubes[4].x != px3){
                        Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                    //if (cubes[0].x != px3 || cubes[1].x != px4 || cubes[2].x != px5 || cubes[3].x != px1 || cubes[4].x != px2){
                        cubes[0].TargetMove(targetX:px3, targetY:py3, targetAngle:0, configID:0);
                        cubes[1].TargetMove(targetX:px4, targetY:py4, targetAngle:90, configID:1);
                        cubes[2].TargetMove(targetX:px5, targetY:py5, targetAngle:180, configID:2);
                        cubes[3].TargetMove(targetX:px1, targetY:py1, targetAngle:270, configID:3);
                        cubes[4].TargetMove(targetX:px2, targetY:py2, targetAngle:0, configID:4);
                    }
                }
                break;
            }
            case 5:
            {
                Debug.Log("phase5");
                Debug.Log($"done1 {done1} done2 {done2} done3 {done3} done4 {done4} done5 {done5} ");
                Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                if (done1 == 0 || done2 == 0 || done3 == 0 || done4 == 0 || done5 == 0){
                    if (cubes[0].x <= px3 - range || cubes[0].x >= px3 + range || 
                        cubes[1].x <= px4 - range || cubes[1].x >= px4 + range || 
                        cubes[2].x <= px5 - range || cubes[2].x >= px5 + range ||
                        cubes[3].x <= px1 - range || cubes[3].x >= px1 + range ||
                        cubes[4].x <= px2 - range || cubes[4].x >= px2 + range){
                    //if (cubes[0].x != px3 || cubes[1].x != px4 || cubes[2].x != px5 || cubes[3].x != px1 || cubes[4].x != px2){
                        Debug.Log($"c1 {cubes[0].x}  c2 {cubes[1].x}  c3 {cubes[2].x}  c4 {cubes[3].x}  c5 {cubes[4].x}");
                    //if (cubes[0].x != px2 || cubes[1].x != px3 || cubes[2].x != px4 || cubes[3].x != px5 || cubes[4].x != px1){
                        cubes[0].TargetMove(targetX:px2, targetY:py2, targetAngle:0, configID:0);
                        cubes[1].TargetMove(targetX:px3, targetY:py3, targetAngle:0, configID:1);
                        cubes[2].TargetMove(targetX:px4, targetY:py4, targetAngle:90, configID:2);
                        cubes[3].TargetMove(targetX:px5, targetY:py5, targetAngle:180, configID:3);
                        cubes[4].TargetMove(targetX:px1, targetY:py1, targetAngle:270, configID:4);
                    }
                }
                //phase = 1;
                break;
            }
        }
    }
}
