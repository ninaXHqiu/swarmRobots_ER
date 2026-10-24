using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class MotorPatterns : MonoBehaviour
{
    CubeManager cm;
    Cube cube;
    bool sound = true;
    bool haptic = false;
    bool led = false; 
    bool start = false;
    float lastTime = 0;
    bool first = true;
    int phase=0;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cube = await cm.SingleConnect();  
        //cube.slopeCallback.AddListener("EventScene", OnSlope);  
        cube.shakeCallback.AddListener("EventScene", OnShake);  
        cube.buttonCallback.AddListener("EventScene", OnPressButton); 
    }
    void OnPressButton(Cube c)
    {
        if (c.isPressed){
        if(first){
            first = false;
            cube.TurnLedOn(255, 0, 0, 200);
        }
        else{
            first = true;
            cube.TurnLedOn(0, 255,0,200);
        }
        }
    }


    void OnShake(Cube c)
    {
        if (c.shakeLevel > 1){
            Debug.Log("shake");
            start = true;
            lastTime = Time.time;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cm.synced){
            if (start && first){
                if(phase ==0){ cube.Move(238, 238, 300); phase ++;}
                
                if (phase == 1 && Time.time-lastTime > 0.4f){ cube.Move(238, 238, 60); phase ++;}
                if (phase == 2 && Time.time-lastTime > 0.7f){ cube.Move(238, 238, 60); phase ++;}
                if (phase == 3 && Time.time-lastTime > 0.9f){ cube.Move(238, 238, 300); phase = 0; start = false;}

            }
            if (start && !first){
                if(phase ==0){ cube.Move(238, 238, 300); phase ++;}
                if (phase == 1 && Time.time-lastTime > 0.48f){ cube.Move(238, 238, 50); phase ++;}
                if (phase == 2 && Time.time-lastTime > 0.78f){ cube.Move(238, 238, 50); phase ++;}
                if (phase == 3 && Time.time-lastTime > 0.98f){ cube.Move(238, 238, 300); phase ++;}
                if (phase == 4 && Time.time-lastTime > 1.6f){ cube.Move(238, 238, 300); phase ++;}
                if (phase == 5 && Time.time-lastTime > 2.1f){ cube.Move(238, 238, 400); phase = 0; start = false;}

            }
        }
        
    }
}
