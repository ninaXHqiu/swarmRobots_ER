using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class buttonPress : MonoBehaviour
{
    CubeManager cubeManager;
    Cube cube;
    bool r = true;
    bool b = false;
    bool g = false;
    int ledColour = 1;
    bool haptic = true;
    bool sound = false;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cube = await cubeManager.SingleConnect();
        cube.buttonCallback.AddListener("EventScene", OnPressButton);
    }

    // Update is called once per frame
    void OnPressButton(Cube c)
    {
        if (c.isPressed)
        {
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
            cube.Move(250, 250, 20);
        }
        //cube.PlayPresetSound(0);
        cube.Move(250, 250, 20);
        
        //cube.Move(250, 250, 20);
    }
}
