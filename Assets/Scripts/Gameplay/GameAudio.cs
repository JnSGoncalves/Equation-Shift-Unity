using UnityEngine;

namespace EquationShift
{
    public enum SoundCue
    {
        MenuConfirm, BlockPush, FormCollect, FormChange, EquationCorrect,
        GateOpen, GateClose, Lever, Tremor
    }

    public static class GameAudio
    {
        private static readonly AudioClip[] Clips = new AudioClip[System.Enum.GetValues(typeof(SoundCue)).Length];
        private static AudioSource source;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            source = null;
            System.Array.Clear(Clips, 0, Clips.Length);
        }

        public static void Play(SoundCue cue)
        {
            int index = (int)cue;
            if (Clips[index] == null) Clips[index] = Resources.Load<AudioClip>("Audio/" + cue);
            AudioClip clip = Clips[index];
            if (clip == null) return;

            if (source == null)
            {
                var player = new GameObject("Sound Effects", typeof(AudioSource));
                Object.DontDestroyOnLoad(player);
                source = player.GetComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
            source.PlayOneShot(clip);
        }
    }
}
