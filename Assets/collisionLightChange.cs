using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class collisionLightChange : MonoBehaviour
{
    // Start is called before the first frame update
    CubeManager cubeManager;
    Cube[] cubes;
    Cube cube;
    bool showId = false;
    bool r = true;
    bool b = false;
    bool g = false;
    int ledColour = 1;
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cube = await cubeManager.SingleConnect();
        cube.collisionCallback.AddListener("EventScene", OnCollision);

        await cube.ConfigMotorRead(true);
        await cube.ConfigAttitudeSensor(Cube.AttitudeFormat.Eulers, 100, Cube.AttitudeNotificationType.OnChanged);
        await cube.ConfigMagneticSensor(Cube.MagneticMode.MagnetState);
    }

    void OnCollision(Cube c)
    {
        //cube.PlayPresetSound(2);
        switch (ledColour){
            case 1:
                cube.TurnLedOn(255,0,0,0);
                ledColour = 2;
                break;
            case 2:
                cube.TurnLedOn(0,255,0,0);
                ledColour = 3;
                break;
            case 3:
                cube.TurnLedOn(0,0,255,0);
                ledColour = 1;
                break;
        }
        //cube.TurnLedOn(5,5,5,0);
        StartCoroutine(DelayedRequestSensor(cube));
    }

    private IEnumerator DelayedRequestSensor(Cube cube)
    {
        yield return new WaitForSecondsRealtime(0.05f);
        cube.RequestMotionSensor();
    }
    // Update is called once per frame
    // void Update()
    // {
    //     if (cube != null)
    //     {
    //         cube.TurnLedOn(255,0,0,0);
            
    //     }
    // }
}
