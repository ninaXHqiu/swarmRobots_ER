using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System.Threading.Tasks;

// Rotation on double tap, Collision
// Shake causes problems: when the cube is spinning it detects a shake
public class DoubleTapRotate : MonoBehaviour
{
    CubeManager cubeManager;
    Cube cube;

    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cube = await cubeManager.SingleConnect();
        cube.doubleTapCallback.AddListener("EventScene", OnDoubleTap);
        //cube.collisionCallback.AddListener("EventScene", OnCollision);
        //cube.shakeCallback.AddListener("EventScene", OnShake);
        
    }

    // async void OnShake(Cube c)
    // {
    //     Debug.Log($"shakeLevel = {c.shakeLevel}");
    //     int spins = c.shakeLevel * 30;
    //     await Task.Delay(1000);
    //     cube.Move(100, -100, spins);
    // }

    void OnDoubleTap(Cube c)
    {
        //c.PlayPresetSound(3);
        cube.TurnLedOn(0,0,255,100);
        cube.Move(250, -250, 200);
    }
    // void OnCollision(Cube c)
    // {
    //     Debug.Log("collision");
    //     //cube.PlayPresetSound(2);
    //     cube.Move(100, -100, 230);
    // }
}
