using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

public class FlickWave : MonoBehaviour
{
    CubeManager cm;
    Cube[] cubes;
    float delayTime = 0.5f;
    float delay = 0.6f;
    float timer = 0.0f;float timer2 = 0.0f;
    int x = 0; int y = 0;
    int speed = 50; bool start = false;
    int retC0 = 0; int wave = 0; bool called = false;int wave2 = 0; int wave3 = 0;
    bool fC1 = false; bool fC2 =false; bool fC3;
    // Start is called before the first frame update
    async void Start()
    {
        cm = new CubeManager(ConnectType.Real);
        cubes = await cm.MultiConnect(4);
        if (cubes.Length == 4)
        {
            x = cubes[0].x;
            y = cubes[0].y;
            Debug.Log($"x y {x} {y}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (start){timer += Time.deltaTime;timer2 += Time.deltaTime;}
        if (cm.synced){
            if (cubes[0].x > x+10 ||cubes[0].x < x-10 || cubes[0].y > y+10 || cubes[0].y < y-10){
                Debug.Log($"location check {cubes[0].x} {cubes[0].y}");
                retC0 = 1;
                if (wave == 0 && !called){wave = 1; called = true; start = true;}
                
                
            }

            if(retC0 == 1){
                var mv0 = cm.navigators[0].Navi2Target(x, y, maxSpd:speed).Exec();
                if (mv0.reached){retC0 = 0; called = false;}
            }
            if(wave == 1){
                Debug.Log($"wave 1");
                if (fC1 == false){
                    cubes[1].Move(50, 50, 600, Cube.ORDER_TYPE.Strong);
                    fC1 = true;
                }
                //cubes[1].Move(50, 50, 600, Cube.ORDER_TYPE.Strong);
                
                if(timer2 >= delay){
                    cubes[1].Move(-50, -50, 600);
                    wave=0;
                    fC1 = false;
                }
                
                wave2 = 1;
            }
            if(wave2 == 1 && (timer >= delayTime)){
                Debug.Log($"wave 2");
                if (fC2 == false){
                    cubes[2].Move(50, 50, 600, Cube.ORDER_TYPE.Strong);
                    fC2 = true;
                }
                //cubes[2].Move(50, 50, 600, Cube.ORDER_TYPE.Strong);
                if(timer2 >= delay+delayTime){
                    cubes[2].Move(-50, -50, 600);
                    wave2=0;
                    fC2 = false;
                    // start = false;
                    // timer = 0.0f;
                    // timer2 = 0.0f;
                }
                wave3 = 1;
                
            }
            if(wave3 == 1 && (timer>= delayTime*2)){
                if (fC3 == false){
                    cubes[3].Move(50, 50, 600, Cube.ORDER_TYPE.Strong);
                    fC3 = true;
                }
                if(timer2 >= delay+delayTime*2){
                    cubes[3].Move(-50, -50, 600);
                    wave3 = 0;
                    fC3 = false;
                    start = false;
                    timer = 0.0f;
                    timer2 = 0.0f;
                }
            }
        }
    }
}
