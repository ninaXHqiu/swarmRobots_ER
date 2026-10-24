using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class SpeedTest : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    Cube firstCube;
    Cube sndCube;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(2);
        // if (cubes.Length > 1)
        // {
        //     firstCube = cubes[0];
        //     sndCube = cubes[1];
        // }
        cubes[0].collisionCallback.AddListener("EventScene", OnCollision);
        cubes[1].collisionCallback.AddListener("EventScene", OnCollision2);
    }

    // Update is called once per frame
    // void Update()
    // {
    //     if (firstCube != null && sndCube != null){
    //         firstCube.Move(255, 255, 2000);
    //         sndCube.Move(170, 170, 2000);
    //     }
    // }
    void OnCollision(Cube c)
    {

        c.TurnLedOn(255,0,0,2000);
        c.Move(255, 255, 1000);
    }
    void OnCollision2(Cube c)
    {
        c.TurnLedOn(0,255,0,2000);
        c.Move(150, 150, 1000);
    }
}
