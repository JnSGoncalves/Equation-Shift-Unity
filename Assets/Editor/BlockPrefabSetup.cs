using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EquationShift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Builds reusable block assets from the authored sprites in the two block groups.</summary>
public static class BlockPrefabSetup
{
    private const string PrefabRoot = "Assets/Prefabs/Blocks";

    [MenuItem("Equation Shift/Padronizar blocos e criar prefabs")]
    public static void ConfigureAllScenes()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var previousScenes = EditorSceneManager.GetSceneManagerSetup();
        var spritesByPrefab = new Dictionary<string, Sprite>();
        int total = 0;
        EnsureTag("StaticBlock");
        EnsureTag("MovableBlock");
        EnsureFolder(PrefabRoot + "/Static");
        EnsureFolder(PrefabRoot + "/Movable");

        try
        {
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" })
                         .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p))
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Transform[] transforms = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                Transform[] groups = transforms.Where(t => t.name == "StaticBlocks" || t.name == "MovableBlocks").ToArray();
                if (groups.Length == 0) continue;
                LevelGrid[] boards = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<LevelGrid>(true)).ToArray();
                if (boards.Length != 1)
                    throw new InvalidOperationException(path + ": configure exatamente um LevelGrid antes de gerar os blocos. Encontrados: " + boards.Length);
                LevelGrid board = boards[0];
                Transform blocksRoot = board.transform.Find("Blocks");
                if (blocksRoot == null)
                {
                    blocksRoot = new GameObject("Blocks").transform;
                    blocksRoot.SetParent(board.transform, false);
                }

                // Snapshot before changing the hierarchy or replacing a prefab connection.
                var entries = new List<BlockEntry>();
                foreach (Transform group in groups)
                {
                    bool movable = group.name == "MovableBlocks";
                    foreach (Transform child in group.GetComponentsInChildren<Transform>(true))
                    {
                        if (child == group || !child.name.StartsWith("block_", StringComparison.OrdinalIgnoreCase)) continue;
                        if (!TryReadValue(child.name, out char value, out string token))
                            throw new InvalidOperationException("Nome de bloco desconhecido: " + child.name);
                        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                        if (renderer == null || renderer.sprite == null)
                            throw new InvalidOperationException(child.name + ": SpriteRenderer precisa de um sprite.");
                        string type = movable ? "Movable" : "Static";
                        string prefabName = "Block_" + type + "_" + token;
                        string assetPath = PrefabRoot + "/" + type + "/" + prefabName + ".prefab";
                        if (spritesByPrefab.TryGetValue(assetPath, out Sprite sprite) && sprite != renderer.sprite)
                            throw new InvalidOperationException("Sprites diferentes para o mesmo prefab: " + assetPath);
                        spritesByPrefab[assetPath] = renderer.sprite;
                        entries.Add(new BlockEntry(child.gameObject, movable, value, prefabName, assetPath));
                    }
                }
                if (entries.Count == 0) throw new InvalidOperationException(path + ": nenhum filho block_<valor> encontrado.");

                // Both collision lookup and equation validation search under Grid/Blocks.
                foreach (Transform group in groups) group.SetParent(blocksRoot, true);
                var validator = blocksRoot.GetComponent<EquationValidator>();
                if (validator == null) validator = blocksRoot.gameObject.AddComponent<EquationValidator>();
                validator.Configure(board, blocksRoot);

                foreach (BlockEntry entry in entries) ConfigureAndConnect(entry, board);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                total += entries.Count;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Equation Shift: " + total + " blocos configurados; " + spritesByPrefab.Count + " prefabs criados/atualizados.");
        }
        finally
        {
            if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
        }
    }

    private static void ConfigureAndConnect(BlockEntry entry, LevelGrid board)
    {
        GameObject actor = entry.Actor;
        string sceneName = actor.name;
        Vector3 scenePosition = actor.transform.position;
        Vector2 center = board.CenterOf(board.CellAt(scenePosition));
        scenePosition.x = center.x;
        scenePosition.y = center.y;

        if (PrefabUtility.IsPartOfPrefabInstance(actor))
            PrefabUtility.UnpackPrefabInstance(actor, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        actor.layer = 0;
        actor.isStatic = false; // Physics body type, rather than batching flags, defines a fixed block.
        actor.tag = entry.Movable ? "MovableBlock" : "StaticBlock";
        actor.transform.rotation = Quaternion.identity;
        actor.transform.localScale = Vector3.one;

        var body = actor.GetComponent<Rigidbody2D>();
        if (body == null) body = actor.AddComponent<Rigidbody2D>();
        body.bodyType = entry.Movable ? RigidbodyType2D.Kinematic : RigidbodyType2D.Static;
        body.simulated = true;
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = entry.Movable ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;
        body.collisionDetectionMode = entry.Movable ? CollisionDetectionMode2D.Continuous : CollisionDetectionMode2D.Discrete;
        body.useFullKinematicContacts = false;
        body.sharedMaterial = null;
        var collider = actor.GetComponent<BoxCollider2D>();
        if (collider == null) collider = actor.AddComponent<BoxCollider2D>();
        collider.enabled = true;
        collider.isTrigger = false;
        collider.offset = Vector2.zero;
        collider.size = Vector2.one * 0.9f;
        collider.edgeRadius = 0f;
        collider.autoTiling = false;
        collider.sharedMaterial = null;
        var block = actor.GetComponent<GridPushBlock>();
        if (block == null) block = actor.AddComponent<GridPushBlock>();
        block.enabled = true;
        block.IsMovable = entry.Movable;
        block.Value = entry.Value;
        var serializedBlock = new SerializedObject(block);
        serializedBlock.FindProperty("moveDuration").floatValue = 0.16f;
        serializedBlock.ApplyModifiedPropertiesWithoutUndo();

        // The asset has neutral coordinates and no references to an individual scene.
        actor.name = entry.PrefabName;
        actor.transform.localPosition = Vector3.zero;
        block.Configure(null);
        try
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(actor, entry.AssetPath, InteractionMode.AutomatedAction);
            if (prefab == null) throw new InvalidOperationException("Nao foi possivel salvar " + entry.AssetPath);
            AssetDatabase.SetLabels(prefab, new[] { "EquationShift", "Block", entry.Movable ? "Movable" : "Static" });
        }
        finally
        {
            actor.name = sceneName;
            actor.transform.position = scenePosition;
            block.Configure(board);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(block);
        }
    }

    private static bool TryReadValue(string name, out char value, out string token)
    {
        string suffix = Regex.Replace(name.Trim(), @"\s+\(\d+\)$", "").Substring("block_".Length).ToLowerInvariant();
        if (suffix.Length == 1 && suffix[0] >= '0' && suffix[0] <= '9')
        {
            value = suffix[0];
            token = "Digit_" + value;
            return true;
        }
        switch (suffix)
        {
            case "+": case "plus": value = '+'; token = "Operator_Plus"; return true;
            case "-": case "minus": value = '-'; token = "Operator_Minus"; return true;
            case "*": case "mult": case "multiply": value = '*'; token = "Operator_Multiply"; return true;
            case "/": case "divis": case "divide": value = '/'; token = "Operator_Divide"; return true;
            case "=": case "equal": case "equals": value = '='; token = "Operator_Equals"; return true;
            default: value = '\0'; token = string.Empty; return false;
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }

    private static void EnsureTag(string tag)
    {
        var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = manager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        manager.ApplyModifiedPropertiesWithoutUndo();
    }

    private readonly struct BlockEntry
    {
        public readonly GameObject Actor;
        public readonly bool Movable;
        public readonly char Value;
        public readonly string PrefabName;
        public readonly string AssetPath;

        public BlockEntry(GameObject actor, bool movable, char value, string prefabName, string assetPath)
        {
            Actor = actor;
            Movable = movable;
            Value = value;
            PrefabName = prefabName;
            AssetPath = assetPath;
        }
    }
}
