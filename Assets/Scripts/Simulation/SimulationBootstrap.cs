using UnityEngine;
using UnityEngine.Rendering;

namespace OCDSimulation
{
    public class SimulationBootstrap : MonoBehaviour
    {
        [Header("Scene Settings")]
        public float roomWidth = 12f;
        public float roomLength = 14f;
        public float roomHeight = 4f;

        [Header("Gameplay Settings")]
        public float seatedCompletionSeconds = 150f;
        public float mindfulnessThreshold = 60f;
        public float mindfulnessDurationSeconds = 12f;
        public float mindfulnessNoCompulsionSeconds = 8f;

        private void Start()
        {
            ConfigureLighting();
            DisableExtraAudioListeners();

            UIManager ui = CreateUI();
            SimplePlayerController player = CreatePlayer();
            SceneRefs refs = BuildScene();
            AudioManager audio = CreateAudio();

            DisableOtherCameras(player.Camera);

            GameObject directorObj = new GameObject("GameDirector");
            GameDirector director = directorObj.AddComponent<GameDirector>();
            director.ui = ui;
            director.player = player;
            director.playerCamera = player.Camera;
            director.scene = refs;
            director.audioManager = audio;
        }

        private void ConfigureLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void DisableExtraAudioListeners()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            bool kept = false;
            foreach (AudioListener listener in listeners)
            {
                if (!kept)
                {
                    kept = true;
                    continue;
                }
                listener.enabled = false;
            }
        }

        private void DisableOtherCameras(Camera playerCamera)
        {
            if (playerCamera == null) return;
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam == playerCamera) continue;
                cam.enabled = false;
                AudioListener listener = cam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }

        private UIManager CreateUI()
        {
            GameObject uiObj = new GameObject("UIManager");
            return uiObj.AddComponent<UIManager>();
        }

        private SimplePlayerController CreatePlayer()
        {
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(0f, 1f, -4f);

            CharacterController controller = playerObj.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            SimplePlayerController player = playerObj.AddComponent<SimplePlayerController>();

            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(playerObj.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";

            player.BindCamera(cam);
            return player;
        }

        private AudioManager CreateAudio()
        {
            GameObject audioObj = new GameObject("AudioManager");
            return audioObj.AddComponent<AudioManager>();
        }

        private SceneRefs BuildScene()
        {
            SceneBuilder builder = new SceneBuilder(roomWidth, roomLength, roomHeight);
            return builder.Build();
        }
    }
}
