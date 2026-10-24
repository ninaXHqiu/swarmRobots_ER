using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
public class Toss : MonoBehaviour
{
    CubeManager cm;
    Cube cube;
    bool sound = true;
    bool haptic = false;
    bool led = false; 
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
        if(sound){
            sound = false;
            haptic = true;
            cube.TurnLedOn(255, 0, 0, 200);
        }
        else{
            haptic = false;
            sound = true;
            cube.TurnLedOn(0, 255,0,200);
        }
        }
    }

    void OnSlope(Cube c)
    {
        cube.PlayPresetSound(1);
        cube.TurnLedOn(0, 255, 0, 100);
    }

    void OnShake(Cube c)
    {
        if (c.shakeLevel > 1){
            if (sound) c.PlayPresetSound(1);
            if (haptic) c.Move(200, 200, 100);
            if (led) c.TurnLedOn(0,65,255,100);
        }
    }
}
