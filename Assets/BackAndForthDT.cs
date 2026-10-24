using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class BackAndForthDT : MonoBehaviour
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
        cube.collisionCallback.AddListener("EventScene", OnDoubleTap);
    }
    void OnDoubleTap(Cube c)
    {
        start = true;
        lastTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (cm.synced){
            if (start){
                if(phase ==0){ cube.Move(150, -150, 300); phase ++;}
                
                if (phase == 1 && Time.time-lastTime > 0.4f){ cube.Move(-150, 150, 300); phase ++; }
                if (phase == 2 && Time.time-lastTime > 0.8f){ cube.Move(150, -150, 300); phase ++;}
                if (phase == 3 && Time.time-lastTime > 1.2f){ cube.Move(-150, 150, 300); phase = 0; start = false;}

            }
            if (!start){
                cube.TurnLedOn(255,100,255, 500);
            }
            
        }
    }
}
