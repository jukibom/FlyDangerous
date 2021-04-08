using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Game : MonoBehaviour
{
    private void OnDestroy() {
        GlobalGameState.Destroy();
    }

    public void Awake() {
        Random.InitState(51224);
    }
}
