using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.MathUtils;
using toio.Navigation;

public class MoveInCircle : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int speed = 60;
    int minRad = 20;
    int maxRad = 80;
    int rad = 60;
    int tm = 0;
    bool connected = false;
    public Navigator.Mode naviMode = Navigator.Mode.BOIDS;
    // Start is called before the first frame update
    async void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(1);
        //Debug.Assert(cubeManager.navigators.Count>1, "Need at least 2 cubes.");
        if (cubes.Length == 1){connected = true;}
        foreach (var navi in cubeManager.navigators)
        {
            navi.usePred = true;
            navi.mode = naviMode;
            // navi.avoid.useSafety = false;
        }
        //cubeManager.navigators[0].usePred = true;
        //cubes[1].buttonCallback.AddListener("EventScene", OnPressButton);
    }
    // void OnPressButton(Cube c)
    // {
    //     if (c.isPressed)
    //     {
    //         if (rad < maxRad){
    //             rad += 10;
    //             if (rad == 20){c.TurnLedOn(127,0,255,1000);}
    //             if (rad == 30){c.TurnLedOn(75,0,130,1000);}
    //             if (rad == 40){c.TurnLedOn(0,0,255,1000);}
    //             if (rad == 50){c.TurnLedOn(0,255,0,1000);}
    //             if (rad == 50){c.TurnLedOn(255,255,0,1000);}
    //             if (rad == 70){c.TurnLedOn(255,165,0,1000);}
    //             if (rad == 80){c.TurnLedOn(255,0,0,1000);}
    //         }
    //         else{
    //             rad = minRad;
    //             c.TurnLedOn(127,0,255,1000);
    //         }
    //         //cube.Move(speed, speed, 20);
    //     }
    //     // if (haptic){
    //     //     c.Move(speed, speed, 20);
    //     // }
    //     // if (sound){
    //     //     c.PlayPresetSound(0);
    //     // }
    // }

    // Update is called once per frame
    void Update()
    {
        // print Time.time;
        Debug.Log(Time.time);
        tm += 1;
        var tar = Vector.fromRadMag(tm/1, rad) + new Vector(250, 250);
        if (cubeManager.synced && connected){
            // cubeManager.navigators[0].mode = naviMode;
            // var mv1 = cubeManager.navigators[0].Navi2Target(tar, maxSpd:speed).Exec();
            for (int i=0; i<cubeManager.navigators.Count; i++)
            {
                var navi = cubeManager.navigators[i];
                navi.mode = naviMode;
                var mv = navi.Navi2Target(tar, maxSpd:60).Exec();
            }
        }
    }
}
