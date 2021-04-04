using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using UnityEngine;

public class GlobalGameState {
    // Input instance shared across anything that needs it 
    // (The default mechanisms for this are woefully inadequate and we bind actions manually in code instead)
    private static FlyDangerousActions _actions;
    public static FlyDangerousActions Actions {
        get {
            return TryCreateAndReturn(ref _actions);
        }
    }

    public static AudioManager AudioManager {
        get {
            return AudioManager.Instance;
        }
    }

    public static void Destroy() {
        TryDisposeAndClear(ref _actions);
    }

    private static T TryCreateAndReturn<T>(ref T obj) where T : new() {
        if (obj == null) {
            obj = new T();
        }
        return obj;
    }
    public static void TryDisposeAndClear<T>(ref T obj) where T : IDisposable {
        try {
            obj.Dispose();
        }
        catch { }

        obj = default(T);
    }
}