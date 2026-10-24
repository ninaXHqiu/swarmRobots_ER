using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// New screen for the SingleCubeInteraction scene, laid out like the mockup:
// Robots across the top, then Controls | Behaviours | Routines.
// Samira's original screen stays loaded but invisible; every control here operates
// her hidden control underneath, so all her interaction logic runs unchanged.
public class SwarmUi : MonoBehaviour
{
    // Look (mockup: light background, white cards, blue accent, square corners)
    static readonly Color Bg = Hex(0xF2F5FA);
    static readonly Color Card = Color.white;
    static readonly Color Line = Hex(0xC9D6EA);
    static readonly Color Ink = Hex(0x10233F);
    static readonly Color Muted = Hex(0x5A6B85);
    static readonly Color Accent = Hex(0x1F5FD6);
    static readonly Color AccentSoft = Hex(0xE6EEFB);

    SingleCubeInteractions app;
    Font font;
    readonly List<Action> refreshers = new List<Action>();
    Text status;
    Transform robotChips;
    int shownRobotCount = -1;

    // Routines
    List<SwarmRoutine> routines = new List<SwarmRoutine>();
    Dropdown routineDropdown;
    Text playLabel;
    Image playImage;
    Text routineStatus;
    Coroutine running;
    RoutineContext runningContext;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateOnLoad()
    {
        var app = FindObjectOfType<SingleCubeInteractions>();
        if (app == null) { return; }
        new GameObject("SwarmUi").AddComponent<SwarmUi>().app = app;
    }

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        // Buttons need an EventSystem to receive clicks; the scene file doesn't contain one
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }
        HideOriginalScreen();
        routines = FindRoutines();
        Build();
        Refresh();
        InvokeRepeating(nameof(Refresh), 0.2f, 0.2f);
    }

    void HideOriginalScreen()
    {
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            var group = canvas.gameObject.GetComponent<CanvasGroup>();
            if (group == null) { group = canvas.gameObject.AddComponent<CanvasGroup>(); }
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
        }
    }

    // ---------- Layout ----------

    void Build()
    {
        var canvasGo = new GameObject("SwarmUiCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 760);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = Box(canvasGo.transform, "Root", Bg);
        Stretch(root.rectTransform);
        var rootLayout = Vertical(root.gameObject, 14, new RectOffset(24, 24, 18, 18));
        rootLayout.childForceExpandHeight = false;

        // Header
        var header = Row(root.transform, 12);
        Label(header, "Swarm Control", 26, FontStyle.Bold, Ink).GetComponent<LayoutElement>().flexibleWidth = 1;
        status = Label(header, "", 15, FontStyle.Bold, Accent);
        status.alignment = TextAnchor.MiddleRight;
        status.GetComponent<LayoutElement>().preferredWidth = 360;
        var originalStatus = Find<Text>("ConnectText");
        refreshers.Add(() => status.text = originalStatus.text);

        // Robots
        var robots = CardBox(root.transform, "Robots");
        var robotRow = Row(robots, 10);
        robotRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
        var chips = new GameObject("Chips", typeof(RectTransform));
        chips.transform.SetParent(robotRow, false);
        var chipLayout = chips.AddComponent<HorizontalLayoutGroup>();
        chipLayout.spacing = 6; chipLayout.childForceExpandWidth = false; chipLayout.childControlWidth = false;
        chipLayout.childControlHeight = false; chipLayout.childForceExpandHeight = false;
        chipLayout.childAlignment = TextAnchor.MiddleLeft;
        chips.AddComponent<LayoutElement>().flexibleWidth = 1;
        robotChips = chips.transform;
        ActionButton(robotRow, "+ Add robot", true, () => Click("Connect (1)"), 130);
        ActionButton(robotRow, "Connect", false, () => Click("ButtonConnect"), 110);
        ActionButton(robotRow, "Disconnect", false, () => Click("ButtonDisconnect"), 110);
        ActionButton(robotRow, "Reconnect", false, () => Click("ButtonReconnect"), 110);
        Label(robots, "Press + once per robot (each opens the pairing popup), then Connect.", 13, FontStyle.Normal, Muted);

        // Three columns
        var columns = Row(root.transform, 14);
        columns.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        columns.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

        var controls = CardBox(columns, "Controls", 1.3f);
        Caption(controls, "Touch reaction");
        MirrorDropdown(controls, "Dropdown", o => o.Replace("Douple", "Double"));
        var toggles1 = Row(controls, 8);
        MirrorToggle(toggles1, "Haptic", "HapticButtonText");
        MirrorToggle(toggles1, "Sound", "SoundButtonText");
        MirrorSlider(controls, "Reaction duration", "DurationSlider");
        Caption(controls, "Remote");
        MirrorDropdown(controls, "RemoteDropdown", o => o);
        MirrorSlider(controls, "Robots following the remote", "NumberOfCubesSlider");
        var toggles2 = Row(controls, 8);
        MirrorToggle(toggles2, "Magnet", "MagnetButtonText");
        MirrorToggle(toggles2, "Repel", "RepelButtonText");
        MirrorToggle(toggles2, "Multi-flick", "MultiFlickButtonText");
        MirrorSlider(controls, "Magnet distance", "RadiusSlider");

        var behaviours = CardBox(columns, "Behaviours", 1f);
        MirrorToggle(behaviours, "Circle", "CircleButtonText");
        MirrorSlider(behaviours, "Robots in circle", "CircleNumberOfCubesSlider");
        MirrorSlider(behaviours, "Circle radius", "CircleRadiusSlider");
        Caption(behaviours, "All robots");
        MirrorSlider(behaviours, "Speed", "speedSlider");

        var routinesCard = CardBox(columns, "Routines", 1f);
        Caption(routinesCard, "Routine");
        routineDropdown = NewDropdown(routinesCard);
        var names = routines.Select(r => r.Name + "  (" + r.MinRobots + "+ robots)").ToList();
        if (names.Count == 0) { names.Add("No routines found"); }
        routineDropdown.AddOptions(names);
        var play = ActionButton(routinesCard, "Play", true, TogglePlay, 0);
        playImage = play.GetComponent<Image>();
        playLabel = play.GetComponentInChildren<Text>();
        routineStatus = Label(routinesCard, "Connect robots, then pick a routine", 13, FontStyle.Normal, Muted);
        Label(routinesCard, "Routines live in Assets/Routines. The participant sits at the bottom edge of the mat.", 12, FontStyle.Normal, Muted);
    }

    // ---------- Mirrors of Samira's hidden controls ----------

    void MirrorToggle(Transform parent, string title, string originalTextName)
    {
        var original = Find<Text>(originalTextName);
        var button = ActionButton(parent, title, false, () => original.GetComponentInParent<Button>().onClick.Invoke(), 0);
        var image = button.GetComponent<Image>();
        var label = button.GetComponentInChildren<Text>();
        var outline = button.GetComponent<Outline>();
        refreshers.Add(() =>
        {
            bool on = original.text.EndsWith("On");
            image.color = on ? Accent : Card;
            label.color = on ? Color.white : Accent;
            label.text = title + (on ? "  ON" : "  off");
            outline.effectColor = Accent;
        });
    }

    void MirrorSlider(Transform parent, string title, string originalName)
    {
        var original = Find<Slider>(originalName);
        var block = Vertical(new GameObject(title, typeof(RectTransform)), 4, new RectOffset(0, 0, 2, 2));
        block.transform.SetParent(parent, false);
        var top = Row(block.transform, 6);
        Label(top, title, 14, FontStyle.Normal, Ink).GetComponent<LayoutElement>().flexibleWidth = 1;
        var value = Label(top, "", 14, FontStyle.Bold, Accent);
        value.alignment = TextAnchor.MiddleRight;
        value.GetComponent<LayoutElement>().preferredWidth = 50;

        var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.transform.SetParent(block.transform, false);
        go.AddComponent<LayoutElement>().preferredHeight = 20;
        var slider = go.GetComponent<Slider>();
        foreach (var img in go.GetComponentsInChildren<Image>(true)) { img.sprite = null; img.color = AccentSoft; }
        slider.fillRect.GetComponent<Image>().color = Accent;
        var handle = slider.handleRect.GetComponent<Image>();
        handle.color = Accent;
        slider.handleRect.sizeDelta = new Vector2(12, 0);
        bool syncing = false;
        // Only forward changes the user makes; copying her range/value into this slider must not write back
        slider.onValueChanged.AddListener(v => { if (!syncing) { original.value = v; } });
        refreshers.Add(() =>
        {
            syncing = true;
            slider.minValue = original.minValue; slider.maxValue = original.maxValue;
            slider.wholeNumbers = original.wholeNumbers;
            slider.SetValueWithoutNotify(original.value);
            syncing = false;
            value.text = original.wholeNumbers ? ((int)original.value).ToString() : original.value.ToString("0.0");
        });
    }

    void MirrorDropdown(Transform parent, string originalName, Func<string, string> rename)
    {
        var original = Find<TMP_Dropdown>(originalName);
        var dropdown = NewDropdown(parent);
        dropdown.AddOptions(original.options.Select(o => rename(o.text)).ToList());
        dropdown.onValueChanged.AddListener(v => original.value = v);
        refreshers.Add(() => { if (dropdown.value != original.value) { dropdown.SetValueWithoutNotify(original.value); } });
    }

    void Click(string originalButtonName)
    {
        var go = GameObject.Find(originalButtonName);
        if (go != null) { go.GetComponent<Button>().onClick.Invoke(); }
    }

    void Refresh()
    {
        foreach (var r in refreshers) { r(); }
        RefreshRobots();
    }

    // One square per paired robot: filled in its LED colour when connected
    void RefreshRobots()
    {
        var cubes = app.Manager == null ? null : app.Manager.cubes;
        int count = cubes == null ? 0 : cubes.Count;
        if (count != shownRobotCount)
        {
            foreach (Transform child in robotChips) { Destroy(child.gameObject); }
            for (int i = 0; i < count; i++)
            {
                var chip = Box(robotChips, "Robot" + (i + 1), Card);
                chip.gameObject.AddComponent<LayoutElement>().preferredWidth = 40;
                chip.rectTransform.sizeDelta = new Vector2(40, 40);
                chip.gameObject.AddComponent<Outline>().effectColor = Line;
                var n = Label(chip.transform, (i + 1).ToString(), 15, FontStyle.Bold, Ink);
                n.alignment = TextAnchor.MiddleCenter;
                Stretch(n.rectTransform);
            }
            if (count == 0)
            {
                var none = Label(robotChips, "No robots yet", 14, FontStyle.Normal, Muted);
                none.GetComponent<LayoutElement>().preferredWidth = 160;
            }
            shownRobotCount = count;
        }
        for (int i = 0; i < count && i < robotChips.childCount; i++)
        {
            var chip = robotChips.GetChild(i).GetComponent<Image>();
            if (chip == null) { continue; }
            var led = SingleCubeInteractions.LedColour(i);
            chip.color = cubes[i].isConnected ? (Color)led : Card;
        }
    }

    // ---------- Routines ----------

    static List<SwarmRoutine> FindRoutines()
    {
        var found = new List<SwarmRoutine>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
            foreach (var type in types)
            {
                if (!typeof(SwarmRoutine).IsAssignableFrom(type) || type.IsAbstract) { continue; }
                if (type.GetConstructor(Type.EmptyTypes) == null) { continue; }
                found.Add((SwarmRoutine)Activator.CreateInstance(type));
            }
        }
        return found.OrderBy(r => r.Name).ToList();
    }

    void TogglePlay()
    {
        if (running != null) { StopRoutine("Stopped"); return; }
        if (routines.Count == 0) { routineStatus.text = "Add a routine file under Assets/Routines"; return; }
        var routine = routines[routineDropdown.value];
        var cubes = app.ConnectedRobots;
        int count = cubes == null ? 0 : cubes.Length;
        if (count < routine.MinRobots)
        {
            routineStatus.text = "Needs " + routine.MinRobots + " robots, " + count + " connected";
            return;
        }
        app.StopInteractions();
        runningContext = new RoutineContext { Cubes = cubes, CubeManager = app.Manager };
        running = StartCoroutine(RunRoutine(routine, runningContext));
    }

    IEnumerator RunRoutine(SwarmRoutine routine, RoutineContext ctx)
    {
        SetPlaying(true);
        routineStatus.text = "Running: " + routine.Name;
        yield return routine.Run(ctx);
        running = null;
        SetPlaying(false);
        routineStatus.text = "Finished: " + routine.Name;
    }

    void StopRoutine(string message)
    {
        StopCoroutine(running);
        running = null;
        if (runningContext != null) { Swarm.StopAll(runningContext); }
        SetPlaying(false);
        routineStatus.text = message;
    }

    void SetPlaying(bool playing)
    {
        playLabel.text = playing ? "Stop" : "Play";
        playImage.color = playing ? Hex(0xF2C94C) : Accent;
        playLabel.color = playing ? Ink : Color.white;
    }

    // ---------- Building blocks ----------

    Transform CardBox(Transform parent, string title, float flexibleWidth = 0)
    {
        var card = Box(parent, title, Card);
        card.gameObject.AddComponent<Outline>().effectColor = Line;
        var layout = Vertical(card.gameObject, 10, new RectOffset(16, 16, 14, 14));
        layout.childForceExpandHeight = false;
        var element = card.gameObject.AddComponent<LayoutElement>();
        if (flexibleWidth > 0) { element.flexibleWidth = flexibleWidth; element.minWidth = 260; }
        Label(card.transform, title, 18, FontStyle.Bold, Ink);
        return card.transform;
    }

    void Caption(Transform parent, string text)
    {
        Label(parent, text, 13, FontStyle.Bold, Muted);
    }

    Text Label(Transform parent, string text, int size, FontStyle style, Color colour)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font; t.text = text; t.fontSize = size; t.fontStyle = style; t.color = colour;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        var fit = go.AddComponent<LayoutElement>();
        fit.minHeight = size + 8;
        return t;
    }

    Button ActionButton(Transform parent, string text, bool primary, Action onClick, float width)
    {
        var img = Box(parent, text, primary ? Accent : Card);
        img.gameObject.AddComponent<Outline>().effectColor = Accent;
        var button = img.gameObject.AddComponent<Button>();
        var colours = button.colors;
        colours.highlightedColor = new Color(0.9f, 0.93f, 1f); colours.pressedColor = new Color(0.8f, 0.85f, 0.95f);
        button.colors = colours;
        button.onClick.AddListener(() => onClick());
        var element = img.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 38;
        if (width > 0) { element.preferredWidth = width; } else { element.flexibleWidth = 1; }
        var label = Label(img.transform, text, 15, FontStyle.Bold, primary ? Color.white : Accent);
        label.alignment = TextAnchor.MiddleCenter;
        Stretch(label.rectTransform);
        return button;
    }

    Dropdown NewDropdown(Transform parent)
    {
        var go = DefaultControls.CreateDropdown(new DefaultControls.Resources());
        go.name = "SwarmUiDropdown"; // Samira's code looks up her own dropdown by the name "Dropdown"
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 34;
        foreach (var img in go.GetComponentsInChildren<Image>(true)) { img.sprite = null; img.color = Card; }
        go.AddComponent<Outline>().effectColor = Accent;
        foreach (var t in go.GetComponentsInChildren<Text>(true)) { t.font = font; t.fontSize = 14; t.color = Ink; }
        var arrow = go.transform.Find("Arrow");
        if (arrow != null) { arrow.gameObject.SetActive(false); } // default arrow has no sprite and renders as a square
        var item = go.GetComponentsInChildren<Toggle>(true).FirstOrDefault();
        if (item != null) { item.targetGraphic.color = Card; var c = item.colors; c.highlightedColor = AccentSoft; item.colors = c; }
        var dropdown = go.GetComponent<Dropdown>();
        dropdown.ClearOptions();
        return dropdown;
    }

    Transform Row(Transform parent, float spacing)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
        return go.transform;
    }

    static VerticalLayoutGroup Vertical(GameObject go, float spacing, RectOffset padding)
    {
        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing; layout.padding = padding;
        layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        return layout;
    }

    static Image Box(Transform parent, string name, Color colour)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = colour;
        return img;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    static T Find<T>(string name) where T : Component
    {
        var go = GameObject.Find(name);
        if (go == null) { throw new Exception("SwarmUi: original control '" + name + "' not found in the scene"); }
        return go.GetComponent<T>();
    }

    static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
