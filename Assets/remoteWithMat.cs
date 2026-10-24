using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using toio;


public class remoteWithMat : MonoBehaviour{
    float intervalTime = 2.0f;
    float elapsedTime = 0;
    CubeManager cubeManager;
    Cube[] cubes;
    Cube firstCube;
    Cube sndCube;
    int connected = 0;
    // Define list of tuples to store the position of the cubes
    int numPoints = 4;

    async void Start(){
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(2);
        if (cubes.Length > 1)
        {
            // firstCube = cubes[0];
            // sndCube = cubes[1];
            connected = 1;
        }
        //cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
    }

    int moveCube = 0; int moveCubeX = 0; int moveCube2 = 0; int x = 0; int y = 0;
    void Update(){
        //elapsedTime += Time.deltaTime;
        if (cubeManager.synced){
            if (connected == 1){
                int x1 = cubes[0].x;
                int y1 = cubes[0].y;
                
                Debug.Log($"cube1 pos x = {cubes[0].x} y = {cubes[0].y}");
                // 4 possible cases:
                // 1. cube 1 x and y are less than cube 2 x and y
                // 2. cube 1 x smaller y bigger
                // 3. cube 1 x bigger y smaller
                // 4. cube 1 x and y are bigger than cube 2 x and y
                if(cubes[1].y != 0){
                    if (cubes[1].x - cubes[0].x > 30 && cubes[1].y - cubes[0].y > 30){
                        moveCube = 1;
                    }
                    else if (cubes[1].x - cubes[0].x > 30 && cubes[1].y - cubes[0].y <= 30 && cubes[0].x < 250 && cubes[0].y < 250){moveCube = 1;}
                    else if (cubes[1].y - cubes[0].y > 30 && cubes[1].x - cubes[0].x <= 30 && cubes[0].x < 250 && cubes[0].y < 250){moveCube = 1;}
                    

                    else if (cubes[1].x - cubes[0].x > 30 && cubes[0].y - cubes[1].y > 30){
                        moveCube = 2;
                    }
                    else if (cubes[1].x - cubes[0].x > 30 && cubes[0].y - cubes[1].y <= 30 && cubes[0].x < 250 && cubes[0].y > 250){moveCube = 2;}
                    else if (cubes[0].y - cubes[1].y > 30 && cubes[1].x - cubes[0].x <= 30 && cubes[0].x < 250 && cubes[0].y > 250){moveCube = 2;}

                    else if (cubes[0].x - cubes[1].x > 30 && cubes[1].y - cubes[0].y > 30){
                        moveCube = 3;
                    }
                    else if (cubes[0].x - cubes[1].x > 30 && cubes[1].y - cubes[0].y <= 30 && cubes[0].x > 250 && cubes[0].y < 250){moveCube = 3;}
                    else if (cubes[1].y - cubes[0].y > 30 && cubes[0].x - cubes[1].x <= 30 && cubes[0].x > 250 && cubes[0].y < 250){moveCube = 3;}


                    else if (cubes[0].x - cubes[1].x > 30 && cubes[0].y - cubes[1].y > 30){
                        moveCube = 4;
                    }
                    else if (cubes[0].x - cubes[1].x > 30 && cubes[0].y - cubes[1].y <= 30 && cubes[0].x > 250 && cubes[0].y > 250){moveCube = 4;}
                    else if (cubes[0].y - cubes[1].y > 30 && cubes[0].x - cubes[1].x <= 30 && cubes[0].x > 250 && cubes[0].y > 250){moveCube = 4;}

    
                    

                    if (moveCube == 1){
                        //cubes[0].Move(100, 100, 300);
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y + 25, maxSpd:150).Exec();
                        if (mv.reached){
                            moveCube = 0;
                        } 
                    }
                    if (moveCube == 2){
                        //cubes[0].Move(100, 100, 300);
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y - 25, maxSpd:150).Exec();
                        if (mv.reached){
                            moveCube = 0;
                        } 
                    }
                    if (moveCube == 3){
                        //cubes[0].Move(100, 100, 300);
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y + 25, maxSpd:150).Exec();
                        if (mv.reached){
                            moveCube = 0;
                        } 
                    }
                    if (moveCube == 4){
                        //cubes[0].Move(100, 100, 300);
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y - 25, maxSpd:150).Exec();
                        if (mv.reached){
                            moveCube = 0;
                        } 
                    }
                    // if (cubes[0].x != cubes[1].x){
                    //     moveCubeX = 1;
                    // }

                    // if (moveCubeX == 1){
                    //     //cubes[0].Move(100, 100, 300);
                    //     var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x, cubes[1].y, maxSpd:150).Exec();
                    //     if (mv.reached){
                    //         moveCubeX = 0;
                    //     } 
                        
                    // }
                }
                
            

            
        }
    }
}
}