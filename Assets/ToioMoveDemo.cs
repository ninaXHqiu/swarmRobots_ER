using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class ToioMoveDemo : MonoBehaviour
{
    Cube[] cubes;
    CubeManager cm;
    bool started = false;
    int phase = -1;
    float lastTime = 0;
    int speed = 80;
    int cubesReached = 0;
    int done0, done1;
    int moveC1 = 0;
    int moveC2 = 0;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cubes = await cm.MultiConnect(2);

        foreach (var cube in cubes)
        {
            cube.targetMoveCallback.AddListener("Sample_Motor", TargetMoveCallback);
        }
        cubes[0].TurnLedOn(255, 0, 0, 0); // Red
        cm.navigators[1].cube.TurnLedOn(0, 255, 0, 0); // Green
        started = true;
        
    }
    void TargetMoveCallback(Cube cube, int configID, Cube.TargetMoveRespondType response)
    {
        Debug.LogFormat("Cube#{0}'s TargetMove ends with response code: {1}", configID, (byte)response);
        phase ++;
    }

    // Update is called once per frame
    void Update()
    {   
        if (!started) return;
        if (cubes.Length==0) return;

        if (Time.time-lastTime < 0.05f) return;
        lastTime = Time.time;
        if(done0 !=0 && done1 != 0){
                phase = 1;
            }
        switch (phase)
        {
            case -1:    // Start TargetMove
            {
                //https://toio.github.io/toio-spec/en/docs/ble_motor/#motor-control-with-target-specified
                Debug.Log("====== Start TargetMove ======");
                if (cubes[0].x > cubes[1].x)    // cubes[0] on the right, so goes to right target
                {                                      // 0->right, 90->down, 180->left, 270->up
                    cubes[0].TargetMove(targetX:270, targetY:250, targetAngle:270, configID:0);
                    cubes[1].TargetMove(targetX:230, targetY:250, targetAngle:90, configID:1);
                }
                else                            // cubes[0] on the left, so goes to left target
                {
                    cubes[1].TargetMove(targetX:270, targetY:250, targetAngle:270, configID:0);
                    cubes[0].TargetMove(targetX:230, targetY:250, targetAngle:90, configID:1);
                }
                phase = 0;
                break;
            }
            
            case 1: case 2: // TargetMove ends & Start AccelerationMove
            {
                if (phase==cubes.Length)    // if all cubes' TargetMove end.
                {
                    foreach (var cube in cubes)
                        // Immediately set speed to 30 by setting acceleration to 0
                        cube.AccelerationMove(targetSpeed:30, acceleration:0, rotationSpeed:-100, controlTime:0);
                    phase = 3;
                }
                break;
            }
            case 3:     // Start AccelerationMove
            {
                Debug.Log("====== Start AccelerationMove ======");
                foreach (var cube in cubes)
                    // Accelerate from speed 30 to 115 by 2 every 100ms
                    cube.AccelerationMove(targetSpeed:115, acceleration:2, rotationSpeed:-100, controlTime:0);
                phase ++;
                break;
            }
            case 4:     // End AccelerationMove
            {
                bool allOut = false;
                foreach (var cube in cubes)
                {
                    if ( (cube.x-250)*(cube.x-250)+(cube.y-250)*(cube.y-250) > 140*140 ) allOut = true;
                }
                if (allOut) phase ++;
                break;
            }
            case 5: 
            {// By default, each navigator is able to see all others
             // But you can also manually make a navigator "blind"
                // cm.navigators[0].ClearOther();
                // cm.navigators[1].ClearOther();
                // cm.navigators[0].AddOther(cm.navigators[1]); // to make it see
                // cm.navigators[1].AddOther(cm.navigators[0]);
                Debug.Log("====== Navigator Move ======");
                moveC1 = 1;
                moveC2 = 1;
                break;
            }
        }
        foreach (var navigator in cm.syncNavigators){
            
            if(moveC1 == 1){
                var mv1 = cm.navigators[0].Navi2Target(150, 150, maxSpd:speed).Exec();
                if (mv1.reached){
                    moveC1 = 2;
                }
            }
            if(moveC2 == 1){
                var mv2 = cm.navigators[1].Navi2Target(350, 350, maxSpd:speed).Exec();
                if (mv2.reached){
                    moveC2 = 2;
                }
            }
        }
        if (moveC1 == 2 && moveC2 == 2){
            phase = -1;
            moveC1 = 0;
            moveC2 = 0;
        } 
    }
}
