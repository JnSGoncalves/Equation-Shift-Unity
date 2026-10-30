using System.IO;
using System.Linq;
using EquationShift;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TitleSceneSetup
{
    public const string ScenePath = "Assets/Scenes/Title.unity";
    private const string ArtFolder = "Assets/Art/Title";

    [MenuItem("Equation Shift/Criar cena de titulo")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = 10;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(16, 22, 29, 255);

        var root = new GameObject("Title Screen", typeof(TitleScreen));
        var field = new GameObject("Drifting prefab blocks").transform;
        field.SetParent(root.transform);
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Blocks/Movable" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
        var blocks = new Transform[54];
        var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        var random = new System.Random(431);
        for (int i = 0; i < blocks.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[(i * 7 + i / 9) % paths.Length]);
            var block = (GameObject)PrefabUtility.InstantiatePrefab(prefab, field);
            // Decorations retain their prefab artwork, without board logic or physics.
            foreach (var behaviour in block.GetComponents<MonoBehaviour>()) Object.DestroyImmediate(behaviour);
            foreach (var collider in block.GetComponents<Collider2D>()) Object.DestroyImmediate(collider);
            foreach (var body in block.GetComponents<Rigidbody2D>()) Object.DestroyImmediate(body);
            var renderer = block.GetComponent<SpriteRenderer>();
            if (unlit != null) renderer.sharedMaterial = unlit;
            float depth = i % 3;
            renderer.color = new Color(0.55f, 0.68f, 0.8f, 0.18f + depth * 0.105f);
            renderer.sortingOrder = -10;
            float x = (i % 9 - 4) * 4.5f + (float)random.NextDouble() * 1.1f;
            float y = (i / 9 - 2.5f) * 4f + (float)random.NextDouble() * 0.8f;
            block.transform.position = new Vector3(x, y, 0);
            block.transform.localScale = Vector3.one * (1.05f + depth * 0.3f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(block.transform);
            blocks[i] = block.transform;
        }

        var canvasObject = new GameObject("Title Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(root.transform);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var font = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/Monocraft.ttf");
        if (font == null) throw new System.InvalidOperationException("Monocraft.ttf ausente em " + ArtFolder);

        var shade = Rect("Title contrast", canvas.transform, Vector2.zero, new Vector2(1280, 720));
        var image = shade.gameObject.AddComponent<Image>();
        image.sprite = CreateShade();
        image.raycastTarget = false;
        Stretch(shade);

        Text title = Label("Equation Shift", canvas.transform, font, "Equation\nShift", 100,
            Vector2.zero, new Vector2(1140, 280), new Color32(242, 231, 201, 255));
        title.lineSpacing = 0.92f;
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 0, 0, 220);
        shadow.effectDistance = new Vector2(5, -6);

        var line = Rect("Amber rule", canvas.transform, new Vector2(0, -165), new Vector2(68, 3));
        line.gameObject.AddComponent<Image>().color = new Color32(206, 158, 88, 255);
        var prompt = Label("Start prompt", canvas.transform, font, "Pressione Enter para iniciar", 19,
            new Vector2(0, -242), new Vector2(1000, 44), new Color32(175, 188, 196, 255)).gameObject.AddComponent<CanvasGroup>();
        var curtain = Rect("Fade to game", canvas.transform, Vector2.zero, Vector2.zero);
        Stretch(curtain);
        curtain.gameObject.AddComponent<Image>().color = Color.black;
        var fade = curtain.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0;
        fade.blocksRaycasts = false;
        root.GetComponent<TitleScreen>().Configure(camera, blocks, prompt, fade);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Capture();
        Debug.Log("Equation Shift: cena Title criada com Monocraft e 54 blocos de prefab.");
    }

    private static Sprite CreateShade()
    {
        var texture = new Texture2D(256, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            float u = (x / 255f - 0.5f) * 2;
            float v = (y / 127f - 0.5f) * 2;
            float alpha = 0.84f * Mathf.Exp(-(u * u * 2f + v * v * 8f));
            texture.SetPixel(x, y, new Color(0.035f, 0.05f, 0.07f, alpha));
        }
        texture.Apply();
        string path = ArtFolder + "/TitleShade.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Text Label(string name, Transform parent, Font font, string content, int size,
        Vector2 position, Vector2 dimensions, Color color)
    {
        var text = Rect(name, parent, position, dimensions).gameObject.AddComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    // Capture with the same camera and canvas used in the game, for visual review.
    public static void Capture()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var camera = Object.FindFirstObjectByType<Camera>();
        Canvas.ForceUpdateCanvases();
        var target = new RenderTexture(1280, 720, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        image.Apply();
        Directory.CreateDirectory("../.codex-build/title");
        File.WriteAllBytes("../.codex-build/title/Title-preview.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(image);
        Object.DestroyImmediate(target);
    }
}
