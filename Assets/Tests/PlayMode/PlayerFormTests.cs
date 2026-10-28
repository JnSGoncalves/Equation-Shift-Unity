#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using EquationShift;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PlayerFormTests
{
    private TopDownGridPlayer player;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Keyboard keyboard;
    private InputSettings inputSettings;
    private InputSettings.BackgroundBehavior previousBackgroundBehavior;
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInputBehavior;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("00-FaseExemplo_1");
        yield return null;
        player = Object.FindFirstObjectByType<TopDownGridPlayer>();
        animator = player.GetComponent<Animator>();
        spriteRenderer = player.GetComponent<SpriteRenderer>();
    }

    [TearDown]
    public void TearDown()
    {
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        keyboard = null;
        if (inputSettings == null) return;
        inputSettings.backgroundBehavior = previousBackgroundBehavior;
        inputSettings.editorInputBehaviorInPlayMode = previousEditorInputBehavior;
        inputSettings = null;
    }

    [Test]
    public void PlayerStartsWithOnlyDefaultAndCannotSelectLockedForms()
    {
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        Assert.IsEmpty(player.UnlockedForms);
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Default));
        foreach (var form in new[] { PlayerForm.Equal, PlayerForm.Plus, PlayerForm.Minus, PlayerForm.Mult, PlayerForm.Divis })
        {
            Assert.IsFalse(player.IsFormUnlocked(form));
            player.CurrentForm = form;
            Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        }
        player.CycleForm();
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        Assert.IsFalse(player.TryGetEquationSymbol(out _));
    }

    [Test]
    public void CycleSkipsLockedFormsAndUnlocksSurviveReactivation()
    {
        Assert.IsTrue(player.UnlockForm(PlayerForm.Minus));
        Assert.IsTrue(player.UnlockForm(PlayerForm.Equal));
        Assert.IsFalse(player.UnlockForm(PlayerForm.Equal));
        Assert.IsFalse(player.UnlockForm(PlayerForm.Default));
        Assert.AreEqual(2, player.UnlockedForms.Count);
        player.CycleForm();
        Assert.AreEqual(PlayerForm.Equal, player.CurrentForm);
        player.CycleForm();
        Assert.AreEqual(PlayerForm.Minus, player.CurrentForm);
        player.CycleForm();
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        player.gameObject.SetActive(false);
        player.gameObject.SetActive(true);
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Equal));
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Minus));
    }

    [UnityTest]
    public IEnumerator InspectorCannotBypassALockedForm()
    {
        var serialized = new SerializedObject(player);
        serialized.FindProperty("currentForm").enumValueIndex = (int)PlayerForm.Plus;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.IsFalse(player.TryGetEquationSymbol(out _));
        yield return null;
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.player_default_idle"));
    }

    [Test]
    public void EveryFormPlaysItsFourSlicedSpritesAndLoops()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(
            "Assets/Texture/animations/characters/player_animations_80x100.png")
            .OfType<Sprite>().ToDictionary(sprite => sprite.name);
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        var forms = new[] { PlayerForm.Default, PlayerForm.Equal, PlayerForm.Plus,
            PlayerForm.Minus, PlayerForm.Mult, PlayerForm.Divis };
        foreach (var form in forms) player.UnlockForm(form);
        player.CurrentForm = PlayerForm.Divis;

        for (int formIndex = 0; formIndex < forms.Length; formIndex++)
        {
            var form = forms[formIndex];
            player.CurrentForm = form;
            string state = "Base Layer.player_" + form.ToString().ToLowerInvariant() + "_idle";
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(state), state);
            var clip = animator.GetCurrentAnimatorClipInfo(0).Single().clip;
            Assert.IsTrue(clip.isLooping, clip.name);
            Assert.AreEqual(12f, clip.frameRate, clip.name);
            Assert.That(clip.length, Is.EqualTo(4f / 12f).Within(0.0001f), clip.name);

            for (int frame = 0; frame < 4; frame++)
            {
                Assert.AreSame(sprites["player_animations_80x100_" + (formIndex * 4 + frame)],
                    spriteRenderer.sprite, form + " frame " + frame);
                animator.Update(1f / 12f + 0.0001f);
            }
            Assert.AreSame(sprites["player_animations_80x100_" + (formIndex * 4)],
                spriteRenderer.sprite, form + " loops back to its first frame");
        }
    }

    [UnityTest]
    public IEnumerator SerializedFormAppliesOnEnableAndInspectorChangesApplyDuringPlay()
    {
        player.UnlockForm(PlayerForm.Mult);
        player.UnlockForm(PlayerForm.Divis);
        player.gameObject.SetActive(false);
        var serialized = new SerializedObject(player);
        serialized.FindProperty("currentForm").enumValueIndex = (int)PlayerForm.Mult;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        player.gameObject.SetActive(true);
        Assert.AreEqual(PlayerForm.Mult, player.CurrentForm);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.player_mult_idle"));
        Assert.AreEqual("player_animations_80x100_16", spriteRenderer.sprite.name);

        serialized.Update();
        serialized.FindProperty("currentForm").enumValueIndex = (int)PlayerForm.Divis;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        yield return null;
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.player_divis_idle"));
        CollectionAssert.Contains(new[] { "player_animations_80x100_20", "player_animations_80x100_21",
            "player_animations_80x100_22", "player_animations_80x100_23" }, spriteRenderer.sprite.name);

        player.gameObject.SetActive(false);
        player.gameObject.SetActive(true);
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.player_divis_idle"));
        Assert.AreEqual("player_animations_80x100_20", spriteRenderer.sprite.name);
    }

    [UnityTest]
    public IEnumerator TabCyclesOncePerPressAndWrapsToDefault()
    {
        // Batch tests have no focused Game View; route the simulated keyboard to the player.
        inputSettings = InputSystem.settings;
        previousBackgroundBehavior = inputSettings.backgroundBehavior;
        previousEditorInputBehavior = inputSettings.editorInputBehaviorInPlayMode;
        inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        inputSettings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        var expectedForms = new[] { PlayerForm.Equal, PlayerForm.Plus, PlayerForm.Minus,
            PlayerForm.Mult, PlayerForm.Divis, PlayerForm.Default };
        foreach (var form in expectedForms) player.UnlockForm(form);
        foreach (var expected in expectedForms)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            yield return null;
            Assert.IsTrue(keyboard.tabKey.isPressed, "The simulated Tab press must reach the game");
            Assert.AreEqual(expected, player.CurrentForm);
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(
                "Base Layer.player_" + expected.ToString().ToLowerInvariant() + "_idle"));
            yield return null;
            Assert.AreEqual(expected, player.CurrentForm, "Holding Tab must not repeat the switch");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }
    }
}
#endif
