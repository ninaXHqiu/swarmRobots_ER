using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class speedControlOnTap : MonoBehaviour
{
    CubeManager cubeManager;
    Cube cube;
    Cube[] cubes;
    int l1 = 85;
    int l2 = 170;
    int l3 = 255;
    int speed = 85;
    string front = "Front";
    string back = "Back";
    string left = "Left";
    string right = "Right";
    string currPose = "Up";
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        //cube = await cubeManager.SingleConnect();
        cubes = await cubeManager.MultiConnect(2);
        cubes[0].doubleTapCallback.AddListener("EventScene", OnDoubleTap);
        cubes[0].poseCallback.AddListener("EventScene", OnPose);
        cubes[0].TurnLedOn(0,255,0,1000);
        cubes[1].doubleTapCallback.AddListener("EventScene", OnDoubleTap2);
        cubes[1].poseCallback.AddListener("EventScene", OnPose2);
        cubes[0].collisionCallback.AddListener("EventScene", OnCollision);
        cubes[1].collisionCallback.AddListener("EventScene", OnCollision);
        
    }

    // Update is called once per frame
    // void Update()
    // {
        
    // }

    void OnDoubleTap(Cube c)
    {
        switch (speed){
            case 85:
                c.PlayPresetSound(6);
                c.TurnLedOn(255,80,0,1000);
                speed = 170;
                break;
            case 170:
                c.PlayPresetSound(9);
                c.TurnLedOn(255,0,0,1000);
                speed = 255;
                break;
            case 255:
                c.PlayPresetSound(3);
                c.TurnLedOn(255,255,0,1000);
                speed = 85;
                break;
        }
        
    }

    void OnDoubleTap2(Cube c)
    {
        switch (speed){
            case 85:
                c.PlayPresetSound(6);
                c.TurnLedOn(255,80,0,1000);
                speed = 170;
                break;
            case 170:
                c.PlayPresetSound(9);
                c.TurnLedOn(255,0,0,1000);
                speed = 255;
                break;
            case 255:
                c.PlayPresetSound(3);
                c.TurnLedOn(255,255,0,1000);
                speed = 85;
                break;
        }
        
    }

    

    void OnPose(Cube c)
    {
        Debug.Log($"pose = {c.pose.ToString()}");
        //cubes[1].TurnLedOn(0,0,255,100);
        //cubes[0].PlayPresetSound(3);
        if (c.pose.ToString() == front){
            cubes[1].Move(speed, speed, 1000);
            currPose = front;
        }
        if (c.pose.ToString() == back){
            cubes[1].Move(-speed, -speed, 1000);
            currPose = back;
        }
        if (c.pose.ToString() == left){
            cubes[1].Move(50, -50, 140);
            //currPose = back;
        }
        if (c.pose.ToString() == right){
            cubes[1].Move(-50, 50, 140);
            //currPose = back;
        }
    }
    void OnPose2(Cube c)
    {
        Debug.Log($"pose = {c.pose.ToString()}");
        //cubes[1].TurnLedOn(0,0,255,100);
        //cubes[0].PlayPresetSound(3);
        if (c.pose.ToString() == front){
            cubes[0].Move(speed, speed, 1000);
        }
        if (c.pose.ToString() == back){
            cubes[0].Move(-speed, -speed, 1000);
        }
        if (c.pose.ToString() == left){
            cubes[0].Move(50, -50, 140);
        }
        if (c.pose.ToString() == right){
            cubes[0].Move(-50, 50, 140);
        }
    }
    void OnCollision(Cube c)
    {

        if (speed == 85){
            c.TurnLedOn(255,255,0,1000);
        }
        if (speed == 170){
            c.TurnLedOn(255,80,0,1000);
        }
        if (speed == 255){
            c.TurnLedOn(255,0,0,1000);
        }
    }
}
