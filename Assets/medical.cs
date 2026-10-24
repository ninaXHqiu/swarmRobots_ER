using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.Navigation;
using toio.MathUtils;
public class medical : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    public Navigator.Mode naviMode = Navigator.Mode.BOIDS;
    int numCubes = 7;
    int x = 0;
    int y = 0;
    int speed = 150;
    int phase = 0;
    float lastTime = 0;
    bool delayMove = false;
    bool connected = false;

    bool mc1 = false; bool mc2 = false; bool mc3 = false; bool mc4 = false; bool mc5 = false; 
    bool r1 = false; bool r2 = false; bool r3 = false; bool r4 = false; bool r5 = false;

    int ipx1 = 160; int ipx2 = 205; int ipx3 = 250; int ipx4 = 295; int ipx5 = 335;
    int yTop = 165; int yBottom = 340;
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
    // Start is called before the first frame update
    async void Start()
    {
        Application.targetFrameRate = 30;
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(5);
        if (cubes.Length == 5){
            connected = true;
            //Debug.Log("====== Start AccelerationMove ======");
        }
        foreach (var cube in cubes)
        {
            cube.targetMoveCallback.AddListener("Sample_Motor", TargetMoveCallback);
        }

        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
        cubeManager.navigators[2].ClearOther();
        cubeManager.navigators[3].ClearOther();
        cubeManager.navigators[4].ClearOther();
    }
    void TargetMoveCallback(Cube cube, int configID, Cube.TargetMoveRespondType response)
    {
        if (response != 0){
            Debug.LogFormat("Cube#{0}'s TargetMove ends with response code: {1}", configID, (byte)response);
            //phase ++;
        }
        //phase ++;
        
        
    }

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced && connected){
            
            if (phase == 0){
                MoveC1(ipx1, yTop); MoveC2(ipx3, yTop); MoveC3(ipx5, yTop); MoveC4(ipx3, yBottom); MoveC5(ipx5, yBottom);
                //Debug.Log($"cube 4 x = {cubes[4].x} y = {cubes[4].y}");
            }
            
            if (r1 && r2 && r3 && r4 && r5){
                lastTime = Time.time;
                phase = 1;
            }
            if (Time.time-lastTime > 9.9f){delayMove = true;}
            if (phase == 1 && delayMove == true){
                // Debug.Log("====== Start Move ======");
                
                // cubes[0].TargetMove(targetX:200, targetY:250, targetAngle:270, configID:1); 
                // cubes[1].TargetMove(targetX:250, targetY:250, targetAngle:270, configID:2);
                // cubes[2].TargetMove(targetX:250, targetY:200, targetAngle:270, configID:3); 
                // cubes[3].TargetMove(targetX:250, targetY:300, targetAngle:270, configID:4); 
                // cubes[4].TargetMove(targetX:300, targetY:250, targetAngle:270, configID:5);
                // phase ++;  
                // Debug.Log(phase);
               MoveC1(200, 250); MoveC2(250, 255); MoveC3(250,200); MoveC4(250,300); MoveC5(300, 250);
            }
            
        }
    }
}
