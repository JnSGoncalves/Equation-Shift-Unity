using System.Collections;
using EquationShift;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class GridMovementTests
{
    private GameObject root;
    private LevelGrid board;
    private TopDownGridPlayer player;
    private GridPushBlock block;
    private Tile tile;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Test grid", typeof(Grid));
        // Scene integration tests can leave the example level loaded.
        root.transform.position = new Vector3(1000f, 1000f, 0f);
        board = root.AddComponent<LevelGrid>();
        Tilemap ground = CreateMap("Ground", 2f);
        Tilemap walls = CreateMap("Walls", 0f);
        board.Configure(ground, walls);
        tile = ScriptableObject.CreateInstance<Tile>();
        tile.colliderType = Tile.ColliderType.Grid;
        for (int x = -3; x <= 3; x++)
            for (int y = -3; y <= 3; y++) ground.SetTile(new Vector3Int(x, y, 0), tile);
        walls.gameObject.AddComponent<TilemapCollider2D>();
        player = CreateActor<TopDownGridPlayer>(new Vector3Int(0, 1, 0));
        block = CreateActor<GridPushBlock>(Vector3Int.zero);
        Physics2D.SyncTransforms();
    }

    private Tilemap CreateMap(string name, float z)
    {
        var map = new GameObject(name, typeof(Tilemap));
        map.transform.SetParent(root.transform);
        map.transform.localPosition = new Vector3(0, 0, z);
        return map.GetComponent<Tilemap>();
    }

    private T CreateActor<T>(Vector3Int cell) where T : Component
    {
        var actor = new GameObject(typeof(T).Name);
        actor.SetActive(false);
        actor.transform.SetParent(root.transform);
        actor.transform.position = board.CenterOf(cell);
        actor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        actor.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
        T component = actor.AddComponent<T>();
        actor.SetActive(true);
        return component;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(tile);
    }

    [UnityTest]
    public IEnumerator PushMovesExactlyOneCellAndKeepsBothActorsAligned()
    {
        Assert.IsTrue(player.TryMove(Vector2Int.down));
        Assert.IsFalse(player.TryMove(Vector2Int.right), "Cannot start another step mid-move");
        Assert.IsFalse(block.TryMove(Vector2Int.left));
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(Vector3Int.zero, player.Cell);
        Assert.AreEqual(new Vector3Int(0, -1, 0), block.Cell);
        Assert.That(Vector2.Distance(player.GetComponent<Rigidbody2D>().position, board.CenterOf(player.Cell)), Is.LessThan(0.001f));
        Assert.That(Vector2.Distance(block.GetComponent<Rigidbody2D>().position, board.CenterOf(block.Cell)), Is.LessThan(0.001f));
    }

    [Test]
    public void WallBehindBlockRejectsPushAndPlayerStep()
    {
        board.Walls.SetTile(new Vector3Int(0, -1, 0), tile);
        Assert.IsFalse(player.TryMove(Vector2Int.down));
        Assert.IsFalse(block.IsMoving);
        Assert.IsFalse(player.IsMoving);
    }

    [UnityTest]
    public IEnumerator ResetRestoresBlocksAndPlayerAndCancelsAnUnfinishedPush()
    {
        player.UnlockForm(PlayerForm.Equal);
        player.UnlockForm(PlayerForm.Mult);
        player.CurrentForm = PlayerForm.Mult;
        Assert.IsTrue(player.TryMove(Vector2Int.down));
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(Vector3Int.down, block.Cell);
        Assert.IsTrue(player.TryMove(Vector2Int.down));
        block.Value = '9';

        Assert.IsTrue(player.TryResetLevel());
        Assert.AreEqual(Vector3Int.zero, block.Cell);
        Assert.AreEqual('0', block.Value);
        Assert.AreEqual(Vector3Int.up, player.Cell);
        Assert.IsFalse(player.IsMoving);
        Assert.IsFalse(block.IsMoving);
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Equal));
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Mult));
        Assert.AreEqual(PlayerForm.Mult, player.CurrentForm);

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(Vector3Int.zero, block.Cell, "An old physics step must not resume after reset.");
        Assert.AreEqual(Vector3Int.up, player.Cell);
        Assert.That(Vector2.Distance(block.GetComponent<Rigidbody2D>().position,
            board.CenterOf(Vector3Int.zero)), Is.LessThan(0.001f));
        Assert.IsTrue(player.TryMove(Vector2Int.down), "The restored block can be pushed again.");
    }

    [UnityTest]
    public IEnumerator ResetIsIgnoredWhileMovementIsLocked()
    {
        Assert.IsTrue(player.TryMove(Vector2Int.down));
        yield return new WaitForSeconds(0.3f);
        var owner = new object();
        player.LockMovement(owner);
        Assert.IsFalse(player.TryResetLevel());
        Assert.AreEqual(Vector3Int.down, block.Cell);
        Assert.AreEqual(Vector3Int.zero, player.Cell);
        player.UnlockMovement(owner);
        Assert.IsTrue(player.TryResetLevel());
        Assert.AreEqual(Vector3Int.zero, block.Cell);
    }

    [Test]
    public void MissingFloorRejectsPush()
    {
        board.Ground.SetTile(new Vector3Int(0, -1, 0), null);
        Assert.IsFalse(player.TryMove(Vector2Int.down));
    }

    [Test]
    public void SecondBlockRejectsChainPush()
    {
        CreateActor<GridPushBlock>(new Vector3Int(0, -1, 0));
        Physics2D.SyncTransforms();
        Assert.IsFalse(player.TryMove(Vector2Int.down));
    }

    [Test]
    public void WallAndMissingFloorRejectPlayerMovement()
    {
        board.Walls.SetTile(new Vector3Int(1, 1, 0), tile);
        board.Ground.SetTile(new Vector3Int(-1, 1, 0), null);
        Assert.IsFalse(player.TryMove(Vector2Int.right));
        Assert.IsFalse(player.TryMove(Vector2Int.left));
    }

    [Test]
    public void DiagonalAndZeroCommandsAreRejected()
    {
        Assert.IsFalse(player.TryMove(Vector2Int.one));
        Assert.IsFalse(block.TryMove(Vector2Int.zero));
    }

    [UnityTest]
    public IEnumerator BlockCannotMoveIntoPlayer()
    {
        Assert.IsFalse(block.TryMove(Vector2Int.up));
        Assert.IsTrue(player.TryMove(Vector2Int.right));
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(new Vector3Int(1, 1, 0), player.Cell);
    }

    [UnityTest]
    public IEnumerator ExampleSceneHasConfiguredActorsAndTileCollisions()
    {
        Object.DestroyImmediate(root);
        root = null;
        yield return SceneManager.LoadSceneAsync("00-FaseExemplo_1");
        yield return null;
        var sceneBoard = Object.FindFirstObjectByType<LevelGrid>();
        var scenePlayer = Object.FindFirstObjectByType<TopDownGridPlayer>();
        Assert.IsNotNull(sceneBoard);
        Assert.IsNotNull(scenePlayer);
        var sceneBlocks = sceneBoard.GetComponentsInChildren<GridPushBlock>();
        Assert.IsNotEmpty(sceneBlocks);
        var wallCollider = sceneBoard.Walls.GetComponent<TilemapCollider2D>();
        Assert.IsNotNull(wallCollider);
        Assert.IsNull(sceneBoard.Walls.GetComponent<BoxCollider2D>());
        wallCollider.ProcessTilemapChanges();
        foreach (var wallCell in sceneBoard.Walls.cellBounds.allPositionsWithin)
            if (sceneBoard.Walls.HasTile(wallCell))
                Assert.IsTrue(wallCollider.OverlapPoint(sceneBoard.Walls.GetCellCenterWorld(wallCell)), "Painted wall cell has solid collision: " + wallCell);
        Assert.IsTrue(sceneBoard.IsFloor(scenePlayer.Cell));
        foreach (var sceneBlock in sceneBlocks)
        {
            Assert.IsTrue(sceneBoard.IsFloor(sceneBlock.Cell), sceneBlock.name + " needs a floor cell");
            Assert.AreSame(sceneBlock, sceneBoard.BlockAt(sceneBlock.Cell), sceneBlock.name + " is registered on the board");
            Assert.That(Vector2.Distance(sceneBlock.GetComponent<Rigidbody2D>().position,
                sceneBoard.CenterOf(sceneBlock.Cell)), Is.LessThan(0.001f), sceneBlock.name + " is centered");
            Assert.IsNotNull(sceneBlock.Collider);
            Assert.IsFalse(sceneBlock.Collider.isTrigger);
        }
    }
}
