using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using System.Threading.Tasks;
using System;

public class magnetsRepel : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    int x1, y1, x2, y2, yc1, yc2, xc1, xc2;
    int speed = 100;
    // Start is called before the first frame update
    async void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real);
        cubes = await cubeManager.MultiConnect(2);
        //await Task.Delay(100);
        // cubes initial location
        x1 = cubes[0].x;
        y1 = cubes[0].y;
        x2 = cubes[1].x;
        y2 = cubes[1].y;
        string message = $"Cube 1 initial X: {x1}, Y: {y1} \t Cube 2 initial X: {x2}, Y: {y2}";
        Debug.Log(message);
        // By default, each navigator is able to see all others
        // But you can also manually make a navigator "blind"
        cubeManager.navigators[0].ClearOther();

        cubeManager.navigators[1].usePred = true;
    }

    int moveCube1 = 0; int moveCube2 = 0;
    void Repel(){
        if (cubeManager.synced){
            int x1 = cubes[0].x;
            int y1 = cubes[0].y;
            int x2 = cubes[1].x;
            int y2 = cubes[1].y;
            int centerX = (x1 + x2) / 2;
            int centerY = (y1 + y2) / 2;
            float radiusRepel = 80;
            //float radiusOutofBound = outerRadius;
            float distanceCube1 = Mathf.Sqrt(Mathf.Pow(x1 - centerX, 2) + Mathf.Pow(y1 - centerY, 2));
            //float distanceCube2 = Mathf.Sqrt(Mathf.Pow(x2 - centerX, 2) + Mathf.Pow(y2 - centerY, 2));
            bool isCube1Inside = (distanceCube1 < radiusRepel);
            //bool isCube2Inside = (distanceCube2 < radiusRepel);

            if (isCube1Inside){
                moveCube1 = 1; moveCube2 = 1;
                int dx1 = cubes[0].x - centerX;
                int dy1 = cubes[0].y - centerY;
                // normalize the vector
                double mag1 = Math.Sqrt(dx1*dx1 + dy1*dy1);
                double ux1 = dx1/mag1;
                double uy1 = dy1/mag1;
                xc1 = centerX + (int)Math.Round(ux1*radiusRepel);
                yc1 = centerY + (int)Math.Round(uy1*radiusRepel);
                int dx = cubes[1].x - centerX;
                int dy = cubes[1].y - centerY;
                // normalize the vector
                double mag = Math.Sqrt(dx*dx + dy*dy);
                double ux = dx/mag;
                double uy = dy/mag;
                xc2 = centerX + (int)Math.Round(ux*radiusRepel);
                yc2 = centerY + (int)Math.Round(uy*radiusRepel);
                // Debug.Log($"cube1 pos x = {x1} y = {y1}");
                // Debug.Log($"cube2 pos x = {x2} y = {y2}");
                // Debug.Log($"center pos x = {centerX} y = {centerY}");
                // Debug.Log($"distance c1 pos = {distanceCube1} ");
                // Debug.Log($"distance c2 pos = {distanceCube2} ");
            }
            if (moveCube1 == 1){
                // calculate direction vector
                

                var mv = cubeManager.navigators[0].Navi2Target(xc1, yc1, maxSpd:speed).Exec();
                if (mv.reached){
                    moveCube1 = 0;
                } 
            }
            if (moveCube2 == 1){
                // calculate direction vector
                
                var mv = cubeManager.navigators[1].Navi2Target(xc2, yc2, maxSpd:speed).Exec();
                if (mv.reached){
                    moveCube2 = 0;
                } 
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (cubeManager.synced){Repel();}
        // if (cubeManager.synced)
        // {
        //     if (cubes[0].x != x1 || cubes[0].y != y1 ){
        //         moveCube1 = 1;
        //     }
        //     if (cubes[1].x != x2 || cubes[1].y != y2){
        //         moveCube2 = 1;
        //     }

        //     if (moveCube1 == 1){
        //         var mv = cubeManager.navigators[0].Navi2Target(x1, y1, maxSpd:150).Exec();
        //         if (mv.reached){
        //             moveCube1 = 0;
        //         } 
        //     }

        //     if (moveCube2 == 1){
        //         var mv = cubeManager.navigators[1].Navi2Target(x2, y2, maxSpd:150).Exec();
        //         if (mv.reached){
        //             moveCube2 = 0;
        //         } 
        //     }
        // }
    }
}
