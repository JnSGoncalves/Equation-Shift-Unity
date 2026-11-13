using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EquationShift
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent, RequireComponent(typeof(LevelGrid))]
    public sealed class VillageSequence : MonoBehaviour
    {
        [SerializeField] private VillageNpc error;
        [SerializeField] private EquationGate gate;
        [SerializeField] private ScenePortal exit;
        [SerializeField] private GateOpeningFeedback feedback;
        [SerializeField, Min(0.1f)] private float walkSpeed = 6f;

        private const int RequiredConversations = 5;
        private static bool escapedThisRun;
        private readonly HashSet<VillageNpc> talkedTo = new HashSet<VillageNpc>();
        private LevelGrid board;
        private TopDownGridPlayer player;
        private VillageNpc[] residents;
        private VillageNpc nearbyNpc;
        private Vector3 errorStart;
        private Camera cutsceneCamera;
        private Vector3 cameraStart;
        private Coroutine escapeRoutine;

        public bool HasEscaped => escapedThisRun;
        public int TalkedCount => talkedTo.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetProgress() => escapedThisRun = false;

        private void Awake()
        {
            board = GetComponent<LevelGrid>();
            player = GetComponentInChildren<TopDownGridPlayer>(true);
            residents = GetComponentsInChildren<VillageNpc>();
            if (error != null) errorStart = error.transform.localPosition;
        }

        private void Start()
        {
            if (!HasEscaped)
            {
                VillageDialogue.Show("Utilize as setas ou WASD para mover o personagem.");
                return;
            }
            if (error != null) error.gameObject.SetActive(false);
            if (gate != null) gate.SetOpen(true, false);
        }

        private void FixedUpdate()
        {
            if (escapeRoutine != null || SceneTransition.IsTravelling || (feedback != null && feedback.IsQuickShaking) ||
                player == null ||
                !player.isActiveAndEnabled || player.IsMoving || player.IsMovementLocked) return;

            VillageNpc npc = FindNearbyNpc();
            if (npc == nearbyNpc) return;
            nearbyNpc = npc;
            if (npc == null) return;

            string dialogue = npc.GetDialogue(HasEscaped);
            if (string.IsNullOrWhiteSpace(dialogue)) return;
            VillageDialogue.Show(dialogue);
            talkedTo.Add(npc);
            if (!HasEscaped && talkedTo.Count >= RequiredConversations && talkedTo.Contains(error) &&
                gate != null && exit != null)
                escapeRoutine = StartCoroutine(Escape());
        }

        private VillageNpc FindNearbyNpc()
        {
            foreach (VillageNpc npc in residents)
            {
                if (!npc.isActiveAndEnabled) continue;
                Vector3Int offset = board.CellAt(npc.transform.position) - player.Cell;
                if (offset.z == 0 && LevelGrid.IsCardinal(new Vector2Int(offset.x, offset.y))) return npc;
            }
            return null;
        }

        private IEnumerator Escape()
        {
            // Give the last resident's line time to finish before taking control.
            while (VillageDialogue.IsVisible) yield return null;
            player.LockMovement(this);
            while (player.IsMoving) yield return null;
            VillageDialogue.Hide();

            if (feedback != null && feedback.TryGetComponent(out cutsceneCamera))
                cameraStart = cutsceneCamera.transform.position;
            Vector2Int inward = exit.SpawnDirection;
            var inwardStep = new Vector3(inward.x, inward.y, 0f);
            yield return WalkTo(gate.transform.position + inwardStep);
            gate.SetOpen(true);
            while (gate.IsOpening) yield return null;
            yield return WalkTo(exit.transform.position - inwardStep);

            error.gameObject.SetActive(false);
            yield return RestoreCamera();
            escapedThisRun = true;
            if (feedback != null)
            {
                GameAudio.Play(SoundCue.Tremor);
                feedback.PlayQuickShake();
            }
            nearbyNpc = null;
            player.UnlockMovement(this);
            escapeRoutine = null;
        }

        private IEnumerator WalkTo(Vector3 destination)
        {
            Transform actor = error.transform;
            destination.z = actor.position.z;
            while (actor.position != destination)
            {
                actor.position = Vector3.MoveTowards(actor.position, destination, walkSpeed * Time.deltaTime);
                FrameWalk(actor.position);
                yield return null;
            }
        }

        private void FrameWalk(Vector3 actorPosition)
        {
            if (cutsceneCamera == null || !cutsceneCamera.orthographic) return;
            Vector3 right = cutsceneCamera.transform.right;
            float gateOffset = Vector3.Dot(gate.transform.position - cameraStart, right);
            float actorOffset = Vector3.Dot(actorPosition - cameraStart, right);
            float focus = Mathf.Clamp(actorOffset, Mathf.Min(0f, gateOffset), Mathf.Max(0f, gateOffset));
            float visibleHalfWidth = Mathf.Max(0f, cutsceneCamera.orthographicSize * cutsceneCamera.aspect - 1.75f);
            cutsceneCamera.transform.position = cameraStart +
                right * (focus - Mathf.Clamp(focus, -visibleHalfWidth, visibleHalfWidth));
        }

        private IEnumerator RestoreCamera()
        {
            if (cutsceneCamera == null) yield break;
            Vector3 from = cutsceneCamera.transform.position;
            const float duration = 0.22f;
            for (float elapsed = 0f; from != cameraStart && elapsed < duration; elapsed += Time.deltaTime)
            {
                cutsceneCamera.transform.position = Vector3.Lerp(from, cameraStart, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }
            ResetCamera();
        }

        private void ResetCamera()
        {
            if (cutsceneCamera != null) cutsceneCamera.transform.position = cameraStart;
            cutsceneCamera = null;
        }

        private void OnDisable()
        {
            if (escapeRoutine == null) return;
            StopCoroutine(escapeRoutine);
            escapeRoutine = null;
            nearbyNpc = null;
            ResetCamera();
            VillageDialogue.Hide();
            if (player != null) player.UnlockMovement(this);
            if (HasEscaped) return;
            if (error != null)
            {
                error.transform.localPosition = errorStart;
                error.gameObject.SetActive(true);
            }
            if (gate != null) gate.SetOpen(false, false);
        }
    }
}
