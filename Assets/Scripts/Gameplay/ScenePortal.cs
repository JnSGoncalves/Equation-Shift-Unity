using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EquationShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ScenePortal : MonoBehaviour
    {
        [SerializeField] private LevelGrid board;
        [SerializeField, Tooltip("Dois caracteres do inicio do nome da cena, por exemplo A0 ou 1A.")]
        private string destinationSceneId = "A0";
        [SerializeField, InspectorName("Spawn Direction"), Tooltip("Direcao para dentro da sala. A chegada fica sempre na celula vizinha.")]
        private Vector2Int spawnOffset = Vector2Int.down;
        [SerializeField, Tooltip("Portao da chegada; vazio procura um portao na celula vizinha.")]
        private EquationGate arrivalGate;
        [SerializeField, Min(0.05f), Tooltip("Duracao do deslizamento da camera entre salas, em segundos.")]
        private float transitionDuration = 0.5f;
        private bool attempted;

        public string DestinationSceneId => destinationSceneId;
        public float TransitionDuration => Mathf.Max(0.05f, transitionDuration);
        public Vector2Int SpawnDirection => CardinalDirection(spawnOffset);
        public Vector3Int ArrivalCell => Board.CellAt(transform.position) +
            new Vector3Int(SpawnDirection.x, SpawnDirection.y, 0);
        public EquationGate ArrivalGate
        {
            get
            {
                if (arrivalGate != null) return arrivalGate;
                if (Board == null) return null;
                foreach (EquationGate gate in Board.GetComponentsInChildren<EquationGate>())
                    if (gate.isActiveAndEnabled && Board.CellAt(gate.transform.position) == ArrivalCell) return gate;
                return null;
            }
        }
        public LevelGrid Board => board != null ? board : board = GetComponentInParent<LevelGrid>();

        public void Configure(LevelGrid grid, string destination, Vector2Int arrivalOffset,
            EquationGate gate = null)
        {
            board = grid;
            destinationSceneId = destination.Trim().ToUpperInvariant();
            spawnOffset = CardinalDirection(arrivalOffset);
            arrivalGate = gate;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private static Vector2Int CardinalDirection(Vector2Int direction) =>
            Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? new Vector2Int(Math.Sign(direction.x), 0) :
                new Vector2Int(0, direction.y == 0 ? -1 : Math.Sign(direction.y));

        internal bool TryReceivePlayer(TopDownGridPlayer player)
        {
            EquationGate gate = ArrivalGate;
            bool wasOpen = gate != null && gate.IsOpen;
            if (gate != null) gate.SetOpen(true, false);
            Physics2D.SyncTransforms();
            if (SceneTransition.IsSafeArrival(player, ArrivalCell) && player.TeleportToCell(ArrivalCell))
            {
                if (gate != null) gate.OpenForArrival(player);
                return true;
            }
            if (gate != null) gate.SetOpen(wasOpen, false);
            return false;
        }

        private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other) => CheckPlayer(other);
        private void OnTriggerStay2D(Collider2D other) => CheckPlayer(other);
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<TopDownGridPlayer>() != null) attempted = false;
        }

        private void CheckPlayer(Collider2D other)
        {
            var player = other.GetComponentInParent<TopDownGridPlayer>();
            if (attempted || player == null || player.Board != Board || player.IsMoving || player.IsMovementLocked ||
                Board.CellAt(transform.position) != player.Cell) return;
            attempted = true;
            SceneTransition.TryTravel(this, player);
        }

        public static bool TryGetSceneId(string sceneName, out string id)
        {
            string name = Path.GetFileNameWithoutExtension(sceneName);
            id = string.Empty;
            if (name == null || name.Length < 2 || (name.Length > 2 && name[2] != '-') ||
                !IsIdCharacter(name[0]) || !IsIdCharacter(name[1])) return false;
            id = name.Substring(0, 2).ToUpperInvariant();
            return true;
        }

        private static bool IsIdCharacter(char value) => value >= '0' && value <= '9' ||
            value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';

        public static bool TryResolveScene(string id, out string path)
        {
            path = null;
            if (id == null || id.Trim().Length != 2 || !TryGetSceneId(id.Trim(), out string normalized)) return false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string candidate = SceneUtility.GetScenePathByBuildIndex(i);
                if (!TryGetSceneId(candidate, out string candidateId) || candidateId != normalized) continue;
                // Ambiguous IDs must not silently load an arbitrary room.
                if (path != null) { path = null; return false; }
                path = candidate;
            }
            return path != null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            var shape = GetComponent<BoxCollider2D>();
            Gizmos.DrawWireCube(transform.TransformPoint(shape.offset),
                Vector3.Scale(shape.size, transform.lossyScale));
            if (Board == null || Board.Ground == null) return;
            Vector3 arrival = Board.CenterOf(ArrivalCell);
            arrival.z = transform.position.z;
            Gizmos.DrawLine(transform.position, arrival);
            Gizmos.DrawWireSphere(arrival, 0.2f);
        }
    }
}
