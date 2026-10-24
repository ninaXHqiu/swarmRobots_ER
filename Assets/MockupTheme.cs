using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Restyles the SingleCubeInteraction scene to the mockup look (white panels, blue accent,
// square corners) when it loads, so the scene file itself doesn't need hand editing.
// Only colours and shapes change; positions and sizes stay as laid out in the scene.
public static class MockupTheme
{
    public static readonly Color Background = Hex(0xF2F5FA);
    public static readonly Color Panel = Color.white;
    public static readonly Color Border = Hex(0xC9D6EA);
    public static readonly Color Ink = Hex(0x10233F);
    public static readonly Color Accent = Hex(0x1F5FD6);
    public static readonly Color AccentSoft = Hex(0xE6EEFB);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnLoad()
    {
        if (Object.FindObjectOfType<SingleCubeInteractions>() == null) { return; }

        foreach (var cam in Object.FindObjectsOfType<Camera>())
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
        }

        // Slider and dropdown parts get their own styling, so remember them first
        var handled = new System.Collections.Generic.HashSet<Graphic>();
        foreach (var slider in Object.FindObjectsOfType<Slider>(true)) { StyleSlider(slider, handled); }
        foreach (var dropdown in Object.FindObjectsOfType<TMP_Dropdown>(true)) { StyleDropdown(dropdown, handled); }
        foreach (var button in Object.FindObjectsOfType<Button>(true)) { StyleButton(button, handled); }

        foreach (var image in Object.FindObjectsOfType<Image>(true))
        {
            if (handled.Contains(image) || image.GetComponent<Canvas>() != null) { continue; }
            // Remaining images are the coloured section panels
            Square(image, Panel);
            AddBorder(image.gameObject, Border);
        }
        foreach (var text in Object.FindObjectsOfType<Text>(true))
        {
            if (!handled.Contains(text)) { text.color = Ink; }
        }
        foreach (var text in Object.FindObjectsOfType<TextMeshProUGUI>(true))
        {
            if (!handled.Contains(text)) { text.color = Ink; }
        }
    }

    static void StyleButton(Button button, System.Collections.Generic.HashSet<Graphic> handled)
    {
        var image = button.targetGraphic as Image;
        if (image != null)
        {
            Square(image, Panel);
            AddBorder(image.gameObject, Accent);
            handled.Add(image);
        }
        var colours = button.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = AccentSoft;
        colours.selectedColor = Color.white;
        colours.pressedColor = Border;
        button.colors = colours;
        foreach (var text in button.GetComponentsInChildren<Text>(true)) { text.color = Accent; handled.Add(text); }
        foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>(true)) { text.color = Accent; handled.Add(text); }
    }

    static void StyleSlider(Slider slider, System.Collections.Generic.HashSet<Graphic> handled)
    {
        foreach (var image in slider.GetComponentsInChildren<Image>(true))
        {
            handled.Add(image);
            if (slider.fillRect != null && image.transform == slider.fillRect) { Square(image, Accent); }
            else if (slider.handleRect != null && image.transform == slider.handleRect) { Square(image, Accent); }
            else { Square(image, AccentSoft); }
        }
    }

    static void StyleDropdown(TMP_Dropdown dropdown, System.Collections.Generic.HashSet<Graphic> handled)
    {
        foreach (var image in dropdown.GetComponentsInChildren<Image>(true))
        {
            handled.Add(image);
            Square(image, Panel);
        }
        var main = dropdown.targetGraphic as Image;
        if (main != null) { AddBorder(main.gameObject, Accent); }
        foreach (var text in dropdown.GetComponentsInChildren<TextMeshProUGUI>(true)) { text.color = Ink; handled.Add(text); }
    }

    // Dropping the rounded sprite turns an Image into a plain square-cornered rectangle
    static void Square(Image image, Color colour)
    {
        if (image.sprite != null && image.sprite.name.Contains("Checkmark")) { image.color = Accent; return; }
        if (image.sprite != null && image.sprite.name.Contains("Arrow")) { image.color = Ink; return; }
        image.sprite = null;
        image.color = colour;
    }

    static void AddBorder(GameObject target, Color colour)
    {
        var outline = target.GetComponent<Outline>();
        if (outline == null) { outline = target.AddComponent<Outline>(); }
        outline.effectColor = colour;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
    }

    static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
