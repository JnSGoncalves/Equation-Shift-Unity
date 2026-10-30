using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EquationShift
{
    [DisallowMultipleComponent]
    public sealed class TitleScreen : MonoBehaviour
    {
        [SerializeField] private Camera backgroundCamera;
        [SerializeField] private Transform[] blocks;
        [SerializeField] private Vector2 diagonalVelocity = new Vector2(0.65f, 0.95f);
        [SerializeField] private CanvasGroup prompt;
        [SerializeField] private CanvasGroup fade;
        [SerializeField] private string firstLevel = "Assets/Scenes/G2-Fase_2.unity";

        private bool starting;
        private float age;
        private const float Margin = 2f;

        public void Configure(Camera camera, Transform[] decorations, CanvasGroup startPrompt, CanvasGroup curtain)
        {
            backgroundCamera = camera;
            blocks = decorations;
            prompt = startPrompt;
            fade = curtain;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            age += delta;
            if (backgroundCamera != null && blocks != null)
            {
                float top = backgroundCamera.orthographicSize + Margin;
                float right = backgroundCamera.orthographicSize * backgroundCamera.aspect + Margin;
                for (int i = 0; i < blocks.Length; i++)
                {
                    if (blocks[i] == null) continue;
                    // Different depths drift at different speeds; recycle only beyond the camera edges.
                    float speed = 0.65f + (i % 3) * 0.22f;
                    Vector3 p = blocks[i].localPosition + (Vector3)(diagonalVelocity * (delta * speed));
                    p.x = Mathf.Repeat(p.x + right, right * 2f) - right;
                    p.y = Mathf.Repeat(p.y + top, top * 2f) - top;
                    blocks[i].localPosition = p;
                }
            }
            if (prompt != null) prompt.alpha = 0.65f + 0.25f * Mathf.Sin(age * 2f);
            if (starting || age < 0.5f) return;
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            bool pressed = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                (pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame)) ||
                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            if (pressed) StartGame();
        }

        public void StartGame()
        {
            if (starting) return;
            if (!Application.CanStreamedLevelBeLoaded(firstLevel))
            {
                Debug.LogError("Inclua a primeira fase nas cenas habilitadas do Build: " + firstLevel, this);
                return;
            }
            starting = true;
            StartCoroutine(EnterLevel());
        }

        private IEnumerator EnterLevel()
        {
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (fade != null) fade.alpha = Mathf.Clamp01(elapsed / 0.4f);
                yield return null;
            }
            yield return SceneManager.LoadSceneAsync(firstLevel);
        }
    }
}
