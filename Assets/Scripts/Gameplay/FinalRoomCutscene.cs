using System.Collections;
using UnityEngine;

namespace EquationShift
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent, RequireComponent(typeof(LevelGrid))]
    public sealed class FinalRoomCutscene : MonoBehaviour
    {
        [SerializeField] private Transform error;
        [SerializeField] private EquationGate gate;
        [SerializeField] private ScenePortal exit;
        [SerializeField] private GateOpeningFeedback feedback;
        [SerializeField] private Vector2 triggerPosition = new Vector2(-0.5f, -5.5f);
        [SerializeField, Min(0.1f)] private float walkSpeed = 6f;
        [SerializeField, Min(0.1f)] private float shakeDuration = 1.4f;

        private const string Dialogue = "Droga, não deveria ter brincado com essas equações. " +
                                        "Vou embora daqui antes que tudo desmorone!";

        private TopDownGridPlayer player;
        private Vector3Int triggerCell;
        private Vector3 errorStart;
        private Coroutine sequence;
        private bool completed;

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
            VillageDialogue.Show(Dialogue);
            while (VillageDialogue.IsVisible) yield return null;

            Vector2Int inward = exit.SpawnDirection;
            Vector2Int sideways = new Vector2Int(-inward.y, inward.x);
            Vector3Int approachCell = triggerCell + new Vector3Int(inward.x, inward.y, 0);
            yield return WalkTo(player.Board.CenterOf(approachCell));
            if (player.TryStepAside(sideways, this) || player.TryStepAside(-sideways, this))
                while (player.IsMoving) yield return null;

            gate.SetOpen(true);
            while (gate.IsOpening) yield return null;
            yield return WalkTo(exit.transform.position - new Vector3(inward.x, inward.y, 0f));
            error.gameObject.SetActive(false);
            gate.SetOpen(false);
            while (gate.IsClosing) yield return null;

            if (feedback != null && feedback.isActiveAndEnabled)
            {
                feedback.PlayQuickShake(shakeDuration);
                yield return new WaitUntil(() => !feedback.isActiveAndEnabled || !feedback.IsQuickShaking);
                yield return null;
            }

            player.RememberRoomEntrance(null);
            completed = true;
            player.UnlockMovement(this);
            sequence = null;
        }

        private IEnumerator WalkTo(Vector3 destination)
        {
            destination.z = error.position.z;
            while (error.position != destination)
            {
                error.position = Vector3.MoveTowards(error.position, destination, walkSpeed * Time.unscaledDeltaTime);
                yield return null;
            }
        }

        private void OnDisable()
        {
            if (sequence == null) return;
            StopCoroutine(sequence);
            sequence = null;
            VillageDialogue.Hide();
            if (player != null) player.UnlockMovement(this);
            if (error != null)
            {
                error.localPosition = errorStart;
                error.gameObject.SetActive(true);
            }
            if (gate != null) gate.SetOpen(true, false);
        }
    }
}
