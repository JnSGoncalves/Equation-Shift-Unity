using System.Collections;
using UnityEngine;

namespace EquationShift
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent, RequireComponent(typeof(LevelGrid))]
    public sealed class RuinsEntranceCutscene : MonoBehaviour
    {
        [SerializeField] private Transform error;
        [SerializeField] private EquationGate gate;
        [SerializeField] private ScenePortal exit;
        [SerializeField] private GateOpeningFeedback feedback;
        [SerializeField] private Vector2 triggerPosition = new Vector2(-12.5f, -1.5f);
        [SerializeField, Min(0.1f)] private float walkSpeed = 6f;
        [SerializeField, Min(0.1f)] private float shakeDuration = 0.8f;

        private const string Dialogue = "Hahaha! Tremor engraçado. Deixa eu ver o que acontece se eu mexer mais por aqui.";
        private TopDownGridPlayer player;
        private Vector3Int triggerCell;
        private Vector3 errorStart;
        private Transform cameraTransform;
        private Vector3 cameraStart;
        private Coroutine sequence;
        private bool completed;
        private bool openedGateForEscape;

        private void Awake()
        {
            var board = GetComponent<LevelGrid>();
            player = GetComponentInChildren<TopDownGridPlayer>(true);
            triggerCell = board.CellAt(transform.TransformPoint(triggerPosition));
            if (error != null) errorStart = error.localPosition;
        }

        private void FixedUpdate()
        {
            if (completed || sequence != null || SceneTransition.IsTravelling || player == null ||
                !player.isActiveAndEnabled || player.IsMoving || player.IsMovementLocked ||
                player.Cell != triggerCell || error == null || gate == null || exit == null) return;
            sequence = StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            player.LockForCutscene(this);
            VillageDialogue.Hide();
            Camera camera = null;
            if (feedback != null && feedback.isActiveAndEnabled)
            {
                feedback.PlayQuickShake(shakeDuration);
                yield return new WaitUntil(() => !feedback.isActiveAndEnabled || !feedback.IsQuickShaking);
                yield return null;
                camera = feedback.GetComponent<Camera>();
                cameraTransform = camera.transform;
                cameraStart = cameraTransform.position;
            }

            FrameError(camera);
            VillageDialogue.Show(Dialogue);
            while (VillageDialogue.IsVisible) yield return null;

            var board = player.Board;
            yield return WalkTo(board.CenterOf(board.CellAt(error.position) + Vector3Int.down), camera);
            Vector2Int inward = exit.SpawnDirection;
            var inwardStep = new Vector3(inward.x, inward.y, 0f);
            yield return WalkTo(gate.transform.position + inwardStep, camera);
            openedGateForEscape = !gate.IsOpen;
            gate.SetOpen(true);
            while (gate.IsOpening) yield return null;
            yield return WalkTo(exit.transform.position - inwardStep, camera);
            error.gameObject.SetActive(false);
            gate.SetOpen(false);
            while (gate.IsClosing) yield return null;

            RestoreCamera();
            completed = true;
            player.UnlockMovement(this);
            sequence = null;
        }

        private IEnumerator WalkTo(Vector3 destination, Camera camera)
        {
            destination.z = error.position.z;
            while (error.position != destination)
            {
                error.position = Vector3.MoveTowards(error.position, destination, walkSpeed * Time.unscaledDeltaTime);
                FrameError(camera);
                yield return null;
            }
        }

        private void FrameError(Camera camera)
        {
            if (camera == null || !camera.orthographic) return;
            float viewWidth = Mathf.Max(0f, camera.orthographicSize * camera.aspect - 1f);
            float focus = Mathf.Min(error.position.x, gate.transform.position.x) - cameraStart.x;
            cameraTransform.position = cameraStart + Vector3.right * Mathf.Max(0f, focus - viewWidth);
        }

        private void RestoreCamera()
        {
            if (cameraTransform != null) cameraTransform.position = cameraStart;
            cameraTransform = null;
        }

        private void OnDisable()
        {
            if (sequence == null) return;
            StopCoroutine(sequence);
            sequence = null;
            VillageDialogue.Hide();
            RestoreCamera();
            if (player != null) player.UnlockMovement(this);
            if (error != null)
            {
                error.localPosition = errorStart;
                error.gameObject.SetActive(true);
            }
            if (openedGateForEscape && gate != null) gate.SetOpen(false, false);
            openedGateForEscape = false;
        }
    }
}
