using System.Collections;
using System.Linq;
using EquationShift;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class EquationValidatorTests
{
    private GameObject root;
    private LevelGrid board;
    private EquationValidator validator;
    private Transform blocksRoot;
    private Tile tile;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Equation test grid", typeof(Grid));
        // Keep physics queries independent of whichever authored scene is open.
        root.transform.position = new Vector3(2000f, 2000f, 0f);
        board = root.AddComponent<LevelGrid>();
        var groundObject = new GameObject("Ground", typeof(Tilemap));
        groundObject.transform.SetParent(root.transform, false);
        var wallsObject = new GameObject("Walls", typeof(Tilemap));
        wallsObject.transform.SetParent(root.transform, false);
        board.Configure(groundObject.GetComponent<Tilemap>(), wallsObject.GetComponent<Tilemap>());
        tile = ScriptableObject.CreateInstance<Tile>();
        for (int x = -2; x <= 15; x++)
            for (int y = -12; y <= 12; y++) board.Ground.SetTile(new Vector3Int(x, y, 0), tile);
        blocksRoot = new GameObject("Blocks").transform;
        blocksRoot.SetParent(root.transform, false);
        validator = blocksRoot.gameObject.AddComponent<EquationValidator>();
        validator.Configure(board, blocksRoot);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(tile);
    }

    private GridPushBlock AddBlock(char value, Vector3Int cell, bool movable = true)
    {
        // Deliberately do not encode the value in the name or tag.
        var actor = new GameObject("A block");
        actor.SetActive(false);
        actor.transform.SetParent(blocksRoot);
        actor.transform.position = board.CenterOf(cell);
        actor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        actor.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
        var block = actor.AddComponent<GridPushBlock>();
        block.Configure(board);
        block.Value = value;
        block.IsMovable = movable;
        actor.SetActive(true);
        return block;
    }

    private void AddEquation(string expression, Vector3Int origin, Vector3Int direction)
    {
        for (int i = 0; i < expression.Length; i++) AddBlock(expression[i], origin + direction * i);
    }

    [TestCase("12+3=15", true)]
    [TestCase("123+45=168", true)]
    [TestCase("2+3*4=14", true)]
    [TestCase("2+3*4=20", false)]
    [TestCase("20/2*3=30", true)]
    [TestCase("10-3-2=5", true)]
    [TestCase("1/3*3=1", true)]
    [TestCase("1/3+1/6=1/2", true)]
    [TestCase("-12+3=-9", true)]
    [TestCase("3*-2=-6", true)]
    [TestCase("001+2=3", true)]
    [TestCase("99999999999999999999+1=100000000000000000000", true)]
    public void ArithmeticUsesPrecedenceAndExactFractions(string expression, bool expected)
    {
        Assert.IsTrue(EquationEvaluator.TryEvaluate(expression, out bool actual, out string error), error);
        Assert.AreEqual(expected, actual);
    }

    [TestCase("1/0=0")]
    [TestCase("1+=1")]
    [TestCase("=1")]
    [TestCase("1=")]
    [TestCase("1=1=1")]
    [TestCase("1+*2=3")]
    [TestCase("1++2=3")]
    [TestCase("1x2=2")]
    [TestCase("1 2=12")]
    [TestCase("12+3")]
    [TestCase("")]
    [TestCase(null)]
    public void InvalidArithmeticIsRejectedWithoutThrowing(string expression)
    {
        Assert.IsFalse(EquationEvaluator.TryEvaluate(expression, out bool isTrue, out string error));
        Assert.IsFalse(isTrue);
        Assert.IsNotEmpty(error);
    }

    [Test]
    public void HorizontalDigitsAreJoinedAndStaticBlocksParticipate()
    {
        AddEquation("123+45=168", Vector3Int.zero, Vector3Int.right);
        foreach (var block in blocksRoot.GetComponentsInChildren<GridPushBlock>()) block.IsMovable = false;
        Assert.IsTrue(validator.ValidateEquations());
        Assert.IsTrue(validator.AllEquationsTrue);
        Assert.AreEqual(1, validator.Results.Count);
        Assert.AreEqual("123+45=168", validator.Results[0].Expression);
        Assert.AreEqual(EquationDirection.Horizontal, validator.Results[0].Direction);
    }

    [Test]
    public void VerticalReadingStartsAtTheTop()
    {
        AddEquation("12-3=9", new Vector3Int(2, 8, 0), Vector3Int.down);
        Assert.IsTrue(validator.ValidateEquations());
        Assert.AreEqual("12-3=9", validator.Results.Single().Expression);
        Assert.AreEqual(EquationDirection.Vertical, validator.Results.Single().Direction);
        Assert.AreEqual(new Vector3Int(2, 8, 0), validator.Results.Single().StartCell);
    }

    [Test]
    public void GapsSplitEquationsInsteadOfJoiningDistantBlocks()
    {
        AddEquation("1+", Vector3Int.zero, Vector3Int.right);
        AddEquation("2=3", new Vector3Int(3, 0, 0), Vector3Int.right);
        Assert.IsFalse(validator.ValidateEquations());
        Assert.AreEqual("2=3", validator.Results.Single().Expression);
    }

    [Test]
    public void WholeRunMustBeValidRatherThanJustASubstring()
    {
        AddEquation("1+2=3+", Vector3Int.zero, Vector3Int.right);
        Assert.IsFalse(validator.ValidateEquations());
        Assert.IsFalse(validator.Results.Single().IsWellFormed);
    }

    [Test]
    public void CrossingAxesAndSeveralEquationsAreReportedSeparately()
    {
        AddEquation("1+2=3", Vector3Int.zero, Vector3Int.right);
        AddBlock('3', new Vector3Int(3, 1, 0));
        AddBlock('3', new Vector3Int(3, -1, 0));
        AddEquation("5-1=0", new Vector3Int(0, -4, 0), Vector3Int.right);
        Assert.IsTrue(validator.ValidateEquations());
        Assert.AreEqual(3, validator.Results.Count);
        Assert.AreEqual(2, validator.Results.Count(r => r.IsTrue));
        Assert.IsFalse(validator.AllEquationsTrue);
    }

    [Test]
    public void OverlappingBlocksCannotProduceAnArbitrarySolution()
    {
        AddEquation("1=1", Vector3Int.zero, Vector3Int.right);
        AddBlock('9', Vector3Int.zero);
        Assert.IsFalse(validator.ValidateEquations());
        Assert.IsNotEmpty(validator.ValidationError);
        Assert.IsEmpty(validator.Results);
    }

    [Test]
    public void StaticBlockCannotBeMoved()
    {
        var block = AddBlock('2', Vector3Int.zero, false);
        Assert.IsFalse(block.TryMove(Vector2Int.right));
        Assert.IsFalse(block.IsMoving);
    }

    [UnityTest]
    public IEnumerator MovingBlocksInvalidateUntilTheyStopAndThenAutomaticallyRevalidate()
    {
        AddEquation("1=", Vector3Int.zero, Vector3Int.right);
        var last = AddBlock('1', new Vector3Int(2, 1, 0));
        Physics2D.SyncTransforms();
        Assert.IsFalse(validator.ValidateEquations());
        Assert.IsTrue(last.TryMove(Vector2Int.down));
        Assert.IsFalse(validator.ValidateEquations());
        Assert.IsTrue(validator.IsWaitingForBlocks);
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(validator.HasTrueEquation);
        Assert.IsFalse(validator.IsWaitingForBlocks);
        last.Value = '2';
        yield return null;
        yield return null;
        Assert.IsFalse(validator.HasTrueEquation);
        Object.Destroy(last.gameObject);
        yield return null;
        yield return null;
        Assert.AreEqual(2, validator.BlockCount);
        Assert.IsFalse(validator.HasTrueEquation);
    }

    [UnityTest]
    public IEnumerator ExampleSceneUsesStoredSymbolsAndTheBlocksParent()
    {
        Object.DestroyImmediate(root);
        root = null;
        yield return SceneManager.LoadSceneAsync("00-FaseExemplo");
        yield return null;
        var sceneValidator = Object.FindFirstObjectByType<EquationValidator>();
        Assert.IsNotNull(sceneValidator);
        Assert.AreEqual("Blocks", sceneValidator.gameObject.name);
        var sceneBlocks = sceneValidator.GetComponentsInChildren<GridPushBlock>();
        sceneValidator.ValidateEquations();
        Assert.AreEqual(sceneBlocks.Length, sceneValidator.BlockCount);
        Assert.IsTrue(sceneBlocks.Any(b => b.Value == '='));
        Assert.IsTrue(sceneBlocks.Any(b => b.Value == '+'));
        foreach (bool movable in new[] { false, true })
        {
            string groupName = movable ? "MovableBlocks" : "StaticBlocks";
            var group = sceneValidator.transform.Find(groupName);
            Assert.IsNotNull(group, groupName + " belongs under the shared Blocks parent");
            var groupBlocks = group.GetComponentsInChildren<GridPushBlock>();
            Assert.AreEqual(15, groupBlocks.Length, groupName + " contains every supported symbol");
            CollectionAssert.AreEquivalent("0123456789+-*/=".ToCharArray(), groupBlocks.Select(b => b.Value));
            Assert.IsTrue(groupBlocks.All(b => b.IsMovable == movable));
        }
    }
}
