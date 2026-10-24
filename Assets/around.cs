using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.MathUtils;

public class around : MonoBehaviour
{
    CubeManager cm;
    Cube cube;
    int mag = 60;
    float lastTime = 0;
    bool delayMove = false;

    // Start is called before the first frame update
    async void Start()
    {
        Application.targetFrameRate = 30;
        cm = new CubeManager(ConnectType.Real);
        cube = await cm.SingleConnect();
        
    }

    // Update is called once per frame
    void Update()
    {
        var tar1 = Vector.fromRadMag(Time.time/1, mag) + new Vector(250, 250);
        if (Time.time-lastTime > 3.9f){delayMove = true;}
        if (cm.synced && delayMove == true){
            var navi1 = cm.navigators[0];
            var mv1 = navi1.Navi2Target(tar1, maxSpd:100).Exec();
        }
        
    }
}
