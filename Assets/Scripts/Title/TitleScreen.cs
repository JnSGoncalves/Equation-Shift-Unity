using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EquationShift
{
    [DisallowMultipleComponent]
    public sealed class TitleScreen : MonoBehaviour
    {
        [SerializeField] private Camera backgroundCamera;
        [SerializeField] private Transform[] blocks;
        [SerializeField] private Vector2 diagonalVelocity = new Vector2(0.65f, 0.95f);
        [SerializeField] private Button actionButton;
        [SerializeField] private CanvasGroup fade;
        [SerializeField] private string firstLevel = "Assets/Scenes/G1-Fase_1.unity";
        [SerializeField] private bool startsNewGame = true;

        private bool starting;
        private const float Margin = 2f;

        public void Configure(Camera camera, Transform[] decorations, Button button, CanvasGroup curtain)
        {
            backgroundCamera = camera;
            blocks = decorations;
            actionButton = button;
            fade = curtain;
        }

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
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
        }

        public void StartGame()
        {
            if (starting) return;
            if (!Application.CanStreamedLevelBeLoaded(firstLevel))
            {
                Debug.LogError("Inclua o destino nas cenas habilitadas do Build: " + firstLevel, this);
                return;
            }
            starting = true;
            GameAudio.Play(SoundCue.MenuConfirm);
            if (actionButton != null) actionButton.interactable = false;
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
            if (startsNewGame) GameProgress.StartNewGame();
            yield return SceneManager.LoadSceneAsync(firstLevel);
        }
    }
}
