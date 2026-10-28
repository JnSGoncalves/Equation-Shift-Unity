using System;
using System.Linq;
using EquationShift;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class GateExampleSetup
{
    private const string ScenePath = "Assets/Scenes/00-FaseExemplo_1.unity";
    private const string AnimationFolder = "Assets/Animations/Gates";
    private const string PrefabFolder = "Assets/Prefabs/Gates";

    [MenuItem("Equation Shift/Configurar portoes da Fase Exemplo")]
    public static void ConfigureSavedExample()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Saia do Play Mode antes de configurar os portoes.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath);
        EnsureFolder(AnimationFolder);
        EnsureFolder(PrefabFolder);

        var board = Object.FindFirstObjectByType<LevelGrid>();
        var validator = board.GetComponentInChildren<EquationValidator>();
        var player = board.GetComponentInChildren<TopDownGridPlayer>();
        var camera = Camera.main;
        if (validator == null || player == null || camera == null)
            throw new InvalidOperationException("A cena precisa de LevelGrid, EquationValidator, Player e Main Camera.");
        var feedback = camera.GetComponent<GateOpeningFeedback>();
        if (feedback == null) feedback = camera.gameObject.AddComponent<GateOpeningFeedback>();
        feedback.Configure(player);
        EditorUtility.SetDirty(feedback);

        ConfigureGate("Gate_Vertical", "block_2_Ancor_L", AncorSide.L,
            "gate-vertical-table-20-60.png", new Vector2(1f, 3f), validator, feedback);
        ConfigureGate("Gate_Horizontal", "block_0_Ancor_D", AncorSide.D,
            "gate-horizontal-table-60x20.png", new Vector2(3f, 1f), validator, feedback);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Equation Shift: ancoras, portoes, animacoes e tremores configurados na fase exemplo.");
    }

    private static void ConfigureGate(string gateName, string anchorName, AncorSide side,
        string textureName, Vector2 size, EquationValidator validator, GateOpeningFeedback feedback)
    {
        var actor = GameObject.Find(gateName);
        var anchorObject = GameObject.Find(anchorName);
        if (actor == null || anchorObject == null)
            throw new InvalidOperationException("Salve os objetos " + gateName + " e " + anchorName + " na cena antes de configurar.");
        var anchor = anchorObject.GetComponent<GridPushBlock>();
        if (anchor == null) throw new InvalidOperationException(anchorName + " precisa de GridPushBlock.");
        anchor.IsAncor = true;
        anchor.AncorSide = side;
        anchor.IsMovable = false;
        anchor.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        EditorUtility.SetDirty(anchor);
        PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
        PrefabUtility.RecordPrefabInstancePropertyModifications(anchor.GetComponent<Rigidbody2D>());

        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath("Assets/Texture/animations/" + textureName)
            .OfType<Sprite>().OrderBy(sprite => sprite.rect.x).ToArray();
        if (frames.Length != 8) throw new InvalidOperationException(textureName + ": esperados 8 quadros de abertura.");
        var closed = SaveClip(gateName + "_Closed", new[] { frames[0] });
        var opening = SaveClip(gateName + "_Opening", frames);
        var open = SaveClip(gateName + "_Open", new[] { frames[frames.Length - 1] });
        var closing = SaveClip(gateName + "_Closing", frames.Reverse().ToArray());
        var controller = SaveController(gateName, closed, opening, open, closing);

        var renderer = actor.GetComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        var animator = actor.GetComponent<Animator>();
        if (animator == null) animator = actor.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.enabled = true;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var shape = actor.GetComponent<BoxCollider2D>();
        if (shape == null) shape = actor.AddComponent<BoxCollider2D>();
        shape.size = size;
        shape.offset = Vector2.zero;
        shape.isTrigger = false;
        shape.enabled = true;
        var gate = actor.GetComponent<EquationGate>();
        if (gate == null) gate = actor.AddComponent<EquationGate>();

        // Save a reusable asset without scene references, then restore this instance's wiring.
        Vector3 position = actor.transform.localPosition;
        try
        {
            actor.transform.localPosition = Vector3.zero;
            gate.Configure(null, null, side, null, opening, closing);
            PrefabUtility.SaveAsPrefabAssetAndConnect(actor, PrefabFolder + "/" + gateName + ".prefab",
                InteractionMode.AutomatedAction);
        }
        finally
        {
            actor.transform.localPosition = position;
            gate.Configure(validator, anchor, side, feedback, opening, closing);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
        }
    }

    private static AnimationClip SaveClip(string name, Sprite[] frames)
    {
        string path = AnimationFolder + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = 8f;
        clip.wrapMode = WrapMode.ClampForever;
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / clip.frameRate, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe
            { time = frames.Length / clip.frameRate, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController SaveController(string name, AnimationClip closed, AnimationClip opening, AnimationClip open, AnimationClip closing)
    {
        string path = AnimationFolder + "/" + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        foreach (var entry in new[] { ("Closed", closed), ("Opening", opening), ("Open", open), ("Closing", closing) })
        {
            var state = machine.states.Select(child => child.state).FirstOrDefault(s => s.name == entry.Item1);
            if (state == null) state = machine.AddState(entry.Item1);
            state.motion = entry.Item2;
            state.writeDefaultValues = false;
            if (entry.Item1 == "Closed") machine.defaultState = state;
        }
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
