#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using EquationShift;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class EquationGateTests
{
    private GameObject root;
    private LevelGrid board;
    private Transform blocksRoot;
    private EquationValidator validator;
    private TopDownGridPlayer player;
    private GateOpeningFeedback feedback;
    private Tile tile;
    private Vector3 cameraPosition;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Gate test grid", typeof(Grid));
        root.transform.position = new Vector3(4000f, 4000f, 0f);
        board = root.AddComponent<LevelGrid>();
        var ground = Child("Ground").AddComponent<Tilemap>();
        var walls = Child("Walls").AddComponent<Tilemap>();
        board.Configure(ground, walls);
        tile = ScriptableObject.CreateInstance<Tile>();
        for (int x = -4; x <= 15; x++)
            for (int y = -4; y <= 15; y++) ground.SetTile(new Vector3Int(x, y, 0), tile);
        blocksRoot = Child("Blocks").transform;
        validator = blocksRoot.gameObject.AddComponent<EquationValidator>();
        validator.Configure(board, blocksRoot);
        var actor = Child("Player");
        actor.SetActive(false);
        actor.transform.position = board.CenterOf(new Vector3Int(-2, -2, 0));
        actor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        actor.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
        player = actor.AddComponent<TopDownGridPlayer>();
        player.Configure(board);
        actor.SetActive(true);
        var camera = Child("Camera");
        camera.transform.localPosition = new Vector3(0, 0, -10);
        feedback = camera.AddComponent<GateOpeningFeedback>();
        feedback.Configure(player);
        cameraPosition = camera.transform.localPosition;
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        return child;
    }

    private GridPushBlock[] Equation(string text, Vector3Int origin, Vector3Int direction)
    {
        var result = new GridPushBlock[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            var actor = Child("Symbol " + i);
            actor.SetActive(false);
            actor.transform.SetParent(blocksRoot, false);
            actor.transform.position = board.CenterOf(origin + direction * i);
            actor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            actor.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
            var block = actor.AddComponent<GridPushBlock>();
            block.Configure(board);
            block.Value = text[i];
            actor.SetActive(true);
            result[i] = block;
        }
        return result;
    }

    private EquationGate Gate(GridPushBlock anchor, AncorSide side, string orientation = "Vertical", bool startsOpen = false)
    {
        anchor.IsAncor = true;
        anchor.AncorSide = side;
        var actor = Child("Gate " + side);
        actor.SetActive(false);
        actor.transform.position = board.CenterOf(new Vector3Int(10, 10, 0));
        actor.AddComponent<SpriteRenderer>();
        var animator = actor.AddComponent<Animator>();
        string path = "Assets/Animations/Gates/Gate_" + orientation;
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path + ".controller");
        Assert.IsNotNull(animator.runtimeAnimatorController);
        actor.AddComponent<BoxCollider2D>().size = orientation == "Vertical" ? new Vector2(1, 3) : new Vector2(3, 1);
        var gate = actor.AddComponent<EquationGate>();
        gate.Configure(validator, anchor, side, feedback,
            AssetDatabase.LoadAssetAtPath<AnimationClip>(path + "_Opening.anim"),
            AssetDatabase.LoadAssetAtPath<AnimationClip>(path + "_Closing.anim"));
        var serializedGate = new SerializedObject(gate);
        serializedGate.FindProperty("isOpen").boolValue = startsOpen;
        serializedGate.ApplyModifiedPropertiesWithoutUndo();
        actor.SetActive(true);
        return gate;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(tile);
    }

    [Test]
    public void AnchorCannotMoveEvenIfMovableWasAccidentallyEnabled()
    {
        var anchor = Equation("2", Vector3Int.zero, Vector3Int.right)[0];
        anchor.IsAncor = true;
        anchor.IsMovable = true;
        Assert.IsFalse(anchor.TryMove(Vector2Int.right));
    }

    [TestCase("1=2")]
    [TestCase("1+=1")]
    [TestCase("1/0=0")]
    public void WrongOrMalformedAnchorEquationHasNoFeedback(string expression)
    {
        var equation = Equation(expression, Vector3Int.zero, Vector3Int.right);
        var gate = Gate(equation.Last(), AncorSide.L);
        Equation("3=3", new Vector3Int(0, 3, 0), Vector3Int.right);
        Assert.IsTrue(validator.ValidateEquations(), "An unrelated correct equation is present");
        Assert.IsFalse(gate.IsOpening);
        Assert.IsFalse(gate.IsOpen);
        Assert.IsFalse(feedback.IsPlaying);
        Assert.IsFalse(player.IsMovementLocked);
        Assert.AreEqual(cameraPosition, feedback.transform.localPosition);
        Assert.IsTrue(gate.GetComponent<Collider2D>().enabled);
    }

    [Test]
    public void CorrectEquationRequiresTheConfiguredAnchorAndSide()
    {
        var equation = Equation("1+1=2", Vector3Int.zero, Vector3Int.right);
        var gate = Gate(equation.Last(), AncorSide.L);
        equation.Last().AncorSide = AncorSide.R;
        Assert.IsTrue(validator.ValidateEquations());
        Assert.IsFalse(gate.IsOpening);
        equation.Last().AncorSide = AncorSide.L;
        equation.Last().IsAncor = false;
        validator.ValidateEquations();
        Assert.IsFalse(gate.IsOpening);
    }

    [UnityTest]
    public IEnumerator CorrectAnchorShakesThenAnimatesAndOnlyThenRemovesCollision()
    {
        var equation = Equation("1+1=2", Vector3Int.zero, Vector3Int.right);
        var gate = Gate(equation.Last(), AncorSide.L);
        var shape = gate.GetComponent<Collider2D>();
        Physics2D.SyncTransforms();
        Assert.IsTrue(validator.ValidateEquations());
        Assert.IsTrue(validator.Results.Single().Contains(equation.Last()));
        Assert.IsTrue(gate.IsOpening);
        Assert.IsTrue(player.IsMovementLocked);
        Assert.IsFalse(player.TryMove(Vector2Int.right));
        Assert.IsTrue(shape.enabled);
        yield return new WaitForSecondsRealtime(0.08f);
        Assert.That(Vector3.Distance(cameraPosition, feedback.transform.localPosition), Is.GreaterThan(0.0001f));
        yield return new WaitForSecondsRealtime(0.22f);
        Assert.IsTrue(gate.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Opening"));
        Assert.IsTrue(shape.enabled, "The closed passage stays solid throughout the opening");
        yield return new WaitForSecondsRealtime(1.1f);
        Assert.IsTrue(gate.IsOpen);
        Assert.IsFalse(gate.IsOpening);
        Assert.IsFalse(shape.enabled);
        Assert.IsFalse(board.HasObstacle(board.CellAt(gate.transform.position), player.GetComponent<Collider2D>()));
        Assert.IsTrue(gate.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Open"));
        Assert.IsFalse(feedback.IsPlaying);
        Assert.IsFalse(player.IsMovementLocked);
        Assert.That(Vector3.Distance(cameraPosition, feedback.transform.localPosition), Is.LessThan(0.0001f));
        equation[0].Value = '9';
        validator.ValidateEquations();
        Assert.IsTrue(gate.IsOpen, "An opened passage remains open when blocks are reused");
        Assert.IsFalse(feedback.IsPlaying);
        Assert.IsTrue(player.TryMove(Vector2Int.right));
    }

    [UnityTest]
    public IEnumerator PlayerEqualityCompletesAnchorEquationAndOpensGate()
    {
        Equation("1", player.Cell + Vector3Int.left, Vector3Int.right);
        var anchor = Equation("1", player.Cell + Vector3Int.right, Vector3Int.right)[0];
        var gate = Gate(anchor, AncorSide.L);
        Assert.IsFalse(validator.ValidateEquations(), "Default form must leave the equation incomplete");
        player.UnlockForm(PlayerForm.Equal);
        player.CurrentForm = PlayerForm.Equal;
        yield return null;
        yield return null;
        Assert.IsTrue(validator.HasTrueEquation);
        Assert.AreEqual("1=1", validator.Results.Single().Expression);
        Assert.AreSame(player, validator.Results.Single().ParticipatingPlayer);
        Assert.IsTrue(validator.Results.Single().Contains(anchor));
        Assert.IsTrue(gate.IsOpening);
        Assert.IsTrue(player.IsMovementLocked);
        yield return new WaitForSecondsRealtime(1.4f);
        Assert.IsTrue(gate.IsOpen);
        Assert.IsFalse(gate.GetComponent<Collider2D>().enabled);
        Assert.IsFalse(player.IsMovementLocked);
    }

    [UnityTest]
    public IEnumerator SimultaneousSolutionsQueueBothOrientationsWithoutUnlockingEarly()
    {
        var first = Gate(Equation("2=2", Vector3Int.zero, Vector3Int.right).Last(), AncorSide.L);
        var second = Gate(Equation("0=0", new Vector3Int(0, 4, 0), Vector3Int.down).Last(), AncorSide.D, "Horizontal");
        validator.ValidateEquations();
        Assert.IsTrue(first.IsOpening && second.IsOpening);
        yield return new WaitForSecondsRealtime(1.35f);
        Assert.AreEqual(1, new[] { first, second }.Count(g => g.IsOpen));
        Assert.IsTrue(player.IsMovementLocked);
        yield return new WaitForSecondsRealtime(1.35f);
        Assert.IsTrue(first.IsOpen && second.IsOpen);
        Assert.IsFalse(player.IsMovementLocked);
    }

    [UnityTest]
    public IEnumerator AnAcceptedStepFinishesBeforeTheCameraEffectStarts()
    {
        var gate = Gate(Equation("2=2", Vector3Int.zero, Vector3Int.right).Last(), AncorSide.L);
        Physics2D.SyncTransforms();
        Assert.IsTrue(player.TryMove(Vector2Int.right));
        validator.ValidateEquations();
        Assert.IsTrue(player.IsMovementLocked);
        yield return new WaitForSecondsRealtime(0.05f);
        Assert.IsTrue(player.IsMoving);
        Assert.AreEqual(cameraPosition, feedback.transform.localPosition);
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.IsFalse(player.IsMoving);
        Assert.IsTrue(player.IsMovementLocked);
        Assert.IsTrue(gate.IsOpening);
        Assert.AreEqual(new Vector3Int(-1, -2, 0), player.Cell);
    }

    [UnityTest]
    public IEnumerator DisablingFeedbackRestoresCameraAndReleasesOnlyItsOwnMovementLock()
    {
        var gate = Gate(Equation("2=2", Vector3Int.zero, Vector3Int.right).Last(), AncorSide.L);
        var anotherOwner = new object();
        player.LockMovement(anotherOwner);
        validator.ValidateEquations();
        yield return new WaitForSecondsRealtime(0.3f);
        feedback.enabled = false;
        Assert.IsFalse(gate.IsOpening);
        Assert.IsFalse(gate.IsOpen);
        Assert.IsTrue(gate.GetComponent<Collider2D>().enabled);
        Assert.AreEqual(cameraPosition, feedback.transform.localPosition);
        Assert.IsTrue(player.IsMovementLocked);
        player.UnlockMovement(anotherOwner);
        Assert.IsFalse(player.IsMovementLocked);
    }

    [UnityTest]
    public IEnumerator GateConfiguredOpenStaysOpenAfterArrivalPlayerLeaves()
    {
        var gate = Gate(Equation("2", Vector3Int.zero, Vector3Int.right)[0], AncorSide.L, startsOpen: true);
        Assert.IsTrue(gate.DefaultIsOpen);
        gate.OpenForArrival(player);
        Assert.IsTrue(player.TeleportToCell(board.CellAt(gate.transform.position)));
        Assert.IsFalse(gate.IsWaitingForArrivalExit);
        Assert.IsTrue(player.TryMove(Vector2Int.right));
        yield return new WaitForSecondsRealtime(gate.ClosingDuration + 0.3f);
        Assert.IsTrue(gate.IsOpen);
        Assert.IsFalse(gate.IsClosing || feedback.IsPlaying || player.IsMovementLocked);
        Assert.IsFalse(gate.GetComponent<Collider2D>().enabled);
    }

    [UnityTest]
    public IEnumerator ManualStateChangesAnimateInBothDirectionsAndCanBeReversed()
    {
        var gate = Gate(Equation("2", Vector3Int.zero, Vector3Int.right)[0], AncorSide.L);
        var animator = gate.GetComponent<Animator>();
        var shape = gate.GetComponent<Collider2D>();
        gate.SetOpen(true);
        Assert.IsTrue(gate.IsOpen && gate.IsOpening);
        Assert.IsTrue(shape.enabled);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Opening"));
        yield return new WaitForSecondsRealtime(gate.OpeningDuration + 0.15f);
        Assert.IsFalse(shape.enabled);
        Assert.IsFalse(gate.IsOpening);
        gate.IsOpen = false;
        Assert.IsTrue(gate.IsClosing && shape.enabled);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Closing"));
        yield return new WaitForSecondsRealtime(0.2f);
        gate.SetOpen(true);
        yield return new WaitForSecondsRealtime(gate.OpeningDuration + 0.15f);
        Assert.IsFalse(shape.enabled);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Open"));
        gate.SetOpen(false);
        yield return new WaitForSecondsRealtime(gate.OpeningDuration + 0.15f);
        Assert.IsFalse(gate.IsOpen || gate.IsClosing);
        Assert.IsTrue(shape.enabled);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Closed"));
    }

    [UnityTest]
    public IEnumerator InspectorStateChangesAnimateAndOverrideQueuedEquationOpening()
    {
        var gate = Gate(Equation("2=2", Vector3Int.zero, Vector3Int.right).Last(), AncorSide.L);
        validator.ValidateEquations();
        Assert.IsTrue(gate.IsOpening);
        gate.SetOpen(false);
        yield return new WaitForSecondsRealtime(1.4f);
        Assert.IsFalse(gate.IsOpen);
        Assert.IsFalse(player.IsMovementLocked);
        var serialized = new SerializedObject(gate);
        serialized.FindProperty("isOpen").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        yield return null;
        Assert.IsTrue(gate.IsOpening);
        yield return new WaitForSecondsRealtime(gate.OpeningDuration + 0.15f);
        Assert.IsFalse(gate.GetComponent<Collider2D>().enabled);
    }

    [UnityTest]
    public IEnumerator ExampleSceneHasBothAnchorsAndAnimatedPassableGateCenters()
    {
        Object.DestroyImmediate(root);
        root = null;
        yield return SceneManager.LoadSceneAsync("00-FaseExemplo_1");
        yield return null;
        var sceneBoard = Object.FindFirstObjectByType<LevelGrid>();
        foreach (var entry in new[] { ("Gate_Vertical", "block_2_Ancor_L", AncorSide.L),
                                     ("Gate_Horizontal", "block_0_Ancor_D", AncorSide.D) })
        {
            var gate = GameObject.Find(entry.Item1).GetComponent<EquationGate>();
            Assert.IsNotNull(gate);
            Assert.AreEqual(entry.Item2, gate.Ancor.name);
            Assert.IsTrue(gate.Ancor.IsAncor);
            Assert.IsFalse(gate.Ancor.IsMovable);
            Assert.AreEqual(entry.Item3, gate.Ancor.AncorSide);
            Assert.AreEqual(entry.Item3, gate.PassageSide);
            Assert.IsNotNull(gate.OpeningAnimation);
            Assert.IsFalse(gate.OpeningAnimation.isLooping);
            Assert.IsTrue(gate.GetComponent<Animator>().enabled);
            Vector3Int cell = sceneBoard.CellAt(gate.transform.position);
            Assert.IsTrue(sceneBoard.IsFloor(cell), entry.Item1 + " must have floor and no wall in the passage center");
        }
        Assert.IsNotNull(Camera.main.GetComponent<GateOpeningFeedback>());
    }
}
#endif
