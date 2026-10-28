using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EquationShift
{
    public sealed class RoomTransitionController : MonoBehaviour
    {
        private sealed class RoomState
        {
            public readonly Scene Scene;
            public readonly List<Behaviour> Paused = new List<Behaviour>();
            public readonly List<Camera> Cameras = new List<Camera>();
            public readonly List<Behaviour> LightingAndAudio = new List<Behaviour>();
            private readonly Dictionary<Transform, Vector3> roots = new Dictionary<Transform, Vector3>();
            private readonly Dictionary<Rigidbody2D, Vector2> bodies = new Dictionary<Rigidbody2D, Vector2>();

            public RoomState(Scene scene, bool destination)
            {
                Scene = scene;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Behaviour actor in root.GetComponentsInChildren<Behaviour>(true))
                    {
                        if (!actor.enabled) continue;
                        if (actor is TopDownGridPlayer || actor is GridPushBlock || actor is EquationValidator ||
                            actor is AbilityPickup || actor is GateOpeningFeedback)
                        {
                            Paused.Add(actor);
                            actor.enabled = false;
                        }
                        if (!destination) continue;
                        if (actor is Camera camera) { Cameras.Add(camera); camera.enabled = false; }
                        if (actor is AudioListener || actor is Light2D)
                        {
                            LightingAndAudio.Add(actor);
                            actor.enabled = false;
                        }
                    }
                }
            }

            public void CaptureLayout()
            {
                foreach (GameObject root in Scene.GetRootGameObjects())
                {
                    roots[root.transform] = root.transform.position;
                    foreach (Rigidbody2D body in root.GetComponentsInChildren<Rigidbody2D>(true))
                        bodies[body] = body.position;
                }
            }

            public void SetOffset(Vector3 offset)
            {
                foreach (var entry in roots)
                    if (entry.Key != null) entry.Key.position = entry.Value + offset;
                foreach (var entry in bodies)
                    if (entry.Key != null) entry.Key.position = entry.Value + (Vector2)offset;
                Physics2D.SyncTransforms();
            }

            public void Resume()
            {
                foreach (Behaviour actor in Paused) if (actor != null) actor.enabled = true;
                foreach (Behaviour actor in LightingAndAudio) if (actor != null) actor.enabled = true;
                foreach (Camera camera in Cameras) if (camera != null) camera.enabled = true;
            }
        }

        private Scene sourceScene;
        private string destinationPath;
        private string sourceId;
        private ScenePortal sourcePortal;
        private TopDownGridPlayer sourcePlayer;
        private TopDownGridPlayer destinationPlayer;
        private Camera sourceCamera;
        private RoomState source;
        private RoomState destination;
        private List<PlayerForm> forms;
        private PlayerForm selectedForm;
        private readonly List<Renderer> hiddenPlayers = new List<Renderer>();
        private SpriteRenderer traveller;
        private Vector3 cameraOriginalPosition;
        private float cameraOriginalSize;
        private bool sourceWasSimulated;
        private bool committed;

        public bool IsSliding { get; private set; }
        public float Progress { get; private set; }
        public Vector3 RoomOffset { get; private set; }
        public Vector3 CameraStart { get; private set; }
        public Vector3 CameraEnd { get; private set; }
        public Scene Destination => destination != null ? destination.Scene : default;

        internal bool Begin(ScenePortal portal, TopDownGridPlayer player, string origin, string path)
        {
            DontDestroyOnLoad(gameObject);
            sourcePortal = portal;
            sourcePlayer = player;
            sourceWasSimulated = player.GetComponent<Rigidbody2D>().simulated;
            sourceScene = portal.gameObject.scene;
            sourceCamera = FindCamera(sourceScene);
            if (sourceCamera == null)
            {
                Debug.LogWarning("A transicao de salas precisa de uma camera ativa na origem.", portal);
                return false;
            }
            sourceId = origin;
            destinationPath = path;
            forms = new List<PlayerForm>(player.UnlockedForms);
            selectedForm = player.CurrentForm;
            player.LockMovement(this);
            source = new RoomState(sourceScene, false);
            cameraOriginalPosition = sourceCamera.transform.position;
            cameraOriginalSize = sourceCamera.orthographicSize;
            SceneManager.sceneLoaded += OnSceneLoaded;
            try
            {
                if (SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive) != null) return true;
            }
            catch (Exception exception) { Debug.LogException(exception, portal); }
            return false;
        }

        internal void PrepareDestination(Scene scene)
        {
            if (destination == null && scene.path == destinationPath)
                destination = new RoomState(scene, true);
        }

        private static Camera FindCamera(Scene scene)
        {
            Camera fallback = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Camera camera in root.GetComponentsInChildren<Camera>())
                {
                    if (camera.CompareTag("MainCamera")) return camera;
                    if (fallback == null) fallback = camera;
                }
            return fallback;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != destinationPath) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            PrepareDestination(scene);
            StartCoroutine(Travel());
        }

        private IEnumerator Travel()
        {
            Scene scene = destination.Scene;
            var portals = new List<ScenePortal>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (TopDownGridPlayer player in root.GetComponentsInChildren<TopDownGridPlayer>())
                    if (destinationPlayer == null && destination.Paused.Contains(player)) destinationPlayer = player;
                portals.AddRange(root.GetComponentsInChildren<ScenePortal>());
            }
            Camera targetCamera = FindCamera(scene);
            if (destinationPlayer == null || targetCamera == null)
            {
                Debug.LogError("A sala de destino precisa de Player e Camera configurados: " + scene.path);
                yield return SceneManager.UnloadSceneAsync(scene);
                Destroy(gameObject);
                yield break;
            }
            destinationPlayer.LockMovement(this);
            destinationPlayer.RestoreForms(forms, selectedForm);
            ScenePortal counterpart = null;
            foreach (ScenePortal portal in portals)
            {
                if (!portal.isActiveAndEnabled || portal.Board != destinationPlayer.Board ||
                    !string.Equals(portal.DestinationSceneId?.Trim(), sourceId, StringComparison.OrdinalIgnoreCase)) continue;
                if (counterpart != null) { counterpart = null; break; }
                counterpart = portal;
            }

            Vector2Int direction = -sourcePortal.SpawnDirection;
            Vector2 cellStep = sourcePlayer.Board.CenterOf(sourcePlayer.Cell + new Vector3Int(direction.x, direction.y, 0)) -
                sourcePlayer.Board.CenterOf(sourcePlayer.Cell);
            RoomOffset = counterpart != null ? sourcePortal.transform.position + (Vector3)cellStep - counterpart.transform.position :
                new Vector3(direction.x * sourceCamera.orthographicSize * 2f * sourceCamera.aspect,
                    direction.y * sourceCamera.orthographicSize * 2f, 0f);
            RoomOffset = new Vector3(RoomOffset.x, RoomOffset.y, 0f);
            destination.CaptureLayout();
            destination.SetOffset(RoomOffset);

            bool received = counterpart != null && counterpart.TryReceivePlayer(destinationPlayer);
            if (!received && (!SceneTransition.TryFindSafeCenter(destinationPlayer, out Vector3Int fallback) ||
                !destinationPlayer.TeleportToCell(fallback)))
            {
                Debug.LogError("Nao existe uma celula livre para receber o Player em " + scene.path);
                yield return SceneManager.UnloadSceneAsync(scene);
                Destroy(gameObject);
                yield break;
            }
            Vector3Int arrivalCell = destinationPlayer.Cell;
            CameraStart = sourceCamera.transform.position;
            CameraEnd = targetCamera.transform.position;
            CameraEnd = new Vector3(CameraEnd.x, CameraEnd.y, CameraStart.z);
            float targetSize = targetCamera.orthographicSize;
            Vector3 playerStart = sourcePlayer.transform.position;
            Vector3 playerEnd = destinationPlayer.transform.position;
            CreateTraveller();
            HidePlayer(sourcePlayer);
            HidePlayer(destinationPlayer);
            var sourceBody = sourcePlayer.GetComponent<Rigidbody2D>();
            sourceWasSimulated = sourceBody.simulated;
            sourceBody.simulated = false;
            IsSliding = true;
            float duration = sourcePortal.TransitionDuration;
            float elapsed = 0f;
            while (elapsed < duration && sourceCamera != null && destinationPlayer != null)
            {
                Progress = Mathf.Clamp01(elapsed / duration);
                float t = Mathf.SmoothStep(0f, 1f, Progress);
                sourceCamera.transform.position = Vector3.Lerp(CameraStart, CameraEnd, t);
                sourceCamera.orthographicSize = Mathf.Lerp(cameraOriginalSize, targetSize, t);
                if (traveller != null)
                {
                    traveller.transform.position = Vector3.Lerp(playerStart, playerEnd, t);
                    var sprite = sourcePlayer.GetComponent<SpriteRenderer>();
                    if (sprite != null) traveller.sprite = sprite.sprite;
                }
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            Progress = 1f;
            IsSliding = false;
            if (sourceCamera == null || destinationPlayer == null || !scene.isLoaded || !sourceScene.isLoaded)
            {
                Destroy(gameObject);
                yield break;
            }
            if (traveller != null) Destroy(traveller.gameObject);
            // Both camera and room change coordinates in this frame, preserving the final image.
            foreach (GameObject root in sourceScene.GetRootGameObjects()) root.SetActive(false);
            destination.SetOffset(Vector3.zero);
            destinationPlayer.TeleportToCell(arrivalCell);
            SceneManager.SetActiveScene(scene);
            destination.Resume();
            foreach (Renderer renderer in hiddenPlayers)
                if (renderer != null && renderer.gameObject.scene == scene) renderer.enabled = true;
            committed = true;
            yield return SceneManager.UnloadSceneAsync(sourceScene);
            destinationPlayer.UnlockMovement(this);
            SceneTransition.Release(this);
            foreach (EquationGate gate in destinationPlayer.Board.GetComponentsInChildren<EquationGate>())
                gate.RefreshSolution();
            Destroy(gameObject);
        }

        private void CreateTraveller()
        {
            var sprite = sourcePlayer.GetComponent<SpriteRenderer>();
            if (sprite == null) return;
            traveller = new GameObject("Travelling Player").AddComponent<SpriteRenderer>();
            traveller.transform.SetParent(transform, false);
            traveller.transform.localScale = sourcePlayer.transform.lossyScale;
            traveller.sprite = sprite.sprite;
            traveller.sharedMaterial = sprite.sharedMaterial;
            traveller.color = sprite.color;
            traveller.flipX = sprite.flipX;
            traveller.flipY = sprite.flipY;
            traveller.sortingLayerID = sprite.sortingLayerID;
            traveller.sortingOrder = sprite.sortingOrder;
        }

        private void HidePlayer(TopDownGridPlayer player)
        {
            foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
                if (renderer.enabled) { hiddenPlayers.Add(renderer); renderer.enabled = false; }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (!committed)
            {
                if (sourceCamera != null)
                {
                    sourceCamera.transform.position = cameraOriginalPosition;
                    sourceCamera.orthographicSize = cameraOriginalSize;
                }
                source?.Resume();
                if (destination != null && destination.Scene.isLoaded)
                {
                    destination.SetOffset(Vector3.zero);
                    destination.Resume();
                }
                if (sourcePlayer != null) sourcePlayer.GetComponent<Rigidbody2D>().simulated = sourceWasSimulated;
                foreach (Renderer renderer in hiddenPlayers) if (renderer != null) renderer.enabled = true;
            }
            if (sourcePlayer != null) sourcePlayer.UnlockMovement(this);
            if (destinationPlayer != null) destinationPlayer.UnlockMovement(this);
            SceneTransition.Release(this);
        }
    }
}
