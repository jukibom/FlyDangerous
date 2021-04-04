using UnityEngine;
using UnityEngine.Audio;

namespace Audio {
    [System.Serializable]
    public class Sound {
        public AudioClip clip;
        public string name;
        
        [Range(0f, 1f)]
        public float volume = 1f;
        
        [Range(.1f, 3f)]
        public float pitch = 1f;

        public bool loop = false;

        [HideInInspector]
        public AudioSource source;
    }
}