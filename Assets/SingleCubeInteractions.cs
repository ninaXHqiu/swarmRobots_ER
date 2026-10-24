using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using TMPro;
using UnityEngine.UI;
using System;
using toio.Navigation;

// Things to add:
// 1. turn on or turn off sound
// 2. LED colour
// 3. Move settings

public class SingleCubeInteractions : MonoBehaviour
{
    CubeManager cubeManager;
    Cube[] cubes; Cube cube;
    Cube cube1; Cube cube2; Cube cube3; Cube cube4; Cube cube5;
    bool initialized = false; bool connected = false;
    int ledColour = 1; bool r = true; bool b = false; bool g = false;
    bool haptic = true; bool sound = false;
    bool flick = false; int fx1 = 0; int fy1 = 0; int fx2 = 0; int fy2 = 0; int fx3 = 0; int fy3 = 0; int fx4 = 0; int fy4 = 0; int fx5 = 0; int fy5 = 0; 
    int phase1 = 0; int phase2 = 0; int phase3 = 0; int phase4 = 0; int phase5 = 0;
    bool magnet = false; int moveCube1 = 0; int moveCube2 = 0; int xc1 = 0; int xc2 = 0; int yc1 = 0; int yc2 = 0; int radius = 30; bool mc1 = false; bool mc2 = false; bool mc3 = false; bool mc4 = false; bool mc5 = false;
    bool multiFlick = false;
    bool repel = false;
    int remoteType = 0;
    int speed = 100; int duration = 300;
    int numOfCubes = 2;
    bool circle = false; int circleRadius = 70; int circleNumOfCubes = 5;
    
    // Any number of robots can connect. Interactions written for exactly 5 robots
    // (magnet, repel, flick, multi-flick, remote) only start when at least 5 are connected.
    const int LEGACY_ROBOTS = 5;
    static readonly Color32[] LedColours = {
        new Color32(0, 255, 0, 255), new Color32(255, 255, 0, 255), new Color32(0, 255, 255, 255),
        new Color32(255, 0, 255, 255), new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255),
        new Color32(255, 128, 0, 255), new Color32(255, 255, 255, 255), new Color32(128, 0, 255, 255),
        new Color32(0, 128, 255, 255)
    };
    bool[] initReached = new bool[0];
    int RobotCount { get { return cubes == null ? 0 : cubes.Length; } }
    bool HasRobots(int needed){
        if (RobotCount >= needed){ return true; }
        ShowStatus("Needs " + needed + " robots");
        return false;
    }
    void ShowStatus(string message){
        if (connectButton != null){ connectButton.text = message; }
    }
    void SetLeds(){
        for (int i = 0; i < RobotCount; i++){
            var c = LedColours[i % LedColours.Length];
            cubes[i].TurnLedOn(c.r, c.g, c.b, 0);
        }
    }

    // Used by RoutinePanel to run routines on the robots connected here
    public Cube[] ConnectedRobots { get { return cubes; } }
    public CubeManager Manager { get { return cubeManager; } }
    // Turn off every built-in interaction so a routine has the robots to itself
    public void StopInteractions(){
        if (circle){ CircleToggle(); }
        if (magnet){ MagnetToggle(); }
        if (repel){ RepelToggle(); }
        if (multiFlick){ MultiFlickToggle(); }
        GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
        GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
        initialized = true; // skip the move-to-start-row step while a routine runs
    }

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

    public void HapticToggle(){
        if (haptic){
            haptic = false;
            hapticButton.text = "Haptic Off";
        } else {
            haptic = true;
            hapticButton.text = "Haptic On";
        }
    }
    public void SoundToggle(){
        if (sound){
            sound = false;
            soundButton.text = "Sound Off";
        } else {
            sound = true;
            soundButton.text = "Sound On";
        }
    }
    public void HandleDuration(int val){
        duration = val;
        durationValue.text = val.ToString();
    }
    public void HandleSpeed(int val){
        speed = val;
        speedValue.text = val.ToString();
    }
    public void HandleRadius(int val){ // for magnet
        radius = val;
        RadiusValue.text = val.ToString();
    }
    public void HandleNumOfCubes(int val){ // Remote
        numOfCubes = val;
        numOfCubesValue.text = val.ToString();
    }
    public void MagnetToggle(){
        if (magnet){
            magnet = false;
            if (repel == true){RepelToggle();}
            // cubeManager.navigators[0].AddOther(cubeManager.navigators[1]);
            // cubeManager.navigators[1].AddOther(cubeManager.navigators[0]);
            magnetButton.text = "Magnet Off";
        } else {
            if (!HasRobots(LEGACY_ROBOTS)){ return; }
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            if (repel){RepelToggle();}
            magnet = true;
            cubeManager.navigators[0].RemoveOther(cubeManager.navigators[1]);
            cubeManager.navigators[1].RemoveOther(cubeManager.navigators[0]);
            mc3 = true; mc4 = true; mc5 = true;
            magnetButton.text = "Magnet On";
        }
    }
    public void RepelToggle(){
        if (repel){
            repel = false;
            //cubeManager.navigators[0].ClearOther();
            //cubeManager.navigators[1].ClearOther();
            repelButton.text = "Repel Off";
        } else {
            if (!HasRobots(LEGACY_ROBOTS)){ return; }
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            if (magnet == true){MagnetToggle();}
            repel = true;
                // cubeManager.navigators[0].AddOther(cubeManager.navigators[1]);
                // cubeManager.navigators[1].AddOther(cubeManager.navigators[0]);
            mc3 = true; mc4 = true; mc5 = true;
            repelButton.text = "Repel On";
        }
    }
    public void MultiFlickToggle(){
        if (multiFlick){
            multiFlick = false;
            multiFlickButton.text = "MultiFlick Off";
        } else {
            if (!HasRobots(LEGACY_ROBOTS)){ return; }
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (circle){CircleToggle();}
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            ph = 0;
            multiFlick = true;
            multiFlickButton.text = "MultiFlick On";
        }
    }
    public void CircleToggle(){
        if (circle){
            circle = false;
            circleButton.text = "Circle Off";
        } else {
            if (!HasRobots(1)){ return; }
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (multiFlick){MultiFlickToggle();}
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            circle = true;
            circleButton.text = "Circle On";
            GetCirclePositions();
            foreach (var navigator in cubeManager.navigators){ navigator.ClearOther(); }
        }
    }
    public void HandleCircleRadius(int val){
        circleRadius = val;
        circleRadiusValue.text = val.ToString();
        GetCirclePositions();
    }
    
    public void HandleCircleNumOfCubes(int val){
        circleNumOfCubes = val;
        circleNumOfCubesValue.text = val.ToString();
        GetCirclePositions();
    }
    void ClearSingleListeners(){
        for (int i = 0; i < RobotCount; i++){
            cubes[i].doubleTapCallback.RemoveListener("EventScene");
            cubes[i].shakeCallback.RemoveListener("EventScene");
            cubes[i].slopeCallback.RemoveListener("EventScene");
        }
    }
    public void HandleInputData(int val){ //Single Interaction
        if (val == 0){
            ClearSingleListeners();
            flick = false;
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
        }
        if (val == 1){
            ClearSingleListeners();
            for (int i = 0; i < RobotCount; i++){ cubes[i].doubleTapCallback.AddListener("EventScene", OnDoubleTap); }
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            flick = false;
        }
        if (val == 2){
            ClearSingleListeners();
            for (int i = 0; i < RobotCount; i++){ cubes[i].shakeCallback.AddListener("EventScene", OnShake); }
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            flick = false;
        }
        if (val == 3){
            ClearSingleListeners();
            for (int i = 0; i < RobotCount; i++){ cubes[i].slopeCallback.AddListener("EventScene", OnSlope); }
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            flick = false;
        }
        if (val == 4){
            // Flick is written for exactly 5 robots
            if (!HasRobots(LEGACY_ROBOTS)){ GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0; return; }
            ClearSingleListeners();
            flick = true;
            fx1 = cubes[0].x; fy1 = cubes[0].y; fx2 = cubes[1].x; fy2 = cubes[1].y; fx3 = cubes[2].x;
            fy3 = cubes[2].y; fx4 = cubes[3].x; fy4 = cubes[3].y; fx5 = cubes[4].x; fy5 = cubes[4].y;
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
        }

    }

    public void RemoteHandleInputData(int val){
        if (val == 0){ // None
            remoteType = 0;
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            if (RobotCount > 0){ cubes[0].poseCallback.RemoveListener("EventScene"); }
        }
        if (val > 0 && !HasRobots(LEGACY_ROBOTS)){
            // Remote modes are written for exactly 5 robots
            GameObject.Find("RemoteDropdown").GetComponent<TMP_Dropdown>().value = 0;
            return;
        }
        if (val == 1){ // remote with mat
            cubeManager.navigators[0].AddOther(cubeManager.navigators[1]);
            cubeManager.navigators[0].AddOther(cubeManager.navigators[2]);
            cubeManager.navigators[0].AddOther(cubeManager.navigators[3]);
            cubeManager.navigators[0].AddOther(cubeManager.navigators[4]);
            cubeManager.navigators[1].AddOther(cubeManager.navigators[0]);
            cubeManager.navigators[1].AddOther(cubeManager.navigators[2]);
            cubeManager.navigators[1].AddOther(cubeManager.navigators[3]);
            cubeManager.navigators[1].AddOther(cubeManager.navigators[4]);
            cubeManager.navigators[2].AddOther(cubeManager.navigators[0]);
            cubeManager.navigators[2].AddOther(cubeManager.navigators[1]);
            cubeManager.navigators[2].AddOther(cubeManager.navigators[3]);
            cubeManager.navigators[2].AddOther(cubeManager.navigators[4]);
            cubeManager.navigators[3].AddOther(cubeManager.navigators[0]);
            cubeManager.navigators[3].AddOther(cubeManager.navigators[1]);
            cubeManager.navigators[3].AddOther(cubeManager.navigators[2]);
            cubeManager.navigators[3].AddOther(cubeManager.navigators[4]);
            cubeManager.navigators[4].AddOther(cubeManager.navigators[0]);
            cubeManager.navigators[4].AddOther(cubeManager.navigators[1]);
            cubeManager.navigators[4].AddOther(cubeManager.navigators[2]);
            cubeManager.navigators[4].AddOther(cubeManager.navigators[3]);
            remoteType = 1;
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            cubes[0].poseCallback.RemoveListener("EventScene");
        }
        if (val == 2){ // remote Continuous
            remoteType = 2;
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            cubes[0].poseCallback.RemoveListener("EventScene");
        }
        if (val == 3){ // remote Incremental
            remoteType = 3;
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            if (magnet){MagnetToggle();}
            if (repel){RepelToggle();}
            if (circle){CircleToggle();}
            if (multiFlick){MultiFlickToggle();}
            cubes[0].poseCallback.AddListener("EventScene", OnPose);
            
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real); // remove connect type to publish
        hapticButton = GameObject.Find("HapticButtonText").GetComponent<Text>();
        soundButton = GameObject.Find("SoundButtonText").GetComponent<Text>();
        magnetButton = GameObject.Find("MagnetButtonText").GetComponent<Text>();
        multiFlickButton = GameObject.Find("MultiFlickButtonText").GetComponent<Text>();
        repelButton = GameObject.Find("RepelButtonText").GetComponent<Text>();
        speedSlider = GameObject.Find("speedSlider").GetComponent<Slider>();
        speedSlider.onValueChanged.AddListener(delegate {HandleSpeed((int)speedSlider.value);});
        speedValue = GameObject.Find("SpeedValText").GetComponent<Text>();
        RadiusSlider = GameObject.Find("RadiusSlider").GetComponent<Slider>();
        RadiusSlider.onValueChanged.AddListener(delegate {HandleRadius((int)RadiusSlider.value);});
        RadiusValue = GameObject.Find("RadiusValText").GetComponent<Text>();
        numOfCubesSlider = GameObject.Find("NumberOfCubesSlider").GetComponent<Slider>();
        numOfCubesSlider.onValueChanged.AddListener(delegate {HandleNumOfCubes((int)numOfCubesSlider.value);});
        numOfCubesValue = GameObject.Find("NumberOfCubesValText").GetComponent<Text>();
        circleButton = GameObject.Find("CircleButtonText").GetComponent<Text>();
        circleRadiusSlider = GameObject.Find("CircleRadiusSlider").GetComponent<Slider>();
        circleRadiusSlider.onValueChanged.AddListener(delegate {HandleCircleRadius((int)circleRadiusSlider.value);});
        circleRadiusValue = GameObject.Find("CircleRadiusValText").GetComponent<Text>();
        circleNumOfCubesSlider = GameObject.Find("CircleNumberOfCubesSlider").GetComponent<Slider>();
        circleNumOfCubesSlider.onValueChanged.AddListener(delegate {HandleCircleNumOfCubes((int)circleNumOfCubesSlider.value);});
        circleNumOfCubesValue = GameObject.Find("CircleNumberOfCubesValText").GetComponent<Text>();
        connectButton = GameObject.Find("ConnectText").GetComponent<Text>();
        // One "+" button pairs robots one at a time, so C2-C5 are no longer needed
        var addRobotButton = GameObject.Find("Connect (1)");
        if (addRobotButton != null){ addRobotButton.GetComponentInChildren<Text>().text = "+"; }
        for (int k = 2; k <= 5; k++){
            var oldButton = GameObject.Find("Connect (" + k + ")");
            if (oldButton != null){ oldButton.SetActive(false); }
        }
        durationSlider = GameObject.Find("DurationSlider").GetComponent<Slider>();
        durationSlider.onValueChanged.AddListener(delegate {HandleDuration((int)durationSlider.value);});
        durationValue = GameObject.Find("DurationValText").GetComponent<Text>();

        // GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().onValueChanged.AddListener(delegate {HandleInputData(GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value);});
    }
    // Pair robots one at a time with the "+" button (one browser popup per robot),
    // then press Connect to start using however many are paired.
    public void Connect()
    {
        cubes = cubeManager.cubes.ToArray();
        if (cubes.Length == 0){ ShowStatus("Add robots first"); return; }
        connected = true; initialized = false;
        initReached = new bool[cubes.Length];
        connectButton.text = "Connected (" + cubes.Length + ")";
        connectButton.color = MockupTheme.Accent;
        foreach (var navigator in cubeManager.navigators){ navigator.ClearOther(); }
        SetLeds();
        // The circle can now use every connected robot
        circleNumOfCubesSlider.minValue = 1;
        circleNumOfCubesSlider.maxValue = cubes.Length;
        circleNumOfCubesSlider.value = cubes.Length;
        GetCirclePositions();
    }
    // The scene's C1-C5 buttons are wired to these; only the first ("+") stays visible.
    public void Connect1(){ AddRobot(); }
    public void Connect2(){ AddRobot(); }
    public void Connect3(){ AddRobot(); }
    public void Connect4(){ AddRobot(); }
    public void Connect5(){ AddRobot(); }
    async void AddRobot(){
        await cubeManager.SingleConnect();
        connectButton.text = "Connect (" + cubeManager.cubes.Count + ")";
        connectButton.color = MockupTheme.Ink;
    }

    public void Disconnect()
    {
        if (RobotCount > 0)
        {
            cubeManager.DisconnectAll();
            connectButton.text = "Connect";
            connectButton.color = MockupTheme.Ink;
            connected = false;
        }
    }

    public async void ReConnect()
    {
        if (RobotCount > 0 && !cubes[0].isConnected)
        {
            await cubeManager.ReConnectAll();
            connectButton.text = "Connected (" + cubes.Length + ")";
            connectButton.color = MockupTheme.Accent;
            initialized = false;
            initReached = new bool[cubes.Length];
            connected = true;
            SetLeds();
        }
    }

    int ipx1 = 127; int ipy1 = 165; int ipx2 = 183; int ipy2 = 165; int ipx3 = 242; int ipy3 = 165; int ipx4 = 302; int ipy4 = 165; int ipx5 = 360; int ipy5 = 165;
    bool r1 = false; bool r2 = false; bool r3 = false; bool r4 = false; bool r5 = false;
    void MoveC1(){
        if (cubes[0].x != ipx1 && cubes[0].y != ipy1){var mv0 = cubeManager.navigators[0].Navi2Target(ipx1, ipy1, maxSpd:speed).Exec(); if(mv0.reached){r1 = true;}}
    }
    void MoveC2(){
        if (cubes[1].x != ipx2 && cubes[1].y != ipy2){var mv1 = cubeManager.navigators[1].Navi2Target(ipx2, ipy2, maxSpd:speed).Exec(); if(mv1.reached){r2 = true; mc2 = false;}}
    }
    void MoveC3(){
        if (cubes[2].x != ipx3 && cubes[2].y != ipy3){var mv2 = cubeManager.navigators[2].Navi2Target(ipx3, ipy3, maxSpd:speed).Exec(); if(mv2.reached){r3 = true; mc3 = false;}}
    }
    void MoveC4(){
        if (cubes[3].x != ipx4 && cubes[3].y != ipy4){var mv3 = cubeManager.navigators[3].Navi2Target(ipx4, ipy4, maxSpd:speed).Exec(); if(mv3.reached){r4 = true; mc4 = false;}}
    }
    void MoveC5(){
        if (cubes[4].x != ipx5 && cubes[4].y != ipy5){var mv4 = cubeManager.navigators[4].Navi2Target(ipx5, ipy5, maxSpd:speed).Exec(); if(mv4.reached){r5 = true; mc5 = false;}}
    }
    // Start position for robot i: Samira's 5 spots in a row, spread evenly when more robots connect
    Vector2Int HomePosition(int i){
        int n = RobotCount;
        if (n <= LEGACY_ROBOTS){
            int[] xs = { ipx1, ipx2, ipx3, ipx4, ipx5 };
            return new Vector2Int(xs[i], ipy1);
        }
        int left = 80; int right = 420;
        return new Vector2Int(left + (right - left) * i / (n - 1), ipy1);
    }
    void Initialize(){
        bool all = true;
        for (int i = 0; i < RobotCount; i++){
            if (initReached[i]){ continue; }
            var home = HomePosition(i);
            var mv = cubeManager.navigators[i].Navi2Target(home.x, home.y, maxSpd:speed).Exec();
            if (mv.reached){ initReached[i] = true; } else { all = false; }
        }
        if (all){ initialized = true; }
    }
    void OnPressButton(Cube c)
    {
        if (c.isPressed)
        {
            switch (ledColour){
            case 1:
                //c.TurnLedOn(255,0,0,0);
                ledColour = 2;
                break;
            case 2:
                //c.TurnLedOn(0,255,0,0);
                ledColour = 3;
                break;
            case 3:
                //c.TurnLedOn(0,0,255,0);
                ledColour = 1;
                break;
            }
        }
        if (haptic){
            c.Move(speed, speed, duration);
        }
        if (sound){
            c.PlayPresetSound(0);
        }
    }

    void OnDoubleTap(Cube c)
    {
        if (sound) {c.PlayPresetSound(3);}
        //c.TurnLedOn(0,0,255,100);
        c.Move(speed, -speed, duration);
    }
    void OnShake(Cube c)
    {
        if (c.shakeLevel >= 1 ){
            if (sound) {c.PlayPresetSound(8);}
            
            switch (ledColour){
                case 1:
                    //c.TurnLedOn(255,0,0,0);
                    ledColour = 2;
                    break;
                case 2:
                    //c.TurnLedOn(0,255,0,0);
                    ledColour = 3;
                    break;
                case 3:
                    //c.TurnLedOn(0,0,255,0);
                    ledColour = 1;
                    break;
            }
            if (haptic) {c.Move(speed, speed, duration);}
        }
    }
    void OnSlope(Cube c)
    {
        if(sound){c.PlayPresetSound(9);}
        //c.TurnLedOn(255,0,0,100);
        if (haptic) {c.Move(speed, speed, duration);}
    }
    void Flick(){
        if (cubes[0].x != fx1 || cubes[0].y != fy1){phase1 = 1;}
        if (phase1 == 1) {
            var mv = cubeManager.navigators[0].Navi2Target(fx1, fy1, maxSpd:speed).Exec();
            if (mv.reached){phase1 = 0;}
        }
        if (cubes[1].x != fx2 || cubes[1].y != fy2){phase2 = 1;}
        if (phase2 == 1) {
            var mv = cubeManager.navigators[1].Navi2Target(fx2, fy2, maxSpd:speed).Exec();
            if (mv.reached){phase2 = 0;}
        }
        if (cubes[2].x != fx3 || cubes[2].y != fy3){phase3 = 1;}
        if (phase3 == 1) {
            var mv = cubeManager.navigators[2].Navi2Target(fx3, fy3, maxSpd:speed).Exec();
            if (mv.reached){phase3 = 0;}
        }
        if (cubes[3].x != fx4 || cubes[3].y != fy4){phase4 = 1;}
        if (phase4 == 1) {
            var mv = cubeManager.navigators[3].Navi2Target(fx4, fy4, maxSpd:speed).Exec();
            if (mv.reached){phase4 = 0;}
        }
        if (cubes[4].x != fx5 || cubes[4].y != fy5){phase5 = 1;}
        if (phase5 == 1) {
            var mv = cubeManager.navigators[4].Navi2Target(fx5, fy5, maxSpd:speed).Exec();
            if (mv.reached){phase5 = 0;}
        }
    }
    void Magnet(){
        if (cubeManager.synced){
            //cubes[1].Move(30, 30, 100);
            int x1 = cubes[0].x;
            int y1 = cubes[0].y;
            int x2 = cubes[1].x;
            int y2 = cubes[1].y;
            
            int centerX = (x1 + x2) / 2;
            int centerY = (y1 + y2) / 2;
            
            float radiusAttract = 10;
            float radiusOutofBound = radius;
            float distanceCube1 = Mathf.Sqrt(Mathf.Pow(x1 - centerX, 2) + Mathf.Pow(y1 - centerY, 2));
            float distanceCube2 = Mathf.Sqrt(Mathf.Pow(x2 - centerX, 2) + Mathf.Pow(y2 - centerY, 2));

            bool isCube1Outside = (distanceCube1 > radiusAttract && distanceCube1 < radiusOutofBound);
            bool isCube2Outside = (distanceCube2 > radiusAttract && distanceCube2 < radiusOutofBound);

            if (isCube1Outside && isCube2Outside){
                moveCube1 = 1; moveCube2 = 1;
            }
            if (moveCube1 == 1){
                if (cubes[0].x >= centerX){
                    xc1 = centerX + 5;
                }
                else{
                    xc1 = centerX - 5;
                }
                var mv = cubeManager.navigators[0].Navi2Target(xc1, centerY, maxSpd:speed).Exec();
                if (mv.reached){
                    moveCube1 = 0;
                } 
            }
            if (moveCube2 == 1){
                if (cubes[1].x >= centerX){
                    xc2 = centerX + 5;
                }
                else{
                    xc2 = centerX - 5;
                }
                var mv = cubeManager.navigators[1].Navi2Target(xc2, centerY, maxSpd:speed).Exec();
                if (mv.reached){
                    moveCube2 = 0;
                } 
            }
        }
    }
    void Repel(){
        if (cubeManager.synced){
            int x1 = cubes[0].x;
            int y1 = cubes[0].y;
            int x2 = cubes[1].x;
            int y2 = cubes[1].y;
            int centerX = (x1 + x2) / 2;
            int centerY = (y1 + y2) / 2;
            float radiusRepel = radius;
            float distanceCube1 = Mathf.Sqrt(Mathf.Pow(x1 - centerX, 2) + Mathf.Pow(y1 - centerY, 2));
            float distanceCube2 = Mathf.Sqrt(Mathf.Pow(x2 - centerX, 2) + Mathf.Pow(y2 - centerY, 2));
            bool isCube1Inside = (distanceCube1 < radiusRepel);
            bool isCube2Inside = (distanceCube2 < radiusRepel);
            if (isCube1Inside && isCube2Inside){
                moveCube1 = 1; moveCube2 = 1;
            }
            if (moveCube1 == 1){
                // calculate direction vector
                int dx = cubes[0].x - centerX;
                int dy = cubes[0].y - centerY;
                // normalize the vector
                double mag = Math.Sqrt(dx*dx + dy*dy);
                double ux = dx/mag;
                double uy = dy/mag;
                xc1 = centerX + (int)Math.Round(ux*radiusRepel);
                yc1 = centerY + (int)Math.Round(uy*radiusRepel);
                var mv = cubeManager.navigators[0].Navi2Target(xc1, yc1, maxSpd:speed).Exec();
                if (mv.reached){
                    moveCube1 = 0;
                } 
            }
            if (moveCube2 == 1){
                // calculate direction vector
                int dx = cubes[1].x - centerX;
                int dy = cubes[1].y - centerY;
                // normalize the vector
                double mag = Math.Sqrt(dx*dx + dy*dy);
                double ux = dx/mag;
                double uy = dy/mag;
                xc2 = centerX + (int)Math.Round(ux*radiusRepel);
                yc2 = centerY + (int)Math.Round(uy*radiusRepel);
                var mv = cubeManager.navigators[1].Navi2Target(xc2, yc2, maxSpd:speed).Exec();
                if (mv.reached){
                    moveCube2 = 0;
                } 
            }
        }
    }
    int moveCube = 0;
    void RemoteMat(){
        if (cubeManager.synced){
            int x1 = cubes[0].x;
            int y1 = cubes[0].y;
            // 4 possible cases:
            // 1. cube 1 x and y are less than cube 2 x and y
            // 2. cube 1 x smaller y bigger
            // 3. cube 1 x bigger y smaller
            // 4. cube 1 x and y are bigger than cube 2 x and y
            if(cubes[1].y != 0){
                if (cubes[1].x - cubes[0].x > 30 && cubes[1].y - cubes[0].y > 30){moveCube = 1;}
                else if (cubes[1].x - cubes[0].x > 30 && cubes[1].y - cubes[0].y <= 30 && cubes[0].x < 250 && cubes[0].y < 250){moveCube = 1;}
                else if (cubes[1].y - cubes[0].y > 30 && cubes[1].x - cubes[0].x <= 30 && cubes[0].x < 250 && cubes[0].y < 250){moveCube = 1;}
                else if (cubes[1].x - cubes[0].x > 30 && cubes[0].y - cubes[1].y > 30){moveCube = 2;}
                else if (cubes[1].x - cubes[0].x > 30 && cubes[0].y - cubes[1].y <= 30 && cubes[0].x < 250 && cubes[0].y > 250){moveCube = 2;}
                else if (cubes[0].y - cubes[1].y > 30 && cubes[1].x - cubes[0].x <= 30 && cubes[0].x < 250 && cubes[0].y > 250){moveCube = 2;}
                else if (cubes[0].x - cubes[1].x > 30 && cubes[1].y - cubes[0].y > 30){moveCube = 3;}
                else if (cubes[0].x - cubes[1].x > 30 && cubes[1].y - cubes[0].y <= 30 && cubes[0].x > 250 && cubes[0].y < 250){moveCube = 3;}
                else if (cubes[1].y - cubes[0].y > 30 && cubes[0].x - cubes[1].x <= 30 && cubes[0].x > 250 && cubes[0].y < 250){moveCube = 3;}
                else if (cubes[0].x - cubes[1].x > 30 && cubes[0].y - cubes[1].y > 30){moveCube = 4;}
                else if (cubes[0].x - cubes[1].x > 30 && cubes[0].y - cubes[1].y <= 30 && cubes[0].x > 250 && cubes[0].y > 250){moveCube = 4;}
                else if (cubes[0].y - cubes[1].y > 30 && cubes[0].x - cubes[1].x <= 30 && cubes[0].x > 250 && cubes[0].y > 250){moveCube = 4;}
                
                if (numOfCubes == 2){
                    if (moveCube == 1){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y + 25, maxSpd:speed).Exec();
                        if (mv.reached){moveCube = 0;} 
                    }
                    if (moveCube == 2){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y - 25, maxSpd:speed).Exec();
                        if (mv.reached){moveCube = 0;} 
                    }
                    if (moveCube == 3){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y + 25, maxSpd:speed).Exec();
                        if (mv.reached){moveCube = 0;} 
                    }
                    if (moveCube == 4){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y - 25, maxSpd:speed).Exec();
                        if (mv.reached){moveCube = 0;} 
                    }
                }
                if (numOfCubes == 3){
                    if (moveCube == 1){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y + 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x + 35, cubes[0].y + 35, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached){moveCube = 0;} 
                    }
                    if (moveCube == 2){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y - 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x + 35, cubes[0].y - 35, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached){moveCube = 0;} 
                    }
                    if (moveCube == 3){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y + 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x - 35, cubes[0].y + 35, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached){moveCube = 0;} 
                    }
                    if (moveCube == 4){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y - 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x - 35, cubes[0].y - 35, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached){moveCube = 0;} 
                    }
                }
                if (numOfCubes == 4){
                    if (moveCube == 1){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y + 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x + 35, cubes[0].y + 35, maxSpd:speed).Exec();
                        var mv3 = cubeManager.navigators[3].Navi2Target(cubes[0].x + 45, cubes[0].y + 45, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached && mv3.reached){moveCube = 0;} 
                    }
                    if (moveCube == 2){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y - 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x + 35, cubes[0].y - 35, maxSpd:speed).Exec();
                        var mv3 = cubeManager.navigators[3].Navi2Target(cubes[0].x + 45, cubes[0].y - 45, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached && mv3.reached){moveCube = 0;} 
                    }
                    if (moveCube == 3){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y + 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x - 35, cubes[0].y + 35, maxSpd:speed).Exec();
                        var mv3 = cubeManager.navigators[3].Navi2Target(cubes[0].x - 45, cubes[0].y + 45, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached && mv3.reached){moveCube = 0;} 
                    }
                    if (moveCube == 4){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y - 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x - 35, cubes[0].y - 35, maxSpd:speed).Exec();
                        var mv3 = cubeManager.navigators[3].Navi2Target(cubes[0].x - 45, cubes[0].y - 45, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached && mv3.reached){moveCube = 0;} 
                    }
                }
                if (numOfCubes == 5){
                    if (moveCube == 1){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y + 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x + 35, cubes[0].y + 35, maxSpd:speed).Exec();
                        var mv3 = cubeManager.navigators[3].Navi2Target(cubes[0].x + 45, cubes[0].y + 45, maxSpd:speed).Exec();
                        var mv4 = cubeManager.navigators[4].Navi2Target(cubes[0].x + 55, cubes[0].y + 55, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached && mv3.reached && mv4.reached){moveCube = 0;} 
                    }
                    if (moveCube == 2){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x + 25, cubes[0].y - 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x + 35, cubes[0].y - 35, maxSpd:speed).Exec();
                        var mv3 = cubeManager.navigators[3].Navi2Target(cubes[0].x + 45, cubes[0].y - 45, maxSpd:speed).Exec();
                        var mv4 = cubeManager.navigators[4].Navi2Target(cubes[0].x + 55, cubes[0].y - 55, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached && mv3.reached && mv4.reached){moveCube = 0;} 
                    }
                    if (moveCube == 3){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y + 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x - 35, cubes[0].y + 35, maxSpd:speed).Exec();
                        var mv3 = cubeManager.navigators[3].Navi2Target(cubes[0].x - 45, cubes[0].y + 45, maxSpd:speed).Exec();
                        var mv4 = cubeManager.navigators[4].Navi2Target(cubes[0].x - 55, cubes[0].y + 55, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached && mv3.reached && mv4.reached){moveCube = 0;} 
                    }
                    if (moveCube == 4){
                        var mv = cubeManager.navigators[1].Navi2Target(cubes[0].x - 25, cubes[0].y - 25, maxSpd:speed).Exec();
                        var mv2 = cubeManager.navigators[2].Navi2Target(cubes[0].x - 35, cubes[0].y - 35, maxSpd:speed).Exec();
                        var mv3 = cubeManager.navigators[3].Navi2Target(cubes[0].x - 45, cubes[0].y - 45, maxSpd:speed).Exec();
                        var mv4 = cubeManager.navigators[4].Navi2Target(cubes[0].x - 55, cubes[0].y - 55, maxSpd:speed).Exec();
                        if (mv.reached && mv2.reached && mv3.reached && mv4.reached){moveCube = 0;} 
                    }
                }
            }
        }
    }
    void RemoteContinuous(){
        //cubes[0].TurnLedOn(0,255,0,1000);
        if (numOfCubes == 2){
            if (cubes[0].pose.ToString() == front){cubes[1].Move(speed, speed, 100);}
            if (cubes[0].pose.ToString() == back){cubes[1].Move(-speed, -speed, 100);}
            if (cubes[0].pose.ToString() == left){cubes[1].Move(10, -10, 50);}
            if (cubes[0].pose.ToString() == right){cubes[1].Move(-10, 10, 50);}
        }
        if (numOfCubes == 3){
            if (cubes[0].pose.ToString() == front){cubes[1].Move(speed, speed, 100); cubes[2].Move(speed, speed, 100);}
            if (cubes[0].pose.ToString() == back){cubes[1].Move(-speed, -speed, 100); cubes[2].Move(-speed, -speed, 100);}
            if (cubes[0].pose.ToString() == left){cubes[1].Move(10, -10, 50); cubes[2].Move(10, -10, 50);}
            if (cubes[0].pose.ToString() == right){cubes[1].Move(-10, 10, 50); cubes[2].Move(-10, 10, 50);}
        }
        if (numOfCubes == 4){
            if (cubes[0].pose.ToString() == front){cubes[1].Move(speed, speed, 100); cubes[2].Move(speed, speed, 100); cubes[3].Move(speed, speed, 100);}
            if (cubes[0].pose.ToString() == back){cubes[1].Move(-speed, -speed, 100); cubes[2].Move(-speed, -speed, 100); cubes[3].Move(-speed, -speed, 100);}
            if (cubes[0].pose.ToString() == left){cubes[1].Move(10, -10, 50); cubes[2].Move(10, -10, 50); cubes[3].Move(10, -10, 50);}
            if (cubes[0].pose.ToString() == right){cubes[1].Move(-10, 10, 50); cubes[2].Move(-10, 10, 50); cubes[3].Move(-10, 10, 50);}
        }
        if (numOfCubes == 5){
            if (cubes[0].pose.ToString() == front){cubes[1].Move(speed, speed, 100); cubes[2].Move(speed, speed, 100); cubes[3].Move(speed, speed, 100); cubes[4].Move(speed, speed, 100);}
            if (cubes[0].pose.ToString() == back){cubes[1].Move(-speed, -speed, 100); cubes[2].Move(-speed, -speed, 100); cubes[3].Move(-speed, -speed, 100); cubes[4].Move(-speed, -speed, 100);}
            if (cubes[0].pose.ToString() == left){cubes[1].Move(10, -10, 50); cubes[2].Move(10, -10, 50); cubes[3].Move(10, -10, 50); cubes[4].Move(10, -10, 50);}
            if (cubes[0].pose.ToString() == right){cubes[1].Move(-10, 10, 50); cubes[2].Move(-10, 10, 50); cubes[3].Move(-10, 10, 50); cubes[4].Move(-10, 10, 50);}
        }
    }
    string front = "Front"; string back = "Back"; string left = "Left"; string right = "Right"; string currPose = "Up";
    void OnPose(Cube c) // remote incremental
    {
        if (numOfCubes == 2){
            if (c.pose.ToString() == front){cubes[1].Move(speed, speed, 1000);}
            if (c.pose.ToString() == back){cubes[1].Move(-speed, -speed, 1000);}
            if (c.pose.ToString() == left){cubes[1].Move(50, -50, 140);}
            if (c.pose.ToString() == right){cubes[1].Move(-50, 50, 140);}
        }
        if (numOfCubes == 3){
            if (c.pose.ToString() == front){cubes[1].Move(speed, speed, 1000); cubes[2].Move(speed, speed, 1000);}
            if (c.pose.ToString() == back){cubes[1].Move(-speed, -speed, 1000); cubes[2].Move(-speed, -speed, 1000);}
            if (c.pose.ToString() == left){cubes[1].Move(50, -50, 140); cubes[2].Move(50, -50, 140);}
            if (c.pose.ToString() == right){cubes[1].Move(-50, 50, 140); cubes[2].Move(-50, 50, 140);}
        }
        if (numOfCubes == 4) {
            if (c.pose.ToString() == front){cubes[1].Move(speed, speed, 1000); cubes[2].Move(speed, speed, 1000); cubes[3].Move(speed, speed, 1000);}
            if (c.pose.ToString() == back){cubes[1].Move(-speed, -speed, 1000); cubes[2].Move(-speed, -speed, 1000); cubes[3].Move(-speed, -speed, 1000);}
            if (c.pose.ToString() == left){cubes[1].Move(50, -50, 140); cubes[2].Move(50, -50, 140); cubes[3].Move(50, -50, 140);}
            if (c.pose.ToString() == right){cubes[1].Move(-50, 50, 140); cubes[2].Move(-50, 50, 140); cubes[3].Move(-50, 50, 140);}
        }
        if (numOfCubes == 5){
            if (c.pose.ToString() == front){cubes[1].Move(speed, speed, 1000); cubes[2].Move(speed, speed, 1000); cubes[3].Move(speed, speed, 1000); cubes[4].Move(speed, speed, 1000);}
            if (c.pose.ToString() == back){cubes[1].Move(-speed, -speed, 1000); cubes[2].Move(-speed, -speed, 1000); cubes[3].Move(-speed, -speed, 1000); cubes[4].Move(-speed, -speed, 1000);}
            if (c.pose.ToString() == left){cubes[1].Move(50, -50, 140); cubes[2].Move(50, -50, 140); cubes[3].Move(50, -50, 140); cubes[4].Move(50, -50, 140);}
            if (c.pose.ToString() == right){cubes[1].Move(-50, 50, 140); cubes[2].Move(-50, 50, 140); cubes[3].Move(-50, 50, 140); cubes[4].Move(-50, 50, 140);}
        }
    }
    // Circle: the first circleNumOfCubes connected robots form the ring,
    // any others wait at their start position.
    int cX = 250; int cY = 250;
    const int CIRCLE_TOLERANCE = 8; // mat units; stops robots re-correcting forever at their spot
    Vector2Int[] circleTargets = new Vector2Int[0];
    bool[] circleMoving = new bool[0];
    void GetCirclePositions(){
        int n = RobotCount;
        if (n == 0){ return; }
        int inRing = Mathf.Clamp(circleNumOfCubes, 1, n);
        circleTargets = new Vector2Int[n];
        circleMoving = new bool[n];
        for (int i = 0; i < n; i++){
            if (i < inRing){
                double angle = 2 * Math.PI * i / inRing;
                circleTargets[i] = new Vector2Int((int)(cX + circleRadius * Math.Cos(angle)), (int)(cY + circleRadius * Math.Sin(angle)));
            } else {
                circleTargets[i] = HomePosition(i);
            }
        }
    }
    void Circle(){
        if (circleTargets.Length != RobotCount){ GetCirclePositions(); }
        for (int i = 0; i < RobotCount; i++){
            var target = circleTargets[i];
            int dx = cubes[i].x - target.x; int dy = cubes[i].y - target.y;
            if (dx * dx + dy * dy > CIRCLE_TOLERANCE * CIRCLE_TOLERANCE){ circleMoving[i] = true; }
            if (circleMoving[i]){
                var mv = cubeManager.navigators[i].Navi2Target(target.x, target.y, maxSpd:150).Exec();
                if (mv.reached){ circleMoving[i] = false; }
            }
        }
    }
    int ph = 0; int mvCube = 0;
    //int checkposY = 335; int checkposX = 345; int range = 10;
    //int px1 = 345; int py1 = 335; int px2 = 236; int py2 = 335; int px3 = 120; int py3 = 335; int px4 = 120; int py4 = 120; int px5 = 345; int py5 = 120;
    //int checkposY = 295; int checkposX = 314; int range = 10;
    // int px1 = 314; int py1 = 295; int px2 = 236; int py2 = 294; int px3 = 154; int py3 = 299; int px4 = 151; int py4 = 200; int px5 = 239; int py5 = 202;
    int checkposX = 330; int checkposY = 305; int range = 10;
    int px1 = 330; int py1 = 305; int px2 = 245; int py2 = 305; int px3 = 145; int py3 = 305; int px4 = 145; int py4 = 185; int px5 = 245; int py5 = 185;
   
    void MultiFlick(){
        int x1 = cubes[0].x;
        int y1 = cubes[0].y;

        if (ph == 0){
            if (cubes[0].x != px1 && cubes[0].y != py1){
                var mv0 = cubeManager.navigators[0].Navi2Target(px1, py1, maxSpd:speed).Exec();
                var mv1 = cubeManager.navigators[1].Navi2Target(px2, py2, maxSpd:speed).Exec();
                var mv2 = cubeManager.navigators[2].Navi2Target(px3, py3, maxSpd:speed).Exec();
                var mv3 = cubeManager.navigators[3].Navi2Target(px4, py4, maxSpd:speed).Exec();
                var mv4 = cubeManager.navigators[4].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
                if (mv0.reached && mv1.reached && mv2.reached && mv3.reached && mv4.reached)
                {
                    ph = 1;
                    cubeManager.navigators[0].ClearOther();
                    cubeManager.navigators[1].ClearOther();
                    cubeManager.navigators[2].ClearOther();
                    cubeManager.navigators[3].ClearOther();
                    cubeManager.navigators[4].ClearOther();
                }
            }
        }
        if (ph == 1){
            if ( cubes[0].y < checkposY - range || cubes[0].y > checkposY + range  || cubes[0].x < checkposX - range || cubes[0].x > checkposX + range){ 
                mvCube = 1;
            }
        }
        if (mvCube == 1){
            var mv0 = cubeManager.navigators[1].Navi2Target(px1, py1, maxSpd:speed).Exec();
            var mv1 = cubeManager.navigators[2].Navi2Target(px2, py2, maxSpd:speed).Exec();
            var mv2 = cubeManager.navigators[3].Navi2Target(px3, py3, maxSpd:speed).Exec();
            var mv3 = cubeManager.navigators[4].Navi2Target(px4, py4, maxSpd:speed).Exec();
            var mv4 = cubeManager.navigators[0].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
            if (mv0.reached || mv1.reached || mv2.reached){ph = 2;mvCube = 0;}
        }
        if (ph == 2){
            if (cubes[1].y < checkposY - range || cubes[1].y > checkposY + range  || cubes[1].x < checkposX - range || cubes[1].x > checkposX + range){ 
                mvCube = 2;
            }
        }
        if (mvCube == 2){
            var mv0 = cubeManager.navigators[2].Navi2Target(px1, py1, maxSpd:speed).Exec();
            var mv1 = cubeManager.navigators[3].Navi2Target(px2, py2, maxSpd:speed).Exec();
            var mv2 = cubeManager.navigators[4].Navi2Target(px3, py3, maxSpd:speed).Exec();
            var mv3 = cubeManager.navigators[0].Navi2Target(px4, py4, maxSpd:speed).Exec();
            var mv4 = cubeManager.navigators[1].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
            if (mv0.reached || mv1.reached || mv2.reached ){ph = 3;mvCube = 0;}
        }
        if (ph == 3){
            if (cubes[2].y < checkposY - range || cubes[2].y > checkposY + range  || cubes[2].x < checkposX - range || cubes[2].x > checkposX + range){
                mvCube = 3;
            }
        }
        if (mvCube == 3){
            var mv0 = cubeManager.navigators[3].Navi2Target(px1, py1, maxSpd:speed).Exec();
            var mv1 = cubeManager.navigators[4].Navi2Target(px2, py2, maxSpd:speed).Exec();
            var mv2 = cubeManager.navigators[0].Navi2Target(px3, py3, maxSpd:speed).Exec();
            var mv3 = cubeManager.navigators[1].Navi2Target(px4, py4, maxSpd:speed).Exec();
            var mv4 = cubeManager.navigators[2].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
            if (mv0.reached || mv1.reached || mv2.reached ){ph = 4;mvCube = 0;}
        }
        if (ph == 4){
            if (cubes[3].y < checkposY - range || cubes[3].y > checkposY + range  || cubes[3].x < checkposX - range || cubes[3].x > checkposX + range){ 
                mvCube = 4;
            }
        }
        if (mvCube == 4){
            var mv0 = cubeManager.navigators[4].Navi2Target(px1, py1, maxSpd:speed).Exec();
            var mv1 = cubeManager.navigators[0].Navi2Target(px2, py2, maxSpd:speed).Exec();
            var mv2 = cubeManager.navigators[1].Navi2Target(px3, py3, maxSpd:speed).Exec();
            var mv3 = cubeManager.navigators[2].Navi2Target(px4, py4, maxSpd:speed).Exec();
            var mv4 = cubeManager.navigators[3].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
            if (mv0.reached || mv1.reached || mv2.reached ){ph = 5;mvCube = 0;}
        }
        if (ph == 5){
            if (cubes[4].y < checkposY - range || cubes[4].y > checkposY + range  || cubes[4].x < checkposX - range || cubes[4].x > checkposX + range){ 
                mvCube = 5;
            }
        }
        if (mvCube == 5){
            var mv0 = cubeManager.navigators[0].Navi2Target(px1, py1, maxSpd:speed).Exec();
            var mv1 = cubeManager.navigators[1].Navi2Target(px2, py2, maxSpd:speed).Exec();
            var mv2 = cubeManager.navigators[2].Navi2Target(px3, py3, maxSpd:speed).Exec();
            var mv3 = cubeManager.navigators[3].Navi2Target(px4, py4, maxSpd:speed).Exec();
            var mv4 = cubeManager.navigators[4].Navi2Target(px5, py5, maxSpd:speed).Exec(); 
            if (mv0.reached || mv1.reached || mv2.reached ){ph = 1;mvCube = 0;}
        }
            
    }
    void Update(){
        if (connected && !initialized && cubeManager.synced){
            Initialize();
        }
        if (flick && cubeManager.synced ){Flick();}
        if (mc1 && cubeManager.synced){MoveC2();}
        if (mc2 && cubeManager.synced){MoveC2();}
        if (mc3 && cubeManager.synced){MoveC3();}
        if (mc4 && cubeManager.synced){MoveC4();}
        if (mc5 && cubeManager.synced){MoveC5();}
        if (magnet && cubeManager.synced){Magnet();}
        if (repel && cubeManager.synced){Repel();}
        //if (remoteType == 1 && cubeManager.synced){RemoteMat();cubes[0].TurnLedOn(0,255,0,1000);}
        if (remoteType == 1 && cubeManager.synced){RemoteMat();}
        if (remoteType == 2 && cubeManager.synced){RemoteContinuous();}
        //if (remoteType == 3 && cubeManager.synced){cubes[0].TurnLedOn(0,255,0,1000);}
        //if (remoteType == 3 && cubeManager.synced){}
        if (circle && cubeManager.synced){Circle();}
        if (multiFlick && cubeManager.synced){MultiFlick();}
    }

    // Duration (durationMs): 0 for unlimited time, so it can be called once and keep running.
    // Order priority (order): Send a one-time instruction safely by setting Cube.ORDER_TYPE.
    // [Details]:
    // When the instruction priority is set to Strong, the system internally adds instructions to the instruction queue and sends them sequentially during the instruction available frame.
    // IsControllable(cube) before the instruction, but in this case, the priority of the instruction is set to Strong, so we do not call cubeManager. IsControllable(cube) and add it to the instruction queue.
    // IsControllable(cube), it will result in instruction loss instead of packet loss.

    //It corresponds to the control function of each operation and later to the button.
    public void Forward() { cubes[0].Move(60, 60, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void Backward() { cubes[0].Move(-40, -40, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void TurnRight() { cubes[0].Move(60, 30, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void TurnLeft() { cubes[0].Move(30, 60, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void Stop() { cubes[0].Move(0, 0, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void PlayPresetSound1() { cubes[0].PlayPresetSound(1); }
    public void PlayPresetSound2() { cubes[0].PlayPresetSound(2); }
    public void LedOn()
    //Here is a function code that allows you to change the color by a trigonometric function. You can try any color you like, or just copy it.
    {
        List<Cube.LightOperation> scenario = new List<Cube.LightOperation>();
        float rad = (Mathf.Deg2Rad * (360.0f / 29.0f));
        for (int i = 0; i < 29; i++)
        {
            byte r = (byte)Mathf.Clamp((128 + (Mathf.Cos(rad * i) * 128)), 0, 255);
            byte g = (byte)Mathf.Clamp((128 + (Mathf.Sin(rad * i) * 128)), 0, 255);
            byte b = (byte)Mathf.Clamp(((Mathf.Abs(Mathf.Cos(rad * i) * 255))), 0, 255);
            scenario.Add(new Cube.LightOperation(100, r, g, b));
        }
        cubes[0].TurnOnLightWithScenario(0, scenario.ToArray());
    }
    public void LedOff() { cubes[0].TurnLedOff(); }
}
