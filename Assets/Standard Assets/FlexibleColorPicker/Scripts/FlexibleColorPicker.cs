using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FlexibleColorPicker : MonoBehaviour {
    public enum MainPickingMode {
        HS,
        HV,
        SH,
        SV,
        VH,
        VS
    }

    //constants
    private const float HUE_LOOP = 5.9999f;
    private const string SHADER_MODE = "_Mode";
    private const string SHADER_C1 = "_Color1";
    private const string SHADER_C2 = "_Color2";
    private const string SHADER_DOUBLE_MODE = "_DoubleMode";
    private const string SHADER_HSV = "_HSV";
    private const string SHADER_HSV_MIN = "_HSV_MIN";
    private const string SHADER_HSV_MAX = "_HSV_MAX";


    /*----------------------------------------------------------
    * ----------------------- PARAMETERS -----------------------
    * ----------------------------------------------------------
    */

    //Unity connections
    [Tooltip("Connections to the FCP's picker images, this should not be adjusted unless in advanced use cases.")]
    public Picker[] pickers;

    [Tooltip("Connection to the FCP's hexadecimal input field.")]
    public InputField hexInput;

    [Tooltip("Connection to the FCP's mode dropdown menu.")]
    public Dropdown modeDropdown;

    [Tooltip("The (starting) 2D picking mode, i.e. the 2 color values that can be picked with the large square picker.")]
    public MainPickingMode mode;

    [Tooltip("Sprites to be used in static mode on the main picker, one for each 2D mode.")]
    public Sprite[] staticSpriteMain;

    //public settings
    [Tooltip(
        "Color set to the color picker on Start(). Before the start function, the standard public color variable is redirected to this value, so it may be changed at run time.")]
    public Color startingColor = Color.white;

    [Tooltip("Use static mode: picker images are replaced by static images in stead of adaptive Unity shaders.")]
    public bool staticMode;

    [Tooltip(
        "Make sure FCP seperates its picker materials so that the dynamic mode works consistently, even when multiple FPCs are active at the same time. Turning this off yields a slight performance boost.")]
    public bool multiInstance = true;

    //advanced settings
    [Tooltip("More specific settings for color picker. Changes are not applied immediately, but require an FCP update to trigger.")]
    public AdvancedSettings advancedSettings;

    //private state
    private BufferedColor bufferedColor;
    private Canvas canvas;
    private Picker focusedPicker;
    private PickerType focusedPickerType;
    private MainPickingMode lastUpdatedMode;
    private bool materialsSeperated;
    private bool triggeredStaticMode;
    private bool typeUpdate;
    private AdvancedSettings AS => advancedSettings;


    /*----------------------------------------------------------
    * ------------------- MAIN COLOR GET/SET -------------------
    * ----------------------------------------------------------
    */

    public Color color {
        /* if called before init in Start(), the color state
         * is equivalent to the starting color parameter.
         */
        get {
            if (bufferedColor == null)
                return startingColor;
            return bufferedColor.color;
        }
        set {
            if (bufferedColor == null) {
                startingColor = value;
                return;
            }

            bufferedColor.Set(value);
            UpdateMarkers();
            UpdateTextures();
            UpdateHex();
            typeUpdate = true;
        }
    }


    /*----------------------------------------------------------
    * ------------------------- UPKEEP -------------------------
    * ----------------------------------------------------------
    */

    private void Start() {
        bufferedColor = new BufferedColor(startingColor);
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update() {
        typeUpdate = false;
        if (lastUpdatedMode != mode)
            ChangeMode(mode);

        if (staticMode != triggeredStaticMode) {
            UpdateTextures();
            triggeredStaticMode = staticMode;
        }

        if (multiInstance && !materialsSeperated) {
            SeperateMaterials();
            materialsSeperated = true;
        }
    }

    private void OnEnable() {
        if (bufferedColor == null)
            bufferedColor = new BufferedColor(startingColor);

        if (multiInstance && !materialsSeperated) {
            SeperateMaterials();
            materialsSeperated = true;
        }

        triggeredStaticMode = staticMode;
        UpdateTextures();
        MakeModeOptions();
        UpdateMarkers();
        UpdateHex();
    }

    /// <summary>
    ///     Equivalent to fcp.color
    ///     Returns starting color if FCP is not initialized.
    /// </summary>
    public Color GetColor() {
        return color;
    }

    /// <summary>
    ///     Equivalent to fcp.color
    ///     Propogates color change to picker images, hex input and other components.
    ///     Modifies starting color if FCP is not initialized.
    /// </summary>
    public void SetColor(Color color) {
        this.color = color;
    }

    /// <summary>
    ///     Returns current fcp color, but with its alpha channel value set to max.
    /// </summary>
    public Color GetColorFullAlpha() {
        var toReturn = color;
        toReturn.a = 1f;
        return toReturn;
    }

    /// <summary>
    ///     Changes fcp color while maintaining its current alpha value.
    /// </summary>
    public void SetColorNoAlpha(Color color) {
        color.a = this.color.a;
        this.color = color;
    }

    /// <summary>
    ///     Change picker that is being focused (and edited) using the pointer.
    /// </summary>
    /// <param name="i">Index of the picker image.</param>
    public void SetPointerFocus(int i) {
        if (i < 0 || i >= pickers.Length)
            Debug.LogWarning("No picker image available of type " + (PickerType)i +
                             ". Did you assign all the picker images in the editor?");
        else
            focusedPicker = pickers[i];
        focusedPickerType = (PickerType)i;
    }

    /// <summary>
    ///     Update color based on the pointer position in the currently focused picker.
    /// </summary>
    /// <param name="e">Pointer event</param>
    public void PointerUpdate(BaseEventData e) {
        var screenPoint = ((PointerEventData)e).position;
        var v = GetNormalizedPointerPosition(canvas, focusedPicker.image.rectTransform, screenPoint);
        bufferedColor = PickColor(bufferedColor, focusedPickerType, v);

        UpdateMarkers();
        UpdateTextures();

        typeUpdate = true;
        UpdateHex();
    }

    // move the marker by an amount
    public void FocusedPickerMove(Vector2 direction) {
        var v = Vector2.zero;
        for (var i = 0; i < pickers.Length; i++)
            if (IsPickerAvailable(i) && pickers[i].image == focusedPicker.image) {
                var type = (PickerType)i;
                v = GetValue(type);
            }

        v += direction;
        bufferedColor = PickColor(bufferedColor, focusedPickerType, v);

        UpdateMarkers();
        UpdateTextures();
        typeUpdate = true;
        UpdateHex();
    }

    /// <summary>
    ///     Softly sanitize hex color input and apply it
    /// </summary>
    public void TypeHex(string input) {
        TypeHex(input, false);

        UpdateTextures();
        UpdateMarkers();
    }

    /// <summary>
    ///     Strongly sanitize hex color input and apply it.
    ///     (appends zeroes to fit proper length in the text box).
    /// </summary>
    public void FinishTypeHex(string input) {
        TypeHex(input, true);

        UpdateTextures();
        UpdateMarkers();
    }

    /// <summary>
    ///     Change mode of the main, 2D picking image
    /// </summary>
    public void ChangeMode(int newMode) {
        ChangeMode((MainPickingMode)newMode);
    }

    /// <summary>
    ///     Change mode of the main, 2D picking image
    /// </summary>
    public void ChangeMode(MainPickingMode mode) {
        this.mode = mode;

        triggeredStaticMode = false;
        UpdateTextures();
        UpdateMarkers();
        UpdateMode(mode);
    }

    private void SeperateMaterials() {
        for (var i = 0; i < pickers.Length; i++) {
            var p = pickers[i];
            if (IsPickerAvailable(i) & (p.dynamicMaterial != null)) {
                var original = p.dynamicMaterial;
                var seperate = new Material(original);
                p.dynamicMaterial = seperate;
                pickers[i] = p;
                if (!staticMode)
                    p.image.material = seperate;
            }
        }
    }


    /*----------------------------------------------------------
    * --------------------- COLOR PICKING ----------------------
    * ----------------------------------------------------------
    * 
    * Get a new color that is the currently selected color but with 
    * one or two values changed. This is the core functionality of 
    * the picking images and the entire color picker script.
    */


    /// <summary>
    ///     Get a color that is the current color, but changed by the given picker and value.
    /// </summary>
    /// <param name="type">Picker type to base change on</param>
    /// <param name="v">normalized x and y values (both values may not be used)</param>
    private BufferedColor PickColor(BufferedColor color, PickerType type, Vector2 v) {
        switch (type) {
            case PickerType.Main: return PickColorMain(color, v);

            case PickerType.Preview:
            case PickerType.PreviewAlpha:
                return color;

            default: return PickColor1D(color, type, v);
        }
    }

    private BufferedColor PickColorMain(BufferedColor color, Vector2 v) {
        return PickColorMain(color, mode, v);
    }

    private BufferedColor PickColor1D(BufferedColor color, PickerType type, Vector2 v) {
        var horizontal = IsHorizontal(pickers[(int)type]);
        var value = horizontal ? v.x : v.y;
        return PickColor1D(color, type, value);
    }

    private BufferedColor PickColorMain(BufferedColor color, MainPickingMode mode, Vector2 v) {
        switch (mode) {
            case MainPickingMode.HS: return PickColor2D(color, PickerType.H, v.x, PickerType.S, v.y);
            case MainPickingMode.HV: return PickColor2D(color, PickerType.H, v.x, PickerType.V, v.y);
            case MainPickingMode.SH: return PickColor2D(color, PickerType.S, v.x, PickerType.H, v.y);
            case MainPickingMode.SV: return PickColor2D(color, PickerType.S, v.x, PickerType.V, v.y);
            case MainPickingMode.VH: return PickColor2D(color, PickerType.V, v.x, PickerType.H, v.y);
            case MainPickingMode.VS: return PickColor2D(color, PickerType.V, v.x, PickerType.S, v.y);
            default: return bufferedColor;
        }
    }

    private BufferedColor PickColor2D(BufferedColor color, PickerType type1, float value1, PickerType type2, float value2) {
        color = PickColor1D(color, type1, value1);
        color = PickColor1D(color, type2, value2);
        return color;
    }

    private BufferedColor PickColor1D(BufferedColor color, PickerType type, float value) {
        switch (type) {
            case PickerType.R: return color.PickR(Mathf.Lerp(AS.r.min, AS.r.max, value));
            case PickerType.G: return color.PickG(Mathf.Lerp(AS.g.min, AS.g.max, value));
            case PickerType.B: return color.PickB(Mathf.Lerp(AS.b.min, AS.b.max, value));
            case PickerType.H: return color.PickH(Mathf.Lerp(AS.h.min, AS.h.max, value) * HUE_LOOP);
            case PickerType.S: return color.PickS(Mathf.Lerp(AS.s.min, AS.s.max, value));
            case PickerType.V: return color.PickV(Mathf.Lerp(AS.v.min, AS.v.max, value));
            case PickerType.A: return color.PickA(Mathf.Lerp(AS.a.min, AS.a.max, value));
            default:
                throw new Exception("Picker type " + type + " is not associated with a single color value.");
        }
    }


    /*----------------------------------------------------------
    * -------------------- MARKER UPDATING ---------------------
    * ----------------------------------------------------------
    * 
    * Update positions of markers on each picking texture, 
    * indicating the currently selected values.
    */


    private void UpdateMarkers() {
        for (var i = 0; i < pickers.Length; i++)
            if (IsPickerAvailable(i)) {
                var type = (PickerType)i;
                var v = GetValue(type);
                UpdateMarker(pickers[i], type, v);
            }
    }

    private void UpdateMarker(Picker picker, PickerType type, Vector2 v) {
        switch (type) {
            case PickerType.Main:
                SetMarker(picker.image, v, true, true);
                break;

            case PickerType.Preview:
            case PickerType.PreviewAlpha:
                break;

            default:
                var horizontal = IsHorizontal(picker);
                SetMarker(picker.image, v, horizontal, !horizontal);
                break;
        }
    }

    private void SetMarker(Image picker, Vector2 v, bool setX, bool setY) {
        RectTransform marker = null;
        RectTransform offMarker = null;
        if (setX && setY) {
            marker = GetMarker(picker, null);
        }
        else if (setX) {
            marker = GetMarker(picker, "hor");
            offMarker = GetMarker(picker, "ver");
        }
        else if (setY) {
            marker = GetMarker(picker, "ver");
            offMarker = GetMarker(picker, "hor");
        }

        if (offMarker != null)
            offMarker.gameObject.SetActive(false);

        if (marker == null)
            return;

        marker.gameObject.SetActive(true);
        var parent = picker.rectTransform;
        var parentSize = parent.rect.size;
        Vector2 localPos = marker.localPosition;

        if (setX)
            localPos.x = (v.x - parent.pivot.x) * parentSize.x;
        if (setY)
            localPos.y = (v.y - parent.pivot.y) * parentSize.y;
        marker.localPosition = localPos;
    }

    private RectTransform GetMarker(Image picker, string search) {
        for (var i = 0; i < picker.transform.childCount; i++) {
            var candidate = picker.transform.GetChild(i).GetComponent<RectTransform>();
            var candidateName = candidate.name.ToLower();
            var match = candidateName.Contains("marker");
            match &= string.IsNullOrEmpty(search)
                     || candidateName.Contains(search);
            if (match)
                return candidate;
        }

        return null;
    }


    /*----------------------------------------------------------
    * -------------------- VALUE RETRIEVAL ---------------------
    * ----------------------------------------------------------
    * 
    * Get individual values associated with a picker image from the 
    * currently selected color.
    * This is needed to properly update markers.
    */

    private Vector2 GetValue(PickerType type) {
        switch (type) {
            case PickerType.Main: return GetValue(mode);

            case PickerType.Preview:
            case PickerType.PreviewAlpha:
                return Vector2.zero;

            default:
                var value = GetValue1D(type);
                return new Vector2(value, value);
        }
    }

    /// <summary>
    ///     Get normalized value of the current color according to the given picker.
    ///     This value can be used to adjust the position of the marker on a slider.
    /// </summary>
    private float GetValue1D(PickerType type) {
        switch (type) {
            case PickerType.R: return Mathf.InverseLerp(AS.r.min, AS.r.max, bufferedColor.r);
            case PickerType.G: return Mathf.InverseLerp(AS.g.min, AS.g.max, bufferedColor.g);
            case PickerType.B: return Mathf.InverseLerp(AS.b.min, AS.b.max, bufferedColor.b);
            case PickerType.H: return Mathf.InverseLerp(AS.h.min, AS.h.max, bufferedColor.h / HUE_LOOP);
            case PickerType.S: return Mathf.InverseLerp(AS.s.min, AS.s.max, bufferedColor.s);
            case PickerType.V: return Mathf.InverseLerp(AS.v.min, AS.v.max, bufferedColor.v);
            case PickerType.A: return Mathf.InverseLerp(AS.a.min, AS.a.max, bufferedColor.a);
            default:
                throw new Exception("Picker type " + type + " is not associated with a single color value.");
        }
    }

    /// <summary>
    ///     Get normalized 2D value of the current color according to the main picker mode.
    ///     This value can be used to adjust the position of the 2D marker.
    /// </summary>
    private Vector2 GetValue(MainPickingMode mode) {
        switch (mode) {
            case MainPickingMode.HS: return new Vector2(GetValue1D(PickerType.H), GetValue1D(PickerType.S));
            case MainPickingMode.HV: return new Vector2(GetValue1D(PickerType.H), GetValue1D(PickerType.V));
            case MainPickingMode.SH: return new Vector2(GetValue1D(PickerType.S), GetValue1D(PickerType.H));
            case MainPickingMode.SV: return new Vector2(GetValue1D(PickerType.S), GetValue1D(PickerType.V));
            case MainPickingMode.VH: return new Vector2(GetValue1D(PickerType.V), GetValue1D(PickerType.H));
            case MainPickingMode.VS: return new Vector2(GetValue1D(PickerType.V), GetValue1D(PickerType.S));
            default: throw new Exception("Unkown main picking mode: " + mode);
        }
    }


    /*----------------------------------------------------------
    * -------------------- TEXTURE UPDATING --------------------
    * ----------------------------------------------------------
    * 
    * Update picker image textures that show gradients of colors 
    * that the user can pick.
    */

    private void UpdateTextures() {
        foreach (PickerType type in Enum.GetValues(typeof(PickerType)))
            if (staticMode || AS.Get((int)type).overrideStatic)
                UpdateStatic(type);
            else
                UpdateDynamic(type);
    }

    private void UpdateStatic(PickerType type) {
        if (!IsPickerAvailable(type))
            return;
        var p = pickers[(int)type];

        var hor = IsHorizontal(p);
        var s = hor ? p.staticSpriteHor : p.staticSpriteVer;
        if (s == null)
            s = hor ? p.staticSpriteVer : p.staticSpriteHor;

        p.image.sprite = s;
        p.image.material = null;
        p.image.color = Color.white;

        var prvw = color;

        switch (type) {
            case PickerType.Main:
                p.image.sprite = staticSpriteMain[(int)mode];
                break;

            case PickerType.Preview:
                prvw.a = 1f;
                p.image.color = prvw;
                break;

            case PickerType.PreviewAlpha:
                p.image.color = prvw;
                break;
        }
    }

    private void UpdateDynamic(PickerType type) {
        if (!IsPickerAvailable(type))
            return;
        var p = pickers[(int)type];
        if (p.dynamicMaterial == null)
            return;

        p.image.material = p.dynamicMaterial;
        p.image.color = Color.white;
        p.image.sprite = p.dynamicSprite;

        var m = p.image.materialForRendering;

        var bc = bufferedColor;

        var alpha = IsAlphaType(type);
        m.SetInt(SHADER_MODE, GetGradientMode(type));
        var c1 = PickColor(bc, type, Vector2.zero).color;
        var c2 = PickColor(bc, type, Vector2.one).color;
        if (!alpha) {
            c1 = new Color(c1.r, c1.g, c1.b);
            c2 = new Color(c2.r, c2.g, c2.b);
        }

        m.SetColor(SHADER_C1, c1);
        m.SetColor(SHADER_C2, c2);
        if (type == PickerType.Main)
            m.SetInt(SHADER_DOUBLE_MODE, (int)mode);
        m.SetVector(SHADER_HSV, new Vector4(bc.h / HUE_LOOP, bc.s, bc.v, alpha ? bc.a : 1f));
        m.SetVector(SHADER_HSV_MIN, new Vector4(AS.h.min, AS.s.min, AS.v.min));
        m.SetVector(SHADER_HSV_MAX, new Vector4(AS.h.max, AS.s.max, AS.v.max));
    }

    private int GetGradientMode(PickerType type) {
        var o = IsHorizontal(pickers[(int)type]) ? 0 : 1;
        switch (type) {
            case PickerType.Main: return 2;
            case PickerType.H: return 3 + o;
            default: return o;
        }
    }

    private bool IsPickerAvailable(PickerType type) {
        return IsPickerAvailable((int)type);
    }

    private bool IsPickerAvailable(int index) {
        if (index < 0 || index >= pickers.Length)
            return false;
        var p = pickers[index];
        if (p.image == null || !p.image.gameObject.activeInHierarchy)
            return false;
        return true;
    }


    /*----------------------------------------------------------
    * ------------------ HEX INPUT UPDATING --------------------
    * ----------------------------------------------------------
    * 
    * Provides an input field for hexadecimal color values.
    * The user can type new values, or use this field to copy 
    * values picked via the picker images.
    */

    private void UpdateHex() {
        if (hexInput == null || !hexInput.gameObject.activeInHierarchy)
            return;
        hexInput.text = "#" + ColorUtility.ToHtmlStringRGB(color);
    }

    private void TypeHex(string input, bool finish) {
        if (typeUpdate)
            return;
        typeUpdate = true;

        var newText = GetSanitizedHex(input, finish);
        var parseText = GetSanitizedHex(input, true);

        var cp = hexInput.caretPosition;
        hexInput.text = newText;
        if (hexInput.caretPosition == 0)
            hexInput.caretPosition = 1;
        else if (newText.Length == 2)
            hexInput.caretPosition = 2;
        else if (input.Length > newText.Length && cp < input.Length)
            hexInput.caretPosition = cp - input.Length + newText.Length;

        Color newColor;
        if (ColorUtility.TryParseHtmlString(parseText, out newColor)) {
            if (bufferedColor != null) {
                bufferedColor.Set(newColor);
                UpdateMarkers();
                UpdateTextures();
            }
            else {
                startingColor = newColor;
            }
        }
    }


    /*----------------------------------------------------------
    * ---------------------- MODE UPDATING ---------------------
    * ----------------------------------------------------------
    * 
    * Allows user to change the 'Main picking mode' which determines 
    * the values shown on the main, 2D picking image.
    */

    private void MakeModeOptions() {
        if (modeDropdown == null || !modeDropdown.gameObject.activeInHierarchy)
            return;

        modeDropdown.ClearOptions();
        var options = new List<string>();
        foreach (MainPickingMode mode in Enum.GetValues(typeof(MainPickingMode)))
            options.Add(mode.ToString());
        modeDropdown.AddOptions(options);

        UpdateMode(this.mode);
    }

    private void UpdateMode(MainPickingMode mode) {
        lastUpdatedMode = mode;
        if (modeDropdown == null || !modeDropdown.gameObject.activeInHierarchy)
            return;
        modeDropdown.value = (int)mode;
    }


    /*----------------------------------------------------------
    * ---------------- STATIC HELPER FUNCTIONS -----------------
    * ----------------------------------------------------------
    */

    private static bool IsPreviewType(PickerType type) {
        switch (type) {
            case PickerType.Preview: return true;
            case PickerType.PreviewAlpha: return true;
            default: return false;
        }
    }

    private static bool IsAlphaType(PickerType type) {
        switch (type) {
            case PickerType.A: return true;
            case PickerType.PreviewAlpha: return true;
            default: return false;
        }
    }

    /// <summary>
    ///     Should given picker image be controlled horizontally?
    ///     Returns true if the image is bigger in the horizontal direction.
    /// </summary>
    private static bool IsHorizontal(Picker p) {
        var size = p.image.rectTransform.rect.size;
        return size.x >= size.y;
    }

    /// <summary>
    ///     Santiive a given string so that it encodes a valid hex color string
    /// </summary>
    /// <param name="input">Input string</param>
    /// <param name="full">Insert zeroes to match #RRGGBB format </param>
    public static string GetSanitizedHex(string input, bool full) {
        if (string.IsNullOrEmpty(input))
            return "#";

        var toReturn = new List<char>();
        toReturn.Add('#');
        var i = 0;
        var chars = input.ToCharArray();
        while (toReturn.Count < 7 && i < input.Length) {
            var nextChar = char.ToUpper(chars[i++]);
            if (IsValidHexChar(nextChar))
                toReturn.Add(nextChar);
        }

        while (full && toReturn.Count < 7)
            toReturn.Insert(1, '0');

        return new string(toReturn.ToArray());
    }

    private static bool IsValidHexChar(char c) {
        var valid = char.IsNumber(c);
        valid |= (c >= 'A') & (c <= 'F');
        return valid;
    }

    /// <summary>
    ///     tries to parse given string input as hexadecimal color e.g.
    ///     "#FF00FF" or "223344" returns black if string failed to
    ///     parse.
    /// </summary>
    public static Color ParseHex(string input) {
        return ParseHex(input, Color.black);
    }

    /// <summary>
    ///     tries to parse given string input as hexadecimal color e.g.
    ///     "#FF00FF" or "223344" returns default color if string failed to
    ///     parse.
    /// </summary>
    public static Color ParseHex(string input, Color defaultColor) {
        var parseText = GetSanitizedHex(input, true);
        Color toReturn;
        if (ColorUtility.TryParseHtmlString(parseText, out toReturn))
            return toReturn;
        return defaultColor;
    }

    /// <summary>
    ///     Get normalized position of the given pointer event relative to the given rect.
    ///     (e.g. return [0,1] for top left corner). This method correctly takes into
    ///     account relative positions, canvas render mode and general transformations,
    ///     including rotations and scale.
    /// </summary>
    /// <param name="canvas">parent canvas of the rect (and therefore the FCP)</param>
    /// <param name="rect">Rect to find relative position to</param>
    /// <param name="e">Pointer event for which to find the relative position</param>
    private static Vector2 GetNormalizedPointerPosition(Canvas canvas, RectTransform rect, Vector2 sceenPoint) {
        switch (canvas.renderMode) {
            case RenderMode.ScreenSpaceCamera:
                if (canvas.worldCamera == null)
                    return GetNormScreenSpace(rect, sceenPoint);
                else
                    return GetNormWorldSpace(canvas, rect, sceenPoint);

            case RenderMode.ScreenSpaceOverlay:
                return GetNormScreenSpace(rect, sceenPoint);

            case RenderMode.WorldSpace:
                if (canvas.worldCamera == null) {
                    Debug.LogError("FCP in world space render mode requires an event camera to be set up on the parent canvas!");
                    return Vector2.zero;
                }

                return GetNormWorldSpace(canvas, rect, sceenPoint);

            default: return Vector2.zero;
        }
    }

    /// <summary>
    ///     Get normalized position in the case of a screen space (overlay)
    ///     type canvas render mode
    /// </summary>
    private static Vector2 GetNormScreenSpace(RectTransform rect, Vector2 screenPoint) {
        Vector2 localPos = rect.worldToLocalMatrix.MultiplyPoint(screenPoint);
        var x = Mathf.Clamp01(localPos.x / rect.rect.size.x + rect.pivot.x);
        var y = Mathf.Clamp01(localPos.y / rect.rect.size.y + rect.pivot.y);
        return new Vector2(x, y);
    }

    /// <summary>
    ///     Get normalized position in the case of a world space (or screen space camera)
    ///     type cavnvas render mode.
    /// </summary>
    private static Vector2 GetNormWorldSpace(Canvas canvas, RectTransform rect, Vector2 screenPoint) {
        var pointerRay = canvas.worldCamera.ScreenPointToRay(screenPoint);
        var canvasPlane = new Plane(canvas.transform.forward, canvas.transform.position);
        float enter;
        canvasPlane.Raycast(pointerRay, out enter);
        var worldPoint = pointerRay.origin + enter * pointerRay.direction;
        Vector2 localPoint = rect.worldToLocalMatrix.MultiplyPoint(worldPoint);

        var x = Mathf.Clamp01(localPoint.x / rect.rect.size.x + rect.pivot.x);
        var y = Mathf.Clamp01(localPoint.y / rect.rect.size.y + rect.pivot.y);
        return new Vector2(x, y);
    }

    /// <summary>
    ///     Get color from hue, saturation, value format
    /// </summary>
    /// <param name="hsv">Vector containing h, s and v values.</param>
    public static Color HSVToRGB(Vector3 hsv) {
        return HSVToRGB(hsv.x, hsv.y, hsv.z);
    }

    /// <summary>
    ///     Get color from hue, saturation, value format
    /// </summary>
    /// <param name="h">hue value, ranging from 0 to 6; red to red</param>
    /// <param name="s">saturation, 0 to 1; gray to colored</param>
    /// <param name="v">value, 0 to 1; black to white</param>
    public static Color HSVToRGB(float h, float s, float v) {
        var c = s * v;
        var m = v - c;
        var x = c * (1f - Mathf.Abs(h % 2f - 1f)) + m;
        c += m;

        var range = Mathf.FloorToInt(h % 6f);

        switch (range) {
            case 0: return new Color(c, x, m);
            case 1: return new Color(x, c, m);
            case 2: return new Color(m, c, x);
            case 3: return new Color(m, x, c);
            case 4: return new Color(x, m, c);
            case 5: return new Color(c, m, x);
            default: return Color.black;
        }
    }

    /// <summary>
    ///     Get hue, saturation and value of a color.
    ///     Complementary to HSVToRGB
    /// </summary>
    public static Vector3 RGBToHSV(Color color) {
        var r = color.r;
        var g = color.g;
        var b = color.b;
        return RGBToHSV(r, g, b);
    }

    /// <summary>
    ///     Get hue, saturation and value of a color.
    ///     Complementary to HSVToRGB
    /// </summary>
    public static Vector3 RGBToHSV(float r, float g, float b) {
        var cMax = Mathf.Max(r, g, b);
        var cMin = Mathf.Min(r, g, b);
        var delta = cMax - cMin;
        var h = 0f;
        if (delta > 0f) {
            if (r >= b && r >= g)
                h = Mathf.Repeat((g - b) / delta, 6f);
            else if (g >= r && g >= b)
                h = (b - r) / delta + 2f;
            else if (b >= r && b >= g)
                h = (r - g) / delta + 4f;
        }

        var s = cMax == 0f ? 0f : delta / cMax;
        var v = cMax;
        return new Vector3(h, s, v);
    }

    [Serializable]
    public struct Picker {
        public Image image;
        public Sprite dynamicSprite;
        public Sprite staticSpriteHor;
        public Sprite staticSpriteVer;
        public Material dynamicMaterial;
    }

    private enum PickerType {
        Main,
        R,
        G,
        B,
        H,
        S,
        V,
        A,
        Preview,
        PreviewAlpha
    }

    [Serializable]
    public class AdvancedSettings {
        public PSettings r;
        public PSettings g;
        public PSettings b;
        public PSettings h;
        public PSettings s;
        public PSettings v;
        public PSettings a;

        /// <summary>
        ///     Get PSettings for value associated with the given picker index.
        ///     Returns default PSettings if none exists.
        /// </summary>
        public PSettings Get(int i) {
            if ((i <= 0) | (i > 7))
                return new PSettings();
            PSettings[] p = { r, g, b, h, s, v, a };
            return p[i - 1];
        }

        [Serializable]
        public class PSettings {
            [Tooltip("Normalized minimum for this color value, for restricting the slider range")] [Range(0f, 1f)]
            public float min;

            [Tooltip("Normalized maximum for this color value, for restricting the slider range")] [Range(0f, 1f)]
            public float max = 1f;

            [Tooltip("Make the picker associated with this value act static, even in a dynamic color picker setup")]
            public bool overrideStatic;
        }
    }


    /*----------------------------------------------------------
    * --------------------- HELPER CLASSES ---------------------
    * ----------------------------------------------------------
    */


    /// <summary>
    ///     Encodes a color while buffering hue and saturation values.
    ///     This is necessary since these values are singular for some
    ///     colors like unsaturated grays and would lead to undesirable
    ///     behaviour when moving sliders towards such colors.
    /// </summary>
    [Serializable]
    private class BufferedColor {
        public Color color;


        public BufferedColor() {
            h = 0f;
            s = 0f;
            color = Color.black;
        }

        public BufferedColor(Color color) : this() {
            Set(color);
        }

        public BufferedColor(Color color, float hue, float sat) : this(color) {
            h = hue;
            s = sat;
        }

        public BufferedColor(Color color, BufferedColor source) :
            this(color, source.h, source.s) {
            Set(color);
        }

        public float r => color.r;
        public float g => color.g;
        public float b => color.b;
        public float a => color.a;
        public float h { get; private set; }

        public float s { get; private set; }

        public float v => RGBToHSV(color).z;

        public void Set(Color color) {
            Set(color, h, s);
        }

        public void Set(Color color, float bufferedHue, float bufferedSaturation) {
            this.color = color;
            var hsv = RGBToHSV(color);

            var hueSingularity = hsv.y == 0f || hsv.z == 0f;
            if (hueSingularity)
                h = bufferedHue;
            else
                h = hsv.x;

            var saturationSingularity = hsv.z == 0f;
            if (saturationSingularity)
                s = bufferedSaturation;
            else
                s = hsv.y;
        }

        public BufferedColor PickR(float value) {
            var toReturn = color;
            toReturn.r = value;
            return new BufferedColor(toReturn, this);
        }

        public BufferedColor PickG(float value) {
            var toReturn = color;
            toReturn.g = value;
            return new BufferedColor(toReturn, this);
        }

        public BufferedColor PickB(float value) {
            var toReturn = color;
            toReturn.b = value;
            return new BufferedColor(toReturn, this);
        }

        public BufferedColor PickA(float value) {
            var toReturn = color;
            toReturn.a = value;
            return new BufferedColor(toReturn, this);
        }

        public BufferedColor PickH(float value) {
            var hsv = RGBToHSV(color);
            var toReturn = HSVToRGB(value, hsv.y, hsv.z);
            toReturn.a = color.a;
            return new BufferedColor(toReturn, value, s);
        }

        public BufferedColor PickS(float value) {
            var hsv = RGBToHSV(color);
            var toReturn = HSVToRGB(h, value, hsv.z);
            toReturn.a = color.a;
            return new BufferedColor(toReturn, h, value);
        }

        public BufferedColor PickV(float value) {
            var toReturn = HSVToRGB(h, s, value);
            toReturn.a = color.a;
            return new BufferedColor(toReturn, h, s);
        }
    }
}