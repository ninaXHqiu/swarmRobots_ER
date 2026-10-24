using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.MathUtils;

public class figure8DT : MonoBehaviour
{
    int speed = 150;
    CubeManager cm;
    Cube[] cubes;
    bool move = false;
    int mag = 0;
    int count = 1;
    int x = 0;
    int y = 0;
    bool increment = true;
    bool circle = false;
    int phase = 0;
    float lastTime = 0;


    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cubes = await cm.MultiConnect(1);
        if (cubes.Length == 1){
            cubes[0].doubleTapCallback.AddListener("EventScene", OnDoubleTap);
        }
        foreach (var cube in cubes)
        {
            cube.targetMoveCallback.AddListener("Sample_Motor", TargetMoveCallback);
            move = false;
            phase = 0;
        }
       // started = true;
        
    }
    void TargetMoveCallback(Cube cube, int configID, Cube.TargetMoveRespondType response)
    {
        Debug.LogFormat("Cube#{0}'s TargetMove ends with response code: {1}", configID, (byte)response);
        if (response == 0){
            move = false;
            Debug.Log($"move {move}");
        }
        
    }
    void OnDoubleTap(Cube c)
    {
        x = cubes[0].x;
        y = cubes[0].y;
        //cubes[0].TargetMove(targetX:250, targetY:250, targetAngle:0, configID:0);
        c.PlayPresetSound(3);
        if (move){
            move = false;
        }
        else{
            lastTime = Time.time;
            move = true;
            phase = 1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cm.synced){
            if (move){
                switch(phase){
                    case 1:
                    {
                        Debug.Log("case 1");
                        cubes[0].AccelerationMove(targetSpeed:60, acceleration:0, rotationSpeed:150, controlTime:0);
                        if (Time.time-lastTime > 2.45f){phase ++;lastTime = Time.time;}
                        break;
                    }
                    case 2:
                    {
                        Debug.Log("case 2");
                        cubes[0].AccelerationMove(targetSpeed:60, acceleration:0, rotationSpeed:-150, controlTime:0);
                         if (Time.time-lastTime > 2.45f){phase ++;}
                         break;
                    }
                    case 3:
                    {
                        Debug.Log("case 3");
                        move = false;
                        phase = 0;
                        cubes[0].Move(10,10,10);
                        break;
                    }
                       
                // }
                // if (increment){count ++;}
                // if (!increment){count --;}
                // var tar1 = Vector.fromRadMag(Time.time/1, mag + count) + new Vector(x, y);
                // var navi1 = cm.navigators[0];
                // var mv1 = navi1.Navi2Target(tar1, maxSpd:speed).Exec();
                
                // if (count == 100){
                //     increment = false;
                //     //move = false;
                // }
                // if (count == 1 ){
                //     //increment = true;
                //     move = false;
                // }
                //cubes[0].AccelerationMove(targetSpeed:60, acceleration:0, rotationSpeed:150, controlTime:0);
                }
            }
        }
    }
}
