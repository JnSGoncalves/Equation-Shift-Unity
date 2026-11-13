using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EquationShift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GateOpeningFeedback : MonoBehaviour
    {
        [SerializeField] private TopDownGridPlayer player;
        [Header("Equacao correta")]
        [SerializeField, Min(0.01f)] private float quickShakeDuration = 0.16f;
        [SerializeField, Min(0f)] private float quickShakeAmplitude = 0.07f;
        [SerializeField, Min(0f)] private float quickShakeFrequency = 35f;
        [Header("Abertura e fechamento do portao")]
        [SerializeField, Min(0f)] private float openingShakeAmplitude = 0.04f;
        [SerializeField, Min(0f)] private float openingShakeFrequency = 5f;
        [Header("Tremor ambiente")]
        [SerializeField, Tooltip("Intervalo em segundos de jogo. Use zero nos dois valores para desativar.")]
        private Vector2 ambientShakeInterval = new Vector2(14f, 24f);

        private const float AmbientShakeDuration = 1.3f;
        private readonly Queue<EquationGate> pendingGates = new Queue<EquationGate>();
        private Camera roomCamera;
        private EquationGate activeGate;
        private Vector3 appliedOffset;
        private Vector3 shakeOffset;
        private float quickShakeElapsed = float.PositiveInfinity;
        private float activeQuickShakeDuration;
        private float ambientShakeElapsed;
        private float timeUntilAmbientShake;

        public bool IsPlaying { get; private set; }
        internal bool IsQuickShaking => quickShakeElapsed < activeQuickShakeDuration;
        public void Configure(TopDownGridPlayer controller) => player = controller;
        internal void PlayQuickShake() => PlayQuickShake(quickShakeDuration);
        internal void PlayQuickShake(float duration)
        {
            activeQuickShakeDuration = duration;
            quickShakeElapsed = 0f;
        }

        private void OnEnable()
        {
            roomCamera = GetComponent<Camera>();
            ambientShakeElapsed = AmbientShakeDuration;
            ScheduleAmbientShake();
        }

        internal bool Enqueue(EquationGate gate)
        {
            if (!isActiveAndEnabled || gate == null || player == null) return false;
            pendingGates.Enqueue(gate);
            if (!IsPlaying)
            {
                IsPlaying = true;
                player.LockMovement(this);
                StartCoroutine(PlayQueue());
            }
            return true;
        }

        private IEnumerator PlayQueue()
        {
            // Finish an already accepted grid step before the first camera effect.
            while (player != null && player.IsMoving) yield return null;
            while (pendingGates.Count > 0)
            {
                activeGate = pendingGates.Dequeue();
                if (activeGate == null || !activeGate.isActiveAndEnabled || !activeGate.IsFeedbackTransition)
                {
                    if (activeGate != null) activeGate.CancelFeedbackTransition();
                    continue;
                }
                if (activeGate.FeedbackOpens) GameAudio.Play(SoundCue.EquationCorrect);
                yield return Shake(quickShakeDuration, quickShakeAmplitude, quickShakeFrequency);
                if (activeGate != null && activeGate.isActiveAndEnabled && activeGate.IsFeedbackTransition)
                {
                    activeGate.BeginFeedbackTransition();
                    yield return Shake(activeGate.FeedbackDuration, openingShakeAmplitude, openingShakeFrequency);
                    if (activeGate != null && activeGate.isActiveAndEnabled) activeGate.CompleteFeedbackTransition();
                    else if (activeGate != null) activeGate.CancelFeedbackTransition();
                }
                else if (activeGate != null) activeGate.CancelFeedbackTransition();
                activeGate = null;
            }
            ResetOffset();
            if (player != null) player.UnlockMovement(this);
            IsPlaying = false;
        }

        private IEnumerator Shake(float duration, float amplitude, float frequency)
        {
            float elapsed = 0f;
            while (elapsed < duration && activeGate != null && activeGate.isActiveAndEnabled && activeGate.IsFeedbackTransition)
            {
                shakeOffset = ShakeOffset(elapsed, duration, amplitude, frequency);
                yield return null;
                elapsed += Time.deltaTime;
            }
            shakeOffset = Vector3.zero;
        }

        private static Vector3 ShakeOffset(float elapsed, float duration, float amplitude, float frequency)
        {
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(elapsed / duration));
            float phase = elapsed * frequency * Mathf.PI * 2f;
            return new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f + 0.6f), 0f) * amplitude * envelope;
        }

        private void LateUpdate()
        {
            if (Time.timeScale == 0f) return;
            transform.localPosition -= appliedOffset;
            appliedOffset = shakeOffset + UpdateAmbientShake();
            if (IsQuickShaking)
            {
                if (!SceneTransition.IsTravelling)
                    appliedOffset += ShakeOffset(quickShakeElapsed, activeQuickShakeDuration, quickShakeAmplitude, quickShakeFrequency);
                quickShakeElapsed += Time.deltaTime;
            }
            transform.localPosition += appliedOffset;
        }

        private void ScheduleAmbientShake()
        {
            float minimum = Mathf.Max(0f, ambientShakeInterval.x);
            float maximum = Mathf.Max(minimum, ambientShakeInterval.y);
            timeUntilAmbientShake = maximum > 0f ? Random.Range(minimum, maximum) : float.PositiveInfinity;
        }

        private Vector3 UpdateAmbientShake()
        {
            if (IsPlaying || IsQuickShaking || SceneTransition.IsTravelling || !roomCamera.isActiveAndEnabled ||
                player == null || !player.isActiveAndEnabled || player.IsMovementLocked || VillageDialogue.IsVisible)
            {
                ambientShakeElapsed = AmbientShakeDuration;
                return Vector3.zero;
            }

            if (ambientShakeElapsed >= AmbientShakeDuration)
            {
                timeUntilAmbientShake -= Time.deltaTime;
                if (timeUntilAmbientShake > 0f) return Vector3.zero;
                ambientShakeElapsed = 0f;
                ScheduleAmbientShake();
                GameAudio.Play(SoundCue.Tremor);
            }

            ambientShakeElapsed += Time.deltaTime;
            return ShakeOffset(ambientShakeElapsed, AmbientShakeDuration, 0.055f, 14f);
        }

        private void ResetOffset()
        {
            transform.localPosition -= appliedOffset;
            appliedOffset = shakeOffset = Vector3.zero;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (activeGate != null) activeGate.CancelFeedbackTransition();
            foreach (EquationGate gate in pendingGates)
                if (gate != null) gate.CancelFeedbackTransition();
            activeGate = null;
            pendingGates.Clear();
            quickShakeElapsed = float.PositiveInfinity;
            ResetOffset();
            if (player != null) player.UnlockMovement(this);
            IsPlaying = false;
        }
    }
}
