using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class Waive : MonoBehaviour
{
    // Track time to have 1 sec delays for when the next cube moves
    // function that gets called in update to start moving the cube
    // flag using double tap to start moving the cube
    // flags to check if the cube is done moving
    CubeManager cubeManager;
    Cube[] cubes; Cube cube;
    float intervalTime = 1.7f;
    float elapsedTime = 1.2f;
    float delayTime = 0.03f;
    float timer = 0.0f;
    bool waive = false; bool firstWaive = false;
    int phase = 0; int speed = 50; int phase1 = 0; int phase2 = 0; int phase3 = 0;
    int ipx1 = 127; int ipy1 = 265; int ipx2 = 183; int ipy2 = 265; int ipx3 = 242; int ipy3 = 265; int ipx4 = 302; int ipy4 = 165; int ipx5 = 360; int ipy5 = 165;
    int y1 = 320; int y2 = 200; int y3 = 260;
    int c1x = 0; int c2x = 0; int c3x = 0;int c4x = 0;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(4);
        cubes[0].doubleTapCallback.AddListener("EventScene", OnDoubleTap);
        //cubes[0].collisionCallback.AddListener("EventScene", OnCollision);
        // float interval = 0.045f;
        // CubeOrderBalancer.Instance.UpdateIntervalSec(interval);
        if (cubes.Length == 4){
            c1x = cubes[0].x; c2x = cubes[1].x; c3x = cubes[2].x;
            c1x = 100; c2x = 200; c3x = 300; c4x = 400;
            Debug.Log($"c1x {c1x} c2x {c2x} c3x {c3x}");
        }
        //cube = await cubeManager.SingleConnect();
        //cube.doubleTapCallback.AddListener("EventScene", OnDoubleTap);
        

    }
    void WaivePath(int nav, Cube c, int x){
        if (phase == 0){
            Debug.Log($"delta time {Time.deltaTime}");
            var mv0 = cubeManager.navigators[nav].Navi2Target(x, y1, maxSpd:speed).Exec();
            if (mv0.reached){phase = 1;}
        }
        if (phase == 1){
            var mv1 = cubeManager.navigators[nav].Navi2Target(x, y2, maxSpd:speed).Exec();
            if (mv1.reached){phase = 2;}
        }
        if (phase == 2){
            var mv2 = cubeManager.navigators[nav].Navi2Target(x, y3, maxSpd:speed).Exec();
            if (mv2.reached){phase = 0;}
        }
    }
    

    
    void OnDoubleTap(Cube c)
    {
        if (waive){waive = false; phase = 0; phase1 = 0; phase2 = 0; phase3 = 0;}
        else{waive = true; firstWaive = true;elapsedTime = Time.deltaTime; timer = 0.0f;}
        // float interval = 0.1f;
        // CubeOrderBalancer.Instance.UpdateIntervalSec(interval);
        //cube.Move(50, 50, 900, Cube.ORDER_TYPE.Strong);
        //cube.Move(-50, -50, 500);
        // cubes[1].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[1].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(50, 50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(-50, -50, 100, Cube.ORDER_TYPE.Strong);

        // cubeManager.navigators[1].Navi2Target(ipx1, ipy1 + 60, maxSpd:100).Exec();
        // cubes[2].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[2].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubeManager.navigators[2].Navi2Target(ipx2, ipy2 + 60, maxSpd:100).Exec();
        // cubes[3].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubes[3].Move(0, 0, 100, Cube.ORDER_TYPE.Strong);
        // cubeManager.navigators[3].Navi2Target(ipx3, ipy3 + 60, maxSpd:100).Exec();
        // cubeManager.navigators[1].Navi2Target(ipx1, ipy1 - 120, maxSpd:100).Exec();
        // cubeManager.navigators[2].Navi2Target(ipx2, ipy2 - 120, maxSpd:100).Exec();
        // cubeManager.navigators[3].Navi2Target(ipx3, ipy3 - 120, maxSpd:100).Exec();


    }
    int moveC1 = 0;
    int moveC2 = 0;
    int moveC3 = 0; int moveC4 = 0;
    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced){
            timer += Time.deltaTime;
            if (waive){
                if (firstWaive){
                    if (phase == 0){moveC1 = 1;}
                    if (phase == 1){moveC1 = 2;}
                    if (phase == 2){moveC1 = 3;}
                    
                    //Debug.Log($"timer {timer}");
                    if (timer >= delayTime){
                        if (phase1 == 0){moveC2 = 1;}
                        if (phase1 == 1){moveC2 = 2;}
                        if (phase1 == 2){moveC2 = 3;}
                    }
                    //Debug.Log($"delaytime *2 {delayTime * 2}");
                    if (timer >= delayTime + 0.015){
                        if (phase2 == 0){moveC3 = 1;}
                        if (phase2 == 1){moveC3 = 2;}
                        if (phase2 == 2){moveC3 = 3;}
                        //firstWaive = false;
                    }
                    if (timer >= delayTime +0.03){
                        if (phase3 == 0){moveC4 = 1;}
                        if (phase3 == 1){moveC4 = 2;}
                        if (phase3 == 2){moveC4 = 3;}
                        firstWaive = false;
                    } 
                }
                else{
                    if (phase == 0){moveC1 = 1;}
                    if (phase == 1){moveC1 = 2;}
                    if (phase == 2){moveC1 = 3;}
                    if (phase1 == 0){moveC2 = 1;}
                    if (phase1 == 1){moveC2 = 2;}
                    if (phase1 == 2){moveC2 = 3;}
                    if (phase2 == 0){moveC3 = 1;}
                    if (phase2 == 1){moveC3 = 2;}
                    if (phase2 == 2){moveC3 = 3;}
                    if (phase3 == 0){moveC4 = 1;}
                    if (phase3 == 1){moveC4 = 2;}
                    if (phase3 == 2){moveC4 = 3;}
                }
            }
        
            if (moveC1 == 1){
                var mv0 = cubeManager.navigators[0].Navi2Target(c1x, y1, maxSpd:speed).Exec();
                if (mv0.reached){phase = 1; Debug.Log($"p1 c1x {cubes[0].x} c1y {cubes[0].y}");}
            }
            if (moveC1 == 2){
                var mv1 = cubeManager.navigators[0].Navi2Target(c1x, y2, maxSpd:speed).Exec();
                if (mv1.reached){phase = 0; Debug.Log($"p2 c1x {cubes[0].x} c1y {cubes[0].y}");}
            }
            if (moveC1 == 3){
                var mv2 = cubeManager.navigators[0].Navi2Target(c1x, y3, maxSpd:speed).Exec();
                if (mv2.reached){phase = 0; Debug.Log($"p3 c1x {cubes[0].x} c1y {cubes[0].y}");}
            }

            if (moveC2 == 1){
                var mv0 = cubeManager.navigators[1].Navi2Target(c2x, y1, maxSpd:speed).Exec();
                if (mv0.reached){phase1 = 1;}
            }
            if (moveC2 == 2){
                var mv1 = cubeManager.navigators[1].Navi2Target(c2x, y2, maxSpd:speed).Exec();
                if (mv1.reached){phase1 = 0;}
            }
            if (moveC2 == 3){
                var mv2 = cubeManager.navigators[1].Navi2Target(c2x, y3, maxSpd:speed).Exec();
                if (mv2.reached){phase1 = 0;}
            }

            if (moveC3 == 1){
                var mv0 = cubeManager.navigators[2].Navi2Target(c3x, y1, maxSpd:speed).Exec();
                if (mv0.reached){phase2 = 1;}
            }
            if (moveC3 == 2){
                var mv1 = cubeManager.navigators[2].Navi2Target(c3x, y2, maxSpd:speed).Exec();
                if (mv1.reached){phase2 = 0;}
            }
            if (moveC3 == 3){
                var mv2 = cubeManager.navigators[2].Navi2Target(c3x, y3, maxSpd:speed).Exec();
                if (mv2.reached){phase2 = 0;}
            }
            if (moveC4 == 1){
                var mv0 = cubeManager.navigators[3].Navi2Target(c4x, y1, maxSpd:speed).Exec();
                if (mv0.reached){phase3 = 1;}
            }
            if (moveC4 == 2){
                var mv1 = cubeManager.navigators[3].Navi2Target(c4x, y2, maxSpd:speed).Exec();
                if (mv1.reached){phase3 = 0;}
            }
            if (moveC4 == 3){
                var mv2 = cubeManager.navigators[3].Navi2Target(c4x, y3, maxSpd:speed).Exec();
                if (mv2.reached){phase3 = 0;}
            }
        }
    }
}
