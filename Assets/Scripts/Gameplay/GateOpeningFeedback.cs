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

        private readonly Queue<EquationGate> pendingGates = new Queue<EquationGate>();
        private EquationGate activeGate;
        private Vector3 appliedOffset;
        private Vector3 shakeOffset;

        public bool IsPlaying { get; private set; }
        public void Configure(TopDownGridPlayer controller) => player = controller;

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
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(elapsed / duration));
                float phase = elapsed * frequency * Mathf.PI * 2f;
                shakeOffset = new Vector3(Mathf.Sin(phase), Mathf.Sin(phase * 1.37f + 0.6f), 0f) * amplitude * envelope;
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            shakeOffset = Vector3.zero;
        }

        private void LateUpdate()
        {
            transform.localPosition -= appliedOffset;
            appliedOffset = shakeOffset;
            transform.localPosition += appliedOffset;
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
            ResetOffset();
            if (player != null) player.UnlockMovement(this);
            IsPlaying = false;
        }
    }
}
