using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace EquationShift
{
    public enum EquationDirection { Horizontal, Vertical }

    [Serializable]
    public sealed class EquationResult
    {
        [SerializeField] private string expression;
        [SerializeField] private Vector3Int startCell;
        [SerializeField] private EquationDirection direction;
        [SerializeField] private bool isWellFormed;
        [SerializeField] private bool isTrue;
        [SerializeField] private string error;

        public string Expression => expression;
        public Vector3Int StartCell => startCell;
        public EquationDirection Direction => direction;
        public bool IsWellFormed => isWellFormed;
        public bool IsTrue => isTrue;
        public string Error => error;

        public EquationResult(string expression, Vector3Int startCell, EquationDirection direction)
        {
            this.expression = expression;
            this.startCell = startCell;
            this.direction = direction;
            isWellFormed = EquationEvaluator.TryEvaluate(expression, out isTrue, out error);
        }
    }

    [DisallowMultipleComponent]
    public sealed class EquationValidator : MonoBehaviour
    {
        [SerializeField] private LevelGrid board;
        [SerializeField] private Transform blocksRoot;
        [SerializeField] private bool validateAutomatically = true;
        [SerializeField] private bool logResultsOnChange;
        [SerializeField] private UnityEvent onEquationSatisfied = new UnityEvent();
        [SerializeField] private UnityEvent onEquationInvalidated = new UnityEvent();

        [Header("Resultados da ultima validacao")]
        [SerializeField] private bool hasTrueEquation;
        [SerializeField] private bool isWaitingForBlocks;
        [SerializeField] private string validationError;
        [SerializeField] private List<EquationResult> results = new List<EquationResult>();

        private readonly List<GridPushBlock> blocks = new List<GridPushBlock>();
        private readonly List<BlockState> previousLayout = new List<BlockState>();
        private readonly Dictionary<Vector3Int, GridPushBlock> cells = new Dictionary<Vector3Int, GridPushBlock>();
        private readonly List<Vector3Int> orderedCells = new List<Vector3Int>();
        private bool hasValidated;

        public IReadOnlyList<EquationResult> Results => results;
        public bool HasTrueEquation => hasTrueEquation;
        public bool IsWaitingForBlocks => isWaitingForBlocks;
        public string ValidationError => validationError;
        public int BlockCount => blocks.Count;
        public bool AllEquationsTrue
        {
            get
            {
                if (isWaitingForBlocks || results.Count == 0 || !string.IsNullOrEmpty(validationError)) return false;
                foreach (EquationResult result in results) if (!result.IsTrue) return false;
                return true;
            }
        }

        public event Action<EquationValidator> ValidationChanged;

        public void Configure(LevelGrid grid, Transform root = null)
        {
            board = grid;
            blocksRoot = root != null ? root : grid.transform;
            hasValidated = false;
        }

        private void Start() => ValidateEquations();

        private void LateUpdate()
        {
            if (!validateAutomatically || !ResolveReferences()) return;
            CollectBlocks();
            if (!hasValidated || LayoutChanged()) ValidateCurrentLayout();
        }

        [ContextMenu("Validar equacoes")]
        public bool ValidateEquations()
        {
            if (!ResolveReferences())
            {
                results.Clear();
                isWaitingForBlocks = false;
                validationError = "Defina o LevelGrid no EquationValidator.";
                PublishResult(false);
                return false;
            }
            CollectBlocks();
            ValidateCurrentLayout();
            return hasTrueEquation;
        }

        private bool ResolveReferences()
        {
            if (board == null) board = GetComponentInParent<LevelGrid>();
            if (board == null) return false;
            if (blocksRoot == null) blocksRoot = board.transform;
            return true;
        }

        private void CollectBlocks() => blocksRoot.GetComponentsInChildren(false, blocks);

        private Vector3Int CellOf(GridPushBlock block) =>
            Application.isPlaying ? block.Cell : board.CellAt(block.transform.position);

        private bool LayoutChanged()
        {
            if (blocks.Count != previousLayout.Count) return true;
            for (int i = 0; i < blocks.Count; i++)
            {
                GridPushBlock block = blocks[i];
                BlockState old = previousLayout[i];
                if (old.Block != block || old.Cell != CellOf(block) ||
                    old.Value != block.Value || old.IsMoving != block.IsMoving) return true;
            }
            return false;
        }

        private void ValidateCurrentLayout()
        {
            results.Clear();
            cells.Clear();
            orderedCells.Clear();
            previousLayout.Clear();
            isWaitingForBlocks = false;
            validationError = string.Empty;
            hasValidated = true;

            foreach (GridPushBlock block in blocks)
            {
                Vector3Int cell = CellOf(block);
                previousLayout.Add(new BlockState(block, cell));
                isWaitingForBlocks |= block.IsMoving;
                if (cells.ContainsKey(cell)) validationError = "Mais de um bloco na celula " + cell + ".";
                else { cells.Add(cell, block); orderedCells.Add(cell); }
            }

            // During a step, the origin and destination belong to the same moving block.
            // Never announce a solution assembled from intermediate positions.
            if (isWaitingForBlocks || !string.IsNullOrEmpty(validationError))
            {
                PublishResult(false);
                return;
            }

            orderedCells.Sort((a, b) => a.y != b.y ? b.y.CompareTo(a.y) : a.x.CompareTo(b.x));
            foreach (Vector3Int cell in orderedCells)
            {
                ReadLine(cell, Vector3Int.right, EquationDirection.Horizontal);
                ReadLine(cell, Vector3Int.down, EquationDirection.Vertical);
            }

            bool anyTrue = false;
            foreach (EquationResult result in results) anyTrue |= result.IsTrue;
            PublishResult(anyTrue);
        }

        private void ReadLine(Vector3Int start, Vector3Int step, EquationDirection direction)
        {
            if (cells.ContainsKey(start - step)) return;
            var expression = new StringBuilder();
            Vector3Int current = start;
            bool hasEquals = false;
            while (cells.TryGetValue(current, out GridPushBlock block))
            {
                expression.Append(block.Value);
                hasEquals |= block.Value == '=';
                current += step;
            }

            // An isolated '=' has no equation on its perpendicular axis.
            if (hasEquals && expression.Length > 1)
                results.Add(new EquationResult(expression.ToString(), start, direction));
        }

        private void PublishResult(bool anyTrue)
        {
            bool wasTrue = hasTrueEquation;
            hasTrueEquation = anyTrue;
            if (logResultsOnChange)
            {
                foreach (EquationResult result in results)
                    Debug.Log(result.Direction + ": " + result.Expression + " -> " +
                        (result.IsWellFormed ? (result.IsTrue ? "verdadeira" : "falsa") : result.Error), this);
                if (!string.IsNullOrEmpty(validationError)) Debug.LogWarning(validationError, this);
            }
            ValidationChanged?.Invoke(this);
            if (!wasTrue && anyTrue) onEquationSatisfied.Invoke();
            else if (wasTrue && !anyTrue) onEquationInvalidated.Invoke();
        }

        private readonly struct BlockState
        {
            public readonly GridPushBlock Block;
            public readonly Vector3Int Cell;
            public readonly char Value;
            public readonly bool IsMoving;

            public BlockState(GridPushBlock block, Vector3Int cell)
            {
                Block = block;
                Cell = cell;
                Value = block.Value;
                IsMoving = block.IsMoving;
            }
        }
    }
}
