using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Rearranges and restyles the SingleCubeInteraction scene's own UI to the mockup layout
// (title + robot buttons on top, five cards, speed at the bottom; white cards, blue accent,
// square corners) and adds the Routines card. Edits the scene file; existing buttons keep
// their wiring. Safe to run again.
// Menu: Tools > Swarm > Restyle scene UI, or batch: -executeMethod RestyleScene.Run
public static class RestyleScene
{
    const string ScenePath = "Assets/development/SingleCubeInteraction.unity";

    static readonly Color PageBg = Hex(0xF2F5FA);
    static readonly Color CardBg = Color.white;
    static readonly Color Line = Hex(0xC9D6EA);
    static readonly Color Ink = Hex(0x10233F);
    static readonly Color Muted = Hex(0x5A6B85);
    static readonly Color Accent = Hex(0x1F5FD6);
    static readonly Color AccentSoft = Hex(0xE6EEFB);

    // Card centres (canvas units, origin at the centre of a 1280x720 layout)
    static readonly float[] CardX = { -492, -246, 0, 246, 492 };
    const float CardY = -20, CardW = 230, CardH = 470, TitleY = 188;

    static Transform panel;
    static Font font;

    [MenuItem("Tools/Swarm/Restyle scene UI")]
    public static void RunFromMenu() { Apply(); }

    public static void Run()
    {
        Apply();
        EditorApplication.Exit(0);
    }

    static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        var canvas = GameObject.Find("UI/Canvas");
        var group = canvas.GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 1; group.interactable = true; group.blocksRaycasts = true; }
        panel = canvas.transform.Find("Panel");

        // Page background fills the whole canvas so the 3D table behind doesn't show
        var bg = panel.Find("BG") ?? canvas.transform.Find("BG");
        var bgRect = (RectTransform)bg;
        bgRect.SetParent(canvas.transform, true);
        bgRect.SetAsFirstSibling();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = PageBg;

        // Header and robot row
        var title = TextObj("AppTitle", "Swarm Control", 30, FontStyle.Bold, Ink, TextAnchor.MiddleLeft);
        Place(title, -460, 305, 360, 50);
        Place(Get("Connect (1)"), -517, 245, 140, 46); Button(Get("Connect (1)"), true, "+ Add robot");
        Place(Get("ButtonConnect"), -362, 245, 160, 46); Button(Get("ButtonConnect"), false, null);
        Place(Get("ButtonDisconnect"), -192, 245, 160, 46); Button(Get("ButtonDisconnect"), false, null);
        Place(Get("ButtonReconnect"), -22, 245, 160, 46); Button(Get("ButtonReconnect"), false, null);
        for (int k = 2; k <= 5; k++) { Get("Connect (" + k + ")").gameObject.SetActive(false); }
        Remove("RobotHint");

        // Cards
        Card("SimpleIntBG", 0); Card("MagnetBG", 1); Card("RemoteBG", 2); Card("CircleBG", 3);
        var routineCard = Card("RoutineBG", 4);
        routineCard.SetSiblingIndex(Get("CircleBG").GetSiblingIndex() + 1);

        // 1. Single robot
        float x = CardX[0];
        CardTitle("SingleCubeTitle", "Single robot", x);
        Place(Get("Dropdown"), x, 128, 200, 46); Dropdown(Get("Dropdown"));
        Place(Get("Haptic"), x, 70, 200, 46); Button(Get("Haptic"), false, null);
        Place(Get("Sound"), x, 16, 200, 46); Button(Get("Sound"), false, null);
        SliderRow("Duration", "DurationVal ", "DurationSlider", "Reaction time (ms)", x, -45);

        // 2. Magnet (+ multi-flick)
        x = CardX[1];
        CardTitle("MagnetTitle", "Magnet", x);
        Place(Get("Magnet"), x, 128, 200, 46); Button(Get("Magnet"), false, null);
        Place(Get("Repel"), x, 70, 200, 46); Button(Get("Repel"), false, null);
        Place(Get("MultiFlick"), x, 12, 200, 46); Button(Get("MultiFlick"), false, null);
        SliderRow("Radius", "RadiusVal", "RadiusSlider", "Distance", x, -45);

        // 3. Remote
        x = CardX[2];
        CardTitle("RemoteTitle", "Remote", x);
        Place(Get("RemoteDropdown"), x, 128, 200, 46); Dropdown(Get("RemoteDropdown"));
        SliderRow("NumberOfCubes", "NumberOfCubesVal", "NumberOfCubesSlider", "Robots that follow", x, 70);

        // 4. Circle
        x = CardX[3];
        CardTitle("CircleTitle", "Circle", x);
        Place(Get("Circle"), x, 128, 200, 46); Button(Get("Circle"), false, null);
        SliderRow("CircleNumberOfCubes", "CircleNumberOfCubesVal", "CircleNumberOfCubesSlider", "Robots in circle", x, 70);
        SliderRow("CircleRadius", "CircleRadiusVal", "CircleRadiusSlider", "Radius", x, -5);

        // 5. Routines (new)
        x = CardX[4];
        var routineTitle = TextObj("RoutineTitle", "Routines", 24, FontStyle.Bold, Ink, TextAnchor.MiddleLeft);
        Place(routineTitle, x, TitleY, 200, 40);
        var dropdown = LegacyDropdown("RoutineDropdown");
        Place(dropdown.transform, x, 128, 200, 46);
        var play = PlayButton("RoutinePlay");
        Place(play.transform, x, 70, 200, 46);
        var status = TextObj("RoutineStatus", "", 15, FontStyle.Normal, Muted, TextAnchor.UpperLeft);
        Place(status, x, 0, 200, 70);
        Remove("RoutineHint");

        // Speed along the bottom
        Label(Get("Speed"), "Speed (all robots)", 20, TextAnchor.MiddleLeft);
        Place(Get("Speed"), -500, -300, 220, 44);
        Slider(Get("speedSlider"));
        Place(Get("speedSlider"), 20, -300, 760, 26);
        Value(Get("SpeedVal"));
        Place(Get("SpeedVal"), 440, -300, 70, 44);

        // Routine runner on sceneUI, wired to the new controls
        var sceneUI = GameObject.Find("sceneUI");
        var runner = sceneUI.GetComponent<RoutineRunner>();
        if (runner == null) { runner = sceneUI.AddComponent<RoutineRunner>(); }
        runner.app = sceneUI.GetComponent<SingleCubeInteractions>();
        runner.routineDropdown = dropdown;
        runner.playLabel = play.GetComponentInChildren<Text>();
        runner.playImage = play.GetComponent<Image>();
        runner.status = status.GetComponent<Text>();
        var playButton = play.GetComponent<Button>();
        for (int i = playButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--) { UnityEventTools.RemovePersistentListener(playButton.onClick, i); }
        UnityEventTools.AddPersistentListener(playButton.onClick, runner.TogglePlay);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("RestyleScene: done");
    }

    // ---------- pieces ----------

    static Transform Get(string name)
    {
        var t = panel.Find(name);
        if (t == null) { throw new System.Exception("RestyleScene: '" + name + "' not found under Panel"); }
        return t;
    }

    static void Remove(string name)
    {
        var t = panel.Find(name);
        if (t != null) { Object.DestroyImmediate(t.gameObject); }
    }

    static void Place(Transform t, float x, float y, float w, float h)
    {
        var rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static Transform Card(string name, int index)
    {
        var t = panel.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(panel, false);
            t = go.transform;
        }
        Place(t, CardX[index], CardY, CardW, CardH);
        var img = t.GetComponent<Image>();
        img.sprite = null; img.color = CardBg;
        Outline(t.gameObject, Line);
        return t;
    }

    static void CardTitle(string name, string text, float x)
    {
        var tmp = Get(name).GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 24; tmp.fontStyle = FontStyles.Bold; tmp.color = Ink;
        tmp.alignment = TextAlignmentOptions.MidlineLeft; tmp.enableWordWrapping = false;
        Place(tmp.transform, x, TitleY, 200, 40);
    }

    // Toggle/action button: square, white with blue outline (or solid blue when primary)
    static void Button(Transform t, bool primary, string text)
    {
        var img = t.GetComponent<Image>();
        img.sprite = null; img.color = primary ? Accent : CardBg;
        Outline(t.gameObject, Accent);
        var button = t.GetComponent<UnityEngine.UI.Button>();
        var colours = button.colors;
        colours.normalColor = Color.white; colours.highlightedColor = new Color(0.9f, 0.93f, 1f);
        colours.pressedColor = new Color(0.8f, 0.85f, 0.95f); colours.selectedColor = Color.white;
        button.colors = colours;
        var label = t.GetComponentInChildren<Text>();
        if (label != null)
        {
            if (text != null) { label.text = text; }
            label.fontSize = 19; label.fontStyle = FontStyle.Bold; label.color = primary ? Color.white : Accent;
            label.alignment = TextAnchor.MiddleCenter;
        }
    }

    // Her label "buttons" become plain text
    static void Label(Transform t, string text, int size, TextAnchor align)
    {
        var img = t.GetComponent<Image>(); if (img != null) { img.enabled = false; }
        var button = t.GetComponent<UnityEngine.UI.Button>(); if (button != null) { button.interactable = false; button.transition = Selectable.Transition.None; }
        var legacy = t.GetComponentInChildren<Text>();
        if (legacy != null) { legacy.text = text; legacy.fontSize = size; legacy.fontStyle = FontStyle.Normal; legacy.color = Ink; legacy.alignment = align; }
        var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) { tmp.text = text; tmp.fontSize = size; tmp.fontStyle = FontStyles.Normal; tmp.color = Ink; tmp.alignment = TextAlignmentOptions.MidlineLeft; }
    }

    // Her value boxes become plain blue numbers
    static void Value(Transform t)
    {
        var img = t.GetComponent<Image>(); if (img != null) { img.enabled = false; }
        var button = t.GetComponent<UnityEngine.UI.Button>(); if (button != null) { button.interactable = false; button.transition = Selectable.Transition.None; }
        var text = t.GetComponentInChildren<Text>();
        text.fontSize = 20; text.fontStyle = FontStyle.Bold; text.color = Accent; text.alignment = TextAnchor.MiddleRight;
    }

    static void Slider(Transform t)
    {
        var slider = t.GetComponent<UnityEngine.UI.Slider>();
        foreach (var img in t.GetComponentsInChildren<Image>(true)) { img.sprite = null; img.color = AccentSoft; }
        slider.fillRect.GetComponent<Image>().color = Accent;
        var handle = slider.handleRect.GetComponent<Image>();
        handle.color = Accent;
        slider.handleRect.sizeDelta = new Vector2(12, slider.handleRect.sizeDelta.y);
    }

    static void SliderRow(string labelName, string valueName, string sliderName, string text, float x, float y)
    {
        Label(Get(labelName), text, 17, TextAnchor.MiddleLeft);
        Place(Get(labelName), x - 30, y, 140, 32);
        Value(Get(valueName));
        Place(Get(valueName), x + 75, y, 50, 32);
        Slider(Get(sliderName));
        Place(Get(sliderName), x, y - 32, 200, 22);
    }

    static void Dropdown(Transform t)
    {
        var img = t.GetComponent<Image>();
        img.sprite = null; img.color = CardBg;
        Outline(t.gameObject, Accent);
        var label = t.Find("Label").GetComponent<TextMeshProUGUI>();
        label.fontSize = 19; label.color = Ink;
        var arrow = (RectTransform)t.Find("Arrow");
        arrow.GetComponent<Image>().color = Accent;
        arrow.sizeDelta = new Vector2(26, 26);
        arrow.anchoredPosition = new Vector2(-18, 0);
    }

    static Dropdown LegacyDropdown(string name)
    {
        var existing = panel.Find(name);
        GameObject go;
        if (existing != null) { go = existing.gameObject; }
        else
        {
            go = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(panel, false);
        }
        foreach (var img in go.GetComponentsInChildren<Image>(true)) { img.sprite = null; img.color = CardBg; }
        Outline(go, Accent);
        foreach (var text in go.GetComponentsInChildren<Text>(true)) { text.font = font; text.fontSize = 16; text.color = Ink; }
        var arrow = go.transform.Find("Arrow");
        if (arrow != null) { arrow.gameObject.SetActive(false); }
        var template = (RectTransform)go.transform.Find("Template");
        template.sizeDelta = new Vector2(template.sizeDelta.x, 120);
        return go.GetComponent<Dropdown>();
    }

    static GameObject PlayButton(string name)
    {
        var existing = panel.Find(name);
        GameObject go;
        if (existing != null) { go = existing.gameObject; }
        else
        {
            go = DefaultControls.CreateButton(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(panel, false);
        }
        var img = go.GetComponent<Image>();
        img.sprite = null; img.color = Accent;
        var label = go.GetComponentInChildren<Text>();
        label.font = font; label.text = "Play"; label.fontSize = 20; label.fontStyle = FontStyle.Bold; label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        return go;
    }

    static Transform TextObj(string name, string content, int size, FontStyle style, Color colour, TextAnchor align)
    {
        var t = panel.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(panel, false);
            t = go.transform;
        }
        var text = t.GetComponent<Text>();
        text.font = font; text.text = content; text.fontSize = size; text.fontStyle = style; text.color = colour;
        text.alignment = align; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    static void Outline(GameObject go, Color colour)
    {
        var outline = go.GetComponent<Outline>();
        if (outline == null) { outline = go.AddComponent<Outline>(); }
        outline.effectColor = colour;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
