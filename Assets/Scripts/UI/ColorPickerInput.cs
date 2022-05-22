using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerInput : Selectable, ISubmitHandler, ICancelHandler {
    
    enum ColourElementType {
        Main,
        Hue
    }
    
    private bool _hasFocus;
    [SerializeField] private ColourElementType colourElementType;
    [SerializeField] private FlexibleColorPicker colorPicker;
    [SerializeField] private float increment = 0.1f;
    
    public override void OnMove(AxisEventData eventData) {
        if (!_hasFocus) 
            base.OnMove(eventData);
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

    public void OnSubmit(BaseEventData eventData) {
        _hasFocus = !_hasFocus;
    }

    public void OnCancel(BaseEventData eventData) {
        _hasFocus = false;
    }

    public override void OnDeselect(BaseEventData eventData) {
        base.OnDeselect(eventData);
        _hasFocus = false;
    }
}
