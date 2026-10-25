using Misc;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core {
    public class Engine : Singleton<Engine> {
        [SerializeField] private GameObject integrations;
        [SerializeField] private NightVision nightVision;
        public MonoBehaviour[] Integrations => integrations.GetComponentsInChildren<MonoBehaviour>();
        public NightVision NightVision => nightVision;

        protected override void Awake() {
            base.Awake();
            DontDestroyOnLoad(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void LoadEngine() {
            Debug.Log("Loading Engine");

            SceneManager.LoadScene("Engine", LoadSceneMode.Additive);
        }
    }
}