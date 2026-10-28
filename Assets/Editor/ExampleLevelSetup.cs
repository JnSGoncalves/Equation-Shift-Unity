using EquationShift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ExampleLevelSetup
{
    [MenuItem("Equation Shift/Configurar Fase Exemplo")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/00-FaseExemplo_1.unity");
        var ground = GameObject.Find("Ground").GetComponent<Tilemap>();
        var walls = GameObject.Find("Walls").GetComponent<Tilemap>();
        var gridObject = GameObject.Find("Grid");
        var board = gridObject.GetComponent<LevelGrid>();
        if (board == null) board = gridObject.AddComponent<LevelGrid>();
        board.Configure(ground, walls);

        // These old boxes cover the interior, even on the empty decorative tilemap.
        foreach (var name in new[] { "Walls", "Structs" })
        {
            var oldBox = GameObject.Find(name).GetComponent<BoxCollider2D>();
            if (oldBox != null) Object.DestroyImmediate(oldBox);
        }
        foreach (var cell in walls.cellBounds.allPositionsWithin)
            if (walls.HasTile(cell)) walls.SetColliderType(cell, Tile.ColliderType.Grid);
        if (walls.GetComponent<TilemapCollider2D>() == null) walls.gameObject.AddComponent<TilemapCollider2D>();

        var player = GameObject.Find("Player");
        var blocks = Object.FindObjectsByType<GridPushBlock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var blocksRoot = GameObject.Find("Blocks");
        if (blocksRoot == null) blocksRoot = new GameObject("Blocks");
        // Keep all board actors under the shared grid without changing world positions.
        player.transform.SetParent(gridObject.transform, true);
        blocksRoot.transform.SetParent(gridObject.transform, true);
        ConfigureBody(player);
        var controller = player.GetComponent<TopDownGridPlayer>();
        if (controller == null) controller = player.AddComponent<TopDownGridPlayer>();
        controller.Configure(board);
        foreach (var block in blocks)
        {
            if (!block.transform.IsChildOf(blocksRoot.transform)) block.transform.SetParent(blocksRoot.transform, true);
            ConfigureBody(block.gameObject);
            block.Configure(board);
        }
        var validator = blocksRoot.GetComponent<EquationValidator>();
        if (validator == null) validator = blocksRoot.AddComponent<EquationValidator>();
        validator.Configure(board, blocksRoot.transform, controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Equation Shift: example level configured and saved.");
    }

    private static void ConfigureBody(GameObject actor)
    {
        var body = actor.GetComponent<Rigidbody2D>();
        if (body == null) body = actor.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var shape = actor.GetComponent<BoxCollider2D>();
        if (shape == null) shape = actor.AddComponent<BoxCollider2D>();
        shape.size = new Vector2(0.9f, 0.9f);
        shape.offset = Vector2.zero;
    }
}
