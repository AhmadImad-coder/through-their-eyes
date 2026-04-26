using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace OCDSimulation
{
    /// <summary>
    /// Drives real URP Volume post-processing effects based on the player's
    /// anxiety level.  Effects escalate progressively:
    ///
    ///   0–30%   Subtle base vignette + soft bloom on lights (always on)
    ///  30–65%   Vignette grows, saturation starts to drain
    ///  65–80%   Strong vignette, noticeable desaturation, faint CA
    ///  80–100%  Max vignette, near-greyscale, heavy CA, film grain, red pulse
    ///
    /// The red panic-pulse overlay is kept as a UI layer on top because it is
    /// jarring in a way real VFX cannot easily replicate.
    /// </summary>
    public class PostProcessingController : MonoBehaviour
    {
        // ── URP Volume handles ────────────────────────────────────────────────
        private Volume              globalVolume;
        private Vignette            vignette;
        private ColorAdjustments    colorAdj;
        private ChromaticAberration chromaticAb;
        private Bloom               bloom;
        // FilmGrain removed — requires full URP package; added back after install

        private bool volumeReady = false;

        // ── UI red-pulse overlay (supplement, not replacement) ────────────────
        private Image desatOverlay;   // fallback grey tint if URP not enabled
        private Image pulseOverlay;   // red panic flash — kept in all modes
        private float pulseTimer;
        private float pulseAlpha;
        private bool  pulsing;

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Called once by SimulationBootstrap after UIManager is ready.</summary>
        public void Initialize(Transform canvasTransform)
        {
            volumeReady = TrySetupVolume();

            // Always create the UI overlays — pulse is used regardless;
            // desaturation overlay acts as a fallback if URP is not configured.
            desatOverlay = CreateOverlay(canvasTransform, "DesatOverlay",
                                         new Color(0.60f, 0.60f, 0.65f, 0f));
            pulseOverlay = CreateOverlay(canvasTransform, "PulseOverlay",
                                         new Color(0.90f, 0.10f, 0.10f, 0f));

            if (!volumeReady)
                Debug.LogWarning("[PostProcessing] URP Volume effects not available. " +
                                 "Enable Post Processing on your Camera and URP Renderer " +
                                 "for the full visual experience.");
        }

        // ── Volume setup ──────────────────────────────────────────────────────
        private bool TrySetupVolume()
        {
            try
            {
                // Create a dedicated Global Volume at maximum priority
                GameObject volObj = new GameObject("OCD_PostProcessVolume");
                globalVolume          = volObj.AddComponent<Volume>();
                globalVolume.isGlobal = true;
                globalVolume.priority = 100f;

                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

                // ── Bloom ────────────────────────────────────────────────────
                // Always on — makes pendant lamps, coffee cups and windows glow
                // softly, giving the café a warm cinematic feel.
                bloom = profile.Add<Bloom>();
                bloom.active = true;
                bloom.threshold.Override(0.85f);
                bloom.intensity.Override(0.50f);
                bloom.scatter.Override(0.65f);
                bloom.tint.Override(new Color(1.0f, 0.92f, 0.80f)); // warm cream tint

                // ── Vignette ─────────────────────────────────────────────────
                // Starts subtle; crushes to black edges at peak anxiety.
                vignette = profile.Add<Vignette>();
                vignette.active = true;
                vignette.color.Override(Color.black);
                vignette.intensity.Override(0.20f);
                vignette.smoothness.Override(0.35f);
                vignette.rounded.Override(true);

                // ── Colour Adjustments ────────────────────────────────────────
                // Saturation drains as anxiety climbs — world loses colour.
                colorAdj = profile.Add<ColorAdjustments>();
                colorAdj.active = true;
                colorAdj.saturation.Override(0f);       // 0 = normal; -100 = full greyscale
                colorAdj.postExposure.Override(0f);
                colorAdj.contrast.Override(0f);

                // ── Chromatic Aberration ──────────────────────────────────────
                // Colour fringing at high anxiety — visual "panic" signal.
                chromaticAb = profile.Add<ChromaticAberration>();
                chromaticAb.active = true;
                chromaticAb.intensity.Override(0f);

                globalVolume.profile = profile;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PostProcessing] Volume setup failed ({e.Message}). " +
                                 "Falling back to UI overlay mode.");
                return false;
            }
        }

        private Image CreateOverlay(Transform parent, string overlayName, Color color)
        {
            GameObject go = new GameObject(overlayName);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            RectTransform r = img.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            return img;
        }

        // ── Per-frame update ──────────────────────────────────────────────────

        /// <summary>Called every frame by GameDirector with the current anxiety (0–100).</summary>
        public void UpdateEffects(float anxiety, Camera cam)
        {
            float a = Mathf.Clamp01(anxiety / 100f);   // normalised 0–1

            if (volumeReady)
                UpdateVolumeEffects(a);
            else
                UpdateFallbackOverlay(a);

            UpdatePulse(a);
        }

        private void UpdateVolumeEffects(float a)
        {
            // ── Vignette: 0.20 base → 0.65 at full anxiety ──────────────────
            float vigTarget = Mathf.Lerp(0.20f, 0.65f, EaseIn(a));
            vignette.intensity.Override(vigTarget);

            // ── Vignette smoothness: tighter at high anxiety ─────────────────
            vignette.smoothness.Override(Mathf.Lerp(0.35f, 0.55f, a));

            // ── Saturation: normal until 30%, then drains to −80 at 100% ────
            float satT   = Mathf.Clamp01((a - 0.30f) / 0.70f);
            float satVal = Mathf.Lerp(0f, -80f, EaseIn(satT));
            colorAdj.saturation.Override(satVal);

            // Slight contrast boost as world gets grey — keeps it readable
            colorAdj.contrast.Override(Mathf.Lerp(0f, 12f, satT));

            // ── Chromatic Aberration: starts at 65%, peaks at 0.7 ────────────
            float caT = Mathf.Clamp01((a - 0.65f) / 0.35f);
            chromaticAb.intensity.Override(Mathf.Lerp(0f, 0.70f, EaseIn(caT)));

            // ── Bloom intensity: slight boost at high anxiety (hyper-aware) ──
            bloom.intensity.Override(Mathf.Lerp(0.50f, 0.85f, a * 0.5f));
        }

        // Fallback for when URP Volume is not configured
        private void UpdateFallbackOverlay(float a)
        {
            float desatT = Mathf.Clamp01((a - 0.65f) / 0.35f);
            if (desatOverlay != null)
                desatOverlay.color = new Color(0.62f, 0.62f, 0.65f, desatT * 0.22f);
        }

        private void UpdatePulse(float a)
        {
            if (pulseOverlay == null) return;

            if (a >= 0.80f)
            {
                // Pulse frequency increases as anxiety climbs
                float interval = Mathf.Lerp(3.0f, 0.7f, (a - 0.80f) / 0.20f);
                pulseTimer += Time.deltaTime;
                if (pulseTimer >= interval)
                {
                    pulseTimer = 0f;
                    pulsing    = true;
                    pulseAlpha = Mathf.Lerp(0.25f, 0.45f, (a - 0.80f) / 0.20f);
                }
            }
            else
            {
                pulseTimer = 0f;
            }

            if (pulsing)
            {
                pulseAlpha = Mathf.Max(0f, pulseAlpha - Time.deltaTime * 2.0f);
                pulseOverlay.color = new Color(0.90f, 0.08f, 0.08f, pulseAlpha);
                if (pulseAlpha <= 0f) pulsing = false;
            }
            else
            {
                pulseOverlay.color = new Color(0.90f, 0.08f, 0.08f, 0f);
            }
        }

        // ── Smooth ease-in curve for more natural ramp-up ─────────────────────
        private static float EaseIn(float t) => t * t;

        // ─────────────────────────────────────────────────────────────────────

        public void ResetEffects(Camera cam)
        {
            if (volumeReady)
            {
                vignette.intensity.Override(0.20f);
                vignette.smoothness.Override(0.35f);
                colorAdj.saturation.Override(0f);
                colorAdj.contrast.Override(0f);
                chromaticAb.intensity.Override(0f);
                bloom.intensity.Override(0.50f);
            }

            if (desatOverlay != null)
                desatOverlay.color = new Color(0.62f, 0.62f, 0.65f, 0f);
            if (pulseOverlay != null)
                pulseOverlay.color = new Color(0.90f, 0.08f, 0.08f, 0f);

            pulseAlpha = 0f;
            pulsing    = false;
            pulseTimer = 0f;
        }
    }
}
