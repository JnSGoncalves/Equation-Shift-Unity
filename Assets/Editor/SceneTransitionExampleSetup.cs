using System;
using System.Linq;
using EquationShift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SceneTransitionExampleSetup
{
    private const string FirstScene = "Assets/Scenes/00-FaseExemplo_1.unity";
    private const string SecondScene = "Assets/Scenes/A0-FaseExemplo_2.unity";
    private const string PrefabFolder = "Assets/Prefabs/Transitions";
    private const string PortalPrefab = PrefabFolder + "/ScenePortal.prefab";

    [MenuItem("Equation Shift/Configurar transicoes das Fases Exemplo")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Saia do Play Mode antes de configurar as cenas.");
        GateExampleSetup.ConfigureSavedExample();
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Prefabs", "Transitions");
        bool hasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefab) != null;
        var template = hasPrefab ? PrefabUtility.LoadPrefabContents(PortalPrefab) :
            new GameObject("ScenePortal", typeof(BoxCollider2D), typeof(ScenePortal));
        template.GetComponent<BoxCollider2D>().size = Vector2.one * 0.8f;
        template.GetComponent<ScenePortal>().Configure(null, "A0", Vector2Int.down);
        PrefabUtility.SaveAsPrefabAsset(template, PortalPrefab);
        if (hasPrefab) PrefabUtility.UnloadPrefabContents(template);
        else Object.DestroyImmediate(template);

        ConfigurePortal(FirstScene, "Exit_Bottom_To_A0", "A0", new Vector3Int(-1, -9, 0), Vector2Int.up);
        ConfigurePortal(SecondScene, "Exit_Top_To_00", "00", new Vector3Int(-1, 8, 0), Vector2Int.down);

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.path != "Assets/Scenes/00-FaseExemplo.unity" &&
                scene.path != FirstScene && scene.path != SecondScene).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(FirstScene, true));
        scenes.Insert(1, new EditorBuildSettingsScene(SecondScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(FirstScene);
        Debug.Log("Equation Shift: transicao 00 <-> A0 configurada com chegadas para dentro das salas.");
    }

    private static void ConfigurePortal(string path, string name, string destination, Vector3Int cell,
        Vector2Int offset)
    {
        Scene scene = EditorSceneManager.OpenScene(path);
        var board = Object.FindFirstObjectByType<LevelGrid>();
        if (board.GetComponent<RoomScene>() == null) board.gameObject.AddComponent<RoomScene>();
        Transform parent = board.transform.Find("Transitions");
        if (parent == null)
        {
            parent = new GameObject("Transitions").transform;
            parent.SetParent(board.transform, false);
        }
        Transform existing = parent.Find(name);
        var portal = existing != null ? existing.GetComponent<ScenePortal>() : null;
        if (portal == null)
            portal = ((GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefab), parent)).GetComponent<ScenePortal>();
        portal.name = name;
        Vector2 position = board.CenterOf(cell);
        portal.transform.position = new Vector3(position.x, position.y, 0f);
        var gate = GameObject.Find("Gate_Horizontal").GetComponent<EquationGate>();
        portal.Configure(board, destination, offset, gate);
        PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
        PrefabUtility.RecordPrefabInstancePropertyModifications(portal.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(portal.gameObject);

        Vector3Int arrival = cell + new Vector3Int(offset.x, offset.y, 0);
        var floor = board.Ground.GetTile(arrival);
        Vector3Int direction = new Vector3Int(Math.Sign(offset.x), Math.Sign(offset.y), 0);
        int steps = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
        for (int i = 0; i <= steps; i++)
        {
            Vector3Int corridorCell = cell + direction * i;
            board.Ground.SetTile(corridorCell, floor);
            Vector3 wallPosition = board.Ground.GetCellCenterWorld(corridorCell);
            wallPosition.z = board.Walls.transform.position.z;
            board.Walls.SetTile(board.Walls.WorldToCell(wallPosition), null);
        }

        gate.SetOpen(false, false);
        PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
        PrefabUtility.RecordPrefabInstancePropertyModifications(gate.GetComponent<SpriteRenderer>());
        foreach (Collider2D shape in gate.GetComponentsInChildren<Collider2D>())
            PrefabUtility.RecordPrefabInstancePropertyModifications(shape);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
