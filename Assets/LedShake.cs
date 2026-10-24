using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class LedShake : MonoBehaviour
{
    CubeManager cm;
    Cube cube;
    bool start = false;
    float lastTime = 0;
    int phase = 0;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cube = await cm.SingleConnect();  
        //cube.slopeCallback.AddListener("EventScene", OnSlope);  
        cube.shakeCallback.AddListener("EventScene", OnShake);  
    }
    void OnShake(Cube c)
    {
        if (c.shakeLevel > 1){
            // cube.PlayPresetSound(6);
            // c.Move(255, 255, 475);
            start = true;
            lastTime = Time.time;
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cm.synced){
            if (start){
                if(phase ==0){ cube.PlayPresetSound(6); phase ++;}
                if (phase == 1 && Time.time-lastTime > 0.2f){ cube.Move(255, 255, 475); phase = 0; start = false;}
            }
        }
    }
}
