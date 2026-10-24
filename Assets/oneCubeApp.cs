using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using TMPro;
using UnityEngine.UI;
using System;

public class oneCubeApp : MonoBehaviour
{
   CubeManager cubeManager;
    Cube[] cubes; Cube cube;
    bool initialized = false; bool connected = false;
    int ledColour = 1; bool r = true; bool b = false; bool g = false;
    bool haptic = true; bool sound = false;
    bool flick = false; int x = 0; int y = 0; int phase = 0;

    int speed = 100;
    int red = 100; int green = 100; int blue = 100; bool ledtry = false;
    int duration = 300;
    
    public TextMeshProUGUI output;
    public Text connectButton;
    public Text hapticButton; public Text soundButton;
    public Slider speedSlider; public Text speedValue;
    public Slider redSlider; public Text redValue;
    public Slider greenSlider; public Text greenValue;
    public Slider blueSlider; public Text blueValue;
    public Slider durationSlider; public Text durationValue;
    public Text ledColourButton;
    

    

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
    public void LedToggle(){
        if (ledtry){
            ledtry = false;
            ledColourButton.text = "Try Colour";
            ledColourButton.color = Color.black;
        } else {
            HandleInputData(0);
            ledtry = true;
            ledColourButton.text = "LED On";
            ledColourButton.color = Color.green;
            HandleInputData(0);
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
        }
    }
    public void HandleSpeed(int val){
        speed = val;
        speedValue.text = val.ToString();
    }
    public void HandleRed(int val){
        red = val;
        redValue.text = val.ToString();
    }
    public void HandleGreen(int val){
        green = val;
        greenValue.text = val.ToString();
    }
    public void HandleBlue(int val){
        blue = val;
        blueValue.text = val.ToString();
    }
    public void HandleDuration(int val){
        duration = val;
        durationValue.text = val.ToString();
    }
    
    public void HandleInputData(int val){ //Single Interaction
        if (val == 0){
            cube.doubleTapCallback.RemoveListener("EventScene");
            cube.shakeCallback.RemoveListener("EventScene");
            cube.slopeCallback.RemoveListener("EventScene");
            flick = false;
        }
        if (val == 1){
            cube.doubleTapCallback.AddListener("EventScene", OnDoubleTap);
            cube.shakeCallback.RemoveListener("EventScene");
            cube.slopeCallback.RemoveListener("EventScene");
            
            flick = false;
        }
        if (val == 2){
            cube.shakeCallback.AddListener("EventScene", OnShake);
            cube.slopeCallback.RemoveListener("EventScene");
            cube.doubleTapCallback.RemoveListener("EventScene");
            
            flick = false;
        }
        if (val == 3){
            cube.slopeCallback.AddListener("EventScene", OnSlope);
            cube.shakeCallback.RemoveListener("EventScene");
            cube.doubleTapCallback.RemoveListener("EventScene");
            
            flick = false;
        }
        if (val == 4){
            flick = true;
            x = cube.x;
            y = cube.y;
            
            cube.doubleTapCallback.RemoveListener("EventScene");
            cube.shakeCallback.RemoveListener("EventScene");
            cube.slopeCallback.RemoveListener("EventScene");
        }

    }

   
    // Start is called before the first frame update
    void Start()
    {
        cubeManager = new CubeManager(ConnectType.Real); // remove connect type to publish
        hapticButton = GameObject.Find("HapticButtonText").GetComponent<Text>();
        soundButton = GameObject.Find("SoundButtonText").GetComponent<Text>();
        speedSlider = GameObject.Find("speedSlider").GetComponent<Slider>();
        speedSlider.onValueChanged.AddListener(delegate {HandleSpeed((int)speedSlider.value);});
        speedValue = GameObject.Find("SpeedValText").GetComponent<Text>();
        connectButton = GameObject.Find("ConnectText").GetComponent<Text>();
        ledColourButton = GameObject.Find("LedColourText").GetComponent<Text>();
        redSlider = GameObject.Find("RedSlider").GetComponent<Slider>();
        redSlider.onValueChanged.AddListener(delegate {HandleRed((int)redSlider.value);});
        redValue = GameObject.Find("RedValText").GetComponent<Text>();
        greenSlider = GameObject.Find("GreenSlider").GetComponent<Slider>();
        greenSlider.onValueChanged.AddListener(delegate {HandleGreen((int)greenSlider.value);});
        greenValue = GameObject.Find("GreenValText").GetComponent<Text>();
        blueSlider = GameObject.Find("BlueSlider").GetComponent<Slider>();
        blueSlider.onValueChanged.AddListener(delegate {HandleBlue((int)blueSlider.value);});
        blueValue = GameObject.Find("BlueValText").GetComponent<Text>();
        durationSlider = GameObject.Find("DurationSlider").GetComponent<Slider>();
        durationSlider.onValueChanged.AddListener(delegate {HandleDuration((int)durationSlider.value);});
        durationValue = GameObject.Find("DurationValText").GetComponent<Text>();
        int dropval = GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value;

        // GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().onValueChanged.AddListener(delegate {HandleInputData(GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value);});
    }
    public async void Connect()
    {
        if (cube == null)
        {
            cube = await cubeManager.SingleConnect();
            cube.buttonCallback.AddListener("EventScene", OnPressButton);
            connectButton.text = "Connected";
            connectButton.color = Color.green;
        }
    }
    
    public void Disconnect()
    {
        if (cube.isConnected)
        {
            cubeManager.DisconnectAll();
            connectButton.text = "Connect";
            connectButton.color = Color.black;
        }
    }

    public async void ReConnect()
    {
        if (cube == null)
        {
            await cubeManager.ReConnectAll();
            connectButton.text = "Connected";
            connectButton.color = Color.green;
        }
    }

    void OnPressButton(Cube c)
    {
        if (c.isPressed)
        {
            HandleInputData(0);
            GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
            if(ledtry){LedToggle();}
            switch (ledColour){
            case 1:
                c.TurnLedOn(255,0,0,0);
                ledColour = 2;
                break;
            case 2:
                c.TurnLedOn(0,255,0,0);
                ledColour = 3;
                break;
            case 3:
                c.TurnLedOn(0,0,255,0);
                ledColour = 1;
                break;
            }
            //cube.Move(speed, speed, 20);
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
        c.TurnLedOn(red,green,blue,100);
        c.Move(speed, -speed, duration);
    }
    void OnShake(Cube c)
    {
        if (c.shakeLevel >= 1 ){
            if (sound) {c.PlayPresetSound(8);}
            
            switch (ledColour){
                case 1:
                    c.TurnLedOn(255,0,0,0);
                    ledColour = 2;
                    break;
                case 2:
                    c.TurnLedOn(0,255,0,0);
                    ledColour = 3;
                    break;
                case 3:
                    c.TurnLedOn(0,0,255,0);
                    ledColour = 1;
                    break;
            }
            if (haptic) {c.Move(speed, speed, duration);}
        }
    }
    void OnSlope(Cube c)
    {
        if(sound){c.PlayPresetSound(9);}
        c.TurnLedOn(255,0,0,100);
        if (haptic) {c.Move(speed, speed, duration);}
    }
    void Flick(){
        if (cube.x != x || cube.y != y){phase = 1;}
        if (phase == 1) {
            var mv = cubeManager.navigators[0].Navi2Target(x, y, maxSpd:speed).Exec();
            if (mv.reached){phase = 0;}
        }
    }
    
    
    void Update(){
        if (flick && cubeManager.synced ){Flick();}
        if (ledtry && cubeManager.synced){
            cube.TurnLedOn(red, green, blue, 100);
        }
    }

    
    

    // Duration (durationMs): 0 for unlimited time, so it can be called once and keep running.
    // Order priority (order): Send a one-time instruction safely by setting Cube.ORDER_TYPE.
    // [Details]:
    // When the instruction priority is set to Strong, the system internally adds instructions to the instruction queue and sends them sequentially during the instruction available frame.
    // IsControllable(cube) before the instruction, but in this case, the priority of the instruction is set to Strong, so we do not call cubeManager. IsControllable(cube) and add it to the instruction queue.
    // IsControllable(cube), it will result in instruction loss instead of packet loss.

    //It corresponds to the control function of each operation and later to the button.
    public void Forward() { cube.Move(60, 60, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void Backward() { cube.Move(-40, -40, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void TurnRight() { cube.Move(60, 30, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void TurnLeft() { cube.Move(30, 60, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void Stop() { cube.Move(0, 0, durationMs:0, order:Cube.ORDER_TYPE.Strong); }
    public void PlayPresetSound0() { cube.PlayPresetSound(0); HandleInputData(0);}
    public void PlayPresetSound1() { cube.PlayPresetSound(1); HandleInputData(0);}
    public void PlayPresetSound2() { cube.PlayPresetSound(2); HandleInputData(0);}
    public void PlayPresetSound3() { cube.PlayPresetSound(3); HandleInputData(0);}
    public void PlayPresetSound4() { cube.PlayPresetSound(4); HandleInputData(0);}
    public void PlayPresetSound5() { cube.PlayPresetSound(5); HandleInputData(0);}
    public void PlayPresetSound6() { cube.PlayPresetSound(6); HandleInputData(0);}
    public void PlayPresetSound7() { cube.PlayPresetSound(7); HandleInputData(0);}
    public void PlayPresetSound8() { cube.PlayPresetSound(8); HandleInputData(0);}
    public void PlayPresetSound9() { cube.PlayPresetSound(9); HandleInputData(0);}
    public void PlayPresetSound10() { cube.PlayPresetSound(10); HandleInputData(0);}

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
        HandleInputData(0);
        GameObject.Find("Dropdown").GetComponent<TMP_Dropdown>().value = 0;
        cube.TurnOnLightWithScenario(0, scenario.ToArray());
    }
    public void LedOff() { cube.TurnLedOff(); }
}
