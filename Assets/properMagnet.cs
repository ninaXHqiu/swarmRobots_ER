using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using toio;

public class properMagnet : MonoBehaviour{
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
        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
    }

    int moveCube1 = 0; int moveCube2 = 0; int xc1 = 0; int xc2 = 0;
    void Update(){
        //elapsedTime += Time.deltaTime;
        if (cubeManager.synced){
            if (connected == 1){
                //cubes[1].Move(30, 30, 100);
                int x1 = cubes[0].x;
                int y1 = cubes[0].y;
                int x2 = cubes[1].x;
                int y2 = cubes[1].y;
                
                int centerX = (x1 + x2) / 2;
                int centerY = (y1 + y2) / 2;
                
                float radiusAttract = 15;
                float radiusOutofBound = 80;
                float distanceCube1 = Mathf.Sqrt(Mathf.Pow(x1 - centerX, 2) + Mathf.Pow(y1 - centerY, 2));
                float distanceCube2 = Mathf.Sqrt(Mathf.Pow(x2 - centerX, 2) + Mathf.Pow(y2 - centerY, 2));
                Debug.Log($"cube1 pos x = {x1} y = {y1}");
                Debug.Log($"cube2 pos x = {x2} y = {y2}");
                Debug.Log($"center pos x = {centerX} y = {centerY}");
                Debug.Log($"distance c1 pos = {distanceCube1} ");
                Debug.Log($"distance c2 pos = {distanceCube2} ");

                bool isCube1Outside = (distanceCube1 > radiusAttract && distanceCube1 < radiusOutofBound);
                bool isCube2Outside = (distanceCube2 > radiusAttract && distanceCube2 < radiusOutofBound);
                // Debug.Log($"isCube1Outside = {isCube1Outside} ");
                // Debug.Log($"isCube2Outside = {isCube2Outside} ");

                if (isCube1Outside && isCube2Outside){
                    moveCube1 = 1; moveCube2 = 1;
                    Debug.Log($"cube1 pos x = {x1} y = {y1}");
                    Debug.Log($"cube2 pos x = {x2} y = {y2}");
                    Debug.Log($"center pos x = {centerX} y = {centerY}");
                    Debug.Log($"distance c1 pos = {distanceCube1} ");
                    Debug.Log($"distance c2 pos = {distanceCube2} ");
                    
                }
                if (moveCube1 == 1){
                    if (cubes[0].x >= centerX){
                        xc1 = centerX + 5;
                    }
                    else{
                        xc1 = centerX - 5;
                    }
                    var mv = cubeManager.navigators[0].Navi2Target(xc1, centerY, maxSpd:150).Exec();
                    if (mv.reached){
                        moveCube1 = 0;
                    } 
                }

                if (moveCube2 == 1){
                    if (cubes[1].x >= centerX){
                        xc2 = centerX + 5;
                    }
                    else{
                        xc2 = centerX - 5;
                    }
                    var mv = cubeManager.navigators[1].Navi2Target(xc2, centerY, maxSpd:150).Exec();
                    if (mv.reached){
                        moveCube2 = 0;
                    } 
                }
            }
            // float radius = 10;
            // float angleIncrement = 2 * Mathf.PI / numPoints;
            // for (int i = 0; i < numPoints; i++){
            //     float angle = i * angleIncrement;
            //     float x = centerX + radius * Mathf.Cos(angle);
            //     float y = centerY + radius * Mathf.Sin(angle);
            //     innerCircle.Add((x, y));
            // }
            // Debug.Log($"cube1 pos x = {x1} y = {y1}");
            // Debug.Log($"cube2 pos x = {x2} y = {y2}");
            // Debug.Log($"midde pos x = {centerX} y = {centerY}");
            // foreach (var point in innerCircle){
            //     Debug.Log($"x = {point.Item1} y = {point.Item2}");
            // }
            
            //check if either cube is outside the inner circle

            
        }
    }

}