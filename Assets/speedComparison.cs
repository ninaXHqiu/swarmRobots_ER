using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio; 

public class speedComparison : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    bool move = false; 
    float lastTime = 0;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(6);
        cubes[0].buttonCallback.AddListener("EventScene", OnPressButton);
        cubes[0].TurnLedOn(0, 255, 0, 0);
        cubes[1].TurnLedOn(255, 0, 255, 0);
        cubes[2].TurnLedOn(0, 0, 255, 0);
        cubes[3].TurnLedOn(255, 255, 0, 0);
        cubes[4].TurnLedOn(255, 100, 0, 0);
        cubes[5].TurnLedOn(255, 0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced){
            if (move == true){
                
                cubes[1].Move(10, 10, 1000);
                cubes[2].Move(63, 63, 1000);
                cubes[3].Move(126, 126, 1000);
                cubes[4].Move(189, 189, 1000);
                cubes[5].Move(255, 255, 1000);
            }
            if (Time.time-lastTime > 0.15f) {
                move = false; 
            }
        }
    }
    void OnPressButton(Cube c)
    {
        if (c.isPressed)
        {
            if (move ==  true){
                move = false; 
            }
            else{
                move = true;
                lastTime = Time.time; 
            } 
        }
        //cube.PlayPresetSound(0);
    }
}
