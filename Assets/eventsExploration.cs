using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class eventsExploration : MonoBehaviour
{
    CubeManager cubeManager;
    Cube cube;
    Cube[] cubes;
    bool showId = false;
    // Start is called before the first frame update
    async void Start()
    {
        // var peripheral = await new NearestScanner().Scan();
        // cube = await new CubeConnecter().Connect(peripheral);
        cubeManager = new CubeManager(ConnectType.Real);
        //cube = await cubeManager.SingleConnect();
        cubes = await cubeManager.MultiConnect(2);
        // コールバック登録
        // cube.buttonCallback.AddListener("EventScene", OnPressButton);
        // cube.slopeCallback.AddListener("EventScene", OnSlope);
        // cube.collisionCallback.AddListener("EventScene", OnCollision);
        // cube.idCallback.AddListener("EventScene", OnUpdateID);
        // cube.standardIdCallback.AddListener("EventScene", OnUpdateStandardID);
        // cube.idMissedCallback.AddListener("EventScene", OnMissedID);
        // cube.standardIdMissedCallback.AddListener("EventScene", OnMissedStandardID);

        // 2.1.0
        //cube.poseCallback.AddListener("EventScene", OnPose);
        cubes[0].doubleTapCallback.AddListener("EventScene", OnDoubleTap);
        cubes[1].doubleTapCallback.AddListener("EventScene", OnDoubleTap);

        // 2.2.0
        //cube.shakeCallback.AddListener("EventScene", OnShake);
        //cube.motorSpeedCallback.AddListener("EventScene", OnMotorSpeed);
        //cube.magnetStateCallback.AddListener("EventScene", OnMagnetState);

        // 2.3.0
        // cube.magneticForceCallback.AddListener("EventScene", OnMagneticForce);
        // cube.attitudeCallback.AddListener("EventScene", OnAttitude);

        // Enable Sensors
        // await cube.ConfigMotorRead(true);
        // await cube.ConfigAttitudeSensor(Cube.AttitudeFormat.Eulers, 100, Cube.AttitudeNotificationType.OnChanged);
        // await cube.ConfigMagneticSensor(Cube.MagneticMode.MagnetState);
    }

    // Update is called once per frame
    // void Update()
    // {
        
    // }
    // void OnCollision(Cube c)
    // {
    //     cube.PlayPresetSound(2);
    //     StartCoroutine(DelayedRequestSensor(cube));
    // }

    // private IEnumerator DelayedRequestSensor(Cube cube)
    // {
    //     yield return new WaitForSecondsRealtime(0.05f);
    //     cube.RequestMotionSensor();
    // }

    // void OnSlope(Cube c)
    // {
    //     Debug.LogFormat("onSlope");
    //     //cube.PlayPresetSound(9);
    //     cube.TurnLedOn(255,0,0,100);
    //     cube.Move(20, 20, 20);
    // }

    void OnPressButton(Cube c)
    {
        if (c.isPressed)
        {
            showId = !showId;
        }
        //cube.PlayPresetSound(0);
        cube.Move(70, 0, 50);
    }

    // void OnUpdateID(Cube c)
    // {
    //     if (showId)
    //     {
    //         Debug.LogFormat("pos=(x:{0}, y:{1}), angle={2}", c.pos.x, c.pos.y, c.angle);
    //     }
    // }

    // void OnUpdateStandardID(Cube c)
    // {
    //     if (showId)
    //     {
    //         Debug.LogFormat("standardId:{0}, angle={1}", c.standardId, c.angle);
    //     }
    // }

    // void OnMissedID(Cube cube)
    // {
    //     Debug.LogFormat("Postion ID Missed.");
    // }

    // void OnMissedStandardID(Cube c)
    // {
    //     Debug.LogFormat("Standard ID Missed.");
    // }

    // void OnPose(Cube c)
    // {
    //     Debug.Log($"pose = {c.pose.ToString()}");
    //     cube.TurnLedOn(0,0,255,100);
    //     cube.PlayPresetSound(3);
    // }

    void OnDoubleTap(Cube c)
    {
        c.PlayPresetSound(3);
        cube.TurnLedOn(0,0,255,100);
        cube.Move(250, -250, 500);
    }

    // void OnShake(Cube c)
    // {
    //     if (c.shakeLevel >= 1 )
    //         //c.PlayPresetSound(4);
    //         cube.TurnLedOn(0,255,0,100);
    // }

    // void OnMotorSpeed(Cube c)
    // {
    //     Debug.Log($"motor speed: left={c.leftSpeed}, right={c.rightSpeed}");
    // }

    // void OnMagnetState(Cube c)
    // {
    //     Debug.Log($"magnet state: {c.magnetState.ToString()}");
    // }

    // void OnMagneticForce(Cube c)
    // {
    //     Debug.Log($"magnetic force = {c.magneticForce}");
    // }

    // void OnAttitude(Cube c)
    // {
    //    // Debug.Log($"attitude = {c.eulers}");
    // }
}
