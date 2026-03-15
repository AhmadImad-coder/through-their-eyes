using UnityEngine;

namespace OCDSimulation
{
    public class AudioManager : MonoBehaviour
    {
        public AudioClip ambientLoop;
        public AudioClip chatterLoop;
        public AudioClip machineLoop;
        public AudioClip clinkLoop;
        public AudioClip whisperLoop;
        public AudioClip breathingLoop;

        private AudioSource ambientSource;
        private AudioSource chatterSource;
        private AudioSource machineSource;
        private AudioSource clinkSource;
        private AudioSource whisperSource;
        private AudioSource breathingSource;

        private void Awake()
        {
            ambientSource = CreateSource("Ambient");
            chatterSource = CreateSource("Chatter");
            machineSource = CreateSource("Machine");
            clinkSource = CreateSource("Clink");
            whisperSource = CreateSource("Whisper");
            breathingSource = CreateSource("Breathing");

            PlayLoop(ambientSource, ambientLoop, 0.4f);
            PlayLoop(chatterSource, chatterLoop, 0.35f);
            PlayLoop(machineSource, machineLoop, 0.2f);
            PlayLoop(clinkSource, clinkLoop, 0.25f);
        }

        private AudioSource CreateSource(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.loop = true;
            return src;
        }

        private void PlayLoop(AudioSource src, AudioClip clip, float volume)
        {
            if (clip == null) return;
            src.clip = clip;
            src.volume = volume;
            src.Play();
        }

        public void SetWhispers(bool enabled)
        {
            if (whisperLoop == null) return;
            whisperSource.volume = enabled ? 0.5f : 0f;
            if (!whisperSource.isPlaying) whisperSource.Play();
        }

        public void SetBreathing(bool enabled)
        {
            if (breathingLoop == null) return;
            if (enabled)
            {
                breathingSource.clip = breathingLoop;
                breathingSource.volume = 0.6f;
                if (!breathingSource.isPlaying) breathingSource.Play();
            }
            else
            {
                breathingSource.Stop();
            }
        }
    }
}
