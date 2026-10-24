using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleInteractionsUI : MonoBehaviour
{
    #if UNITY_EDITOR
    private int updateCnt = 0;

    void Update()
    {
        if (updateCnt < 3) updateCnt++;
        if (updateCnt == 2){
            // Moving the camera to the left will move the simulation screen to the right.
            // var localPos = Camera.main.transform.localPosition;
            // localPos.x = -0.15f; // Amount of movement
            // Camera.main.transform.localPosition = localPos;

            // Using the parent-child relationship with "SimCanvas", change the relative coordinates and move UI (Canvas) to the left.
            var canvasObj = GameObject.Find("Canvas");
            var simCanvasObj = GameObject.Find("SimCanvas");
            canvasObj.transform.SetParent(simCanvasObj.transform);
            canvasObj.transform.localScale *= 0.9f;
            canvasObj.transform.position = new Vector3(canvasObj.transform.position.x, 780/2 * canvasObj.transform.localScale.y * 0.8f, canvasObj.transform.position.z);
            // To fit UI and the simulation screen, the position of the canvas is set by the "Width" of 720 in the Inspector and the "Scale Factor" of 0.8 in SimCanvas.
            // canvasObj.transform.position = new Vector3(
            //     720/2 * canvasObj.transform.localScale.x * 0.8f,
            //     canvasObj.transform.position.y,
            //     canvasObj.transform.position.z
            // );
        }
    }
    #endif
}
