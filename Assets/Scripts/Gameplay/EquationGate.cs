using System.Collections;
using UnityEngine;

namespace EquationShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator), typeof(BoxCollider2D))]
    public sealed class EquationGate : MonoBehaviour
    {
        [SerializeField] private EquationValidator validator;
        [SerializeField] private GridPushBlock ancor;
        [SerializeField] private AncorSide passageSide;
        [SerializeField] private GateOpeningFeedback feedback;
        [SerializeField] private AnimationClip openingAnimation;
        [SerializeField] private AnimationClip closingAnimation;
        [SerializeField, Tooltip("Estado inicial; alterar durante Play anima a abertura ou o fechamento.")]
        private bool isOpen;
        [SerializeField] private Collider2D[] blockingColliders;

        private Animator animator;
        private EquationValidator subscribedValidator;
        private Coroutine stateChange;
        private bool requestedOpen;
        private TopDownGridPlayer arrivingPlayer;

        public GridPushBlock Ancor => ancor;
        public AncorSide PassageSide => passageSide;
        public AnimationClip OpeningAnimation => openingAnimation;
        public float OpeningDuration => openingAnimation != null ? openingAnimation.length : 1f;
        public float ClosingDuration => closingAnimation != null ? closingAnimation.length : OpeningDuration;
        public bool DefaultIsOpen { get; private set; }
        public bool IsWaitingForArrivalExit => arrivingPlayer != null;
        public bool IsOpening { get; private set; }
        public bool IsClosing { get; private set; }
        internal bool IsFeedbackTransition { get; private set; }
        internal bool FeedbackOpens { get; private set; }
        internal float FeedbackDuration => FeedbackOpens ? OpeningDuration : ClosingDuration;
        public bool IsOpen
        {
            get => isOpen;
            set => SetOpen(value);
        }

        public void Configure(EquationValidator equationValidator, GridPushBlock anchor,
            AncorSide side, GateOpeningFeedback openingFeedback, AnimationClip clip, AnimationClip closingClip = null)
        {
            Unsubscribe();
            validator = equationValidator;
            ancor = anchor;
            passageSide = side;
            feedback = openingFeedback;
            openingAnimation = clip;
            closingAnimation = closingClip;
            blockingColliders = GetComponentsInChildren<Collider2D>(true);
            if (Application.isPlaying && isActiveAndEnabled) Subscribe();
        }

        private void Awake()
        {
            DefaultIsOpen = isOpen;
            animator = GetComponent<Animator>();
            if (blockingColliders == null || blockingColliders.Length == 0)
                blockingColliders = GetComponentsInChildren<Collider2D>(true);
            ApplyConfiguredState();
        }

        private void OnEnable()
        {
            ApplyConfiguredState();
            Subscribe();
        }
        private void Start() => CheckSolution(validator);
        internal void RefreshSolution() => CheckSolution(validator);
        private void OnDisable()
        {
            Unsubscribe();
            if (stateChange != null) StopCoroutine(stateChange);
            stateChange = null;
            arrivingPlayer = null;
            IsFeedbackTransition = IsOpening = IsClosing = false;
            ApplyConfiguredState();
        }

        private void Update()
        {
            if (SceneTransition.IsTravelling) return;
            // Serialized Inspector edits follow the same path as calls to SetOpen.
            if (isOpen != requestedOpen) SetOpen(isOpen);
            if (arrivingPlayer == null || arrivingPlayer.IsMoving || OverlapsArrivalPlayer()) return;
            arrivingPlayer = null;
            if (!TryQueueFeedback(false)) SetOpen(false);
        }

        public void OpenForArrival(TopDownGridPlayer player)
        {
            SetOpen(true, false);
            if (!DefaultIsOpen) arrivingPlayer = player;
        }

        private bool OverlapsArrivalPlayer()
        {
            Vector2 position = arrivingPlayer.Board.CenterOf(arrivingPlayer.Cell);
            Vector2 playerExtents = arrivingPlayer.GetComponent<Collider2D>().bounds.extents;
            foreach (Collider2D shape in blockingColliders)
            {
                if (shape == null || shape.isTrigger || !(shape is BoxCollider2D box)) continue;
                // Disabled gate colliders have empty bounds; derive their footprint from the box.
                Vector2 center = box.transform.TransformPoint(box.offset);
                Vector2 xAxis = box.transform.TransformVector(Vector2.right * box.size.x * 0.5f);
                Vector2 yAxis = box.transform.TransformVector(Vector2.up * box.size.y * 0.5f);
                Vector2 extents = new Vector2(Mathf.Abs(xAxis.x) + Mathf.Abs(yAxis.x),
                    Mathf.Abs(xAxis.y) + Mathf.Abs(yAxis.y)) + playerExtents;
                Vector2 delta = position - center;
                if (Mathf.Abs(delta.x) <= extents.x && Mathf.Abs(delta.y) <= extents.y) return true;
            }
            return false;
        }

        public void SetOpen(bool open, bool animate = true)
        {
            bool changed = requestedOpen != open || IsFeedbackTransition;
            isOpen = requestedOpen = open;
            arrivingPlayer = null;
            IsFeedbackTransition = false;
            if (!Application.isPlaying || !isActiveAndEnabled || !animate)
            {
                if (stateChange != null) StopCoroutine(stateChange);
                stateChange = null;
                IsOpening = IsClosing = false;
                ApplyConfiguredState();
                return;
            }
            if (!changed) return;
            if (stateChange != null) StopCoroutine(stateChange);
            IsOpening = open;
            IsClosing = !open;
            // Closing blocks immediately; opening becomes passable after its last frame.
            SetCollision(true);
            stateChange = StartCoroutine(AnimateState(open));
        }

        private IEnumerator AnimateState(bool open)
        {
            SetAnimation(open ? "Opening" : "Closing");
            float duration = open ? OpeningDuration : ClosingDuration;
            yield return new WaitForSecondsRealtime(duration);
            ApplyConfiguredState();
            IsOpening = IsClosing = false;
            stateChange = null;
        }

        public void ApplyConfiguredState()
        {
            requestedOpen = isOpen;
            if (blockingColliders == null || blockingColliders.Length == 0)
                blockingColliders = GetComponentsInChildren<Collider2D>(true);
            SetCollision(!isOpen);
            if (Application.isPlaying) SetAnimation(isOpen ? "Open" : "Closed");
            else if (openingAnimation != null)
                openingAnimation.SampleAnimation(gameObject, isOpen ? openingAnimation.length : 0f);
        }

        private void Subscribe()
        {
            if (validator == null)
            {
                var grid = GetComponentInParent<LevelGrid>();
                if (grid != null) validator = grid.GetComponentInChildren<EquationValidator>();
            }
            if (validator == null || subscribedValidator == validator) return;
            subscribedValidator = validator;
            subscribedValidator.ValidationChanged += CheckSolution;
        }

        private void Unsubscribe()
        {
            if (subscribedValidator != null) subscribedValidator.ValidationChanged -= CheckSolution;
            subscribedValidator = null;
        }

        private void CheckSolution(EquationValidator source)
        {
            if (SceneTransition.IsTravelling || !isActiveAndEnabled || IsOpen || IsOpening || IsClosing || source == null ||
                source.IsWaitingForBlocks || !string.IsNullOrEmpty(source.ValidationError) ||
                ancor == null || !ancor.isActiveAndEnabled || !ancor.IsAncor || ancor.AncorSide != passageSide) return;

            foreach (EquationResult equation in source.Results)
            {
                if (!equation.IsTrue || !equation.Contains(ancor)) continue;
                if (feedback == null || !feedback.isActiveAndEnabled) return;
                TryQueueFeedback(true);
                return;
            }
        }

        private bool TryQueueFeedback(bool open)
        {
            if (feedback == null || !feedback.isActiveAndEnabled) return false;
            FeedbackOpens = open;
            IsFeedbackTransition = true;
            IsOpening = open;
            IsClosing = !open;
            if (feedback.Enqueue(this)) return true;
            IsFeedbackTransition = IsOpening = IsClosing = false;
            return false;
        }

        internal void BeginFeedbackTransition()
        {
            if (!IsFeedbackTransition) return;
            if (!FeedbackOpens) SetCollision(true);
            SetAnimation(FeedbackOpens ? "Opening" : "Closing");
        }

        internal void CompleteFeedbackTransition()
        {
            if (!IsFeedbackTransition) return;
            isOpen = requestedOpen = FeedbackOpens;
            IsFeedbackTransition = IsOpening = IsClosing = false;
            ApplyConfiguredState();
        }

        internal void CancelFeedbackTransition()
        {
            if (!IsFeedbackTransition) return;
            IsFeedbackTransition = IsOpening = IsClosing = false;
            ApplyConfiguredState();
        }

        private void SetCollision(bool solid)
        {
            if (blockingColliders == null) return;
            foreach (Collider2D shape in blockingColliders)
                if (shape != null && !shape.isTrigger) shape.enabled = solid;
        }

        private void SetAnimation(string state)
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.enabled = true;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.speed = 1f;
            int stateHash = Animator.StringToHash("Base Layer." + state);
            if (!animator.HasState(0, stateHash)) return;
            animator.Play(stateHash, 0, 0f);
            animator.Update(0f);
        }
    }
}
