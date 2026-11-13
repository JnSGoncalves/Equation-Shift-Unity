#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using EquationShift;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class FinalRoomCutsceneTests
{
    [UnityTest]
    public IEnumerator FinalRoomPlaysTheEscapeAndEndingThenRestartsFromTheTitle()
    {
        Assert.IsTrue(ScenePortal.TryResolveScene("A4", out string path));
        Assert.IsTrue(ScenePortal.TryResolveScene("B4", out string previousPath));
        yield return SceneManager.LoadSceneAsync(previousPath, LoadSceneMode.Single);
        var entrancePlayer = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var entrance = Array.Find(Object.FindObjectsByType<ScenePortal>(FindObjectsSortMode.None),
            portal => portal.DestinationSceneId == "A4");
        entrance.ArrivalGate.SetOpen(true, false);
        Assert.IsTrue(SceneTransition.TryTravel(entrance, entrancePlayer));
        yield return Until(() => SceneManager.GetActiveScene().path == path && !SceneTransition.IsTravelling,
            "Arrival from B4");

        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        var error = GameObject.Find("NPC_Error");
        var gate = GameObject.Find("Gate_Horizontal_D").GetComponent<EquationGate>();
        var camera = Object.FindFirstObjectByType<GateOpeningFeedback>().transform;
        Vector3 restingCamera = camera.localPosition;
        Vector3Int triggerCell = player.Board.CellAt(new Vector3(-0.5f, -5.5f));

        Assert.AreEqual(new Vector2(-0.5f, 0.5f), (Vector2)error.transform.position);
        Assert.IsTrue(gate.IsOpen);
        Assert.IsFalse(VillageDialogue.IsVisible);
        Assert.IsFalse(player.IsMovementLocked);
        while (player.Cell.y < triggerCell.y)
        {
            Assert.IsTrue(player.TryMove(Vector2Int.up));
            yield return Until(() => !player.IsMoving, "Player walks up from the gate");
        }
        yield return Until(() => VillageDialogue.IsVisible, "Dialogue at the trigger cell");

        Assert.AreEqual(triggerCell, player.Cell);
        Assert.IsTrue(player.IsMovementLocked);
        Assert.IsFalse(player.TryMove(Vector2Int.left));
        var text = Object.FindFirstObjectByType<VillageDialogue>().GetComponentInChildren<Text>();
        Assert.That(text.text, Does.Contain("Vou embora daqui antes que tudo desmorone!"));
        VillageDialogue.Hide();

        yield return Until(() => player.Cell != triggerCell, "Error pushes the player sideways");
        Assert.AreEqual(triggerCell.y, player.Cell.y);
        Assert.AreEqual(1, Mathf.Abs(player.Cell.x - triggerCell.x));
        Assert.IsTrue(error.activeSelf);
        Assert.IsTrue(player.IsMovementLocked);

        yield return Until(() => !error.activeSelf, "Error exits through the gate");
        Assert.That(error.transform.position.y, Is.LessThan(gate.transform.position.y));
        Assert.IsTrue(gate.IsClosing);
        Assert.IsTrue(player.IsMovementLocked);
        Assert.AreEqual(restingCamera, camera.localPosition);
        yield return Until(() => !gate.IsClosing, "The gate closes");
        Assert.IsFalse(gate.IsOpen);
        Assert.IsTrue(gate.GetComponent<BoxCollider2D>().enabled);
        Assert.IsTrue(player.IsMovementLocked);
        yield return Until(() => Vector3.Distance(restingCamera, camera.localPosition) > 0.001f, "Camera shake");
        Assert.IsTrue(player.IsMovementLocked);
        yield return Until(() => !player.IsMovementLocked, "Control returns after the shake");
        Assert.That(Vector3.Distance(restingCamera, camera.localPosition), Is.LessThan(0.001f));

        Vector3Int checkpoint = player.Cell;
        Assert.IsTrue(player.TeleportToCell(triggerCell));
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsFalse(VillageDialogue.IsVisible);
        Assert.IsTrue(player.TryResetLevel());
        Assert.AreEqual(checkpoint, player.Cell);
        Assert.IsFalse(gate.IsOpen);

        // Keep the room's two anchors and fill the rest of the final equation.
        var validator = Object.FindFirstObjectByType<EquationValidator>();
        var blocks = validator.GetComponentsInChildren<GridPushBlock>();
        var first = blocks.Single(block => block.name.EndsWith("_Ancor_D1", StringComparison.Ordinal));
        var second = blocks.Single(block => block.name.EndsWith("_Ancor_D2", StringComparison.Ordinal));
        foreach (var block in blocks)
            if (block != first && block != second) block.gameObject.SetActive(false);

        const string hint = "Acho que para estabilizar a função principal devo usar todos os blocos móveis disponíveis";
        var firstDigit = PlaceSymbol(validator, player.Board, first.Cell - Vector3Int.right * 2, '0');
        var firstEquals = PlaceSymbol(validator, player.Board, first.Cell - Vector3Int.right, '=');
        Assert.IsFalse(validator.ValidateEquations());
        Assert.IsFalse(VillageDialogue.IsVisible);

        firstDigit.Value = '1';
        Assert.IsTrue(validator.ValidateEquations());
        Assert.IsTrue(VillageDialogue.IsVisible);
        Assert.AreEqual(hint, text.text);
        Assert.IsFalse(gate.IsOpening);
        Assert.IsFalse(player.IsMovementLocked);
        VillageDialogue.Hide();
        Assert.IsTrue(validator.ValidateEquations());
        Assert.IsFalse(VillageDialogue.IsVisible, "An unchanged equation must not keep reopening the hint");
        firstDigit.gameObject.SetActive(false);
        firstEquals.gameObject.SetActive(false);

        var secondEquals = PlaceSymbol(validator, player.Board, second.Cell + Vector3Int.right, '=');
        var secondDigit = PlaceSymbol(validator, player.Board, second.Cell + Vector3Int.right * 2, '2');
        Assert.IsTrue(validator.ValidateEquations());
        Assert.IsTrue(VillageDialogue.IsVisible);
        Assert.AreEqual(hint, text.text);
        Assert.IsFalse(gate.IsOpening);
        Assert.IsFalse(player.IsMovementLocked);
        secondEquals.gameObject.SetActive(false);
        secondDigit.gameObject.SetActive(false);

        const string equation = "48/2+11=20+2*9-3";
        Vector3Int origin = first.Cell - Vector3Int.right * 6;
        Assert.AreEqual(origin + Vector3Int.right * 8, second.Cell);
        for (int i = 0; i < equation.Length; i++)
        {
            if (i == 6 || i == 8) continue;
            PlaceSymbol(validator, player.Board, origin + Vector3Int.right * i, equation[i]);
        }
        Assert.IsTrue(validator.ValidateEquations());
        Assert.That(validator.Results.Any(result => result.IsTrue && result.Expression == equation));
        yield return Until(() => VillageDialogue.IsVisible && text.text != hint, "The protagonist speaks after solving the equation");
        Assert.IsTrue(gate.IsOpen);
        Assert.IsTrue(player.IsMovementLocked);
        Assert.AreEqual("Sempre me perguntei qual era minha utilidade. Acho que finalmente encontrei meu propósito.", text.text);
        Assert.IsFalse(Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None).Any(group => group.name == "Ending Fade"));
        yield return Until(() => Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None)
            .Any(group => group.name == "Ending Fade" && group.alpha > 0f && group.alpha < 1f),
            "The ending fades to black");
        yield return Until(() => SceneManager.GetActiveScene().name == "GameOver", "The game over screen loads");
        Assert.That(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(label => label.text == "Fim de\nJogo"));
        var replay = Object.FindFirstObjectByType<Button>();
        Assert.AreEqual("Jogar Novamente", replay.GetComponentInChildren<Text>().text);
        yield return Click(replay);
        yield return Until(() => SceneManager.GetActiveScene().name == "Title", "Replay returns to the title screen");
        var play = Object.FindFirstObjectByType<Button>();
        Assert.AreEqual("Jogar", play.GetComponentInChildren<Text>().text);
        yield return Click(play);
        yield return Until(() => SceneManager.GetActiveScene().name == "G1-Fase_1", "Jogar starts a new game");
        Assert.IsFalse(Object.FindFirstObjectByType<VillageSequence>().HasEscaped);
    }

    private static GridPushBlock PlaceSymbol(EquationValidator validator, LevelGrid board, Vector3Int cell, char value)
    {
        var actor = new GameObject("Symbol " + value);
        actor.SetActive(false);
        actor.transform.SetParent(validator.transform);
        actor.transform.position = board.CenterOf(cell);
        var block = actor.AddComponent<GridPushBlock>();
        block.Configure(board);
        block.Value = value;
        block.IsMovable = false;
        actor.SetActive(true);
        return block;
    }

    private static IEnumerator Click(Button button)
    {
        yield return null;
        var canvas = button.GetComponentInParent<Canvas>();
        Canvas.ForceUpdateCanvases();
        Vector2 position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, button.transform.position);
        var events = EventSystem.current;
        Assert.IsNotNull(events);

        // Simulated devices must work when the Play Mode test runner is in a hidden window.
        var previousBehavior = InputSystem.settings.backgroundBehavior;
        var previousRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            yield return null;
            var input = events.currentInputModule as InputSystemUIInputModule;
            Assert.IsNotNull(input);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            Assert.AreEqual(position, mouse.position.ReadValue());
            Assert.AreEqual(button.gameObject, input.GetLastRaycastResult(mouse.deviceId).gameObject);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
            yield return null;
            Assert.IsTrue(mouse.leftButton.isPressed);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.editorInputBehaviorInPlayMode = previousRouting;
            InputSystem.settings.backgroundBehavior = previousBehavior;
        }
    }

    private static IEnumerator Until(Func<bool> condition, string message)
    {
        float deadline = Time.realtimeSinceStartup + 6f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), message);
    }
}
#endif
