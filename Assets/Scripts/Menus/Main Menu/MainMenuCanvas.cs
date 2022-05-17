using UnityEngine;
using UnityEngine.EventSystems;

namespace Menus.Main_Menu {
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
            )
                cursor.SetLocalPosition(canvasPosition);
        }
    }
}