using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EquationShift
{
    [DisallowMultipleComponent]
    public sealed class FinalRoomEnding : MonoBehaviour
    {
        [SerializeField] private EquationGate gate;
        [SerializeField, Min(0.1f)] private float fadeDuration = 2.5f;

        private const string EndingScene = "Assets/Scenes/GameOver.unity";
        private const string Dialogue = "Sempre me perguntei qual era minha utilidade. " +
                                        "Acho que finalmente encontrei meu propósito.";
        private const string Hint = "Acho que para estabilizar a função principal devo usar todos os blocos móveis disponíveis";

        private TopDownGridPlayer player;
        private Coroutine ending;
        private CanvasGroup curtain;

        private void Awake() => player = GetComponentInChildren<TopDownGridPlayer>(true);

        private void OnEnable()
        {
            if (gate == null) return;
            gate.EquationSolved += BeginEnding;
            gate.SingleAnchorEquationFound += ShowHint;
        }

        private void ShowHint()
        {
            if (ending == null && !VillageDialogue.IsVisible) VillageDialogue.Show(Hint);
        }

        private void BeginEnding()
        {
            if (ending != null || player == null || SceneTransition.IsTravelling) return;
            if (!Application.CanStreamedLevelBeLoaded(EndingScene))
            {
                Debug.LogError("Inclua a cena de fim de jogo no Build: " + EndingScene, this);
                return;
            }
            ending = StartCoroutine(PlayEnding());
        }

        private IEnumerator PlayEnding()
        {
            player.LockForCutscene(this);
            VillageDialogue.Show(Dialogue);
            while (VillageDialogue.IsVisible) yield return null;

            var overlay = new GameObject("Ending Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            overlay.transform.SetParent(transform, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;
            curtain = overlay.GetComponent<CanvasGroup>();
            curtain.alpha = 0f;

            var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
            black.transform.SetParent(overlay.transform, false);
            var rect = (RectTransform)black.transform;
            rect.anchorMax = Vector2.one;
            rect.anchorMin = rect.offsetMin = rect.offsetMax = Vector2.zero;
            black.GetComponent<Image>().color = Color.black;

            for (float elapsed = 0f; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                curtain.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            curtain.alpha = 1f;
            yield return SceneManager.LoadSceneAsync(EndingScene);
        }

        private void OnDisable()
        {
            if (gate != null)
            {
                gate.EquationSolved -= BeginEnding;
                gate.SingleAnchorEquationFound -= ShowHint;
            }
            if (ending == null) return;
            StopCoroutine(ending);
            ending = null;
            VillageDialogue.Hide();
            if (curtain != null) Destroy(curtain.gameObject);
            if (player != null) player.UnlockMovement(this);
        }
    }
}
