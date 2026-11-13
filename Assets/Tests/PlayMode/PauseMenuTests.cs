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

public class PauseMenuTests
{
    private const string Warning = "Tem certeza? Você perderá seu progresso até o momento e terá que recomeçar o jogo.";
    private Keyboard keyboard;
    private Mouse mouse;
    private InputSettings.BackgroundBehavior backgroundBehavior;
    private InputSettings.EditorInputBehaviorInPlayMode inputRouting;

    [SetUp]
    public void SetUp()
    {
        backgroundBehavior = InputSystem.settings.backgroundBehavior;
        inputRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
    }

    [TearDown]
    public void TearDown()
    {
        var menu = Object.FindFirstObjectByType<PauseMenu>();
        if (menu != null) menu.enabled = false;
        InputSystem.RemoveDevice(keyboard);
        InputSystem.RemoveDevice(mouse);
        InputSystem.settings.backgroundBehavior = backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = inputRouting;
    }

    [UnityTest]
    public IEnumerator EscapeFreezesTheRoomAndResetUsesTheRoomCheckpoint()
    {
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/G3-Fase_3.unity");
        yield return null;
        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        Vector3Int checkpoint = player.Cell;
        player.UnlockForm(PlayerForm.Equal);
        player.CurrentForm = PlayerForm.Equal;
        Vector2Int direction = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down }
            .First(side => player.CanStandAt(checkpoint + (Vector3Int)side));
        Assert.IsTrue(player.TryMove(direction));

        yield return Press(Key.Escape);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(AudioListener.pause);
        Assert.IsTrue(player.IsMovementLocked);
        Assert.IsTrue(player.IsMoving);
        Vector3 pausedPosition = player.transform.position;
        yield return Press(Key.Tab);
        yield return new WaitForSecondsRealtime(.25f);
        Assert.AreEqual(pausedPosition, player.transform.position);
        Assert.AreEqual(PlayerForm.Equal, player.CurrentForm);

        yield return Click("Continuar");
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(AudioListener.pause);
        yield return Until(() => !player.IsMoving);
        Assert.AreEqual(checkpoint + (Vector3Int)direction, player.Cell);

        yield return Press(Key.Escape);
        yield return Click("Resetar fase");
        Assert.AreEqual(checkpoint, player.Cell);
        Assert.AreEqual(PlayerForm.Equal, player.CurrentForm);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator ConfirmationsCanBeCanceledAndReturningToTitleStartsAFreshGame()
    {
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/G1-Fase_1.unity");
        yield return null;
        var player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        player.UnlockForm(PlayerForm.Plus);
        player.CurrentForm = PlayerForm.Plus;
        yield return Press(Key.Escape);

        yield return Click("Sair do Jogo");
        AssertWarning();
        yield return Press(Key.Escape);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsNotNull(FindButton("Continuar"));

        yield return Click("Voltar ao menu");
        AssertWarning();
        yield return Click("Cancelar");
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Plus));
        Assert.AreEqual("G1-Fase_1", SceneManager.GetActiveScene().name);
        Assert.AreEqual(0f, Time.timeScale);

        yield return Click("Voltar ao menu");
        yield return Click("Confirmar");
        yield return Until(() => SceneManager.GetActiveScene().name == "Title");
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsFalse(AudioListener.pause);
        Assert.IsNull(Object.FindFirstObjectByType<PauseMenu>());

        yield return Click("Jogar");
        yield return Until(() => SceneManager.GetActiveScene().name == "G1-Fase_1");
        player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        Assert.IsFalse(player.IsFormUnlocked(PlayerForm.Plus));
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
    }

    [UnityTest]
    public IEnumerator GateAnimationAndDialogueWaitForResume()
    {
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/G1-Fase_1.unity");
        yield return null;
        var gate = Object.FindFirstObjectByType<EquationGate>();
        var animator = gate.GetComponent<Animator>();
        gate.SetOpen(true);
        Assert.IsTrue(gate.IsOpening);
        Assert.IsTrue(VillageDialogue.IsVisible);

        yield return Press(Key.Escape);
        float frame = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return new WaitForSecondsRealtime(4.1f);
        Assert.AreEqual(frame, animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        Assert.IsTrue(gate.IsOpening);
        Assert.IsTrue(VillageDialogue.IsVisible);

        yield return Press(Key.Escape);
        yield return Until(() => !gate.IsOpening);
        Assert.IsFalse(gate.GetComponent<Collider2D>().enabled);
    }

    private static void AssertWarning()
    {
        var view = Object.FindFirstObjectByType<PauseMenuView>();
        Assert.That(view.GetComponentsInChildren<Text>().Any(label => label.text == Warning));
        Assert.IsNull(FindButton("Continuar"));
        Assert.AreEqual("Cancelar", EventSystem.current.currentSelectedGameObject.name);
    }

    private static Button FindButton(string caption) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
        .SingleOrDefault(button => button.GetComponentInChildren<Text>().text == caption);

    private IEnumerator Press(Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private IEnumerator Click(string caption)
    {
        yield return null;
        var button = FindButton(caption);
        Assert.IsNotNull(button, caption);
        var canvas = button.GetComponentInParent<Canvas>();
        Canvas.ForceUpdateCanvases();
        Vector2 position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, button.transform.position);
        var input = EventSystem.current.currentInputModule as InputSystemUIInputModule;
        Assert.IsNotNull(input);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return null;
        Assert.AreEqual(button.gameObject, input.GetLastRaycastResult(mouse.deviceId).gameObject);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        yield return null;
    }

    private static IEnumerator Until(Func<bool> condition)
    {
        yield return new WaitUntil(condition, TimeSpan.FromSeconds(6), () => Assert.Fail("Operation timed out."));
    }
}
#endif
