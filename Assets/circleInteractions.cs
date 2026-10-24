using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using TMPro;
using UnityEngine.UI;
using System;
using toio.Navigation;
using toio.MathUtils;

public class circleInteractions : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes;
    bool circle = false;
    Cube cube1; Cube cube2; Cube cube3; Cube cube4; Cube cube5; Cube cube6;
    bool initialized = false; bool connected = false;
    bool haptic = false;
    int circleRadius = 60;  int minRad = 30; int maxRad = 180;
    string front = "Front";
    string back = "Back";
    int circleNumOfCubes = 5;
    int speed = 250; int speedHaptic = 100; int duration = 300;
    int speedMoving = 35;
    int mag = 160; bool move = false; bool colour = true;
    bool sound = false;
    public TextMeshProUGUI output;
    public Text connectButton;
    public Text hapticButton; public Text soundButton;
    public Text magnetButton; public Text repelButton; public Text multiFlickButton;
    public Slider speedSlider; public Text speedValue;
    public Slider RadiusSlider; public Text RadiusValue;
    public Slider numOfCubesSlider; public Text numOfCubesValue;
    public Text circleButton; public Slider circleRadiusSlider; public Text circleRadiusValue;
    public Slider circleNumOfCubesSlider; public Text circleNumOfCubesValue;
    public Slider durationSlider; public Text durationValue;
    // Start is called before the first frame update
    public void HapticToggle(){
        if (haptic){
            haptic = false;
            hapticButton.text = "Haptic Off";
        } else {
            haptic = true;
            hapticButton.text = "Haptic On";
        }
    }
    public void HandleSpeed(int val){
        speedHaptic = val;
        speedValue.text = val.ToString();
    }
    public void HandleDuration(int val){
        duration = val;
        durationValue.text = val.ToString();
    }
    public void CircleToggle(){
        if (circle){
            circle = false;
            cubes[0].TurnLedOn(0, 255, 0, 0);
            circleButton.text = "Circle Expand-Contract";
            cubes[0].buttonCallback.RemoveListener("EventScene");
            cubes[0].poseCallback.RemoveListener("EventScene");
        } 
        else {
            circle = true;
            circleButton.text = "Moving Circle";
            cubes[0].buttonCallback.AddListener("EventScene", OnPressButton);
            cubes[0].poseCallback.AddListener("EventScene", OnPose);
        }
    }
     public void SoundToggle(){
        if (sound){
            sound = false;
            soundButton.text = "Collision Avoid Off";
            cubeManager.navigators[0].ClearOther();
            cubeManager.navigators[1].ClearOther();
            cubeManager.navigators[2].ClearOther();
            cubeManager.navigators[3].ClearOther();
            cubeManager.navigators[4].ClearOther();
            cubeManager.navigators[5].ClearOther();
        } else {
            sound = true;
            soundButton.text = "Collision Avoid On";
            cubeManager.navigators[5].AddOther(cubeManager.navigators[1]);
            cubeManager.navigators[5].AddOther(cubeManager.navigators[2]);
            cubeManager.navigators[5].AddOther(cubeManager.navigators[3]);
            cubeManager.navigators[5].AddOther(cubeManager.navigators[4]);
            cubeManager.navigators[1].AddOther(cubeManager.navigators[5]);
            cubeManager.navigators[1].AddOther(cubeManager.navigators[2]);
            cubeManager.navigators[1].AddOther(cubeManager.navigators[3]);
            cubeManager.navigators[1].AddOther(cubeManager.navigators[4]);
            cubeManager.navigators[2].AddOther(cubeManager.navigators[5]);
            cubeManager.navigators[2].AddOther(cubeManager.navigators[1]);
            cubeManager.navigators[2].AddOther(cubeManager.navigators[3]);
            cubeManager.navigators[2].AddOther(cubeManager.navigators[4]);
            cubeManager.navigators[3].AddOther(cubeManager.navigators[5]);
            cubeManager.navigators[3].AddOther(cubeManager.navigators[1]);
            cubeManager.navigators[3].AddOther(cubeManager.navigators[2]);
            cubeManager.navigators[3].AddOther(cubeManager.navigators[4]);
            cubeManager.navigators[4].AddOther(cubeManager.navigators[5]);
            cubeManager.navigators[4].AddOther(cubeManager.navigators[1]);
            cubeManager.navigators[4].AddOther(cubeManager.navigators[2]);
            cubeManager.navigators[4].AddOther(cubeManager.navigators[3]);
        }
    }
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        cubeManager = new CubeManager(ConnectType.Real); // remove connect type to publish
        hapticButton = GameObject.Find("HapticButtonText").GetComponent<Text>();
        soundButton = GameObject.Find("SoundButtonText").GetComponent<Text>();
        speedSlider = GameObject.Find("speedSlider").GetComponent<Slider>();
        speedSlider.onValueChanged.AddListener(delegate {HandleSpeed((int)speedSlider.value);});
        speedValue = GameObject.Find("SpeedValText").GetComponent<Text>();
        // RadiusSlider = GameObject.Find("RadiusSlider").GetComponent<Slider>();
        // RadiusSlider.onValueChanged.AddListener(delegate {HandleRadius((int)RadiusSlider.value);});
        // RadiusValue = GameObject.Find("RadiusValText").GetComponent<Text>();
        // numOfCubesSlider = GameObject.Find("NumberOfCubesSlider").GetComponent<Slider>();
        // numOfCubesSlider.onValueChanged.AddListener(delegate {HandleNumOfCubes((int)numOfCubesSlider.value);});
        // numOfCubesValue = GameObject.Find("NumberOfCubesValText").GetComponent<Text>();
        circleButton = GameObject.Find("CircleButtonText").GetComponent<Text>();
        // circleRadiusSlider = GameObject.Find("CircleRadiusSlider").GetComponent<Slider>();
        // circleRadiusSlider.onValueChanged.AddListener(delegate {HandleCircleRadius((int)circleRadiusSlider.value);});
        // circleRadiusValue = GameObject.Find("CircleRadiusValText").GetComponent<Text>();
        // circleNumOfCubesSlider = GameObject.Find("CircleNumberOfCubesSlider").GetComponent<Slider>();
        // circleNumOfCubesSlider.onValueChanged.AddListener(delegate {HandleCircleNumOfCubes((int)circleNumOfCubesSlider.value);});
        // circleNumOfCubesValue = GameObject.Find("CircleNumberOfCubesValText").GetComponent<Text>();
        connectButton = GameObject.Find("ConnectText").GetComponent<Text>();
        durationSlider = GameObject.Find("DurationSlider").GetComponent<Slider>();
        durationSlider.onValueChanged.AddListener(delegate {HandleDuration((int)durationSlider.value);});
        durationValue = GameObject.Find("DurationValText").GetComponent<Text>();
    }
    public void Connect()
    {
        if (null == cubes || !cubes[0].isConnected || !cubes[1].isConnected)
        {
            cubes = cubeManager.cubes.ToArray();
            if (cubeManager.cubes.Count == 6){Debug.Log("Worked");}
            if (cubes.Length == 6){connected = true;}
            //else{connected = false; initialized = false; Debug.Log("Not Connected");
            connectButton.text = "Connected";
            connectButton.color = Color.green;
            cubeManager.navigators[0].ClearOther();
            cubeManager.navigators[1].ClearOther();
            cubeManager.navigators[2].ClearOther();
            cubeManager.navigators[3].ClearOther();
            cubeManager.navigators[4].ClearOther();
            cubeManager.navigators[5].ClearOther();
            cubes[0].TurnLedOn(0, 255, 0, 0);
            cubes[1].TurnLedOn(255, 255, 0, 0);
            cubes[2].TurnLedOn(0, 255, 255, 0);
            cubes[3].TurnLedOn(255, 0, 255, 0);
            cubes[4].TurnLedOn(255, 0, 0, 0);
            cubes[5].TurnLedOn(0, 0, 255, 0);
        }
    }
    public async void Connect1(){
        cube1 = await cubeManager.SingleConnect();
    }
    public async void Connect2(){
        cube2 = await cubeManager.SingleConnect();
    }
    public async void Connect3(){
        cube3 = await cubeManager.SingleConnect();
    }
    public async void Connect4(){
        cube4 = await cubeManager.SingleConnect();
    }
    public async void Connect5(){
        cube5 = await cubeManager.SingleConnect();
    }
    public async void Connect6(){
        cube6 = await cubeManager.SingleConnect();
    }
    public void Disconnect()
    {
        if (null != cubes && cubes[0].isConnected && cubes[1].isConnected)
        {
            cubeManager.DisconnectAll();
            connectButton.text = "Connect";
            connectButton.color = Color.black;
        }
    }

    public async void ReConnect()
    {
        if (null != cubes && !cubes[0].isConnected && !cubes[1].isConnected)
        {
            await cubeManager.ReConnectAll();
            connectButton.text = "Connected";
            connectButton.color = Color.green;
            initialized = false;
            cubes[0].TurnLedOn(0, 255, 0, 0);
            cubes[1].TurnLedOn(255, 255, 0, 0);
            cubes[2].TurnLedOn(0, 255, 255, 0);
            cubes[3].TurnLedOn(255, 0, 255, 0);
            cubes[4].TurnLedOn(255, 0, 0, 0);
            cubes[5].TurnLedOn(0, 0, 255, 0);
        }
    }
    int cX = 250; int cY = 250;
    int x0 = 334; int y0 = 229; int x1 = 316; int y1 = 312; int x2 = 176; int y2 = 228; int x3 = 257; int y3 = 173; int x4 = 199; int y4 = 311;
     void GetCirclePositions(){
        double circumference = 2 * Math.PI * circleRadius;
        double angleIncrement = 2*Math.PI / circleNumOfCubes;
        if (circleNumOfCubes == 5){
            double angle1 = 0*angleIncrement;
            double angle2 = 1*angleIncrement;
            double angle3 = 2*angleIncrement;
            double angle4 = 3*angleIncrement;
            double angle5 = 4*angleIncrement;
            x0 = (int)(cX + circleRadius * Math.Cos(angle1));
            y0 = (int)(cY + circleRadius * Math.Sin(angle1));
            x1 = (int)(cX + circleRadius * Math.Cos(angle2));
            y1 = (int)(cY + circleRadius * Math.Sin(angle2));
            x2 = (int)(cX + circleRadius * Math.Cos(angle3));
            y2 = (int)(cY + circleRadius * Math.Sin(angle3));
            x3 = (int)(cX + circleRadius * Math.Cos(angle4));
            y3 = (int)(cY + circleRadius * Math.Sin(angle4));
            x4 = (int)(cX + circleRadius * Math.Cos(angle5));
            y4 = (int)(cY + circleRadius * Math.Sin(angle5));
        }
     }
     int moveC0 = 0; int moveC1 = 0; int moveC2 = 0; int moveC3 = 0; int moveC4 = 0;
     void Circle(){
        if (cubes[1].x != x0 || cubes[1].y != y0 ){moveC0 = 1;}
        if (cubes[2].x != x1 || cubes[2].y != y1){moveC1 = 1;}
        if (cubes[3].x != x2 || cubes[3].y != y2){moveC2 = 1;}
        if (cubes[4].x != x3 || cubes[4].y != y3){moveC3 = 1;}
        if (cubes[5].x != x4 || cubes[5].y != y4){moveC4 = 1;}
        if (moveC0 == 1){
            var mv = cubeManager.navigators[1].Navi2Target(x0, y0, maxSpd:speed).Exec();
            if (mv.reached){moveC0 = 0;} 
        }
        if (moveC1 == 1){
            var mv = cubeManager.navigators[2].Navi2Target(x1, y1, maxSpd:speed).Exec();
            if (mv.reached){moveC1 = 0;} 
        }
        if (moveC2 == 1){
            var mv = cubeManager.navigators[3].Navi2Target(x2, y2, maxSpd:speed).Exec();
            if (mv.reached){moveC2 = 0;} 
        }
        if (moveC3 == 1){
            var mv = cubeManager.navigators[4].Navi2Target(x3, y3, maxSpd:speed).Exec();
            if (mv.reached){moveC3 = 0;} 
        }
        if (moveC4 == 1){
            var mv = cubeManager.navigators[5].Navi2Target(x4, y4, maxSpd:speed).Exec();
            if (mv.reached){moveC4 = 0;} 
        }
    }
    void MoveCircle() {
        if (cubeManager.synced){
            if (cubes[0].pose.ToString() == front){
                if (circleRadius > minRad){
                    circleRadius -= 2;
                }
            }
            if (cubes[0].pose.ToString() == back){
                if (circleRadius < maxRad){
                    circleRadius += 2;
                }
            }
            GetCirclePositions();
            Circle();
            if (haptic){
                if (moveC0 != 0 || moveC1 != 0 || moveC2 != 0 || moveC3 != 0 || moveC4 != 0){
                    //Debug.Log($"M0 {moveC0} M1 {moveC1} M2 {moveC2} M3 {moveC3} M4 {moveC4} ");
                    cubes[0].Move(speedHaptic, speedHaptic, duration);
                    
                }
            }
        }

    }
    void OnPose(Cube c)
    {
        if (cubes[0].pose.ToString() == front){
        if (speedMoving < 65){
            speedMoving += 10;
        }
        else{
            speedMoving = 35;
        }
        }
    }
    void OnPressButton(Cube c)
    {
        if (c.isPressed)
        {
            if (!move){
                move = true;
            }
            else{
                move = false;
            }
        }
    }
    void MovingCircle(){
        var tar1 = Vector.fromRadMag(Time.time/1, mag) + new Vector(250, 250);
        var tar2 = Vector.fromRadMag((Time.time/1)+5, mag) + new Vector(250, 250);
        var tar3 = Vector.fromRadMag((Time.time/1)+10, mag) + new Vector(250, 250);
        var tar4 = Vector.fromRadMag((Time.time/1)+15, mag) + new Vector(250, 250);
        var tar5 = Vector.fromRadMag((Time.time/1)+20, mag) + new Vector(250, 250);
        if (cubeManager.synced){
            if (move){
                var navi1 = cubeManager.navigators[1];
                var mv1 = navi1.Navi2Target(tar1, maxSpd:speedMoving).Exec();

                var navi2 = cubeManager.navigators[2];
                var mv2 = navi2.Navi2Target(tar2, maxSpd:speedMoving).Exec();

                var navi3 = cubeManager.navigators[3];
                var mv3 = navi3.Navi2Target(tar3, maxSpd:speedMoving).Exec();

                var navi4 = cubeManager.navigators[4];
                var mv4 = navi4.Navi2Target(tar4, maxSpd:speedMoving).Exec();

                var navi5 = cubeManager.navigators[5];
                var mv5 = navi5.Navi2Target(tar5, maxSpd:speedMoving).Exec();
            }
            if (!move && !colour) cubes[0].TurnLedOn(100, 0, 200, 100);
            if (!move && colour) cubes[0].TurnLedOn(100, 0, 200, 100);
            if (move){
                if (speedMoving == 35){
                    if(colour) cubes[0].TurnLedOn(0, 0, 255, 100);
                    //if(haptic) cubes[0].Move(35, 35, 100);
                }
                if (speedMoving == 45){ 
                    if(colour) cubes[0].TurnLedOn(100, 0, 200, 100);
                    //if(haptic) cubes[0].Move(45, 45, 100, Cube.ORDER_TYPE.Strong);
                }
                if (speedMoving == 55){
                    if(colour) cubes[0].TurnLedOn(0, 255, 0, 100);
                    //if(haptic) cubes[0].Move(55, 55, 100);
                }
                if (speedMoving == 65){
                    if(colour) cubes[0].TurnLedOn(200, 0, 0, 100);
                    //if(haptic) cubes[0].Move(65, 65, 100);
                }
            }
            
        }
    }
    // Update is called once per frame
     void Update()
    {
        if(connected){
            if (!circle && cubeManager.synced ){MoveCircle();}
            if (circle && cubeManager.synced ){MovingCircle();}
        }
    }
}
