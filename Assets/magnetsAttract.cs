using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System.Threading.Tasks;

public class magnetsAttract : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int x1, y1, x2, y2;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(2);
        await Task.Delay(100);
        // cubes initial location
        x1 = 252;
        y1 = 248;
        x2 = 242;
        y2 = 248;

        // string message = $"Cube 1 initial X: {x1}, Y: {y1} \t Cube 2 initial X: {x2}, Y: {y2}";
        // Debug.Log(message);
        // By default, each navigator is able to see all others
        // But you can also manually make a navigator "blind"
        cubeManager.navigators[0].ClearOther();
        cubeManager.navigators[1].ClearOther();

        //cubeManager.navigators[1].usePred = true;
    }

    int moveCube1 = 0; int moveCube2 = 0;
    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced)
        {
            if (cubes[0].x != x1 || cubes[0].y != y1 ){
                moveCube1 = 1;
            }
            if (cubes[1].x != x2 || cubes[1].y != y2){
                moveCube2 = 1;
            }

            if (moveCube1 == 1){
                var mv = cubeManager.navigators[0].Navi2Target(x1, y1, maxSpd:150).Exec();
                if (mv.reached){
                    moveCube1 = 0;
                    // var rawX = cubes[0].x;
                    // var rawY = cubes[0].y;
                    // string message2 = $"Phase 1 X: {rawX}, Y: {rawY}";
                    // Debug.Log(message2);
                } 
            }

            if (moveCube2 == 1){
                var mv = cubeManager.navigators[1].Navi2Target(x2, y2, maxSpd:150).Exec();
                if (mv.reached){
                    moveCube2 = 0;
                } 
            }
        }
    }
}
