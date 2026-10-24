using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using toio;

public class springLoadedCar : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int x1, y1, x2, y2;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(1);
        await Task.Delay(100);
        // cubes initial location
        x1 = cubes[0].x;
        y1 = cubes[0].y;
    }

    int moveCube1 = 0; int cubeMoved = 0;
    // Update is called once per frame
    void Update()
    {
        
        if (cubeManager.synced)
        {
            Debug.Log($"cube1 pos x = {cubes[0].x} y = {cubes[0].y}");
            if(cubes[0].y != 0){
                if (cubes[0].y != y1 && moveCube1 == 0 ){
                    moveCube1 = 1;
                }

                if (moveCube1 == 1){
                    //cubes[0].Move(100, 100, 300);
                    var mv = cubeManager.navigators[0].Navi2Target(cubes[0].x, y1, maxSpd:150).Exec();
                    if (mv.reached){
                        moveCube1 = 0;
                    } 
                    
                }
            }
        }
    }
}
    