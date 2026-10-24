using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class Position : MonoBehaviour
{
    // Magnet without mat
    CubeManager cubeManager;
    Cube[] cubes;
    Cube firstCube;
    Cube sndCube;
    int x = 0;
    int y = 0;
    int connected = 0;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(2);
        if (cubes.Length > 1)
        {
            firstCube = cubes[0];
            sndCube = cubes[1];
            connected = 1;
        }
        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();
        cubes[0].collisionCallback.AddListener("EventScene", OnCollision);
        cubes[1].collisionCallback.AddListener("EventScene", OnCollision);
        // cubes[2].collisionCallback.AddListener("EventScene", OnCollision);
        // cubes[3].collisionCallback.AddListener("EventScene", OnCollision);
        // cubes[4].collisionCallback.AddListener("EventScene", OnCollision);
        // cubes[5].collisionCallback.AddListener("EventScene", OnCollision);
        // cubes[6].collisionCallback.AddListener("EventScene", OnCollision);
        // cubes[7].collisionCallback.AddListener("EventScene", OnCollision);
        // cubes[8].collisionCallback.AddListener("EventScene", OnCollision);
        // cubes[9].collisionCallback.AddListener("EventScene", OnCollision);
    }

    // Update is called once per frame
    // void Update()
    // {
    //     if (firstCube != null && sndCube != null){
    //         firstCube.Move(30, 30, 1000);
    //         sndCube.Move(30, 30, 1000);
    //     }
        
    // }
    int moveCube1 = 0; int moveCube2 = 0; int xc1 = 0; int xc2 = 0;
    void OnCollision(Cube c)
    {
        Debug.Log($"pose = {c.pose.ToString()}");
        c.PlayPresetSound(2);
        Debug.Log($"cube 0 x = {cubes[0].x} y = {cubes[0].y}");
        Debug.Log($"cube 1 x = {cubes[1].x} y = {cubes[1].y}");
        // Debug.Log($"cube 2 x = {cubes[2].x} y = {cubes[2].y}");
        // Debug.Log($"cube 3 x = {cubes[3].x} y = {cubes[3].y}");
        // Debug.Log($"cube 4 x = {cubes[4].x} y = {cubes[4].y}");
        // Debug.Log($"cube 5 x = {cubes[5].x} y = {cubes[5].y}");
        // Debug.Log($"cube 6 x = {cubes[6].x} y = {cubes[6].y}");
        // Debug.Log($"cube 7 x = {cubes[7].x} y = {cubes[7].y}");
        // Debug.Log($"cube 8 x = {cubes[8].x} y = {cubes[8].y}");
        // Debug.Log($"cube 9 x = {cubes[9].x} y = {cubes[9].y}");

    //if (cubeManager.synced){
            // if (connected == 1){
            //     //cubes[1].Move(30, 30, 100);
            //     int x1 = cubes[0].x;
            //     int y1 = cubes[0].y;
            //     int x2 = cubes[1].x;
            //     int y2 = cubes[1].y;
                
            //     int centerX = (x1 + x2) / 2;
            //     int centerY = (y1 + y2) / 2;
                
            //     float radiusAttract = 60;
            //     float radiusOutofBound = 80;
            //     float distanceCube1 = Mathf.Sqrt(Mathf.Pow(x1 - centerX, 2) + Mathf.Pow(y1 - centerY, 2));
            //     float distanceCube2 = Mathf.Sqrt(Mathf.Pow(x2 - centerX, 2) + Mathf.Pow(y2 - centerY, 2));
            //     Debug.Log($"cube1 pos x = {x1} y = {y1}");
            //     Debug.Log($"cube2 pos x = {x2} y = {y2}");
            //     Debug.Log($"center pos x = {centerX} y = {centerY}");
            //     Debug.Log($"distance c1 pos = {distanceCube1} ");
            //     Debug.Log($"distance c2 pos = {distanceCube2} ");

            //     bool isCube1Outside = (distanceCube1 > radiusAttract && distanceCube1 < radiusOutofBound);
            //     bool isCube2Outside = (distanceCube2 > radiusAttract && distanceCube2 < radiusOutofBound);
            //     // Debug.Log($"isCube1Outside = {isCube1Outside} ");
            //     // Debug.Log($"isCube2Outside = {isCube2Outside} ");

            //     if (isCube1Outside && isCube2Outside){
            //         moveCube1 = 1; moveCube2 = 1;
            //         Debug.Log($"cube1 pos x = {x1} y = {y1}");
            //         Debug.Log($"cube2 pos x = {x2} y = {y2}");
            //         Debug.Log($"center pos x = {centerX} y = {centerY}");
            //         Debug.Log($"distance c1 pos = {distanceCube1} ");
            //         Debug.Log($"distance c2 pos = {distanceCube2} ");
                    
            //     }
            //     if (moveCube1 == 1){
            //         if (cubes[0].x >= centerX){
            //             xc1 = centerX + 5;
            //         }
            //         else{
            //             xc1 = centerX - 5;
            //         }
            //         var mv = cubeManager.navigators[0].Navi2Target(xc1, centerY, maxSpd:150).Exec();
            //         if (mv.reached){
            //             moveCube1 = 0;
            //         } 
            //     }

            //     if (moveCube2 == 1){
            //         if (cubes[1].x >= centerX){
            //             xc2 = centerX + 5;
            //         }
            //         else{
            //             xc2 = centerX - 5;
            //         }
            //         var mv = cubeManager.navigators[1].Navi2Target(xc2, centerY, maxSpd:150).Exec();
            //         if (mv.reached){
            //             moveCube2 = 0;
            //         } 
            //     }
            // }
       // }
    }
}
