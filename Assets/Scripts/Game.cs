using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    private void OnDestroy() {
        GlobalGameState.Destroy();
    }
}
