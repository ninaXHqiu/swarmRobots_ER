using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class Snake : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int ph = 0;
    int speed = 250;
    bool connected = false;
    int mvCube1 = 0; int mvCube2 = 0; int mvCube3 = 0;
    int up = 226;
    int down = 266;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real); //ConnectType.Real
        cubes = await cubeManager.MultiConnect(5);
        if (cubes.Length == 5){connected = true;}
    }
    int px1 = 318; int py1 = 246; int px2 = 233; int py2 = 246; int px3 = 154; int py3 = 248; int px4 = 302; int py4 = 165; int px5 = 360; int py5 = 165;
    // Update is called once per frame
    void Update()
    {
        if(cubeManager.synced && connected)
        {
            
            if (ph == 0){
                if (cubes[0].x != px1 && cubes[0].y != py1){
                    var mv0 = cubeManager.navigators[0].Navi2Target(px1, py1, maxSpd:speed).Exec();
                    var mv1 = cubeManager.navigators[1].Navi2Target(px2, py2, maxSpd:speed).Exec();
                    var mv2 = cubeManager.navigators[2].Navi2Target(px3, py3, maxSpd:speed).Exec();
                    var mv3 = cubeManager.navigators[3].Navi2Target(px4, py4, maxSpd:speed).Exec();
                    var mv4 = cubeManager.navigators[4].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
                    if (mv0.reached && mv1.reached && mv2.reached && mv3.reached && mv4.reached)
                    {ph = 1;}
                }
            }
            if (ph == 1){
                if (cubes[0].y < up){ 
                    mvCube2 = 1; mvCube1 = 1;
                }
                if (cubes[0].y > down){ 
                    mvCube2 = 4; mvCube1 = 1;
                }
            }
            if (mvCube1 == 1){
                var mv = cubeManager.navigators[0].Navi2Target(px1, py1, maxSpd:speed).Exec();
                if (mv.reached){mvCube1 = 0;}
            }
            //  if mvCube2 == 1 move up and set mvCube2 = 2                 mvCube3 = 1
            //  if mvCube2 == 2 move all the way down and set mvCube2 = 3   mvCube3 = 2
            //  if mvCube2 == 3 move back to center and set mvCube2 = 0     mvCube3 = 3
            if (mvCube2 == 1){
                var mv = cubeManager.navigators[1].Navi2Target(px2, py2-40, maxSpd:speed).Exec();
                if (mv.reached){mvCube2 = 3; mvCube3 = 1;}
            }
            if (mvCube2 == 2){
                var mv = cubeManager.navigators[1].Navi2Target(px2, py2+40, maxSpd:speed).Exec();
                if (mv.reached){mvCube2 = 3; mvCube3 = 2;}
            }
            if (mvCube2 == 3){
                var mv = cubeManager.navigators[1].Navi2Target(px2, py2, maxSpd:speed).Exec();
                if (mv.reached){mvCube2 = 0; mvCube3 = 3;}
            }
            if (mvCube3 == 1){
                var mv = cubeManager.navigators[2].Navi2Target(px3, py3-40, maxSpd:speed).Exec();
                if (mv.reached){mvCube3 = 3;}
            }
            if (mvCube3 == 2){
                var mv = cubeManager.navigators[2].Navi2Target(px3, py3+40, maxSpd:speed).Exec();
                if (mv.reached){mvCube3 = 3;}
            }
            if (mvCube3 == 3){
                var mv = cubeManager.navigators[2].Navi2Target(px3, py3, maxSpd:speed).Exec();
                if (mv.reached){mvCube3 = 0;}
            }
            

            // if mvCube2 == 4 move down and set mvCube2 = 5                mvCube3 = 4
            // if mvCube2 == 5 move all the way up and set mvCube2 = 6      mvCube3 = 5
            // if mvCube2 == 6 move back to center and set mvCube2 = 0      mvCube3 = 6
            if (mvCube2 == 4){
                var mv = cubeManager.navigators[1].Navi2Target(px2, py2+20, maxSpd:speed).Exec();
                if (mv.reached){mvCube2 = 5; mvCube3 = 4;}
            }
            if (mvCube2 == 5){
                var mv = cubeManager.navigators[1].Navi2Target(px2, py2-40, maxSpd:speed).Exec();
                if (mv.reached){mvCube2 = 6; mvCube3 = 5;}
            }
            if (mvCube2 == 6){
                var mv = cubeManager.navigators[1].Navi2Target(px2, py2, maxSpd:speed).Exec();
                if (mv.reached){mvCube2 = 0; mvCube3 = 6;}
            }
            if (mvCube3 == 4){
                var mv = cubeManager.navigators[2].Navi2Target(px3, py3+20, maxSpd:speed).Exec();
                if (mv.reached){mvCube3 = 5;}
            }
            if (mvCube3 == 5){
                var mv = cubeManager.navigators[2].Navi2Target(px3, py3-40, maxSpd:speed).Exec();
                if (mv.reached){mvCube3 = 6;}
            }
            if (mvCube3 == 6){
                var mv = cubeManager.navigators[2].Navi2Target(px3, py3, maxSpd:speed).Exec();
                if (mv.reached){mvCube3 = 0;}
            }
        }
    }
}
