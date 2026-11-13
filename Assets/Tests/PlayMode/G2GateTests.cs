#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using EquationShift;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class G2GateTests
{
    [UnityTest]
    public IEnumerator ArrivingFromTheVillageDoesNotClaimTheRightGate()
    {
        yield return SceneManager.LoadSceneAsync("G2-Fase_2");
        yield return null;
        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var leftExit = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None)
            .Single(portal => portal.DestinationSceneId == "G1");
        Assert.IsNull(leftExit.ArrivalGate);
        Assert.IsTrue(player.TeleportToCell(leftExit.ArrivalCell));
        Assert.IsTrue(player.TryMove(Vector2Int.left));
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "G1-Fase_1" && !SceneTransition.IsTravelling,
            "The left corridor did not return to the village.");

        Assert.IsTrue(Object.FindFirstObjectByType<TopDownGridPlayer>().TryMove(Vector2Int.right));
        yield return WaitFor(() => SceneManager.GetActiveScene().name == "G2-Fase_2" && !SceneTransition.IsTravelling,
            "The village corridor did not lead back to the ruins.");
        var arrival = Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None)
            .Single(portal => portal.DestinationSceneId == "G1");
        Assert.AreEqual(arrival.ArrivalCell, Object.FindFirstObjectByType<TopDownGridPlayer>().Cell);
        var rightGate = GameObject.Find("Gate_Vertical_R").GetComponent<EquationGate>();
        Assert.IsFalse(rightGate.IsWaitingForArrivalExit);
    }

    [UnityTest]
    public IEnumerator CorrectEquationOpensRightGateWithCameraAndPlayerFeedback()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/G2-Fase_2.unity",
            new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var board = Object.FindFirstObjectByType<LevelGrid>();
        var player = board.GetComponentInChildren<TopDownGridPlayer>();
        var validator = board.GetComponentInChildren<EquationValidator>();
        var gate = GameObject.Find("Gate_Vertical_R").GetComponent<EquationGate>();
        var camera = Camera.main;
        Vector3 cameraPosition = camera.transform.localPosition;
        var sequence = Object.FindFirstObjectByType<RuinsEntranceCutscene>();
        var error = sequence.GetComponentsInChildren<Transform>(true).Single(t => t.name == "NPC_Error");
        camera.aspect = 4f / 3f;
        Assert.AreEqual(AncorSide.R, gate.PassageSide);
        Assert.AreEqual(AncorSide.R, gate.Ancor.AncorSide);
        Assert.IsTrue(gate.Ancor.IsAncor);
        Assert.IsTrue(error.gameObject.activeInHierarchy);
        Assert.AreEqual(new Vector2(10.5f, -0.5f), (Vector2)error.position);
        Assert.IsTrue(player.TryMove(Vector2Int.right));
        yield return WaitFor(() => (camera.transform.localPosition - cameraPosition).sqrMagnitude > 0.000001f,
            "The entrance shake did not start.");
        Assert.AreEqual(board.CellAt(new Vector3(-12.5f, -1.5f)), player.Cell);
        Assert.IsTrue(player.IsMovementLocked);
        Assert.IsTrue(error.gameObject.activeInHierarchy);
        yield return WaitFor(() => VillageDialogue.IsVisible, "Error did not speak after the shake.");
        Assert.IsTrue(error.gameObject.activeSelf);
        Assert.AreEqual(new Vector2(10.5f, -0.5f), (Vector2)error.position);
        Assert.That(Object.FindFirstObjectByType<VillageDialogue>().GetComponentInChildren<Text>().text,
            Does.Contain("Tremor engraçado"));
        VillageDialogue.Hide();
        yield return WaitFor(() => error.position.y < -0.6f, "Error did not walk down.");
        Assert.That(error.position.x, Is.EqualTo(10.5f).Within(0.001f));
        yield return WaitFor(() => !error.gameObject.activeSelf, "Error did not leave through the right gate.");
        Assert.That(error.position.x, Is.GreaterThan(gate.transform.position.x));
        Assert.That(error.position.y, Is.EqualTo(-1.5f).Within(0.001f));
        yield return WaitFor(() => !player.IsMovementLocked, "Control did not return after closing the gate.");
        Assert.That(Vector3.Distance(cameraPosition, camera.transform.localPosition), Is.LessThan(0.0001f));
        Assert.IsFalse(gate.IsOpen);
        Assert.IsTrue(gate.GetComponent<Collider2D>().enabled);

        var blocks = board.GetComponentsInChildren<GridPushBlock>();
        var two = blocks.Single(block => block.Value == '2');
        var four = blocks.Single(block => block.Value == '4');
        Vector3Int target = gate.Ancor.Cell + Vector3Int.left * 2;
        Vector3Int twoTarget = gate.Ancor.Cell + Vector3Int.left * 4;
        if (two.IsMovable && two.Cell == twoTarget + Vector3Int.left)
        {
            Physics2D.SyncTransforms();
            Assert.IsTrue(player.TeleportToCell(two.Cell + Vector3Int.left));
            Physics2D.SyncTransforms();
            Assert.IsTrue(player.TryMove(Vector2Int.right));
            yield return new WaitUntil(() => !two.IsMoving && !player.IsMoving);
        }
        Assert.AreEqual(twoTarget, two.Cell);
        Assert.IsTrue(four.IsMovable);
        Assert.AreEqual(target.x, four.Cell.x);
        int steps = Mathf.Abs(target.y - four.Cell.y);
        Assert.Greater(steps, 0);
        Vector2Int push = target.y > four.Cell.y ? Vector2Int.up : Vector2Int.down;
        Physics2D.SyncTransforms();
        Assert.IsTrue(player.TeleportToCell(four.Cell - new Vector3Int(push.x, push.y, 0)));
        Physics2D.SyncTransforms();
        for (int step = 0; step < steps; step++)
        {
            Assert.IsTrue(player.TryMove(push));
            yield return new WaitUntil(() => !four.IsMoving && !player.IsMoving);
        }
        Assert.IsTrue(validator.ValidateEquations());
        Assert.IsTrue(validator.Results.Any(result => result.IsTrue && result.Expression == "2+4=6" && result.Contains(gate.Ancor)));
        Assert.IsTrue(gate.IsOpening);
        Assert.IsTrue(player.IsMovementLocked);
        Assert.IsTrue(camera.GetComponent<GateOpeningFeedback>().IsPlaying);
        yield return new WaitForSecondsRealtime(gate.OpeningDuration + 0.3f);
        Assert.IsTrue(gate.IsOpen);
        Assert.IsFalse(gate.IsOpening);
        Assert.IsFalse(gate.GetComponent<Collider2D>().enabled);
        Assert.IsFalse(player.IsMovementLocked);
        Assert.IsTrue(gate.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Open"));
        Assert.That(Vector3.Distance(cameraPosition, camera.transform.localPosition), Is.LessThan(0.0001f));
    }

    private static IEnumerator WaitFor(System.Func<bool> condition, string message)
    {
        yield return new WaitUntil(condition, System.TimeSpan.FromSeconds(6), () => Assert.Fail(message));
    }
}
#endif
