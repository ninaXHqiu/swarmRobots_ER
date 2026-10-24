using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class PlaceAndSpin : MonoBehaviour
{
    CubeManager cm;
    Cube[] cube;
    int x = 0;
    int y = 0;
    float lastTime = 0;
    int move = 0;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cube = await cm.MultiConnect(1); 
        if (cube.Length == 1){
            Debug.Log($"at start initial position x {x} y{y}");
            x = cube[0].x;
            y = cube[0].y;
        }
        lastTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if(cm.synced && x != 0 && y != 0){
            if(cube[0].x != x || cube[0].y != y){
                move = 1;
            }
        }
        if (Time.time-lastTime > 2.05f && cm.synced){
            x = cube[0].x;
            y = cube[0].y;
            Debug.Log($"get initial position x {x} y{y}");
            lastTime = Time.time;
            // if(cube[0].x != x || cube[0].y != y){
            //     move = 1;
            //     Debug.Log($" move {move}");
            // }
        }
        if(move == 1){
            Debug.Log($" move is 1 {move}");
            cube[0].Move(30, -30, 100);
            move = 0;
            Debug.Log($" move is 0 {move}");
        }
    }
}
