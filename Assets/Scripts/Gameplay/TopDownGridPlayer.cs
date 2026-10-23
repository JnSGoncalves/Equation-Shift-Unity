using UnityEngine;
using UnityEngine.InputSystem;

namespace EquationShift
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class TopDownGridPlayer : MonoBehaviour
    {
        [SerializeField] private LevelGrid board;
        [SerializeField, Min(0.02f)] private float moveDuration = 0.16f;
        private Rigidbody2D body;
        private Collider2D shape;
        private Vector3Int cell;
        private Vector3Int targetCell;
        private Vector2 start;
        private Vector2Int input;
        private Vector2Int queuedInput;
        private float elapsed;

        public bool IsMoving { get; private set; }
        public Vector3Int Cell => cell;

        public void Configure(LevelGrid grid) => board = grid;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<Collider2D>();
            if (board == null) board = GetComponentInParent<LevelGrid>();
            cell = board.CellAt(transform.position);
            body.position = board.CenterOf(cell);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
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
            if (IsMoving || !LevelGrid.IsCardinal(direction)) return false;
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
