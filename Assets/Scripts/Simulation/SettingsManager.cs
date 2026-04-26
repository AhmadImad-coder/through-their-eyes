using System;
using System.Collections;
using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Pause-menu settings panel (opened with Escape during gameplay).
    /// Persists values via PlayerPrefs. Static properties let any script read
    /// current settings without a direct reference.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        // ── Static settings readable from anywhere ──────────────────────────
        public static float Sensitivity  { get; private set; } = 2f;
        public static float Volume       { get; private set; } = 1f;
        public static bool  ComfortMode  { get; private set; } = false;
        public static bool  HighContrast { get; private set; } = false;

        public bool IsOpen { get; private set; } = false;

        /// <summary>Fires when the settings panel is dismissed.</summary>
        public Action OnSettingsClosed;

        private UIManager               ui;
        private SimplePlayerController  player;
        private AudioManager            audioManager;

        // ── Initialisation ───────────────────────────────────────────────────
        public void Initialize(UIManager uiManager,
                               SimplePlayerController playerController,
                               AudioManager audio)
        {
            ui          = uiManager;
            player      = playerController;
            audioManager = audio;

            // Load persisted values
            Sensitivity  = PlayerPrefs.GetFloat("Sensitivity",  2f);
            Volume       = PlayerPrefs.GetFloat("Volume",       1f);
            ComfortMode  = PlayerPrefs.GetInt("ComfortMode",    0) == 1;
            HighContrast = PlayerPrefs.GetInt("HighContrast",   0) == 1;

            // Apply volume immediately
            AudioListener.volume = Volume;

            // Subscribe to UI events
            ui.OnSensitivityChanged  += ApplySensitivity;
            ui.OnVolumeChanged       += ApplyVolume;
            ui.OnComfortModeChanged  += ApplyComfortMode;
            ui.OnHighContrastChanged += ApplyHighContrast;
            ui.OnSettingsClose       += CloseSettings;
        }

        // ── Public interface ─────────────────────────────────────────────────
        public void ToggleSettings()
        {
            if (IsOpen) CloseSettings();
            else OpenSettings();
        }

        // ── Internal ─────────────────────────────────────────────────────────
        private void OpenSettings()
        {
            IsOpen = true;
            player.suppressEscapeKey = true;   // prevent player ctrl from unlocking cursor
            player.SetInputEnabled(false);
            ui.ShowSettings(true, Sensitivity, Volume, ComfortMode, HighContrast);
        }

        private void CloseSettings()
        {
            IsOpen = false;
            ui.ShowSettings(false, 0, 0, false, false);
            OnSettingsClosed?.Invoke();
            // Defer the flag reset by one frame so the same Escape keydown that closed
            // settings is not immediately picked up by SimplePlayerController.
            StartCoroutine(ResetSuppressNextFrame());
        }

        private IEnumerator ResetSuppressNextFrame()
        {
            yield return null; // wait one frame
            player.suppressEscapeKey = false;
        }

        private void ApplySensitivity(float v)
        {
            Sensitivity = v;
            PlayerPrefs.SetFloat("Sensitivity", v);
            PlayerPrefs.Save();
            if (player != null) player.lookSpeed = v;
        }

        private void ApplyVolume(float v)
        {
            Volume = v;
            PlayerPrefs.SetFloat("Volume", v);
            PlayerPrefs.Save();
            AudioListener.volume = v;
        }

        private void ApplyComfortMode(bool v)
        {
            ComfortMode = v;
            PlayerPrefs.SetInt("ComfortMode", v ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplyHighContrast(bool v)
        {
            HighContrast = v;
            PlayerPrefs.SetInt("HighContrast", v ? 1 : 0);
            PlayerPrefs.Save();
            ui.ApplyHighContrast(v);
        }
    }
}
