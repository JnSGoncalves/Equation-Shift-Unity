#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using EquationShift;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class G2GateTests
{
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
        Assert.AreEqual(AncorSide.R, gate.PassageSide);
        Assert.AreEqual(AncorSide.R, gate.Ancor.AncorSide);
        Assert.IsTrue(gate.Ancor.IsAncor);
        Assert.IsFalse(gate.IsOpen);

        var blocks = board.GetComponentsInChildren<GridPushBlock>();
        var four = blocks.Single(block => block.Value == '4');
        var equals = blocks.Single(block => block.Value == '=');
        Physics2D.SyncTransforms();
        Assert.IsTrue(four.TryMove(Vector2Int.right));
        Assert.IsTrue(equals.TryMove(Vector2Int.down));
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(validator.ValidateEquations());
        Assert.IsTrue(validator.Results.Any(result => result.IsTrue && result.Expression == "4-1+3=6" && result.Contains(gate.Ancor)));
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
}
#endif
