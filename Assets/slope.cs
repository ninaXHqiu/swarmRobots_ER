using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
 
public class slope : MonoBehaviour
{
    // Start is called before the first frame update
    CubeManager cubeManager;
    Cube cube;
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cube = await cubeManager.SingleConnect();
        cube.slopeCallback.AddListener("EventScene", OnSlope);
    }

    void OnSlope(Cube c)
    {
        Debug.LogFormat("onSlope");
        cube.PlayPresetSound(9);
        cube.TurnLedOn(255,0,0,100);
        cube.Move(30, 30, 20);
    }

    // Update is called once per frame
    // void Update()
    // {
        
    // }
}
