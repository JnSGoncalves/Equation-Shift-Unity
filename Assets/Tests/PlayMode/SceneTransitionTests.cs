using System.Collections;
using EquationShift;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class SceneTransitionTests
{
    private enum ArrivalProblem { Disabled, Obstacle, WrongDestination, Duplicate, NoGround, Wall, NoGate }
    private const string First = "00-FaseExemplo_1";
    private const string Second = "A0-FaseExemplo_2";

    [TestCase("1A-Sala", "1A")]
    [TestCase("4G-Sala", "4G")]
    [TestCase("Assets/Scenes/A0-FaseExemplo_2.unity", "A0")]
    [TestCase("00-FaseExemplo_1", "00")]
    [TestCase("a0", "A0")]
    public void TwoCharacterPrefixIdentifiesRoom(string name, string expected)
    {
        Assert.IsTrue(ScenePortal.TryGetSceneId(name, out string id));
        Assert.AreEqual(expected, id);
    }

    [TestCase("")]
    [TestCase("A")]
    [TestCase("ABC-Sala")]
    [TestCase("@0-Sala")]
    public void InvalidRoomNamesAreRejected(string name) => Assert.IsFalse(ScenePortal.TryGetSceneId(name, out _));

    [Test]
    public void BuildResolvesBothExamplesAndRejectsMissingRoom()
    {
        Assert.IsTrue(ScenePortal.TryResolveScene("00", out string first));
        Assert.IsTrue(ScenePortal.TryResolveScene("a0", out string second));
        StringAssert.EndsWith(First + ".unity", first);
        StringAssert.EndsWith(Second + ".unity", second);
        Assert.IsFalse(ScenePortal.TryResolveScene("ZZ", out _));
        Assert.IsFalse(ScenePortal.TryResolveScene("A0-extra", out _));
    }

    private static IEnumerator WaitForRoom(string name)
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while ((SceneManager.GetActiveScene().name != name || SceneTransition.IsTravelling) &&
            Time.realtimeSinceStartup < deadline) yield return null;
        Assert.AreEqual(name, SceneManager.GetActiveScene().name);
        Assert.IsFalse(SceneTransition.IsTravelling);
        yield return null;
    }

    [UnityTest]
    public IEnumerator MissingRoomShowsNoticeInFirstRoom() => CheckMissingRoomNotice(First);

    [UnityTest]
    public IEnumerator MissingRoomShowsNoticeInSecondRoom() => CheckMissingRoomNotice(Second);

    private static IEnumerator CheckMissingRoomNotice(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return null;
        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var portal = Object.FindFirstObjectByType<ScenePortal>();
        string originalDestination = portal.DestinationSceneId;
        Vector3Int originalCell = player.Cell;
        portal.Configure(player.Board, "ZZ", portal.SpawnDirection);
        LogAssert.Expect(LogType.Warning, "Passagem sem destino unico nas cenas habilitadas do Build: ZZ");
        Assert.IsFalse(SceneTransition.TryTravel(portal, player));

        var notice = Object.FindFirstObjectByType<RoomUnavailableNotice>();
        Assert.IsNotNull(notice);
        Assert.IsTrue(notice.IsVisible);
        var label = notice.GetComponentInChildren<Text>();
        Assert.AreEqual("Opa, aparentemente essa sala ainda está sendo montada...", label.text);
        Assert.IsNotNull(label.font);
        Assert.AreEqual("Monocraft", label.font.name);
        Assert.AreEqual(Color.black, notice.GetComponentInChildren<Image>().color);
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, notice.GetComponentInChildren<Canvas>().renderMode);
        Assert.IsFalse(label.raycastTarget);
        Assert.IsFalse(player.IsMovementLocked);
        Assert.IsFalse(SceneTransition.IsTravelling);
        Assert.AreEqual(originalCell, player.Cell);
        Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name);

        yield return new WaitForSecondsRealtime(5.1f);
        Assert.IsFalse(notice.IsVisible, "The warning disappears without requiring player input.");

        LogAssert.Expect(LogType.Warning, "Passagem sem destino unico nas cenas habilitadas do Build: ZZ");
        Assert.IsFalse(SceneTransition.TryTravel(portal, player));
        Assert.IsTrue(notice.IsVisible);
        Assert.AreEqual(1, Object.FindObjectsByType<RoomUnavailableNotice>(FindObjectsSortMode.None).Length);

        portal.Configure(player.Board, originalDestination, portal.SpawnDirection);
        Assert.IsTrue(SceneTransition.TryTravel(portal, player));
        Assert.IsFalse(notice.IsVisible, "A valid transition clears the previous warning.");
        yield return WaitForRoom(sceneName == First ? Second : First);
        Assert.IsNull(Object.FindFirstObjectByType<RoomUnavailableNotice>(), "The overlay belongs to the previous room.");
    }

    private static T InScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>();
            if (component != null) return component;
        }
        return null;
    }

    [UnityTest]
    public IEnumerator CameraSlidesWithBothRoomsLoadedAndRestoresRoomCoordinates()
    {
        yield return SceneManager.LoadSceneAsync(First);
        yield return null;
        var oldRoom = SceneManager.GetActiveScene();
        var player = InScene<TopDownGridPlayer>(oldRoom);
        var camera = InScene<Camera>(oldRoom);
        Vector3 cameraPosition = camera.transform.position;
        Vector3 boardPosition = player.Board.transform.position;
        Assert.IsTrue(SceneTransition.TryTravel(InScene<ScenePortal>(oldRoom), player));
        float deadline = Time.realtimeSinceStartup + 10f;
        RoomTransitionController slide = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            slide = Object.FindFirstObjectByType<RoomTransitionController>();
            if (slide != null && slide.IsSliding && slide.Progress >= 0.2f) break;
            yield return null;
        }
        Assert.IsNotNull(slide);
        Assert.IsTrue(slide.IsSliding);
        Assert.IsTrue(oldRoom.isLoaded && slide.Destination.isLoaded);
        Assert.AreEqual(oldRoom, SceneManager.GetActiveScene());
        Assert.IsTrue(player.IsMovementLocked);
        Assert.IsFalse(player.enabled);
        Assert.That(slide.RoomOffset.y, Is.LessThan(0f));
        Assert.That(Mathf.Abs(slide.RoomOffset.x), Is.LessThan(0.001f));
        float distance = Vector3.Distance(cameraPosition, camera.transform.position);
        Assert.That(distance, Is.GreaterThan(0.01f));
        Assert.That(distance, Is.LessThan(Vector3.Distance(slide.CameraStart, slide.CameraEnd)));
        int activeCameras = 0;
        foreach (Camera view in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (view.isActiveAndEnabled) activeCameras++;
        Assert.AreEqual(1, activeCameras);
        var newPlayer = InScene<TopDownGridPlayer>(slide.Destination);
        Assert.IsTrue(newPlayer.IsMovementLocked);
        Assert.IsFalse(newPlayer.enabled);
        Assert.That(Vector3.Distance(newPlayer.Board.transform.position, boardPosition + slide.RoomOffset), Is.LessThan(0.001f));
        yield return WaitForRoom(Second);
        Assert.IsFalse(oldRoom.isLoaded);
        newPlayer = Object.FindFirstObjectByType<TopDownGridPlayer>();
        Assert.IsTrue(newPlayer.enabled);
        Assert.That(Vector3.Distance(newPlayer.Board.transform.position, boardPosition), Is.LessThan(0.001f));
        Assert.That(Vector3.Distance(Camera.main.transform.position, cameraPosition), Is.LessThan(0.001f));
        Assert.That(Vector2.Distance(newPlayer.GetComponent<Rigidbody2D>().position,
            newPlayer.Board.CenterOf(newPlayer.Cell)), Is.LessThan(0.001f));
        foreach (GridPushBlock block in newPlayer.Board.GetComponentsInChildren<GridPushBlock>())
            Assert.That(Vector2.Distance(block.GetComponent<Rigidbody2D>().position,
                newPlayer.Board.CenterOf(block.Cell)), Is.LessThan(0.001f));
    }

    [UnityTest]
    public IEnumerator MissingDestinationCameraRestoresSourceRoomAndControls()
    {
        yield return SceneManager.LoadSceneAsync(First);
        yield return null;
        Scene origin = SceneManager.GetActiveScene();
        var player = InScene<TopDownGridPlayer>(origin);
        var camera = InScene<Camera>(origin);
        Vector3 cameraPosition = camera.transform.position;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> removeCamera = (scene, mode) =>
        {
            if (scene.name == Second) Object.DestroyImmediate(InScene<Camera>(scene).gameObject);
        };
        SceneManager.sceneLoaded += removeCamera;
        try
        {
            LogAssert.Expect(LogType.Error, "A sala de destino precisa de Player e Camera configurados: Assets/Scenes/" + Second + ".unity");
            Assert.IsTrue(SceneTransition.TryTravel(InScene<ScenePortal>(origin), player));
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneTransition.IsTravelling && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(SceneTransition.IsTravelling);
            Assert.AreEqual(origin, SceneManager.GetActiveScene());
            Assert.IsFalse(SceneManager.GetSceneByName(Second).isLoaded);
            Assert.IsTrue(player.enabled && player.GetComponent<Rigidbody2D>().simulated);
            Assert.IsFalse(player.IsMovementLocked);
            Assert.IsTrue(camera.isActiveAndEnabled);
            Assert.AreEqual(cameraPosition, camera.transform.position);
            Assert.IsTrue(player.TryMove(Vector2Int.down));
        }
        finally { SceneManager.sceneLoaded -= removeCamera; }
    }

    [UnityTest]
    public IEnumerator WalkingThroughBottomGateArrivesInsideTopEntranceAndCanReturn()
    {
        yield return SceneManager.LoadSceneAsync(First);
        yield return null;
        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var exit = Object.FindFirstObjectByType<ScenePortal>();
        var gate = GameObject.Find("Gate_Horizontal").GetComponent<EquationGate>();
        Assert.AreEqual("A0", exit.DestinationSceneId);
        Assert.IsTrue(exit.GetComponent<Collider2D>().isTrigger);
        Assert.IsNull(exit.GetComponent<SpriteRenderer>());
        Assert.IsTrue(player.TeleportToCell(new Vector3Int(-1, -7, 0)));
        Physics2D.SyncTransforms();
        Assert.IsFalse(player.TryMove(Vector2Int.down), "Closed gate blocks the route");
        gate.SetOpen(true, false);
        player.UnlockForm(PlayerForm.Equal);
        player.UnlockForm(PlayerForm.Mult);
        player.CurrentForm = PlayerForm.Mult;
        Assert.IsTrue(player.TryMove(Vector2Int.down));
        yield return new WaitForSeconds(0.25f);
        Assert.AreEqual(First, SceneManager.GetActiveScene().name);
        Assert.IsTrue(player.TryMove(Vector2Int.down));
        yield return WaitForRoom(Second);
        player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var entrance = Object.FindFirstObjectByType<ScenePortal>();
        Assert.AreEqual(entrance.ArrivalCell, player.Cell);
        Assert.AreEqual(new Vector3Int(-1, 7, 0), player.Cell);
        Assert.AreEqual(1, (player.Cell - player.Board.CellAt(entrance.transform.position)).sqrMagnitude);
        var arrivalGate = entrance.ArrivalGate;
        Assert.IsFalse(arrivalGate.DefaultIsOpen);
        Assert.IsTrue(arrivalGate.IsOpen && arrivalGate.IsWaitingForArrivalExit);
        Assert.IsFalse(arrivalGate.GetComponent<Collider2D>().enabled);
        Assert.AreEqual(PlayerForm.Mult, player.CurrentForm);
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Equal));
        Assert.IsFalse(player.IsMovementLocked);
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(Second, SceneManager.GetActiveScene().name, "Arrival must not retrigger the portal");
        Assert.IsTrue(player.TryMove(Vector2Int.up));
        yield return WaitForRoom(First);
        player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        Assert.AreEqual(new Vector3Int(-1, -8, 0), player.Cell);
        Assert.AreEqual(PlayerForm.Mult, player.CurrentForm);
        Assert.IsTrue(GameObject.Find("Gate_Horizontal").GetComponent<EquationGate>().IsOpen);
    }

    [UnityTest]
    public IEnumerator ResetAfterTravellingKeepsAbilitiesFromPreviousRooms()
    {
        yield return SceneManager.LoadSceneAsync(First);
        yield return null;
        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        player.UnlockForm(PlayerForm.Equal);
        player.UnlockForm(PlayerForm.Mult);
        player.CurrentForm = PlayerForm.Mult;
        Assert.IsTrue(SceneTransition.TryTravel(Object.FindFirstObjectByType<ScenePortal>(), player));
        Assert.IsFalse(player.TryResetLevel(), "Reset cannot interrupt a room transition.");
        yield return WaitForRoom(Second);

        player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var entrance = Object.FindFirstObjectByType<ScenePortal>();
        Vector3Int arrival = player.Cell;
        Assert.AreEqual(entrance.ArrivalCell, arrival);
        Assert.IsTrue(player.TryMove(entrance.SpawnDirection));
        yield return new WaitForSeconds(0.3f);
        float resetDeadline = Time.realtimeSinceStartup + 5f;
        while (player.IsMovementLocked && Time.realtimeSinceStartup < resetDeadline) yield return null;
        Assert.AreNotEqual(arrival, player.Cell);
        Assert.IsTrue(player.TryResetLevel());
        Assert.AreEqual(arrival, player.Cell, "Reset returns to the entrance from the previous room.");
        Assert.IsTrue(entrance.ArrivalGate.IsOpen);
        Assert.IsTrue(entrance.ArrivalGate.IsWaitingForArrivalExit);
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Equal));
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Mult));
        Assert.AreEqual(PlayerForm.Mult, player.CurrentForm);
        player.UnlockForm(PlayerForm.Plus);
        Assert.IsTrue(player.TryResetLevel());
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Plus), "Reset also preserves abilities collected in this room.");

        // Leaving the arrival gate can queue its closing animation.
        yield return new WaitForSecondsRealtime(2f);
        Assert.IsTrue(SceneTransition.TryTravel(Object.FindFirstObjectByType<ScenePortal>(), player));
        yield return WaitForRoom(First);
        player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Equal));
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Mult));
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Plus));
        Assert.AreEqual(PlayerForm.Mult, player.CurrentForm);
        arrival = player.Cell;
        Assert.AreEqual(Object.FindFirstObjectByType<ScenePortal>().ArrivalCell, arrival);
        Assert.IsTrue(player.TryResetLevel());
        Assert.AreEqual(arrival, player.Cell, "Returning to a room updates its reset entrance.");
    }

    [UnityTest]
    public IEnumerator ArrivalGateClosesOnlyAfterPlayerLeavesWithCameraFeedback()
    {
        yield return SceneManager.LoadSceneAsync(First);
        yield return null;
        Assert.IsTrue(SceneTransition.TryTravel(Object.FindFirstObjectByType<ScenePortal>(),
            Object.FindFirstObjectByType<TopDownGridPlayer>()));
        yield return WaitForRoom(Second);
        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var gate = Object.FindFirstObjectByType<ScenePortal>().ArrivalGate;
        var camera = Camera.main;
        var feedback = camera.GetComponent<GateOpeningFeedback>();
        Vector3 cameraPosition = camera.transform.localPosition;
        Assert.IsFalse(gate.DefaultIsOpen);
        Assert.IsTrue(gate.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Open"));
        yield return new WaitForSeconds(0.2f);
        Assert.IsTrue(gate.IsOpen && gate.IsWaitingForArrivalExit);
        Assert.IsFalse(feedback.IsPlaying);
        Assert.IsTrue(player.TryMove(Vector2Int.down));
        yield return new WaitForSeconds(0.05f);
        Assert.IsTrue(player.IsMoving);
        Assert.IsTrue(gate.IsOpen);
        Assert.IsFalse(gate.IsClosing || feedback.IsPlaying, "Wait until the player clears the gate");
        float deadline = Time.realtimeSinceStartup + 4f;
        float peakShake = 0f;
        bool sawClosingAnimation = false;
        while (Time.realtimeSinceStartup < deadline)
        {
            peakShake = Mathf.Max(peakShake, Vector3.Distance(cameraPosition, camera.transform.localPosition));
            if (gate.IsClosing)
            {
                Assert.IsFalse(player.IsMoving);
                Assert.IsTrue(player.IsMovementLocked && feedback.IsPlaying);
                if (gate.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Closing"))
                {
                    sawClosingAnimation = true;
                    Assert.IsTrue(gate.GetComponent<Collider2D>().enabled);
                }
            }
            if (!gate.IsOpen && !feedback.IsPlaying) break;
            yield return null;
        }
        Assert.IsTrue(sawClosingAnimation);
        Assert.That(peakShake, Is.GreaterThan(0.001f));
        Assert.IsFalse(gate.IsOpen || gate.IsClosing || gate.IsWaitingForArrivalExit);
        Assert.IsFalse(player.IsMovementLocked);
        Assert.IsTrue(gate.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Closed"));
        Assert.That(Vector3.Distance(cameraPosition, camera.transform.localPosition), Is.LessThan(0.0001f));
        Assert.AreEqual(new Vector3Int(-1, 6, 0), player.Cell);
        Assert.IsFalse(player.TryMove(Vector2Int.up), "Gate closes behind the player");
    }

    [UnityTest]
    public IEnumerator ReciprocalDestinationSelectsReturnExitAmongOtherRooms()
    {
        yield return SceneManager.LoadSceneAsync(First);
        yield return null;
        Vector3Int expectedArrival = default;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> prepareDestination = (scene, mode) =>
        {
            if (scene.name != Second) return;
            var entrance = InScene<ScenePortal>(scene);
            entrance.name = "Return exit with arbitrary name";
            expectedArrival = entrance.ArrivalCell;
            foreach (string room in new[] { "4G", "1A" })
            {
                var other = new GameObject("Exit to " + room, typeof(BoxCollider2D), typeof(ScenePortal));
                other.transform.SetParent(entrance.Board.transform, false);
                other.transform.position = entrance.Board.CenterOf(new Vector3Int(room == "4G" ? 3 : 4, 3, 0));
                other.GetComponent<BoxCollider2D>().size = Vector2.one * 0.8f;
                other.GetComponent<ScenePortal>().Configure(entrance.Board, room, Vector2Int.down);
            }
        };
        SceneManager.sceneLoaded += prepareDestination;
        try
        {
            Assert.IsTrue(SceneTransition.TryTravel(Object.FindFirstObjectByType<ScenePortal>(),
                Object.FindFirstObjectByType<TopDownGridPlayer>()));
            yield return WaitForRoom(Second);
            Assert.AreEqual(3, Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Length);
            Assert.AreEqual(expectedArrival, Object.FindFirstObjectByType<TopDownGridPlayer>().Cell,
                "Only the exit pointing back to the origin room is the counterpart");
            Assert.IsTrue(GameObject.Find("Gate_Horizontal").GetComponent<EquationGate>().IsOpen);
        }
        finally { SceneManager.sceneLoaded -= prepareDestination; }
    }

    [UnityTest]
    public IEnumerator MissingCounterpartUsesFreeCenter() => TravelWithUnavailableArrival(ArrivalProblem.Disabled);

    [UnityTest]
    public IEnumerator ArrivalColliderDoesNotRedirectToCenter() => TravelWithUnavailableArrival(ArrivalProblem.Obstacle);

    [UnityTest]
    public IEnumerator EntranceWithoutGateStillUsesItsArrivalCell() => TravelWithUnavailableArrival(ArrivalProblem.NoGate);

    [UnityTest]
    public IEnumerator ArrivalWithoutGroundUsesFreeCenter() => TravelWithUnavailableArrival(ArrivalProblem.NoGround);

    [UnityTest]
    public IEnumerator ArrivalOnWallUsesFreeCenter() => TravelWithUnavailableArrival(ArrivalProblem.Wall);

    [UnityTest]
    public IEnumerator ExitToAnotherRoomIsNotCounterpart() => TravelWithUnavailableArrival(ArrivalProblem.WrongDestination);

    [UnityTest]
    public IEnumerator DuplicateReturnExitsUseFreeCenter() => TravelWithUnavailableArrival(ArrivalProblem.Duplicate);

    private static IEnumerator TravelWithUnavailableArrival(ArrivalProblem problem)
    {
        yield return SceneManager.LoadSceneAsync(First);
        yield return null;
        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var exit = Object.FindFirstObjectByType<ScenePortal>();
        Vector3Int expectedArrival = default;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> prepareDestination = (scene, mode) =>
        {
            if (scene.name != Second) return;
            var entrance = InScene<ScenePortal>(scene);
            expectedArrival = entrance.ArrivalCell;
            switch (problem)
            {
                case ArrivalProblem.Disabled:
                    entrance.enabled = false;
                    break;
                case ArrivalProblem.Obstacle:
                    var obstacle = new GameObject("Obstructed arrival", typeof(BoxCollider2D));
                    obstacle.transform.SetParent(entrance.Board.transform, false);
                    obstacle.transform.position = entrance.Board.CenterOf(entrance.ArrivalCell);
                    break;
                case ArrivalProblem.NoGate:
                    Object.DestroyImmediate(entrance.ArrivalGate.gameObject);
                    // An oversized portal trigger may overlap the valid arrival tile.
                    entrance.GetComponent<BoxCollider2D>().size = Vector2.one * 3f;
                    break;
                case ArrivalProblem.NoGround:
                    entrance.Board.Ground.SetTile(entrance.ArrivalCell, null);
                    break;
                case ArrivalProblem.Wall:
                    var ground = entrance.Board.Ground;
                    var walls = entrance.Board.Walls;
                    Vector3 world = ground.GetCellCenterWorld(entrance.ArrivalCell);
                    world.z = walls.transform.position.z;
                    walls.SetTile(walls.WorldToCell(world), ground.GetTile(entrance.ArrivalCell));
                    break;
                case ArrivalProblem.WrongDestination:
                    entrance.Configure(entrance.Board, "4G", entrance.SpawnDirection, entrance.ArrivalGate);
                    break;
                case ArrivalProblem.Duplicate:
                    var duplicate = Object.Instantiate(entrance.gameObject, entrance.transform.parent);
                    duplicate.name = "Second exit to the same origin room";
                    duplicate.transform.position = entrance.Board.CenterOf(new Vector3Int(3, 3, 0));
                    break;
            }
        };
        SceneManager.sceneLoaded += prepareDestination;
        try
        {
            Assert.IsTrue(SceneTransition.TryTravel(exit, player));
            Assert.IsTrue(player.IsMovementLocked);
            Assert.IsFalse(SceneTransition.TryTravel(exit, player), "Only one load can run at once");
            yield return WaitForRoom(Second);
            player = Object.FindFirstObjectByType<TopDownGridPlayer>();
            if (problem == ArrivalProblem.NoGate || problem == ArrivalProblem.Obstacle)
            {
                Assert.AreEqual(expectedArrival, player.Cell);
                Assert.IsFalse(player.IsMovementLocked);
                yield return null;
                Assert.IsFalse(SceneTransition.IsTravelling, "Overlapping a portal trigger must not send the player back.");
                Assert.AreEqual(Second, SceneManager.GetActiveScene().name);
                Assert.IsTrue(player.TryResetLevel());
                Assert.AreEqual(expectedArrival, player.Cell, "Reset uses the same entrance rules.");
                yield break;
            }
            Assert.IsTrue(SceneTransition.TryFindSafeCenter(player, out Vector3Int center));
            Assert.AreEqual(center, player.Cell);
            Assert.IsTrue(player.CanStandAt(player.Cell));
            Assert.IsFalse(player.IsMoving);
            Assert.IsFalse(player.IsMovementLocked);
            var destinationGate = GameObject.Find("Gate_Horizontal").GetComponent<EquationGate>();
            Assert.IsFalse(destinationGate.IsOpen || destinationGate.IsWaitingForArrivalExit,
                "Fallback must leave the unused gate in its configured closed state");
        }
        finally { SceneManager.sceneLoaded -= prepareDestination; }
    }
}
