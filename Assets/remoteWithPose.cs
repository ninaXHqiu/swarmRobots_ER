using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class remoteWithPose : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    Cube firstCube;
    Cube sndCube;
    string front = "Front";
    string back = "Back";
    string left = "Left";
    string right = "Right";
    string currPose = "Up";
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(4);
        if (cubes.Length > 1)
        {
            firstCube = cubes[0];
            sndCube = cubes[1];
        }
        // cubes[0].poseCallback.AddListener("EventScene", OnPose);
        // cubes[0].TurnLedOn(0,255,0,1000);
    }

    // Update is called once per frame
    // void OnPose(Cube c)
    // {
    //     Debug.Log($"pose = {c.pose.ToString()}");
    //     //cubes[1].TurnLedOn(0,0,255,100);
    //     //cubes[0].PlayPresetSound(3);
    //     if (c.pose.ToString() == front){
    //         cubes[1].Move(50, 50, 1000);
    //         cubes[2].Move(50, 50, 1000);
    //         cubes[3].Move(50, 50, 1000);
    //         currPose = front;
    //     }
    //     if (c.pose.ToString() == back){
    //         cubes[1].Move(-50, -50, 1000);
    //         cubes[2].Move(-50, -50, 1000);
    //         cubes[3].Move(-50, -50, 1000);
    //         currPose = back;
    //     }
    //     if (c.pose.ToString() == left){
    //         cubes[1].Move(50, -50, 140);
    //         cubes[2].Move(50, -50, 140);
    //         cubes[3].Move(50, -50, 140);
    //         //currPose = back;
    //     }
    //     if (c.pose.ToString() == right){
    //         cubes[1].Move(-50, 50, 140);
    //         cubes[2].Move(-50, 50, 140);
    //         cubes[3].Move(-50, 50, 140);
    //         //currPose = back;
    //     }
    // }

    //Update is called once per frame
    void Update()
    {
        if (firstCube != null && sndCube != null){
            cubes[0].TurnLedOn(0,255,0,1000);
            if (cubes[0].pose.ToString() == front){
                cubes[1].Move(30, 30, 100);
                cubes[2].Move(30, 30, 100);
                currPose = front;
            }
            if (cubes[0].pose.ToString() == back){
                cubes[1].Move(-30, -30, 100);
                cubes[2].Move(-30, -30, 100);
                currPose = back;
            }

            if (cubes[0].pose.ToString() == left){
                cubes[1].Move(10, -10, 50);
                cubes[2].Move(10, -10, 50);
                currPose = back;
            }

            if (cubes[0].pose.ToString() == right){
                cubes[1].Move(-10, 10, 50);
                cubes[2].Move(-10, 10, 50);
                currPose = back;
            }
        }
    }
}
