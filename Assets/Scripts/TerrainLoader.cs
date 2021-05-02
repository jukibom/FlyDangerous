using System;
using System.Collections;
using System.Collections.Generic;
using MapMagic.Core;
using UnityEngine;

public class TerrainLoader : MonoBehaviour {
    [SerializeField] private GameObject mapMagicObject;

    // Start is called before the first frame update
    void Start() {
        Game.Instance.isTerrainMap = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
