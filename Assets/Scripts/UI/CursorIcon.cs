using UnityEngine;

public class CursorIcon : MonoBehaviour {
    public void SetLocalPosition(Vector2 position) {
        transform.localPosition = new Vector3(
            position.x,
            position.y,
            0f
        );
    }
}