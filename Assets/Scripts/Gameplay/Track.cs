using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using Core.MapData;
using Core.MapData.Serializable;
using Core.Player;
using Gameplay.Game_Modes.Components;
using JetBrains.Annotations;
using NaughtyAttributes;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections;
using Core.Scores;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Gameplay {
    [ExecuteAlways]
    public class Track : MonoBehaviour {
        public delegate void CheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds);

        public event CheckpointHit OnCheckpointHit;

        [SerializeField] private Checkpoint checkpointPrefab;
        [SerializeField] private ModifierSpawner modifierPrefab;
        [SerializeField] private BillboardSpawner billboardPrefab;

        [SerializeField] private GameModeCheckpoints checkpointContainer;
        [SerializeField] private GameModeModifiers modifierContainer;
        [SerializeField] private GameModeBillboards billboardContainer;
        [SerializeField] private Transform geometryContainer;

        [HorizontalLine] [SerializeField] private string trackName;
        [HorizontalLine] [SerializeField] private string authorName;

        [Dropdown("GetGameModes")] [OnValueChanged("SetGameMode")] [SerializeField]
        private string gameMode;

        [Dropdown("GetEnvironments")] [OnValueChanged("SetEnvironment")] [SerializeField]
        private string environment;

        [Dropdown("GetMusicTracks")] [OnValueChanged("PlayMusicTrack")] [SerializeField]
        private string musicTrack;

        [SerializeField] [OnValueChanged("UpdateTimesFromAuthor")]
        private float authorTimeTarget;

        [ReadOnly] [UsedImplicitly] [SerializeField]
        private float goldTime;

        [ReadOnly] [UsedImplicitly] [SerializeField]
        private float silverTime;

        [ReadOnly] [UsedImplicitly] [SerializeField]
        private float bronzeTime;

        [OnValueChanged("UpdateStartLocation")] [HorizontalLine] [SerializeField]
        private Vector3 startPosition;

        [OnValueChanged("UpdateStartLocation")] [SerializeField]
        private Vector3 startRotation;

        private bool _loadingEnvironment;

        public GameModeCheckpoints GameModeCheckpoints => checkpointContainer;

        private void OnDestroy() {
            if (checkpointContainer != null)
                foreach (var checkpoint in checkpointContainer.Checkpoints)
                    checkpoint.OnHit -= HandleOnCheckpointHit;
        }

        public LevelData Serialize() {
            var loadedLevelData = Game.Instance.LoadedLevelData;

            var levelData = new LevelData {
                name = trackName,
                author = authorName,
                authorTimeTarget = authorTimeTarget,
                gameType = GameType.FromString(gameMode),
                location = loadedLevelData.location,
                musicTrack = MusicTrack.FromString(musicTrack),
                environment = Environment.FromString(environment),
                terrainSeed = string.IsNullOrEmpty(loadedLevelData.terrainSeed) ? null : loadedLevelData.terrainSeed,
                startPosition = SerializableVector3.FromVector3(startPosition),
                startRotation = SerializableVector3.FromVector3(startRotation)
            };

#if UNITY_EDITOR
            // refresh containers
            OnValidate();
#endif

            if (checkpointContainer.Checkpoints.Count > 0)
                levelData.checkpoints = checkpointContainer
                    .Checkpoints
                    .ConvertAll(SerializableCheckpoint.FromCheckpoint);

            if (billboardContainer.BillboardSpawners.Count > 0)
                levelData.billboards = billboardContainer.BillboardSpawners
                    .ConvertAll(SerializableBillboard.FromBillboardSpawner);

            if (modifierContainer.ModifierSpawners.Count > 0)
                levelData.modifiers = modifierContainer.ModifierSpawners
                    .ConvertAll(SerializableModifier.FromModifierSpawner);

            // TODO: geometry

            return levelData;
        }

        // Build level geometry from json
        public void Deserialize(LevelData levelData) {
            trackName = levelData.name;
            authorName = levelData.author;
            gameMode = levelData.gameType.Name;
            authorTimeTarget = levelData.authorTimeTarget > 0 ? levelData.authorTimeTarget : 60;
            environment = levelData.environment.Name;
            musicTrack = levelData.musicTrack.Name;

            startPosition = levelData.startPosition.ToVector3();
            startRotation = levelData.startRotation.ToVector3();

            if (levelData.checkpoints?.Count > 0)
                levelData.checkpoints.ForEach(c => {
                    var checkpoint = checkpointContainer.AddCheckpoint(c);
                    checkpoint.OnHit += HandleOnCheckpointHit;
                });

            if (levelData.billboards?.Count > 0)
                levelData.billboards.ForEach(b => billboardContainer.AddBillboard(b));

            if (levelData.modifiers?.Count > 0)
                levelData.modifiers.ForEach(m => modifierContainer.AddModifier(m));

            // TODO: geometry
        }

        private void HandleOnCheckpointHit(Checkpoint checkpoint, float excessTimeToHitSeconds) {
            OnCheckpointHit?.Invoke(checkpoint, excessTimeToHitSeconds);
        }

#if UNITY_EDITOR

        #region Editor Hooks

        [UsedImplicitly]
        private List<string> GetGameModes() {
            return GameType.List().Select(b => b.Name).ToList();
        }

        [UsedImplicitly]
        private void SetGameMode() {
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                Game.Instance.LoadedLevelData.startPosition = SerializableVector3.FromVector3(player.AbsoluteWorldPosition);
                Game.Instance.LoadedLevelData.startRotation = SerializableVector3.FromVector3(player.transform.rotation.eulerAngles);
                Game.Instance.GameModeHandler.InitialiseGameMode(player, Serialize(), GameType.FromString(gameMode).GameMode, player.User.InGameUI, this);
                Game.Instance.GameModeHandler.Begin();
                Game.Instance.RestartSession();
            }
        }

        [UsedImplicitly]
        private List<string> GetEnvironments() {
            return Environment.List().Select(b => b.Name).ToList();
        }

        [UsedImplicitly]
        private List<string> GetMusicTracks() {
            return MusicTrack.List().Select(b => b.Name).ToList();
        }

        [UsedImplicitly]
        private void SetEnvironment() {
            var sceneToLoad = Environment.FromString(environment).SceneToLoad;
            SetEnvironmentByName(sceneToLoad);
        }

        public void SetEnvironmentByName(string sceneToLoad) {
            if (_loadingEnvironment) return;
            _loadingEnvironment = true;

            void Cleanup() {
                // unload any other matching environment scenes
                foreach (var environmentType in Environment.List()) {
                    if (environmentType.SceneToLoad == sceneToLoad)
                        continue;

                    for (var i = 0; i < SceneManager.sceneCount; i++) {
                        var loadedScene = SceneManager.GetSceneAt(i);
                        if (loadedScene.name == environmentType.SceneToLoad) SceneManager.UnloadSceneAsync(loadedScene);
                    }
                }

                _loadingEnvironment = false;
            }


            IEnumerator SetNewEnvironmentInGame(string sceneName) {
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
                Cleanup();
            }

            // load new
            if (Application.isPlaying) {
                Game.Instance.StartCoroutine(SetNewEnvironmentInGame(sceneToLoad));
            }
            else {
                EditorSceneManager.OpenScene($"Assets/Scenes/Environments/{sceneToLoad}.unity", OpenSceneMode.Additive);
                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
                Cleanup();
            }
        }

        [UsedImplicitly]
        private void PlayMusicTrack() {
            if (Application.isPlaying)
                MusicManager.Instance.PlayMusic(MusicTrack.FromString(musicTrack), true, true, false);
        }

        [UsedImplicitly]
        private void UpdateTimesFromAuthor() {
            var levelData = Serialize();
            goldTime = Score.GoldTimeTarget(levelData);
            silverTime = Score.SilverTimeTarget(levelData);
            bronzeTime = Score.BronzeTimeTarget(levelData);
        }

        [Button("Set start from ship position")]
        [UsedImplicitly]
        private void SetFromShip() {
            Undo.RecordObject(this, "Set player transform");
            var player = FdPlayer.FindLocalShipPlayer;
            if (player) {
                startPosition = player.AbsoluteWorldPosition;
                startRotation = player.transform.rotation.eulerAngles;
                UpdateStartLocation();
            }
        }

        private void UpdateStartLocation() {
            Game.Instance.LoadedLevelData.startPosition = SerializableVector3.FromVector3(startPosition);
            Game.Instance.LoadedLevelData.startRotation = SerializableVector3.FromVector3(startRotation);
            Game.Instance.GameModeHandler.Begin();
        }

        [Button("Copy level to Clipboard")]
        private void CopyToClipboard() {
            GUIUtility.systemCopyBuffer = Serialize().ToJsonString();
        }

        #endregion

        #region Track editing

        [HorizontalLine] [UsedImplicitly] [ReorderableList] [SerializeField]
        private List<Checkpoint> Checkpoints = new();

        [UsedImplicitly] [ReorderableList] [SerializeField]
        private List<ModifierSpawner> Modifiers = new();

        [UsedImplicitly] [ReorderableList] [SerializeField]
        private List<BillboardSpawner> Billboards = new();

        [Button("Create Checkpoint At Ship Location")]
        [UsedImplicitly]
        private void CreateCheckpointAtShip() {
            var ship = FdPlayer.FindLocalShipPlayer;
            if (ship == null) {
                Debug.LogError("No ship found - are you actually playing?...");
                return;
            }

            var checkpoint = CreateNewTrackObject(checkpointPrefab, checkpointContainer);
            checkpoint.transform.SetPositionAndRotation(ship.Position, ship.transform.rotation);
        }

        [Button("Create Checkpoint")]
        [UsedImplicitly]
        private void CreateCheckpointAtSelectedPosition() {
            CreateNewTrackObject(checkpointPrefab, checkpointContainer);
        }

        [Button("Create Modifier")]
        [UsedImplicitly]
        private void CreateModifier() {
            CreateNewTrackObject(modifierPrefab, modifierContainer);
        }

        [Button("Create Billboard ")]
        [UsedImplicitly]
        private void CreateBillboard() {
            CreateNewTrackObject(billboardPrefab, billboardContainer);
        }


        private T CreateNewTrackObject<T>(T objectToInstantiate, MonoBehaviour container) where T : MonoBehaviour {
            Undo.RecordObject(this, $"Create new {objectToInstantiate.name}");
            var position = Vector3.zero;
            var rotation = Quaternion.identity;

            // if anything is currently selected in the editor, place it there instead of at origin
            if (Selection.activeGameObject != null) {
                position = Selection.activeGameObject.transform.position;
                rotation = Selection.activeGameObject.transform.rotation;
            }

            var newObject = Instantiate(objectToInstantiate, container.transform);
            newObject.transform.SetPositionAndRotation(position, rotation);
            Selection.activeGameObject = newObject.gameObject;
            ForceRefresh();
            return newObject;
        }

        #endregion

        #region Visualisation

        private void RefreshLineRenderer() {
            var curvedLineRenderer = GetComponent<CurvedLineRenderer>();

            if (curvedLineRenderer.enabled) {
                // we add the curved line point as a child transform so it can be manually tweaked if needed in the editor without moving the checkpoints
                foreach (var checkpoint in Checkpoints) {
                    var curvedLinePoint = checkpoint.GetComponentInChildren<CurvedLinePoint>();
                    if (curvedLinePoint != null) Destroy(curvedLinePoint.gameObject);

                    var linePoint = new GameObject("Curved Line Point");
                    linePoint.AddComponent<CurvedLinePoint>();
                    linePoint.transform.SetParent(checkpoint.transform, false);
                }

                // Join up for laps
                if (gameMode == GameType.Laps.Name) {
                    var loopBackCheckpoint = Checkpoints.FindLast(c => c.Type == CheckpointType.End) ??
                                             Checkpoints.Find(c => c.Type == CheckpointType.Start);
                    if (loopBackCheckpoint != null) {
                        var linePoint = new GameObject("Curved Line Point End");
                        linePoint.AddComponent<CurvedLinePoint>();
                        var loopBackTransform = loopBackCheckpoint.transform;
                        linePoint.transform.SetPositionAndRotation(loopBackTransform.position, loopBackTransform.rotation);
                        linePoint.transform.SetParent(checkpointContainer.transform, true);
                    }
                }
            }
        }

        #endregion

        private void OnValidate() {
            checkpointContainer.RefreshCheckpoints();
            Checkpoints = checkpointContainer.Checkpoints;

            modifierContainer.RefreshModifierSpawners();
            Modifiers = modifierContainer.ModifierSpawners;

            billboardContainer.RefreshBillboardSpawners();
            Billboards = billboardContainer.BillboardSpawners;
        }

        [Button("Toggle line drawing")]
        [UsedImplicitly]
        private void ToggleLineDrawing() {
            var lineRenderer = GetComponent<LineRenderer>();
            var curvedLineRenderer = GetComponent<CurvedLineRenderer>();
            var shouldShow = !lineRenderer.enabled;

            lineRenderer.enabled = shouldShow;
            curvedLineRenderer.enabled = shouldShow;

            RefreshLineRenderer();
        }

        [Button("Force Refresh")]
        [UsedImplicitly]
        private void ForceRefresh() {
            OnValidate();
            RefreshLineRenderer();
        }
#endif
    }
}