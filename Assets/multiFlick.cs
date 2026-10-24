using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class multiFlick : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int ph = 0;
    int speed = 200;
    bool connected = false;
    int mvCube = 0;
    
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(5);
        Debug.Assert(cubeManager.navigators.Count>1, "Need at least 2 cubes.");
        if (cubes.Length == 5){connected = true;}
    }
    int checkpos = 260;
    int px1 = 250; int py1 = 281; int px2 = 193; int py2 = 283; int px3 = 140; int py3 = 280; int px4 = 140; int py4 = 227; int px5 = 140; int py5 = 182;
    void Update()
    {
        if (cubeManager.synced && connected)
        {
            int x1 = cubes[0].x;
            int y1 = cubes[0].y;

            if (ph == 0){
                if (cubes[0].x != px1 && cubes[0].y != py1){
                    var mv0 = cubeManager.navigators[0].Navi2Target(px1, py1, maxSpd:speed).Exec();
                    var mv1 = cubeManager.navigators[1].Navi2Target(px2, py2, maxSpd:speed).Exec();
                    var mv2 = cubeManager.navigators[2].Navi2Target(px3, py3, maxSpd:speed).Exec();
                    var mv3 = cubeManager.navigators[3].Navi2Target(px4, py4, maxSpd:speed).Exec();
                    var mv4 = cubeManager.navigators[4].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
                    if (mv0.reached && mv1.reached && mv2.reached && mv3.reached && mv4.reached)
                    {
                        ph = 1;
                        cubeManager.navigators[0].ClearOther();
                        cubeManager.navigators[1].ClearOther();
                        cubeManager.navigators[2].ClearOther();
                        cubeManager.navigators[3].ClearOther();
                        cubeManager.navigators[4].ClearOther();
                    }
                }
            }
            if (ph == 1){
                if ( cubes[0].y < checkpos){ 
                    mvCube = 1;
                }
            }
            if (mvCube == 1){
                var mv0 = cubeManager.navigators[1].Navi2Target(px1, py1, maxSpd:speed).Exec();
                var mv1 = cubeManager.navigators[2].Navi2Target(px2, py2, maxSpd:speed).Exec();
                var mv2 = cubeManager.navigators[3].Navi2Target(px3, py3, maxSpd:speed).Exec();
                var mv3 = cubeManager.navigators[4].Navi2Target(px4, py4, maxSpd:speed).Exec();
                var mv4 = cubeManager.navigators[0].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
                if (mv0.reached && mv1.reached && mv2.reached && mv3.reached){ph = 2;mvCube = 0;}
            }
            if (ph == 2){
                if (cubes[1].y < checkpos){ 
                    mvCube = 2;
                }
            }
            if (mvCube == 2){
                var mv0 = cubeManager.navigators[2].Navi2Target(px1, py1, maxSpd:speed).Exec();
                var mv1 = cubeManager.navigators[3].Navi2Target(px2, py2, maxSpd:speed).Exec();
                var mv2 = cubeManager.navigators[4].Navi2Target(px3, py3, maxSpd:speed).Exec();
                var mv3 = cubeManager.navigators[0].Navi2Target(px4, py4, maxSpd:speed).Exec();
                var mv4 = cubeManager.navigators[1].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
                if (mv0.reached && mv1.reached && mv2.reached && mv3.reached){ph = 3;mvCube = 0;}
            }
            if (ph == 3){
                if (cubes[2].y < checkpos){
                    mvCube = 3;
                }
            }
            if (mvCube == 3){
                var mv0 = cubeManager.navigators[3].Navi2Target(px1, py1, maxSpd:speed).Exec();
                var mv1 = cubeManager.navigators[4].Navi2Target(px2, py2, maxSpd:speed).Exec();
                var mv2 = cubeManager.navigators[0].Navi2Target(px3, py3, maxSpd:speed).Exec();
                var mv3 = cubeManager.navigators[1].Navi2Target(px4, py4, maxSpd:speed).Exec();
                var mv4 = cubeManager.navigators[2].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
                if (mv0.reached && mv1.reached && mv2.reached && mv3.reached){ph = 4;mvCube = 0;}
            }
            if (ph == 4){
                if (cubes[3].y < checkpos){ 
                    mvCube = 4;
                }
            }
            if (mvCube == 4){
                var mv0 = cubeManager.navigators[4].Navi2Target(px1, py1, maxSpd:speed).Exec();
                var mv1 = cubeManager.navigators[0].Navi2Target(px2, py2, maxSpd:speed).Exec();
                var mv2 = cubeManager.navigators[1].Navi2Target(px3, py3, maxSpd:speed).Exec();
                var mv3 = cubeManager.navigators[2].Navi2Target(px4, py4, maxSpd:speed).Exec();
                var mv4 = cubeManager.navigators[3].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
                if (mv0.reached && mv1.reached && mv2.reached && mv3.reached){ph = 5;mvCube = 0;}
            }
            if (ph == 5){
                if (cubes[4].y < checkpos){ 
                    mvCube = 5;
                }
            }
            if (mvCube == 5){
                var mv0 = cubeManager.navigators[0].Navi2Target(px1, py1, maxSpd:speed).Exec();
                var mv1 = cubeManager.navigators[1].Navi2Target(px2, py2, maxSpd:speed).Exec();
                var mv2 = cubeManager.navigators[2].Navi2Target(px3, py3, maxSpd:speed).Exec();
                var mv3 = cubeManager.navigators[3].Navi2Target(px4, py4, maxSpd:speed).Exec();
                var mv4 = cubeManager.navigators[4].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
                if (mv0.reached && mv1.reached && mv2.reached && mv3.reached){ph = 1;mvCube = 0;}
            }
            
        }
    }
}
