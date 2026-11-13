using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EquationShift
{
    [DefaultExecutionOrder(-31000), DisallowMultipleComponent]
    public sealed class PauseMenu : MonoBehaviour
    {
        private const string TitleScene = "Assets/Scenes/Title.unity";

        private TopDownGridPlayer player;
        private PauseMenuView view;
        private EventSystem events;
        private bool ownsEventSystem;
        private bool navigationWasEnabled;
        private GameObject previouslySelected;
        private bool isPaused;
        private bool leaving;
        private int openedOnFrame;
        private float previousTimeScale;
        private bool audioWasPaused;
        private Action pendingAction;
        private Button selectedBeforeConfirmation;

        private void Awake() => player = GetComponentInChildren<TopDownGridPlayer>(true);

        private void Update()
        {
            if (!isPaused && !leaving && Keyboard.current?.escapeKey.wasPressedThisFrame == true &&
                !SceneTransition.IsTravelling && player != null && player.isActiveAndEnabled &&
                gameObject.scene == SceneManager.GetActiveScene()) Open();
        }

        private void Open()
        {
            if (view == null)
            {
                var prefab = Resources.Load<GameObject>("UI/PauseMenu");
                if (prefab == null)
                {
                    Debug.LogError("Prefab Resources/UI/PauseMenu não encontrado.", this);
                    return;
                }
                view = Instantiate(prefab, transform).GetComponent<PauseMenuView>();
                view.Build(Resume, ResetLevel, () => AskConfirmation(ReturnToTitle),
                    () => AskConfirmation(QuitGame), CancelConfirmation, Confirm);
            }

            events = EventSystem.current;
            ownsEventSystem = events == null;
            if (ownsEventSystem)
            {
                var input = new GameObject("Pause Menu Events");
                input.transform.SetParent(transform, false);
                events = input.AddComponent<EventSystem>();
                input.AddComponent<InputSystemUIInputModule>();
            }
            previouslySelected = events.currentSelectedGameObject;
            navigationWasEnabled = events.sendNavigationEvents;
            events.sendNavigationEvents = true;

            previousTimeScale = Time.timeScale;
            audioWasPaused = AudioListener.pause;
            player.LockMovement(this);
            Time.timeScale = 0f;
            AudioListener.pause = true;
            isPaused = true;
            openedOnFrame = Time.frameCount;
            view.gameObject.SetActive(true);
            view.ShowMenu(events);
            view.ResetButton.interactable = player.CanResetLevel(this);
        }

        private void Resume()
        {
            if (!isPaused) return;
            pendingAction = null;
            selectedBeforeConfirmation = null;
            isPaused = false;
            if (events != null)
            {
                events.SetSelectedGameObject(previouslySelected);
                events.sendNavigationEvents = navigationWasEnabled;
                if (ownsEventSystem)
                {
                    events.gameObject.SetActive(false);
                    Destroy(events.gameObject);
                }
            }
            events = null;
            previouslySelected = null;
            if (view != null) view.gameObject.SetActive(false);
            if (player != null) player.UnlockMovement(this);
            Time.timeScale = previousTimeScale;
            AudioListener.pause = audioWasPaused;
        }

        private void ResetLevel()
        {
            if (!isPaused || pendingAction != null || !player.CanResetLevel(this)) return;
            Resume();
            player.TryResetLevel();
        }

        private void AskConfirmation(Action action)
        {
            if (!isPaused || pendingAction != null) return;
            selectedBeforeConfirmation = events.currentSelectedGameObject != null
                ? events.currentSelectedGameObject.GetComponent<Button>() : null;
            pendingAction = action;
            view.ShowConfirmation(events);
        }

        private void CancelConfirmation()
        {
            pendingAction = null;
            view.ShowMenu(events, selectedBeforeConfirmation);
            selectedBeforeConfirmation = null;
        }

        private void LateUpdate()
        {
            if (!isPaused || leaving || Keyboard.current?.escapeKey.wasPressedThisFrame != true ||
                Time.frameCount == openedOnFrame) return;
            if (pendingAction != null) CancelConfirmation();
            else Resume();
        }

        private void Confirm()
        {
            if (!leaving) pendingAction?.Invoke();
        }

        private void ReturnToTitle()
        {
            if (!Application.CanStreamedLevelBeLoaded(TitleScene))
            {
                Debug.LogError("Inclua a tela de título nas cenas habilitadas do Build.", this);
                CancelConfirmation();
                return;
            }

            leaving = true;
            SceneManager.LoadSceneAsync(TitleScene, LoadSceneMode.Single);
        }

        private void QuitGame()
        {
            leaving = true;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDisable()
        {
            Resume();
            leaving = false;
        }
    }
}
