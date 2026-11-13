#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using EquationShift;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class VillageSequenceTests
{
    private static readonly Vector3Int[] Sides =
        { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };

    [UnityTest]
    public IEnumerator VillageRequiresFiveDifferentNpcsIncludingErrorBeforeOpeningTheExit()
    {
        Assert.IsTrue(ScenePortal.TryResolveScene("G1", out string villagePath));
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/Title.unity", LoadSceneMode.Single);
        GameObject.Find("Title Screen").SendMessage("StartGame");
        yield return WaitUntil(() => SceneManager.GetActiveScene().path == villagePath && VillageDialogue.IsVisible,
            7f, "The movement hint did not appear when starting from the title screen.");
        Scene village = SceneManager.GetActiveScene();
        var roots = village.GetRootGameObjects();
        var sequence = roots.SelectMany(root => root.GetComponentsInChildren<VillageSequence>()).Single();
        var player = roots.SelectMany(root => root.GetComponentsInChildren<TopDownGridPlayer>()).Single();
        var npcs = roots.SelectMany(root => root.GetComponentsInChildren<VillageNpc>()).ToArray();
        var gate = roots.SelectMany(root => root.GetComponentsInChildren<EquationGate>()).Single();
        var camera = roots.SelectMany(root => root.GetComponentsInChildren<GateOpeningFeedback>()).Single().transform;
        var serialized = new SerializedObject(sequence);
        var error = (VillageNpc)serialized.FindProperty("error").objectReferenceValue;
        var guard = npcs.Single(npc => Vector2.Distance(npc.transform.position, new Vector2(12.5f, -2.5f)) < 0.001f);

        Assert.AreEqual(11, npcs.Length);
        var counts = npcs.GroupBy(npc => npc.GetComponent<Animator>().runtimeAnimatorController.name)
            .ToDictionary(group => group.Key, group => group.Count());
        foreach (string kind in new[] { "Question", "Equal", "Plus", "Multiply", "Divide" })
            Assert.AreEqual(2, counts["NPC_" + kind]);
        Assert.AreEqual(1, counts["NPC_Error"]);
        Assert.IsTrue(npcs.All(npc => player.Board.IsFloor(player.Board.CellAt(npc.transform.position))));
        Assert.AreEqual(0, sequence.TalkedCount);
        Assert.IsFalse(gate.IsOpen);
        Assert.IsTrue(gate.GetComponent<BoxCollider2D>().enabled);
        var dialogue = Object.FindFirstObjectByType<VillageDialogue>();
        var label = dialogue.GetComponentInChildren<Text>();
        Assert.AreEqual("Utilize as setas ou WASD para mover o personagem.", label.text);
        Assert.IsFalse(player.IsMovementLocked);

        yield return Approach(player, guard, npcs);
        Assert.AreEqual(1, sequence.TalkedCount);
        Assert.IsTrue(VillageDialogue.IsVisible);
        Assert.That(label.font.name, Does.Contain("Monocraft").IgnoreCase);
        Assert.AreEqual(Color.black, dialogue.GetComponentInChildren<Image>().color);
        Assert.That(label.text, Does.Contain("ruínas da matemática").IgnoreCase);
        string originalGuardDialogue = label.text;
        yield return WaitUntil(() => !VillageDialogue.IsVisible, 6f, "The dialogue did not close automatically.");
        yield return null;
        Assert.IsFalse(VillageDialogue.IsVisible, "Standing beside the same NPC must not reopen its dialogue.");

        Assert.IsTrue(player.TeleportToCell(FindQuietCell(player, npcs)));
        yield return new WaitForFixedUpdate();
        yield return null;
        yield return Approach(player, guard, npcs);
        Assert.AreEqual(1, sequence.TalkedCount, "A repeated conversation must count only once.");
        Assert.IsTrue(VillageDialogue.IsVisible);

        foreach (VillageNpc npc in npcs.Where(npc => npc != guard && npc != error).Take(4))
            yield return Approach(player, npc, npcs);
        Assert.AreEqual(5, sequence.TalkedCount);
        VillageDialogue.Hide();
        yield return null;
        Assert.IsFalse(gate.IsOpen, "Five conversations without Error must leave the gate closed.");
        Assert.IsFalse(player.IsMovementLocked);

        yield return Approach(player, error, npcs);
        Assert.IsTrue(VillageDialogue.IsVisible);
        Assert.That(label.text, Does.Contain("chata").IgnoreCase);
        Assert.That(label.text, Does.Contain("divert").IgnoreCase);
        VillageDialogue.Hide();

        var view = camera.GetComponent<Camera>();
        view.aspect = 4f / 3f;
        Vector3 cameraOrigin = camera.position;
        float rightmost = error.transform.position.x;
        bool sawMovementLock = false;
        bool sawFramedGate = false;
        float deadline = Time.realtimeSinceStartup + 20f;
        while (!sequence.HasEscaped && Time.realtimeSinceStartup < deadline)
        {
            sawMovementLock |= player.IsMovementLocked;
            if (error != null) rightmost = Mathf.Max(rightmost, error.transform.position.x);
            if (gate.IsOpening)
            {
                Vector3 viewport = view.WorldToViewportPoint(gate.transform.position);
                sawFramedGate |= viewport.x > 0f && viewport.x < 1f;
            }
            yield return null;
        }
        Assert.IsTrue(sequence.HasEscaped, "The cutscene did not finish.");
        Assert.IsTrue(sawMovementLock);
        Assert.IsTrue(sawFramedGate, "The camera must frame the opening gate in a narrow window.");
        Assert.That(rightmost, Is.GreaterThan(gate.transform.position.x));
        Assert.IsTrue(error == null || !error.gameObject.activeInHierarchy);
        Assert.IsTrue(gate.IsOpen);
        Assert.IsFalse(gate.GetComponent<BoxCollider2D>().enabled);

        bool sawExtendedShake = false;
        float shakeStartedAt = Time.realtimeSinceStartup;
        deadline = shakeStartedAt + 1.1f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (Time.realtimeSinceStartup - shakeStartedAt > 0.3f)
                sawExtendedShake |= Vector3.Distance(camera.position, cameraOrigin) > 0.001f;
            yield return null;
        }
        Assert.IsTrue(sawExtendedShake, "The camera shake ended too soon after Error left.");
        Assert.That(Vector3.Distance(camera.position, cameraOrigin), Is.LessThan(0.001f));
        Assert.IsFalse(player.IsMovementLocked);

        yield return Approach(player, guard, npcs);
        Assert.IsTrue(VillageDialogue.IsVisible);
        Assert.AreNotEqual(originalGuardDialogue, label.text);
        Assert.That(label.text, Does.Contain("funções").IgnoreCase);
        foreach (VillageNpc npc in npcs.Where(npc => npc != error && npc != guard))
        {
            var lines = new SerializedObject(npc);
            string alternative = lines.FindProperty("aftermathDialogue").stringValue;
            Assert.IsNotEmpty(alternative);
            Assert.AreNotEqual(lines.FindProperty("dialogue").stringValue, alternative);
            yield return Approach(player, npc, npcs);
            Assert.IsTrue(VillageDialogue.IsVisible);
            Assert.AreEqual(alternative, label.text, npc.name);
        }

        Assert.IsTrue(player.TeleportToCell(player.Board.CellAt(gate.transform.position)));
        Assert.IsTrue(player.TryMove(Vector2Int.right));
        Assert.IsTrue(ScenePortal.TryResolveScene("G2", out string ruinsPath));
        yield return WaitUntil(() => SceneManager.GetActiveScene().path == ruinsPath && !SceneTransition.IsTravelling,
            7f, "The open gate did not allow travel to G2.");
        Assert.IsFalse(VillageDialogue.IsVisible);
    }

    private static IEnumerator Approach(TopDownGridPlayer player, VillageNpc npc, VillageNpc[] npcs)
    {
        Vector3Int npcCell = player.Board.CellAt(npc.transform.position);
        Vector3Int side = Sides.Select(offset => npcCell + offset).First(cell =>
            player.CanStandAt(cell) && npcs.All(other => other == null || other == npc || !other.isActiveAndEnabled ||
                Distance(cell, player.Board.CellAt(other.transform.position)) > 1));
        Assert.IsTrue(player.TeleportToCell(side));
        yield return new WaitForFixedUpdate();
        yield return null;
    }

    private static int Distance(Vector3Int a, Vector3Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private static Vector3Int FindQuietCell(TopDownGridPlayer player, VillageNpc[] npcs)
    {
        foreach (Vector3Int cell in player.Board.Ground.cellBounds.allPositionsWithin)
            if (player.CanStandAt(cell) && npcs.All(npc => Distance(cell, player.Board.CellAt(npc.transform.position)) > 1))
                return cell;
        throw new InvalidOperationException("The village needs a walkable cell outside conversation range.");
    }

    private static IEnumerator WaitUntil(Func<bool> condition, float seconds, string message)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), message);
    }
}
#endif
