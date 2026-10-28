using System;
using System.Collections.Generic;
using System.Linq;
using EquationShift;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AbilityPickupSetup
{
    private const string AnimationFolder = "Assets/Animations/Abilities";
    private const string SpritePath = "Assets/Texture/animations/habilities_80x100.png";

    [MenuItem("Equation Shift/Configurar coletaveis de habilidades")]
    public static void ConfigureCurrentScene()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Saia do Play Mode antes de configurar os coletaveis.");
        var scene = SceneManager.GetActiveScene();
        var boards = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LevelGrid>(true)).ToArray();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SpritePath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        var forms = new[] { PlayerForm.Equal, PlayerForm.Minus, PlayerForm.Plus, PlayerForm.Mult, PlayerForm.Divis };
        var controllers = new Dictionary<PlayerForm, AnimatorController>();
        if (!AssetDatabase.IsValidFolder(AnimationFolder))
            AssetDatabase.CreateFolder("Assets/Animations", "Abilities");
        for (int i = 0; i < forms.Length; i++)
        {
            var frames = Enumerable.Range(i * 4, 4).Select(index => sprites["habilities_80x100_" + index]).ToArray();
            string name = "habilitie_" + forms[i].ToString().ToLowerInvariant() + "_idle";
            var clip = SaveClip(name, frames);
            controllers.Add(forms[i], SaveController(name, clip));
        }

        int count = 0;
        foreach (var board in boards)
        {
            var root = board.transform.Find("Interactables");
            if (root == null) continue;
            var player = board.GetComponentInChildren<TopDownGridPlayer>(true);
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.StartsWith("habilitie_", StringComparison.OrdinalIgnoreCase)) continue;
                if (!TryReadForm(child.name.Substring("habilitie_".Length), out var form))
                    throw new InvalidOperationException("Nome de coletavel desconhecido: " + child.name);
                var pickup = child.GetComponent<AbilityPickup>();
                if (pickup == null) pickup = child.gameObject.AddComponent<AbilityPickup>();
                pickup.Configure(board, form, player);
                var renderer = child.GetComponent<SpriteRenderer>();
                renderer.sprite = sprites["habilities_80x100_" + (Array.IndexOf(forms, form) * 4)];
                var animator = child.GetComponent<Animator>();
                animator.runtimeAnimatorController = controllers[form];
                foreach (var collider in child.GetComponents<Collider2D>()) collider.isTrigger = true;
                EditorUtility.SetDirty(pickup);
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(animator);
                count++;
            }
        }
        if (count == 0) throw new InvalidOperationException("Adicione objetos habilitie_<forma> em Grid/Interactables.");
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Equation Shift: " + count + " coletaveis de habilidades configurados; posicoes preservadas.");
    }

    private static bool TryReadForm(string token, out PlayerForm form)
    {
        switch (token.ToLowerInvariant())
        {
            case "equal": case "=": form = PlayerForm.Equal; return true;
            case "minus": case "-": form = PlayerForm.Minus; return true;
            case "plus": case "+": form = PlayerForm.Plus; return true;
            case "mult": case "*": form = PlayerForm.Mult; return true;
            case "divis": case "/": form = PlayerForm.Divis; return true;
            default: form = PlayerForm.Default; return false;
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
        clip.frameRate = 12f;
        var keys = frames.Select((frame, index) => new ObjectReferenceKeyframe
            { time = index / clip.frameRate, value = frame }).ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.stopTime = frames.Length / clip.frameRate;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController SaveController(string name, AnimationClip clip)
    {
        string path = AnimationFolder + "/" + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(child => child.state).FirstOrDefault(candidate => candidate.name == name);
        if (state == null) state = machine.AddState(name);
        state.motion = clip;
        state.writeDefaultValues = false;
        machine.defaultState = state;
        EditorUtility.SetDirty(controller);
        return controller;
    }
}
