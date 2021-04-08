using UnityEngine;

public class Game : MonoBehaviour
{
    private void OnDestroy() {
        GlobalGameState.Destroy();
    }
}
