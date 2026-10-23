#if UNITY_EDITOR
using System.Collections;
using EquationShift;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class BlockPrefabTests
{
    private GameObject root;
    private LevelGrid board;
    private Transform blocksRoot;
    private Transform staticRoot;
    private Transform movableRoot;
    private Tile tile;

    public static IEnumerable PrefabVariants
    {
        get
        {
            foreach (bool movable in new[] { false, true })
            {
                string kind = movable ? "Movable" : "Static";
                for (int digit = 0; digit <= 9; digit++)
                    yield return new TestCaseData(movable, "Digit_" + digit, (char)('0' + digit))
                        .SetName("BlockPrefab_" + kind + "_Digit_" + digit);
                foreach (var operation in new[]
                {
                    ("Plus", '+'), ("Minus", '-'), ("Multiply", '*'), ("Divide", '/'), ("Equals", '=')
                })
                    yield return new TestCaseData(movable, "Operator_" + operation.Item1, operation.Item2)
                        .SetName("BlockPrefab_" + kind + "_Operator_" + operation.Item1);
            }
        }
    }

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Prefab test grid", typeof(Grid));
        root.transform.position = new Vector3(3000f, 3000f, 0f);
        board = root.AddComponent<LevelGrid>();
        var ground = NewChild("Ground", root.transform).gameObject.AddComponent<Tilemap>();
        var walls = NewChild("Walls", root.transform).gameObject.AddComponent<Tilemap>();
        board.Configure(ground, walls);
        tile = ScriptableObject.CreateInstance<Tile>();
        for (int x = -3; x <= 8; x++)
            for (int y = -3; y <= 3; y++) ground.SetTile(new Vector3Int(x, y, 0), tile);
        blocksRoot = NewChild("Blocks", root.transform);
        staticRoot = NewChild("StaticBlocks", blocksRoot);
        movableRoot = NewChild("MovableBlocks", blocksRoot);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(tile);
    }

    private static Transform NewChild(string name, Transform parent)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static GameObject LoadPrefab(bool movable, string symbol)
    {
        string kind = movable ? "Movable" : "Static";
        string path = "Assets/Prefabs/Blocks/" + kind + "/Block_" + kind + "_" + symbol + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.IsNotNull(prefab, path + " must exist");
        return prefab;
    }

    private GridPushBlock Spawn(bool movable, string symbol, Vector3Int cell)
    {
        // The asset has no reference to this scene. Its parent must resolve the board during Awake.
        var instance = Object.Instantiate(LoadPrefab(movable, symbol), board.CenterOf(cell),
            Quaternion.identity, movable ? movableRoot : staticRoot);
        var block = instance.GetComponent<GridPushBlock>();
        Assert.IsNotNull(block);
        return block;
    }

    [TestCaseSource(nameof(PrefabVariants))]
    public void PrefabHasItsSymbolAndMobilityAndResolvesTheParentGrid(bool movable, string symbol, char value)
    {
        var prefab = LoadPrefab(movable, symbol);
        var blockAsset = prefab.GetComponent<GridPushBlock>();
        Assert.IsNotNull(blockAsset);
        Assert.AreEqual(value, blockAsset.Value);
        Assert.AreEqual(movable, blockAsset.IsMovable);
        Assert.AreEqual(movable ? "MovableBlock" : "StaticBlock", prefab.tag);
        Assert.IsNull(new SerializedObject(blockAsset).FindProperty("board").objectReferenceValue,
            "A reusable prefab must not hold a scene board reference");

        var renderer = prefab.GetComponent<SpriteRenderer>();
        Assert.IsNotNull(renderer);
        Assert.IsNotNull(renderer.sprite, "Every variant needs its authored sprite");
        var body = prefab.GetComponent<Rigidbody2D>();
        Assert.IsNotNull(body);
        Assert.AreEqual(movable ? RigidbodyType2D.Kinematic : RigidbodyType2D.Static, body.bodyType);
        Assert.AreEqual(0f, body.gravityScale);
        Assert.IsTrue((body.constraints & RigidbodyConstraints2D.FreezeRotation) != 0);
        var collider = prefab.GetComponent<BoxCollider2D>();
        Assert.IsNotNull(collider);
        Assert.IsFalse(collider.isTrigger);
        Assert.AreEqual(Vector2.zero, collider.offset);
        Assert.AreEqual(Vector2.one * 0.9f, collider.size);

        var cell = new Vector3Int(2, 1, 0);
        var block = Spawn(movable, symbol, cell);
        Assert.AreEqual(cell, block.Cell);
        Assert.AreEqual(value, block.Value);
        Assert.AreSame(block, board.BlockAt(cell));
        Physics2D.SyncTransforms();
        Assert.AreEqual(movable, block.TryMove(Vector2Int.right));
    }

    [UnityTest]
    public IEnumerator PlayerPushesMovablePrefabButCannotPushStaticPrefab()
    {
        var movable = Spawn(true, "Digit_2", Vector3Int.zero);
        var fixedBlock = Spawn(false, "Digit_3", new Vector3Int(1, -1, 0));
        var playerObject = NewChild("Player", blocksRoot).gameObject;
        playerObject.SetActive(false);
        playerObject.transform.position = board.CenterOf(new Vector3Int(0, 1, 0));
        playerObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        playerObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
        var player = playerObject.AddComponent<TopDownGridPlayer>();
        playerObject.SetActive(true);
        Physics2D.SyncTransforms();

        Assert.IsTrue(player.TryMove(Vector2Int.down));
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(Vector3Int.zero, player.Cell);
        Assert.AreEqual(new Vector3Int(0, -1, 0), movable.Cell);
        Assert.That(Vector2.Distance(movable.GetComponent<Rigidbody2D>().position,
            board.CenterOf(movable.Cell)), Is.LessThan(0.001f));

        Assert.IsTrue(player.TryMove(Vector2Int.right));
        yield return new WaitForSeconds(0.3f);
        Assert.IsFalse(player.TryMove(Vector2Int.down), "The fixed prefab occupies the next cell");
        Assert.IsFalse(fixedBlock.TryMove(Vector2Int.down));
        Assert.AreEqual(new Vector3Int(1, -1, 0), fixedBlock.Cell);
        Assert.IsFalse(player.IsMoving);
    }

    [Test]
    public void ValidatorReadsAnEquationAcrossStaticAndMovablePrefabGroups()
    {
        Spawn(false, "Digit_1", Vector3Int.zero);
        Spawn(true, "Operator_Plus", Vector3Int.right);
        Spawn(true, "Digit_2", Vector3Int.right * 2);
        Spawn(false, "Operator_Equals", Vector3Int.right * 3);
        Spawn(false, "Digit_3", Vector3Int.right * 4);
        var validator = blocksRoot.gameObject.AddComponent<EquationValidator>();

        Assert.IsTrue(validator.ValidateEquations());
        Assert.AreEqual(5, validator.BlockCount);
        Assert.AreEqual(1, validator.Results.Count);
        Assert.AreEqual("1+2=3", validator.Results[0].Expression);
        Assert.IsTrue(validator.AllEquationsTrue);
    }
}
#endif
