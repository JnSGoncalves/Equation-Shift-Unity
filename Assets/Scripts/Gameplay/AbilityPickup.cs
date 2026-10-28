using System.Collections;
using UnityEngine;

namespace EquationShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public sealed class AbilityPickup : MonoBehaviour
    {
        [SerializeField] private LevelGrid board;
        [SerializeField] private TopDownGridPlayer player;
        [SerializeField] private PlayerForm form = PlayerForm.Equal;
        [SerializeField, Min(0.02f)] private float collectionDuration = 0.8f;

        private SpriteRenderer spriteRenderer;
        private TopDownGridPlayer collector;
        private Vector3 collectionOffset;
        private Color initialColor;
        private float visualZ;
        private float elapsed;

        public PlayerForm Form => form;
        public bool IsCollected { get; private set; }
        public float CollectionDuration => collectionDuration;

        public void Configure(LevelGrid grid, PlayerForm abilityForm, TopDownGridPlayer gridPlayer = null)
        {
            if (abilityForm <= PlayerForm.Default || abilityForm > PlayerForm.Divis)
                throw new System.ArgumentOutOfRangeException(nameof(abilityForm));
            board = grid;
            player = gridPlayer;
            form = abilityForm;
        }

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        private bool ResolveReferences()
        {
            if (board == null) board = GetComponentInParent<LevelGrid>();
            if (board == null) return false;
            if (player == null) player = board.GetComponentInChildren<TopDownGridPlayer>();
            return true;
        }

        private void Update()
        {
            if (!IsCollected && ResolveReferences()) TryCollect(player);
        }

        public bool TryCollect(TopDownGridPlayer gridPlayer)
        {
            if (IsCollected || !isActiveAndEnabled || gridPlayer == null || !gridPlayer.isActiveAndEnabled ||
                gridPlayer.IsMoving || form <= PlayerForm.Default || form > PlayerForm.Divis ||
                !ResolveReferences() || gridPlayer.Board != board ||
                gridPlayer.Cell != board.CellAt(transform.position)) return false;

            IsCollected = true;
            collector = gridPlayer;
            initialColor = spriteRenderer.color;
            visualZ = transform.position.z;
            Vector2 offset = board.CenterOf(collector.Cell + Vector3Int.up) - board.CenterOf(collector.Cell);
            collectionOffset = new Vector3(offset.x, offset.y, 0f);
            var playerSprite = collector.GetComponent<SpriteRenderer>();
            if (playerSprite != null)
            {
                spriteRenderer.sortingLayerID = playerSprite.sortingLayerID;
                spriteRenderer.sortingOrder = playerSprite.sortingOrder + 1;
            }
            ApplyCollectionProgress(0f);
            return true;
        }

        private void LateUpdate()
        {
            if (!IsCollected) return;
            if (collector == null || !collector.gameObject.activeInHierarchy) { Destroy(gameObject); return; }
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.02f, collectionDuration));
            ApplyCollectionProgress(progress);
            if (progress >= 1f)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                // The player's coroutine survives removal of the item and resumes next frame.
                collector.StartCoroutine(TransformAfterRemoval(collector, form));
            }
        }

        private static IEnumerator TransformAfterRemoval(TopDownGridPlayer target, PlayerForm collectedForm)
        {
            yield return null;
            if (target == null) yield break;
            target.UnlockForm(collectedForm);
            target.CurrentForm = collectedForm;
        }

        private void ApplyCollectionProgress(float progress)
        {
            Vector3 position = collector.transform.position;
            position.z = visualZ;
            transform.position = position + collectionOffset * (1f - Mathf.SmoothStep(0f, 1f, progress));
            Color color = initialColor;
            color.a *= 1f - progress;
            spriteRenderer.color = color;
        }
    }
}
