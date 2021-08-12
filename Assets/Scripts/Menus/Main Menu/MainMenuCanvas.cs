using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MainMenuCanvas : MonoBehaviour, IPointerMoveHandler {
    [SerializeField] private CursorIcon cursor;
    private RectTransform _rectTransform;

    private void Start() {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerMove(PointerEventData eventData) {
        if (
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, 
                eventData.position, 
                eventData.enterEventCamera, 
                out var canvasPosition)
        ) {
            cursor.OnPointerMove(canvasPosition);
        }
    }
}
