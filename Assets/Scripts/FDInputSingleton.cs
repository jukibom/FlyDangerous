using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FDInputSingleton
{

    public FlyDangerousActions Actions {
        get
        {
            return this._actions;
        }
    }
    
    private static FDInputSingleton _instance;
    public static FDInputSingleton Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new FDInputSingleton();
            }

            return _instance;
        }
    }

    private FlyDangerousActions _actions;
    private FDInputSingleton() {
        this._actions = new FlyDangerousActions();
    }
    
    void Awake()
    {
        _instance = this;
    }
}