using System;
using System.Collections.Generic;
using UnityEngine;

namespace EquationShift
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class RoomLever : MonoBehaviour
    {
        [SerializeField] private string leverId;

        private static readonly HashSet<string> ActivatedIds = new HashSet<string>(StringComparer.Ordinal);
        private static readonly int ActivatedParameter = Animator.StringToHash("IsActivated");
        private static readonly int ActiveState = Animator.StringToHash("Base Layer.Active");

        private Animator animator;
        private LevelGrid board;
        private TopDownGridPlayer player;

        internal static event Action Activated;
        internal static bool IsIdActivated(string id) => ActivatedIds.Contains(id);
        public bool IsActivated => IsIdActivated(leverId);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetProgress()
        {
            ActivatedIds.Clear();
            Activated = null;
        }

        public void Configure(string id)
        {
            leverId = id;
            if (isActiveAndEnabled) RestoreVisual();
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            board = GetComponentInParent<LevelGrid>();
        }

        private void OnEnable() => RestoreVisual();

        private void RestoreVisual()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetBool(ActivatedParameter, IsActivated);
            if (IsActivated && animator.HasState(0, ActiveState))
            {
                animator.Play(ActiveState, 0, 0f);
                animator.Update(0f);
            }
        }

        private void FixedUpdate()
        {
            if (board == null || string.IsNullOrWhiteSpace(leverId) || IsActivated || SceneTransition.IsTravelling)
                return;
            if (player == null) player = board.GetComponentInChildren<TopDownGridPlayer>();
            if (player == null || !player.isActiveAndEnabled || player.Board != board ||
                player.IsMoving || player.IsMovementLocked) return;

            Vector3Int offset = player.Cell - board.CellAt(transform.position);
            if (offset.z != 0 || !LevelGrid.IsCardinal(new Vector2Int(offset.x, offset.y))) return;

            ActivatedIds.Add(leverId);
            if (animator.runtimeAnimatorController != null) animator.SetBool(ActivatedParameter, true);
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                var feedback = root.GetComponentInChildren<GateOpeningFeedback>();
                if (feedback == null || !feedback.isActiveAndEnabled) continue;
                feedback.PlayQuickShake();
                break;
            }
            Activated?.Invoke();
        }
    }
}
