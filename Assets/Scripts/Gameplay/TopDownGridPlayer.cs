using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EquationShift
{
    public enum PlayerForm
    {
        Default = 0,
        Equal = 1,
        Plus = 2,
        Minus = 3,
        Mult = 4,
        Divis = 5
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class TopDownGridPlayer : MonoBehaviour
    {
        [SerializeField] private LevelGrid board;
        [SerializeField, Min(0.02f)] private float moveDuration = 0.16f;
        [SerializeField] private PlayerForm currentForm = PlayerForm.Default;
        [SerializeField] private List<PlayerForm> unlockedForms = new List<PlayerForm>();
        private static readonly int[] IdleStateHashes =
        {
            Animator.StringToHash("Base Layer.player_default_idle"),
            Animator.StringToHash("Base Layer.player_equal_idle"),
            Animator.StringToHash("Base Layer.player_plus_idle"),
            Animator.StringToHash("Base Layer.player_minus_idle"),
            Animator.StringToHash("Base Layer.player_mult_idle"),
            Animator.StringToHash("Base Layer.player_divis_idle")
        };
        private Animator animator;
        private int animatedStateHash;
        private Rigidbody2D body;
        private Collider2D shape;
        private Vector3Int cell;
        private Vector3Int targetCell;
        private Vector2 start;
        private Vector2Int input;
        private Vector2Int queuedInput;
        private float elapsed;
        private readonly HashSet<object> movementLocks = new HashSet<object>();

        public bool IsMoving { get; private set; }
        public bool IsMovementLocked => movementLocks.Count > 0;
        public Vector3Int Cell => cell;
        public LevelGrid Board => board;
        public IReadOnlyList<PlayerForm> UnlockedForms => unlockedForms;
        public PlayerForm CurrentForm
        {
            get => currentForm;
            set
            {
                if ((uint)value > (uint)PlayerForm.Divis)
                    throw new System.ArgumentOutOfRangeException(nameof(value));
                if (!IsFormUnlocked(value)) return;
                currentForm = value;
                ApplyFormAnimation();
            }
        }

        public void Configure(LevelGrid grid) => board = grid;

        public bool CanStandAt(Vector3Int destination) => board != null && board.IsFloor(destination) &&
            board.BlockAt(destination) == null && !board.HasObstacle(destination, shape);

        public bool TeleportToCell(Vector3Int destination)
        {
            if (!CanStandAt(destination)) return false;
            cell = targetCell = destination;
            start = board.CenterOf(destination);
            IsMoving = false;
            elapsed = 0f;
            input = queuedInput = Vector2Int.zero;
            body.linearVelocity = Vector2.zero;
            body.position = start;
            transform.position = new Vector3(start.x, start.y, transform.position.z);
            return true;
        }

        internal void RestoreForms(IReadOnlyList<PlayerForm> forms, PlayerForm selected)
        {
            unlockedForms.Clear();
            foreach (PlayerForm form in forms) UnlockForm(form);
            CurrentForm = selected;
        }

        public bool IsFormUnlocked(PlayerForm form) => form == PlayerForm.Default || unlockedForms.Contains(form);

        public bool UnlockForm(PlayerForm form)
        {
            if (form <= PlayerForm.Default || form > PlayerForm.Divis || IsFormUnlocked(form)) return false;
            unlockedForms.Add(form);
            return true;
        }

        public void CycleForm()
        {
            for (int offset = 1; offset <= IdleStateHashes.Length; offset++)
            {
                var next = (PlayerForm)(((int)currentForm + offset) % IdleStateHashes.Length);
                if (!IsFormUnlocked(next)) continue;
                CurrentForm = next;
                return;
            }
        }

        public bool TryGetEquationSymbol(out char symbol)
        {
            if (!IsFormUnlocked(currentForm)) { symbol = '\0'; return false; }
            switch (currentForm)
            {
                case PlayerForm.Equal: symbol = '='; return true;
                case PlayerForm.Plus: symbol = '+'; return true;
                case PlayerForm.Minus: symbol = '-'; return true;
                case PlayerForm.Mult: symbol = '*'; return true;
                case PlayerForm.Divis: symbol = '/'; return true;
                default: symbol = '\0'; return false;
            }
        }

        private void ApplyFormAnimation()
        {
            if (!IsFormUnlocked(currentForm)) currentForm = PlayerForm.Default;
            if (!isActiveAndEnabled || animator == null || !animator.isActiveAndEnabled ||
                animator.runtimeAnimatorController == null) return;
            int stateHash = IdleStateHashes[(int)currentForm];
            if (animatedStateHash == stateHash) return;
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
            animatedStateHash = stateHash;
        }

        public void LockMovement(object owner)
        {
            if (owner == null) return;
            movementLocks.Add(owner);
            input = queuedInput = Vector2Int.zero;
        }

        public void UnlockMovement(object owner)
        {
            if (owner != null) movementLocks.Remove(owner);
            input = queuedInput = Vector2Int.zero;
        }

        private void Awake()
        {
            unlockedForms.Clear();
            currentForm = PlayerForm.Default;
            animator = GetComponent<Animator>();
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<Collider2D>();
            if (board == null) board = GetComponentInParent<LevelGrid>();
            cell = board.CellAt(transform.position);
            body.position = board.CenterOf(cell);
        }

        private void OnEnable()
        {
            animatedStateHash = 0;
            ApplyFormAnimation();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame) CycleForm();
            // Also reflect changes made to Current Form in the Inspector during Play Mode.
            ApplyFormAnimation();
            if (IsMovementLocked) { input = queuedInput = Vector2Int.zero; return; }
            if (keyboard == null) { input = Vector2Int.zero; return; }
            input = Vector2Int.zero;
            // A single axis wins when two keys are held; diagonal steps are never produced.
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input = Vector2Int.up;
            else if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input = Vector2Int.down;
            else if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input = Vector2Int.left;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input = Vector2Int.right;

            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) queuedInput = Vector2Int.up;
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) queuedInput = Vector2Int.down;
            else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) queuedInput = Vector2Int.left;
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) queuedInput = Vector2Int.right;
        }

        private void OnDisable() { input = Vector2Int.zero; queuedInput = Vector2Int.zero; }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) { input = Vector2Int.zero; queuedInput = Vector2Int.zero; }
        }

        public bool TryMove(Vector2Int direction)
        {
            if (IsMovementLocked || IsMoving || !LevelGrid.IsCardinal(direction)) return false;
            Vector3Int destination = cell + new Vector3Int(direction.x, direction.y, 0);
            if (!board.IsFloor(destination)) return false;
            GridPushBlock block = board.BlockAt(destination);
            if (board.HasObstacle(destination, shape, block != null ? block.Collider : null)) return false;
            if (block != null && !block.TryMove(direction)) return false;

            targetCell = destination;
            start = body.position;
            elapsed = 0f;
            IsMoving = true;
            return true;
        }

        private void FixedUpdate()
        {
            if (!IsMoving)
            {
                Vector2Int direction = queuedInput != Vector2Int.zero ? queuedInput : input;
                queuedInput = Vector2Int.zero;
                TryMove(direction);
            }
            if (!IsMoving) return;
            elapsed += Time.fixedDeltaTime;
            float progress = Mathf.Clamp01(elapsed / moveDuration);
            body.MovePosition(Vector2.Lerp(start, board.CenterOf(targetCell), progress));
            if (progress < 1f) return;
            cell = targetCell;
            IsMoving = false;
        }
    }
}
