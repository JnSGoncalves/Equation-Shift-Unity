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
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public class RoomLeverTests
{
    private const string LeverPrefabPath = "Assets/Prefabs/Interactables/RoomLever.prefab";
    private static readonly Vector3Int[] Sides =
        { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    private GameObject testRoom;
    private Tile floorTile;
    private Scene loadedRoom;

    [UnityTearDown]
    public IEnumerator CleanUp()
    {
        Object.DestroyImmediate(testRoom);
        Object.DestroyImmediate(floorTile);
        if (!loadedRoom.IsValid() || !loadedRoom.isLoaded) yield break;
        SceneManager.SetActiveScene(SceneManager.CreateScene("Lever test cleanup"));
        yield return SceneManager.UnloadSceneAsync(loadedRoom);
    }

    [UnityTest]
    public IEnumerator LeverRespondsToEveryCardinalSideButNotTheDiagonal()
    {
        var player = CreateTestPlayer();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LeverPrefabPath);
        Assert.IsNotNull(prefab);

        foreach (Vector3Int side in Sides)
        {
            Assert.IsTrue(player.TeleportToCell(new Vector3Int(1, 1, 0)));
            var actor = Object.Instantiate(prefab, testRoom.transform);
            actor.transform.position = player.Board.CenterOf(Vector3Int.zero);
            var lever = actor.GetComponent<RoomLever>();
            lever.Configure(Guid.NewGuid().ToString());

            yield return new WaitForFixedUpdate();
            Assert.IsFalse(lever.IsActivated, "Diagonal contact must not activate a lever");
            Assert.IsTrue(player.TeleportToCell(side));
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(lever.IsActivated, $"A player at {side} must activate the lever");
            Assert.IsFalse(player.IsMovementLocked);
            Object.DestroyImmediate(actor);
        }
    }

    [UnityTest]
    public IEnumerator B4GateOpensAfterBothRoomLeversAndRestoresTheirStateOnReentry()
    {
        yield return LoadRoom("B4");
        AssertTopGateOpen(false);

        yield return LoadRoom("C2");
        yield return ActivateRoomLever();
        yield return LoadRoom("B4");
        AssertTopGateOpen(false);

        yield return LoadRoom("C6");
        yield return ActivateRoomLever();
        yield return LoadRoom("B4");
        AssertTopGateOpen(true);

        foreach (string code in new[] { "C2", "C6" })
        {
            yield return LoadRoom(code);
            var lever = InRoom<RoomLever>();
            Assert.IsTrue(lever.IsActivated, $"{code} must retain its activation after unloading");
            Assert.IsTrue(lever.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Active"));
        }
    }

    private TopDownGridPlayer CreateTestPlayer()
    {
        testRoom = new GameObject("Lever test grid", typeof(Grid));
        testRoom.transform.position = new Vector3(8000f, 8000f, 0f);
        var board = testRoom.AddComponent<LevelGrid>();
        var floor = Child("Ground").AddComponent<Tilemap>();
        var walls = Child("Walls").AddComponent<Tilemap>();
        board.Configure(floor, walls);
        floorTile = ScriptableObject.CreateInstance<Tile>();
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++) floor.SetTile(new Vector3Int(x, y, 0), floorTile);

        var actor = Child("Player");
        actor.SetActive(false);
        actor.transform.position = board.CenterOf(new Vector3Int(1, 1, 0));
        actor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        actor.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
        var player = actor.AddComponent<TopDownGridPlayer>();
        player.Configure(board);
        actor.SetActive(true);
        return player;
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(testRoom.transform, false);
        return child;
    }

    private IEnumerator LoadRoom(string code)
    {
        Assert.IsTrue(ScenePortal.TryResolveScene(code, out string path), $"{code} must resolve uniquely in the Build");
        yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        loadedRoom = SceneManager.GetSceneByPath(path);
        Assert.IsTrue(loadedRoom.isLoaded);
        yield return null;
    }

    private T InRoom<T>() where T : Component => loadedRoom.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>()).Single();

    private IEnumerator ActivateRoomLever()
    {
        var player = InRoom<TopDownGridPlayer>();
        var lever = InRoom<RoomLever>();
        var animator = lever.GetComponent<Animator>();
        var camera = InRoom<GateOpeningFeedback>().transform;
        Vector3 restingPosition = camera.localPosition;
        Assert.IsFalse(lever.IsActivated);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Inactive"));

        Physics2D.SyncTransforms();
        Vector3Int leverCell = player.Board.CellAt(lever.transform.position);
        Vector3Int[] sides = Sides.Select(side => leverCell + side).Where(player.CanStandAt).ToArray();
        Assert.GreaterOrEqual(sides.Length, 2);
        Assert.IsTrue(player.TeleportToCell(sides[0]));
        yield return new WaitForFixedUpdate();
        Assert.IsTrue(lever.IsActivated);
        bool cameraMoved = false;
        float deadline = Time.realtimeSinceStartup + 2f;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Active") && Time.realtimeSinceStartup < deadline)
        {
            cameraMoved |= Vector3.Distance(restingPosition, camera.localPosition) > 0.0001f;
            Assert.IsFalse(player.IsMovementLocked);
            yield return null;
        }
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Active"));
        Assert.IsTrue(cameraMoved, "Activating the lever must shake the camera");
        Assert.That(Vector3.Distance(restingPosition, camera.localPosition), Is.LessThan(0.0001f));
    }

    private void AssertTopGateOpen(bool expected)
    {
        var gate = loadedRoom.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EquationGate>())
            .Single(candidate => candidate.PassageSide == AncorSide.T);
        Assert.AreEqual(expected, gate.IsOpen);
        Assert.AreEqual(!expected, gate.GetComponent<BoxCollider2D>().enabled);
        Assert.IsTrue(gate.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName(expected ? "Open" : "Closed"));
    }
}
#endif
