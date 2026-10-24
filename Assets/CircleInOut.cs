using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System;

public class CircleInOut : MonoBehaviour
{
    CubeManager cm;
    Cube[] cubes;
    int speed = 250;
    bool haptic = false;
    bool incremental = false;
    bool led = true;
    int circleRadius = 60;
    int minRad = 30;
    int maxRad = 180;
    float lastTime = 0;
    int circleNumOfCubes = 5;
    string front = "Front";
    string back = "Back";
    string left = "Left";
    string right = "Right";
    string currPose = "Up";
    bool ledAll = true;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cubes = await cm.MultiConnect(6);
        if (cubes.Length == 6){
            cm.navigators[1].ClearOther();
            cm.navigators[2].ClearOther();
            cm.navigators[3].ClearOther();
            cm.navigators[4].ClearOther();
            cm.navigators[5].ClearOther();
            cubes[0].doubleTapCallback.AddListener("EventScene", OnDoubleTap);
            cubes[0].buttonCallback.AddListener("EventScene", OnPressButton);
            //cubes[0].collisionCallback.AddListener("EventScene", OnCollision);
        }
        
        //cubes[0].poseCallback.AddListener("EventScene", OnPose);
    }

    void OnPressButton(Cube c)
        {
            if (c.isPressed)
            {
                if (incremental){
                    incremental = false;
                    Debug.Log("removed pose listener");
                    cubes[0].poseCallback.RemoveListener("EventScene");
                }
                else {
                    Debug.Log("added pose listener");
                    incremental = true;
                    cubes[0].poseCallback.AddListener("EventScene", OnPose);
                }
            }
        }
        void OnCollision(Cube c)
        {
            //ledAll = true;
        }
    void OnDoubleTap(Cube c)
    {
        c.PlayPresetSound(3);
        if(haptic){
            haptic = false;
            led = true;
            Debug.Log("haptic false");
        }
        else{
            haptic = true;
            led = false;
            Debug.Log("haptic true");
        }
    }
    void OnPose(Cube c)
    {
        Debug.Log("on pose");
        if (c.pose.ToString() == back){
            if (circleRadius < maxRad){
                circleRadius += 30;
            }
            //if (haptic) cubes[0].Move(speed, speed, 1000);
        }
        if (c.pose.ToString() == front){
            if (circleRadius > minRad){
                circleRadius -= 30;
            }
            //if (haptic) cubes[0].Move(speed, speed, 1000);
        }
        GetCirclePositions();
    }
    int cX = 250; int cY = 250;
    int x0 = 334; int y0 = 229; int x1 = 316; int y1 = 312; int x2 = 176; int y2 = 228; int x3 = 257; int y3 = 173; int x4 = 199; int y4 = 311;
     void GetCirclePositions(){
        double circumference = 2 * Math.PI * circleRadius;
        double angleIncrement = 2*Math.PI / circleNumOfCubes;
        if (circleNumOfCubes == 5){
            double angle1 = 0*angleIncrement;
            double angle2 = 1*angleIncrement;
            double angle3 = 2*angleIncrement;
            double angle4 = 3*angleIncrement;
            double angle5 = 4*angleIncrement;
            x0 = (int)(cX + circleRadius * Math.Cos(angle1));
            y0 = (int)(cY + circleRadius * Math.Sin(angle1));
            x1 = (int)(cX + circleRadius * Math.Cos(angle2));
            y1 = (int)(cY + circleRadius * Math.Sin(angle2));
            x2 = (int)(cX + circleRadius * Math.Cos(angle3));
            y2 = (int)(cY + circleRadius * Math.Sin(angle3));
            x3 = (int)(cX + circleRadius * Math.Cos(angle4));
            y3 = (int)(cY + circleRadius * Math.Sin(angle4));
            x4 = (int)(cX + circleRadius * Math.Cos(angle5));
            y4 = (int)(cY + circleRadius * Math.Sin(angle5));
        }
     }
     int moveC0 = 0; int moveC1 = 0; int moveC2 = 0; int moveC3 = 0; int moveC4 = 0;
     void Circle(){
        if (cubes[1].x != x0 || cubes[1].y != y0 ){moveC0 = 1;}
        if (cubes[2].x != x1 || cubes[2].y != y1){moveC1 = 1;}
        if (cubes[3].x != x2 || cubes[3].y != y2){moveC2 = 1;}
        if (cubes[4].x != x3 || cubes[4].y != y3){moveC3 = 1;}
        if (cubes[5].x != x4 || cubes[5].y != y4){moveC4 = 1;}
        if (moveC0 == 1){
            var mv = cm.navigators[1].Navi2Target(x0, y0, maxSpd:speed).Exec();
            if (mv.reached){moveC0 = 0;} 
        }
        if (moveC1 == 1){
            var mv = cm.navigators[2].Navi2Target(x1, y1, maxSpd:speed).Exec();
            if (mv.reached){moveC1 = 0;} 
        }
        if (moveC2 == 1){
            var mv = cm.navigators[3].Navi2Target(x2, y2, maxSpd:speed).Exec();
            if (mv.reached){moveC2 = 0;} 
        }
        if (moveC3 == 1){
            var mv = cm.navigators[4].Navi2Target(x3, y3, maxSpd:speed).Exec();
            if (mv.reached){moveC3 = 0;} 
        }
        if (moveC4 == 1){
            var mv = cm.navigators[5].Navi2Target(x4, y4, maxSpd:speed).Exec();
            if (mv.reached){moveC4 = 0;} 
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if (Time.time-lastTime < 0.25f) return;
        lastTime = Time.time;
        if (cm.synced){
            if(incremental && led){
                cubes[0].TurnLedOn(0,0,255,1000);
            }
            if(!incremental && led){
                cubes[0].TurnLedOn(0,255,0,1000);
            }
            //Debug.Log($"incremental {incremental}");
            if (!incremental){
                if (cubes[0].pose.ToString() == front){
                    if (circleRadius > minRad){
                        circleRadius -= 2;
                    }
                    //if (haptic) cubes[0].Move(speed, speed, 1000);
                }
                if (cubes[0].pose.ToString() == back){
                    if (circleRadius < maxRad){
                        circleRadius += 2;
                    }
                    //if (haptic) cubes[0].Move(speed, speed, 1000);
                }
                GetCirclePositions();
            }
            Circle();
            if (haptic){
                if (moveC0 != 0 || moveC1 != 0 || moveC2 != 0 || moveC3 != 0 || moveC4 != 0){
                    if(moveC0!= 0){Debug.Log($"cube 1x:{cubes[1].x}, y: {cubes[1].y}");}
                    if(moveC1!= 0){Debug.Log($"cube 2x:{cubes[2].x}, y: {cubes[2].y}");}
                    if(moveC2!= 0){Debug.Log($"cube 3x:{cubes[3].x}, y: {cubes[3].y}");}
                    if(moveC3!= 0){Debug.Log($"cube 4x:{cubes[4].x}, y: {cubes[4].y}");}
                    if(moveC4!= 0){Debug.Log($"cube 5x:{cubes[5].x}, y: {cubes[5].y}");}
                    //Debug.Log($"M0 {moveC0} M1 {moveC1} M2 {moveC2} M3 {moveC3} M4 {moveC4} ");
                    cubes[0].Move(speed, speed, 20);
                    
                    
                }
            }
            // if (ledAll && moveC0 == 0 && moveC1 == 0 && moveC2 == 0 && moveC3 == 0 && moveC4 == 0){
            //         //Debug.Log($"M0 {moveC0} M1 {moveC1} M2 {moveC2} M3 {moveC3} M4 {moveC4} ");
            //         cubes[1].TurnLedOn(64, 224, 208, 1000);
            //         cubes[2].TurnLedOn(230, 230, 250, 1000);
            //         cubes[3].TurnLedOn(220, 20, 60, 1000);
            //         cubes[4].TurnLedOn(255, 215, 0, 1000);
            //         cubes[5].TurnLedOn(135, 206, 235, 1000);
            //         ledAll = false;
                    
            // }
        }
    }
}
