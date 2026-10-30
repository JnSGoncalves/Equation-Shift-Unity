using UnityEngine;

namespace EquationShift
{
    public enum AncorSide { T, D, L, R }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class GridPushBlock : MonoBehaviour
    {
        [SerializeField] private LevelGrid board;
        [SerializeField, Min(0.02f)] private float moveDuration = 0.16f;
        [SerializeField] private bool isMovable = true;
        [SerializeField] private char value = '0';
        [SerializeField] private bool isAncor;
        [Tooltip("Lado da passagem associada: T = Top, D = Down, L = Left, R = Right.")]
        [SerializeField] private AncorSide ancorSide;

        private Rigidbody2D body;
        private Collider2D shape;
        private Vector3Int cell;
        private Vector3Int targetCell;
        private Vector2 start;
        private float elapsed;
        private Vector3Int initialCell;
        private char initialValue;
        private bool initiallyMovable;
        private bool initiallyAncor;
        private AncorSide initialAncorSide;

        public bool IsMoving { get; private set; }
        public bool IsMovable { get => isMovable; set => isMovable = value; }
        public char Value { get => value; set => this.value = value; }
        public bool IsAncor { get => isAncor; set => isAncor = value; }
        public AncorSide AncorSide { get => ancorSide; set => ancorSide = value; }
        public Vector3Int Cell => cell;
        public Collider2D Collider => shape;

        public void Configure(LevelGrid grid) => board = grid;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<Collider2D>();
            if (board == null) board = GetComponentInParent<LevelGrid>();
            if (board == null)
            {
                Debug.LogError("Coloque o bloco dentro de Grid/Blocks ou atribua o LevelGrid no Inspector.", this);
                enabled = false;
                return;
            }
            cell = board.CellAt(transform.position);
            body.position = board.CenterOf(cell);
            initialCell = cell;
            initialValue = value;
            initiallyMovable = isMovable;
            initiallyAncor = isAncor;
            initialAncorSide = ancorSide;
        }

        internal void ResetToInitialState()
        {
            if (body == null || board == null) return;
            cell = targetCell = initialCell;
            start = board.CenterOf(cell);
            elapsed = 0f;
            IsMoving = false;
            value = initialValue;
            isMovable = initiallyMovable;
            isAncor = initiallyAncor;
            ancorSide = initialAncorSide;
            body.linearVelocity = Vector2.zero;
            body.position = start;
            transform.position = new Vector3(start.x, start.y, transform.position.z);
        }

        public bool Occupies(Vector3Int position) =>
            cell == position || (IsMoving && targetCell == position);

        // Public entry point for later abilities. Never moves diagonally or pushes a chain.
        public bool TryMove(Vector2Int direction)
        {
            if (!isMovable || isAncor || IsMoving || !LevelGrid.IsCardinal(direction)) return false;
            Vector3Int destination = cell + new Vector3Int(direction.x, direction.y, 0);
            if (!board.IsFloor(destination) || board.BlockAt(destination) != null ||
                board.HasObstacle(destination, shape)) return false;

            targetCell = destination;
            start = body.position;
            elapsed = 0f;
            IsMoving = true;
            return true;
        }

        private void FixedUpdate()
        {
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
