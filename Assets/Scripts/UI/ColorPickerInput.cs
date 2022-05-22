using Misc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerInput : Selectable, ISubmitHandler, ICancelHandler {
    [SerializeField] private ColourElementType colourElementType;
    [SerializeField] private FlexibleColorPicker colorPicker;
    [SerializeField] private float increment = 0.1f;
    [SerializeField] private RectTransform selectionCursor;

    private bool _hasFocus;

    public void Update() {
        var scale = MathfExtensions.Remap(0, 1, 1, 2f, Mathf.PingPong(Time.time * 3, 1));
        selectionCursor.localScale = _hasFocus ? Vector3.one * scale : Vector3.one;
    }

    public void OnCancel(BaseEventData eventData) {
        _hasFocus = false;
    }

    public void OnSubmit(BaseEventData eventData) {
        _hasFocus = !_hasFocus;
    }

    public override void OnMove(AxisEventData eventData) {
        if (!_hasFocus) {
            base.OnMove(eventData);
        }
        else if (colourElementType == ColourElementType.Hue && (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)) {
            base.OnMove(eventData);
        }
        else {
            // maps to internal FCP int types
            var colourPickerIndex = colourElementType == ColourElementType.Main ? 0 : 4;
            colorPicker.SetPointerFocus(colourPickerIndex);
            switch (eventData.moveDir) {
                case MoveDirection.Left:
                    colorPicker.FocusedPickerMove(new Vector2(-increment, 0));
                    break;
                case MoveDirection.Right:
                    colorPicker.FocusedPickerMove(new Vector2(increment, 0));
                    break;
                case MoveDirection.Up:
                    colorPicker.FocusedPickerMove(new Vector2(0f, increment));
                    break;
                case MoveDirection.Down:
                    colorPicker.FocusedPickerMove(new Vector2(0, -increment));
                    break;
            }
        }
    }

    public override void OnDeselect(BaseEventData eventData) {
        base.OnDeselect(eventData);
        _hasFocus = false;
    }

    private enum ColourElementType {
        Main,
        Hue
    }
}