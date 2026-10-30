using UnityEngine;

namespace EquationShift
{
    public static class SceneTransition
    {
        internal static RoomTransitionController Controller { get; private set; }
        public static bool IsTravelling => Controller != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Controller = null;

        public static bool TryTravel(ScenePortal portal, TopDownGridPlayer player)
        {
            if (IsTravelling || portal == null || player == null || !portal.isActiveAndEnabled ||
                player.IsMoving || player.IsMovementLocked || player.Board != portal.Board ||
                portal.gameObject.scene != player.gameObject.scene) return false;
            if (!ScenePortal.TryResolveScene(portal.DestinationSceneId, out string destinationPath))
            {
                RoomUnavailableNotice.Show();
                Debug.LogWarning("Passagem sem destino unico nas cenas habilitadas do Build: " +
                    portal.DestinationSceneId, portal);
                return false;
            }
            if (!ScenePortal.TryGetSceneId(portal.gameObject.scene.name, out string sourceId)) return false;
            if (portal.gameObject.scene.path == destinationPath) return false;
            RoomUnavailableNotice.Hide();
            Controller = new GameObject("Room Transition").AddComponent<RoomTransitionController>();
            if (Controller.Begin(portal, player, sourceId, destinationPath)) return true;
            Object.Destroy(Controller.gameObject);
            Controller = null;
            return false;
        }

        internal static void Release(RoomTransitionController controller)
        {
            if (Controller == controller) Controller = null;
        }

        public static bool TryFindSafeCenter(TopDownGridPlayer player, out Vector3Int cell)
        {
            cell = default;
            if (player == null || player.Board == null || player.Board.Ground == null) return false;
            BoundsInt bounds = player.Board.Ground.cellBounds;
            Vector3 center = bounds.center - Vector3.one * 0.5f;
            float bestDistance = float.PositiveInfinity;
            bool found = false;
            Physics2D.SyncTransforms();
            foreach (Vector3Int candidate in bounds.allPositionsWithin)
            {
                float distance = ((Vector3)candidate - center).sqrMagnitude;
                if (distance >= bestDistance || !IsSafeArrival(player, candidate)) continue;
                cell = candidate;
                bestDistance = distance;
                found = true;
            }
            return found;
        }

        internal static bool IsSafeArrival(TopDownGridPlayer player, Vector3Int cell)
        {
            if (!player.CanStandAt(cell)) return false;
            Vector2 size = player.GetComponent<Collider2D>().bounds.size;
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(player.Board.CenterOf(cell), size, 0f))
                if (hit.GetComponent<ScenePortal>() != null) return false;
            return true;
        }
    }
}
