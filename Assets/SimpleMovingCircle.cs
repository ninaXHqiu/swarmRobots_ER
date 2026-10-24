using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.Navigation;
using toio.MathUtils;

public class SimpleMovingCircle : MonoBehaviour
{
    bool haptic = false; 
    bool colour = true;
     CubeManager cm;
     int N = 6;
     int speed = 35;
     int mag = 160;
     Cube[] cubes;
     string front = "Front";
    string back = "Back";
    bool move = false;

    //public Navigator.Mode naviMode = Navigator.Mode.AVOID;
    // Start is called before the first frame update
    async void Start()
    {
         QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        cm = new CubeManager(ConnectType.Real);
        cubes = await cm.MultiConnect(N);

        foreach (var navi in cm.navigators)
        {
            navi.usePred = true;
            navi.ClearOther();
            //navi.mode = naviMode;
            // navi.avoid.useSafety = false;
        }
        cubes[0].buttonCallback.AddListener("EventScene", OnPressButton);
        cubes[0].poseCallback.AddListener("EventScene", OnPose);
        
    }
    void OnPose(Cube c)
        {
        //    if (cubes[0].pose.ToString() == front){
        //     if (speed > 35){
        //         speed -= 10;
        //     }
        //    }
           if (cubes[0].pose.ToString() == front){
            if (speed < 65){
                speed += 10;
            }
            else{
                speed = 35;
            }
           }
        }
     void OnPressButton(Cube c)
        {
            if (c.isPressed)
            {
                if (!move){
                    move = true;
                }
                else{
                    move = false;
                }
                // if (speed < 65){
                //     speed += 10;
                // }
                // else{
                //     speed = 35;
                // }
                // if (mag < 200){
                //     mag += 20;
                // }
                // else{
                //     mag = 60;
                // }
            }
        }

    // Update is called once per frame
    void Update()
    {
        //var tar = Vector.fromRadMag(Time.time/1, 100) + new Vector(250, 250);
        //var navi = cm.navigators[0];
        //var mv = navi.Navi2Target(tar, maxSpd:30).Exec();
        
        var tar1 = Vector.fromRadMag(Time.time/1, mag) + new Vector(250, 250);
        var tar2 = Vector.fromRadMag((Time.time/1)+5, mag) + new Vector(250, 250);
        var tar3 = Vector.fromRadMag((Time.time/1)+10, mag) + new Vector(250, 250);
        var tar4 = Vector.fromRadMag((Time.time/1)+15, mag) + new Vector(250, 250);
        var tar5 = Vector.fromRadMag((Time.time/1)+20, mag) + new Vector(250, 250);
        if (cm.synced){
            if (move){
                var navi1 = cm.navigators[1];
                var mv1 = navi1.Navi2Target(tar1, maxSpd:speed).Exec();

                var navi2 = cm.navigators[2];
                var mv2 = navi2.Navi2Target(tar2, maxSpd:speed).Exec();

                var navi3 = cm.navigators[3];
                var mv3 = navi3.Navi2Target(tar3, maxSpd:speed).Exec();

                var navi4 = cm.navigators[4];
                var mv4 = navi4.Navi2Target(tar4, maxSpd:speed).Exec();

                var navi5 = cm.navigators[5];
                var mv5 = navi5.Navi2Target(tar5, maxSpd:speed).Exec();
            }
            if (!move && !colour) cubes[0].TurnLedOn(100, 0, 200, 100);
            if (!move && colour) cubes[0].TurnLedOn(100, 0, 200, 100);
            if (move){
                if (speed == 35){
                    if(colour) cubes[0].TurnLedOn(0, 0, 255, 100);
                    if(haptic) cubes[0].Move(35, 35, 100);
                }
                if (speed == 45){ 
                    if(colour) cubes[0].TurnLedOn(100, 0, 200, 100);
                    if(haptic) cubes[0].Move(45, 45, 100, Cube.ORDER_TYPE.Strong);
                }
                if (speed == 55){
                    if(colour) cubes[0].TurnLedOn(0, 255, 0, 100);
                    if(haptic) cubes[0].Move(55, 55, 100);
                }
                if (speed == 65){
                    if(colour) cubes[0].TurnLedOn(200, 0, 0, 100);
                    if(haptic) cubes[0].Move(65, 65, 100);
                }
            }
            
        }
    }
}