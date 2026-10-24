using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using toio;
using toio.Navigation;


public class NavigatorBoidsAvoid : MonoBehaviour{
    CubeManager cubeManager;
    Cube[] cubes;
    bool started = false;
    CubeNavigator navigatorNotBoids1;

    async void Start(){
        cubeManager = new CubeManager(ConnectType.Real);
        cubes =    await cubeManager.MultiConnect(9);
            Debug.Assert(cubeManager.navigators.Count>1, "Need at least 2 cubes.");

            // Choose 1 cube not to be of boids and remote
            CubeNavigator navigatorNotBoids0 = cubeManager.navigators[0];
            
            // Choose 1 cube not to be of boids and obsticle
            navigatorNotBoids1 = cubeManager.navigators[1];
            if (CubeScanner.actualTypeOfAuto == ConnectType.Real){
                for (int i = 0; i<6; i++){
                    if(i%2 == 0){
                        if (cubeManager.navigators[i].cube.localName == "Cube Not Boids")
                            navigatorNotBoids0 = cubeManager.navigators[i];
                    }
                    else{
                        if (cubeManager.navigators[i].cube.localName == "Cube Not Boids")
                            navigatorNotBoids1 = cubeManager.navigators[i];
                    }
                }
            }
            // Use LED color to distinguish cubes
            foreach (var navigator in cubeManager.navigators)
            {
                if (navigator == navigatorNotBoids0) navigator.cube.TurnLedOn(255,0,0,0); // Red
                else if (navigator == navigatorNotBoids1) navigator.cube.TurnLedOn(0,0,255,0); // Blue
                else navigator.cube.TurnLedOn(0,255,0,0);  // Green
            }

            // Set to BOIDS_AVOID mode, except navigatorNotBoids or
            // Set to BOIDS mode, except navigatorNotBoids
            foreach (var navigator in cubeManager.navigators){
                navigator.mode = CubeNavigator.Mode.BOIDS_AVOID;
                //navigator.mode = CubeNavigator.Mode.BOIDS;
                navigator.usePred = true;
            }

            // By default, all navigators are in one group of boids
            // here, separate Red cube and Blue cube from the group
            navigatorNotBoids0.SetRelation(cubeManager.navigators, CubeNavigator.Relation.NONE);
            foreach (var navigator in cubeManager.navigators){
                navigator.SetRelation(navigatorNotBoids0, CubeNavigator.Relation.NONE);
                navigator.SetRelation(navigatorNotBoids1, CubeNavigator.Relation.NONE);
            }

            started = true;
    }
    void Update(){
        if (!started) return;
        // ------ Sync ------
        foreach (var navigator in cubeManager.syncNavigators)
            {
                // Cube (1) stay still
                if (navigator != navigatorNotBoids1)
                    navigator.Navi2Target(cubes[0].x, cubes[0].y, maxSpd:50).Exec();
            }
    }
}
