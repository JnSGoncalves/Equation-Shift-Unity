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

public class AbilityPickupTests
{
    private static readonly PlayerForm[] AtlasForms =
        { PlayerForm.Equal, PlayerForm.Minus, PlayerForm.Plus, PlayerForm.Mult, PlayerForm.Divis };
    private GameObject root;
    private LevelGrid board;
    private TopDownGridPlayer player;
    private Transform interactables;
    private Tile tile;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Ability test grid", typeof(Grid));
        root.transform.position = new Vector3(6000f, 6000f, 0f);
        board = root.AddComponent<LevelGrid>();
        var ground = Child("Ground").AddComponent<Tilemap>();
        var walls = Child("Walls").AddComponent<Tilemap>();
        board.Configure(ground, walls);
        tile = ScriptableObject.CreateInstance<Tile>();
        for (int x = -3; x <= 6; x++)
            for (int y = -3; y <= 6; y++) ground.SetTile(new Vector3Int(x, y, 0), tile);
        interactables = Child("Interactables").transform;
        var actor = Child("Player");
        actor.SetActive(false);
        actor.transform.position = board.CenterOf(Vector3Int.zero);
        actor.AddComponent<SpriteRenderer>().sortingOrder = 7;
        actor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        actor.AddComponent<BoxCollider2D>().size = Vector2.one * 0.9f;
        player = actor.AddComponent<TopDownGridPlayer>();
        player.Configure(board);
        actor.SetActive(true);
    }

    private GameObject Child(string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        return child;
    }

    private AbilityPickup AddPickup(PlayerForm form, Vector3Int cell)
    {
        var actor = Child("habilitie_" + form.ToString().ToLowerInvariant());
        actor.SetActive(false);
        actor.transform.SetParent(interactables, true);
        actor.transform.position = board.CenterOf(cell);
        var pickup = actor.AddComponent<AbilityPickup>();
        pickup.Configure(board, form, player);
        string name = "habilitie_" + form.ToString().ToLowerInvariant() + "_idle";
        actor.GetComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/Abilities/" + name + ".controller");
        int firstFrame = System.Array.IndexOf(AtlasForms, form) * 4;
        actor.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAllAssetsAtPath(
            "Assets/Texture/animations/habilities_80x100.png").OfType<Sprite>()
            .Single(sprite => sprite.name == "habilities_80x100_" + firstFrame);
        actor.SetActive(true);
        return pickup;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(tile);
    }

    [UnityTest]
    public IEnumerator CollectingEachItemUnlocksAndSelectsOnlyAfterAnimationCompletes(
        [ValueSource(nameof(AtlasForms))] PlayerForm form)
    {
        var pickup = AddPickup(form, player.Cell);
        var removalProbe = pickup.gameObject.AddComponent<PickupRemovalProbe>();
        PlayerForm formWhenHidden = (PlayerForm)(-1);
        PlayerForm formWhenDestroyed = (PlayerForm)(-1);
        bool unlockedWhenHidden = true;
        bool unlockedWhenDestroyed = true;
        removalProbe.Disabled = () =>
        {
            formWhenHidden = player.CurrentForm;
            unlockedWhenHidden = player.IsFormUnlocked(form);
        };
        removalProbe.Destroyed = () =>
        {
            formWhenDestroyed = player.CurrentForm;
            unlockedWhenDestroyed = player.IsFormUnlocked(form);
        };
        Assert.IsEmpty(player.UnlockedForms);
        Assert.IsTrue(pickup.TryCollect(player));
        Assert.IsTrue(pickup.IsCollected);
        Assert.IsFalse(player.IsFormUnlocked(form));
        Assert.IsEmpty(player.UnlockedForms);
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        Assert.IsFalse(pickup.TryCollect(player));
        Assert.That(pickup.transform.position.y - player.transform.position.y, Is.EqualTo(1f).Within(0.001f));
        Assert.AreEqual(8, pickup.GetComponent<SpriteRenderer>().sortingOrder);
        player.CycleForm();
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm, "Tab cannot select the new form before collection finishes");
        yield return new WaitForSeconds(pickup.CollectionDuration * 0.4f);
        Assert.IsFalse(player.IsFormUnlocked(form));
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm, "The form stays unchanged during the animation");
        yield return new WaitForSeconds(pickup.CollectionDuration * 0.7f);
        Assert.IsTrue(pickup == null);
        Assert.AreEqual(PlayerForm.Default, formWhenHidden, "The item must disappear before the player transforms");
        Assert.IsFalse(unlockedWhenHidden);
        Assert.AreEqual(PlayerForm.Default, formWhenDestroyed, "Removal must finish before the form is granted");
        Assert.IsFalse(unlockedWhenDestroyed);
        Assert.IsTrue(player.IsFormUnlocked(form));
        Assert.AreEqual(1, player.UnlockedForms.Count);
        Assert.AreEqual(form, player.CurrentForm);
        player.CycleForm();
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        player.CycleForm();
        Assert.AreEqual(form, player.CurrentForm);
        foreach (var other in AtlasForms)
            Assert.AreEqual(other == form, player.IsFormUnlocked(other));
    }

    [UnityTest]
    public IEnumerator DuplicateItemsDoNotDuplicateTheUnlockedForm()
    {
        var first = AddPickup(PlayerForm.Plus, player.Cell);
        Assert.IsTrue(first.TryCollect(player));
        yield return new WaitForSeconds(first.CollectionDuration + 0.1f);
        player.CurrentForm = PlayerForm.Default;
        var second = AddPickup(PlayerForm.Plus, player.Cell);
        Assert.IsTrue(second.TryCollect(player));
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        yield return new WaitForSeconds(second.CollectionDuration + 0.1f);
        Assert.AreEqual(PlayerForm.Plus, player.CurrentForm, "A duplicate selects its form after the animation");
        Assert.AreEqual(1, player.UnlockedForms.Count);
    }

    [Test]
    public void PickupCannotBeCollectedByAPlayerFromAnotherGrid()
    {
        var pickup = AddPickup(PlayerForm.Equal, player.Cell);
        var otherBoard = Child("Other grid").AddComponent<LevelGrid>();
        otherBoard.Configure(board.Ground, board.Walls);
        player.Configure(otherBoard);
        Assert.IsFalse(pickup.TryCollect(player));
        Assert.IsFalse(pickup.IsCollected);
        Assert.IsEmpty(player.UnlockedForms);
    }

    [Test]
    public void CollectionHeightUsesOneGridCellInsteadOfOneWorldUnit()
    {
        root.GetComponent<Grid>().cellSize = new Vector3(2f, 3f, 1f);
        player.transform.position = board.CenterOf(player.Cell);
        var pickup = AddPickup(PlayerForm.Equal, player.Cell);
        Assert.IsTrue(pickup.TryCollect(player));
        Assert.That(pickup.transform.position.y - player.transform.position.y, Is.EqualTo(3f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator EnteringTheItemCellCollectsOnlyAfterTheStepCompletes()
    {
        var pickup = AddPickup(PlayerForm.Mult, Vector3Int.right);
        Physics2D.SyncTransforms();
        Assert.IsFalse(pickup.TryCollect(player));
        Assert.IsTrue(player.TryMove(Vector2Int.right), "The pickup must not block its cell");
        Assert.IsFalse(pickup.TryCollect(player));
        Assert.IsEmpty(player.UnlockedForms);
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(Vector3Int.right, player.Cell);
        Assert.IsTrue(pickup.IsCollected);
        Assert.IsFalse(player.IsFormUnlocked(PlayerForm.Mult));
        Assert.AreEqual(PlayerForm.Default, player.CurrentForm);
        yield return new WaitForSeconds(pickup.CollectionDuration + 0.1f);
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Mult));
        Assert.AreEqual(PlayerForm.Mult, player.CurrentForm);
    }

    [UnityTest]
    public IEnumerator CollectionDescendsFadesFollowsThePlayerAndRemovesTheItem()
    {
        player.UnlockForm(PlayerForm.Plus);
        player.CurrentForm = PlayerForm.Plus;
        var pickup = AddPickup(PlayerForm.Divis, player.Cell);
        var renderer = pickup.GetComponent<SpriteRenderer>();
        Assert.IsTrue(pickup.TryCollect(player));
        float top = pickup.transform.position.y;
        yield return new WaitForSeconds(0.2f);
        Assert.That(pickup.transform.position.y, Is.LessThan(top).And.GreaterThan(player.transform.position.y));
        Assert.That(renderer.color.a, Is.GreaterThan(0f).And.LessThan(1f));
        Assert.AreEqual(PlayerForm.Plus, player.CurrentForm);
        Assert.IsFalse(player.IsFormUnlocked(PlayerForm.Divis));
        Physics2D.SyncTransforms();
        Assert.IsTrue(player.TryMove(Vector2Int.right));
        yield return new WaitForSeconds(0.25f);
        Assert.That(pickup.transform.position.x, Is.EqualTo(player.transform.position.x).Within(0.001f));
        yield return new WaitForSeconds(pickup.CollectionDuration + 0.1f);
        Assert.IsTrue(pickup == null, "The collected object is removed after becoming transparent");
        Assert.IsTrue(player.IsFormUnlocked(PlayerForm.Divis));
        Assert.AreEqual(PlayerForm.Divis, player.CurrentForm);
    }

    [Test]
    public void EveryPickupLoopsItsFourAtlasFramesInTheAuthoredOrder()
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Texture/animations/habilities_80x100.png")
            .OfType<Sprite>().ToDictionary(sprite => sprite.name);
        for (int formIndex = 0; formIndex < AtlasForms.Length; formIndex++)
        {
            var pickup = AddPickup(AtlasForms[formIndex], new Vector3Int(4, formIndex, 0));
            var animator = pickup.GetComponent<Animator>();
            string state = "Base Layer.habilitie_" + AtlasForms[formIndex].ToString().ToLowerInvariant() + "_idle";
            animator.Play(state, 0, 0f);
            animator.Update(0f);
            var clip = animator.GetCurrentAnimatorClipInfo(0).Single().clip;
            Assert.IsTrue(clip.isLooping);
            Assert.AreEqual(12f, clip.frameRate);
            Assert.That(clip.length, Is.EqualTo(4f / 12f).Within(0.0001f));
            for (int frame = 0; frame < 4; frame++)
            {
                Assert.AreSame(sprites["habilities_80x100_" + (formIndex * 4 + frame)],
                    pickup.GetComponent<SpriteRenderer>().sprite);
                animator.Update(1f / 12f + 0.0001f);
            }
            Assert.AreSame(sprites["habilities_80x100_" + (formIndex * 4)], pickup.GetComponent<SpriteRenderer>().sprite);
        }
    }

    [UnityTest]
    public IEnumerator ExampleSceneHasFiveAnimatedPickupsOnReachableCells()
    {
        Object.DestroyImmediate(root);
        root = null;
        yield return SceneManager.LoadSceneAsync("00-FaseExemplo_1");
        yield return null;
        var sceneBoard = Object.FindFirstObjectByType<LevelGrid>();
        var scenePlayer = sceneBoard.GetComponentInChildren<TopDownGridPlayer>();
        var sceneRoot = sceneBoard.transform.Find("Interactables");
        Assert.IsNotNull(sceneRoot);
        var pickups = sceneRoot.GetComponentsInChildren<AbilityPickup>();
        Assert.AreEqual(5, pickups.Length);
        CollectionAssert.AreEquivalent(AtlasForms, pickups.Select(pickup => pickup.Form));
        foreach (var pickup in pickups)
        {
            Assert.IsNotNull(pickup.GetComponent<Animator>().runtimeAnimatorController);
            Vector3Int cell = sceneBoard.CellAt(pickup.transform.position);
            Assert.IsTrue(sceneBoard.IsFloor(cell), pickup.name + " needs a floor cell");
            Assert.IsFalse(sceneBoard.HasObstacle(cell, scenePlayer.GetComponent<Collider2D>()),
                pickup.name + " must have an unblocked cell");
        }
        Assert.AreEqual(PlayerForm.Default, scenePlayer.CurrentForm);
        Assert.IsEmpty(scenePlayer.UnlockedForms);
    }
}

public sealed class PickupRemovalProbe : MonoBehaviour
{
    public System.Action Disabled;
    public System.Action Destroyed;

    private void OnDisable() => Disabled?.Invoke();
    private void OnDestroy() => Destroyed?.Invoke();
}
#endif
