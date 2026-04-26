using System.Collections;
using UnityEngine;

namespace OCDSimulation
{
    public class AudioManager : MonoBehaviour
    {
        // ── Clips (assigned by SimulationBootstrap before Init()) ─────────────
        [HideInInspector] public AudioClip ambientLoop;
        [HideInInspector] public AudioClip chatterLoop;
        [HideInInspector] public AudioClip machineLoop;
        [HideInInspector] public AudioClip clinkLoop;
        [HideInInspector] public AudioClip whisperLoop;
        [HideInInspector] public AudioClip breathingLoop;
        [HideInInspector] public AudioClip streetAmbientLoop;
        [HideInInspector] public AudioClip footstepLoop;

        // ── Volumes (set by SimulationBootstrap from Inspector sliders) ───────
        [HideInInspector] public float ambientVolume   = 0.40f;
        [HideInInspector] public float chatterVolume   = 0.35f;
        [HideInInspector] public float machineVolume   = 0.22f;
        [HideInInspector] public float clinkVolume     = 0.20f;
        [HideInInspector] public float whisperVolume   = 0.50f;
        [HideInInspector] public float breathingVolume = 0.60f;
        [HideInInspector] public float streetVolume    = 0.50f;
        [HideInInspector] public float footstepVolume  = 0.25f;

        // ── Burst timing ──────────────────────────────────────────────────────
        [HideInInspector] public float machineBurstDuration   = 3.5f;
        [HideInInspector] public float machineSilenceDuration = 12f;
        [HideInInspector] public float clinkBurstDuration     = 1.5f;
        [HideInInspector] public float clinkSilenceDuration   = 7f;

        // ── Sources ───────────────────────────────────────────────────────────
        private AudioSource ambientSource;
        private AudioSource chatterSource;
        private AudioSource machineSource;
        private AudioSource clinkSource;
        private AudioSource whisperSource;
        private AudioSource breathingSource;
        private AudioSource streetSource;
        private AudioSource footstepSource;

        // ── Burst coroutine handles (so we can pause/resume them) ─────────────
        private Coroutine machineBurstCoroutine;
        private Coroutine clinkBurstCoroutine;

        private bool isIndoor   = false;
        private bool isCalming  = false;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            ambientSource   = CreateSource("Ambient",   loop: true);
            chatterSource   = CreateSource("Chatter",   loop: true);
            machineSource   = CreateSource("Machine",   loop: false); // burst-driven
            clinkSource     = CreateSource("Clink",     loop: false); // burst-driven
            whisperSource   = CreateSource("Whisper",   loop: true);
            breathingSource = CreateSource("Breathing", loop: false);
            streetSource    = CreateSource("Street",    loop: true);
            footstepSource  = CreateSource("Footstep",  loop: true);
            // Clips arrive from SimulationBootstrap — Init() is called next.
        }

        /// <summary>
        /// Called by SimulationBootstrap after all clips and settings
        /// have been transferred from the Inspector.
        /// </summary>
        public void Init()
        {
            SetOutdoorMode();
        }

        // ── Source factory ────────────────────────────────────────────────────
        private AudioSource CreateSource(string sourceName, bool loop)
        {
            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            AudioSource src = go.AddComponent<AudioSource>();
            src.loop        = loop;
            src.playOnAwake = false;
            return src;
        }

        private void StartLoop(AudioSource src, AudioClip clip, float volume)
        {
            if (clip == null || src == null) return;
            src.clip   = clip;
            src.volume = volume;
            if (!src.isPlaying) src.Play();
        }

        // ── Outdoor / Indoor modes ────────────────────────────────────────────

        public void SetOutdoorMode()
        {
            if (isIndoor) return;
            StartLoop(streetSource,   streetAmbientLoop, streetVolume);
            StartLoop(footstepSource, footstepLoop,       footstepVolume);
            // Keep all indoor sources silent and stopped
            ambientSource.volume = 0f;  ambientSource.Stop();
            chatterSource.volume = 0f;  chatterSource.Stop();
            machineSource.volume = 0f;  machineSource.Stop();
            clinkSource.volume   = 0f;  clinkSource.Stop();
        }

        public void TransitionToIndoor()
        {
            if (isIndoor) return;
            isIndoor = true;
            StartCoroutine(CrossFadeToIndoor());
        }

        private IEnumerator CrossFadeToIndoor()
        {
            // Prime indoor sources at zero volume
            StartLoop(ambientSource, ambientLoop, 0f);
            StartLoop(chatterSource, chatterLoop, 0f);

            float elapsed      = 0f;
            float duration     = 2f;
            float streetStart  = streetSource.volume;
            float stepStart    = footstepSource.volume;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                streetSource.volume   = Mathf.Lerp(streetStart, 0f,            t);
                footstepSource.volume = Mathf.Lerp(stepStart,   0f,            t);
                ambientSource.volume  = Mathf.Lerp(0f,          ambientVolume, t);
                chatterSource.volume  = Mathf.Lerp(0f,          chatterVolume, t);
                yield return null;
            }

            streetSource.Stop();
            footstepSource.Stop();

            // Start burst sources AFTER the cross-fade finishes so they
            // don't overlap the transition.
            if (machineLoop != null)
                machineBurstCoroutine = StartCoroutine(
                    BurstLoop(machineSource, machineLoop,
                              machineVolume,
                              machineBurstDuration,
                              machineSilenceDuration));

            if (clinkLoop != null)
                clinkBurstCoroutine = StartCoroutine(
                    BurstLoop(clinkSource, clinkLoop,
                              clinkVolume,
                              clinkBurstDuration,
                              clinkSilenceDuration,
                              initialDelay: 2.5f)); // offset so machine & clink don't fire at once
        }

        /// <summary>
        /// Plays a clip for <burstDuration> seconds, waits <silenceDuration>
        /// seconds, then repeats — forever, until the coroutine is stopped.
        /// Simulates real-world intermittent sounds (machines, cutlery).
        /// </summary>
        private IEnumerator BurstLoop(AudioSource src, AudioClip clip,
                                      float volume, float burstDuration,
                                      float silenceDuration,
                                      float initialDelay = 0f)
        {
            if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);

            while (true)
            {
                // ── Play burst ──────────────────────────────────────────────
                src.clip   = clip;
                src.volume = volume;
                src.Play();
                yield return new WaitForSeconds(burstDuration);
                src.Stop();

                // ── Silence ─────────────────────────────────────────────────
                yield return new WaitForSeconds(silenceDuration);
            }
        }

        // ── Calming mode (everything fades out except breathing) ──────────────

        /// <summary>
        /// Called when the player starts a breathing or grounding session.
        /// Smoothly fades all café sounds to silence so only the breathing
        /// audio is audible, creating a true relaxation atmosphere.
        /// </summary>
        public void EnterCalmingMode()
        {
            if (isCalming) return;
            isCalming = true;

            // Stop the burst coroutines — machine & clink must go silent
            if (machineBurstCoroutine != null) { StopCoroutine(machineBurstCoroutine); machineBurstCoroutine = null; }
            if (clinkBurstCoroutine   != null) { StopCoroutine(clinkBurstCoroutine);   clinkBurstCoroutine   = null; }

            StartCoroutine(FadeSources(to: 0f, duration: 0.8f));
        }

        /// <summary>
        /// Called when the breathing / grounding session ends.
        /// Smoothly fades café sounds back to their normal volumes and
        /// restarts the intermittent burst sources.
        /// </summary>
        public void ExitCalmingMode()
        {
            if (!isCalming) return;
            isCalming = false;

            StartCoroutine(FadeSources(to: 1f, duration: 1.5f, restartBursts: true));
        }

        /// <summary>Fades all ambient/chatter sources toward a target multiplier (0=silent, 1=full volume).</summary>
        private IEnumerator FadeSources(float to, float duration, bool restartBursts = false)
        {
            float elapsed = 0f;

            float ambStart     = ambientSource.volume;
            float chatStart    = chatterSource.volume;
            float whisperStart = whisperSource.volume;

            float ambTarget     = ambientVolume  * to;
            float chatTarget    = chatterVolume  * to;
            float whisperTarget = whisperSource.isPlaying ? whisperVolume * to : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                ambientSource.volume  = Mathf.Lerp(ambStart,     ambTarget,     t);
                chatterSource.volume  = Mathf.Lerp(chatStart,    chatTarget,    t);
                whisperSource.volume  = Mathf.Lerp(whisperStart, whisperTarget, t);

                // Silence burst sources immediately (they're already stopped)
                machineSource.volume = 0f;
                clinkSource.volume   = 0f;

                yield return null;
            }

            // Once fade-in finishes, restart burst coroutines
            if (restartBursts && isIndoor)
            {
                if (machineLoop != null)
                    machineBurstCoroutine = StartCoroutine(
                        BurstLoop(machineSource, machineLoop,
                                  machineVolume,
                                  machineBurstDuration,
                                  machineSilenceDuration));

                if (clinkLoop != null)
                    clinkBurstCoroutine = StartCoroutine(
                        BurstLoop(clinkSource, clinkLoop,
                                  clinkVolume,
                                  clinkBurstDuration,
                                  clinkSilenceDuration,
                                  initialDelay: 1.5f));
            }
        }

        // ── Whispers / Breathing helpers ──────────────────────────────────────

        public void SetWhispers(bool enabled)
        {
            if (whisperLoop == null) return;
            if (enabled)
            {
                StartLoop(whisperSource, whisperLoop, whisperVolume);
            }
            else
            {
                whisperSource.volume = 0f;
                whisperSource.Stop();
            }
        }

        public void SetBreathing(bool enabled)
        {
            if (breathingLoop == null) return;
            if (enabled)
            {
                breathingSource.clip   = breathingLoop;
                breathingSource.volume = breathingVolume;
                if (!breathingSource.isPlaying) breathingSource.Play();
            }
            else
            {
                breathingSource.Stop();
            }
        }

        // ── Master volume (used by SettingsManager) ───────────────────────────
        public void SetMasterVolume(float vol)
        {
            AudioListener.volume = vol;
        }
    }
}
