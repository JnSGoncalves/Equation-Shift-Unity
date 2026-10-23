using UnityEngine;
using UnityEngine.Tilemaps;

namespace EquationShift
{
    public sealed class LevelGrid : MonoBehaviour
    {
        [SerializeField] private Tilemap ground;
        [SerializeField] private Tilemap walls;

        public Tilemap Ground => ground;
        public Tilemap Walls => walls;

        public void Configure(Tilemap floor, Tilemap boundaries)
        {
            ground = floor;
            walls = boundaries;
        }

        public Vector3Int CellAt(Vector3 worldPosition)
        {
            // Tilemaps are drawn at different Z depths, but share the same XY board.
            worldPosition.z = ground.transform.position.z;
            return ground.WorldToCell(worldPosition);
        }

        public Vector2 CenterOf(Vector3Int cell) => ground.GetCellCenterWorld(cell);

        public bool IsFloor(Vector3Int cell)
        {
            if (!ground.HasTile(cell)) return false;
            Vector3 world = ground.GetCellCenterWorld(cell);
            world.z = walls.transform.position.z;
            return !walls.HasTile(walls.WorldToCell(world));
        }

        public GridPushBlock BlockAt(Vector3Int cell)
        {
            foreach (GridPushBlock block in GetComponentsInChildren<GridPushBlock>())
                if (block.Occupies(cell)) return block;
            return null;
        }

        public bool HasObstacle(Vector3Int cell, Collider2D self, Collider2D ignored = null)
        {
            Vector2 size = self.bounds.size;
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(CenterOf(cell), size * 0.98f, 0f))
            {
                if (hit == self || hit == ignored || hit.isTrigger) continue;
                return true;
            }
            return false;
        }

        public static bool IsCardinal(Vector2Int direction) =>
            Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1;
    }
}
