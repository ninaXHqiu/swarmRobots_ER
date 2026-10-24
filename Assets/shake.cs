using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class shake : MonoBehaviour
{
    CubeManager cubeManager;
    Cube cube;
    bool r = true;
    bool b = false;
    bool g = false;
    int ledColour = 1;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cube = await cubeManager.SingleConnect();

        cube.shakeCallback.AddListener("EventScene", OnShake);
    }

    void OnShake(Cube c)
    {
        if (c.shakeLevel >= 1 ){
            //c.PlayPresetSound(8);
            
            switch (ledColour){
                case 1:
                    cube.TurnLedOn(255,0,0,0);
                    ledColour = 2;
                    break;
                case 2:
                    cube.TurnLedOn(0,255,0,0);
                    ledColour = 3;
                    break;
                case 3:
                    cube.TurnLedOn(0,0,255,0);
                    ledColour = 1;
                    break;
            }
            c.Move(250, 250, 30);
        }
    }

    // Update is called once per frame
    // void Update()
    // {
        
    // }
}
