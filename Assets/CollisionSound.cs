using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class CollisionSound : MonoBehaviour
{
    CubeManager cm;
    Cube cube;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cube = await cm.SingleConnect();  
        cube.collisionCallback.AddListener("EventScene", OnDoubleTap);
    }
   void OnDoubleTap(Cube c)
        {
            cube.PlayPresetSound(7);
        }
}
