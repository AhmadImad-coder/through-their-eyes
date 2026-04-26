using UnityEngine;
using UnityEngine.Rendering;

namespace OCDSimulation
{
    /// <summary>
    /// Scene entry point. Creates every system in the correct order and wires
    /// the inter-system references before any Update() runs.
    /// </summary>
    public class SimulationBootstrap : MonoBehaviour
    {
        [Header("Scene Settings")]
        public float roomWidth  = 15f;
        public float roomLength = 18f;
        public float roomHeight = 4.5f;

        [Header("Audio Clips — Indoor (Café)")]
        public AudioClip ambientLoop;
        public AudioClip chatterLoop;
        public AudioClip machineLoop;
        public AudioClip clinkLoop;
        public AudioClip whisperLoop;
        public AudioClip breathingLoop;

        [Header("Indoor Volume (0 = off, 1 = full)")]
        [Range(0f, 1f)] public float ambientVolume  = 0.40f;
        [Range(0f, 1f)] public float chatterVolume  = 0.35f;
        [Range(0f, 1f)] public float machineVolume  = 0.22f;
        [Range(0f, 1f)] public float clinkVolume    = 0.20f;
        [Range(0f, 1f)] public float whisperVolume  = 0.50f;
        [Range(0f, 1f)] public float breathingVolume = 0.60f;

        [Header("Machine / Clink — Burst Timing")]
        [Tooltip("Seconds the espresso machine sound plays each burst")]
        [Range(1f, 10f)] public float machineBurstDuration = 3.5f;
        [Tooltip("Seconds of silence between machine bursts")]
        [Range(3f, 30f)] public float machineSilenceDuration = 12f;
        [Tooltip("Seconds the clink sound plays each burst")]
        [Range(0.5f, 5f)] public float clinkBurstDuration = 1.5f;
        [Tooltip("Seconds of silence between clink bursts")]
        [Range(2f, 20f)] public float clinkSilenceDuration = 7f;

        [Header("Audio Clips — Outdoor (Street)")]
        public AudioClip streetAmbientLoop;
        public AudioClip footstepLoop;

        [Header("Outdoor Volume")]
        [Range(0f, 1f)] public float streetVolume   = 0.50f;
        [Range(0f, 1f)] public float footstepVolume = 0.25f;

        [Header("Scene Textures (optional — drag .png/.jpg from Assets)")]
        [Tooltip("Seamless hardwood or tile texture for the café floor")]
        public Texture2D floorTexture;
        [Tooltip("Seamless brick or plaster texture for the back wall")]
        public Texture2D wallTexture;
        [Tooltip("Seamless wood texture for table tops")]
        public Texture2D tableTexture;

        // ══════════════════════════════════════════════════════════════════════
        // NPC POSITIONS — edit here, tick "Rebuild NPCs Now" to see live.
        // Position Y: ~0.50 sits cleanly in a regular chair, ~0.78 sits on a stool.
        // RotationY:  180 = faces -Z   0 = faces +Z
        //              90 = faces +X   270 = faces -X
        // ══════════════════════════════════════════════════════════════════════

        // Rotation axes:  X = tilt forward/back   Y = turn left/right   Z = lean sideways
        // Default Y follows Unity's forward direction:
        // 0 = +Z, 90 = +X, 180 = -Z, 270 = -X.

        [Header("FRIENDS")]
        public Vector3 emmaPos  = new Vector3(-3.20f, 0.40f, -3.66f);
        public Vector3 emmaRot  = new Vector3(0f,   0f, 0f);

        public Vector3 jakePos  = new Vector3(-2.04f, 0.40f, -2.50f);
        public Vector3 jakeRot  = new Vector3(0f, 270f, 0f);

        public Vector3 miaPos   = new Vector3(-4.36f, 0.40f, -2.50f);
        public Vector3 miaRot   = new Vector3(0f,  90f, 0f);

        [Range(0.5f, 2f)] public float friendScale = 1.0f;

        // Table B center (3.0, 0, −3.0) → right/left chairs at X ±1.1
        [Header("BG CUSTOMERS — Table B (right-near)")]
        public Vector3 bgChat1Pos  = new Vector3( 4.16f, 0.40f, -3.00f);
        public Vector3 bgChat1Rot  = new Vector3(0f, 270f, 0f);   // right chair → faces table center
        public Vector3 bgChat2Pos  = new Vector3( 1.84f, 0.40f, -3.00f);
        public Vector3 bgChat2Rot  = new Vector3(0f,  90f, 0f);   // left chair → faces table center

        // Table C center (−1.5, 0, 1.5) → back/front chairs at Z ±1.1
        [Header("BG CUSTOMERS — Table C (centre)")]
        public Vector3 bgLaptop1Pos  = new Vector3(-1.50f, 0.40f,  2.66f);
        public Vector3 bgLaptop1Rot  = new Vector3(0f, 180f, 0f);  // north chair → faces table center
        public Vector3 bgLaptop2Pos  = new Vector3(-1.50f, 0.40f,  0.34f);
        public Vector3 bgLaptop2Rot  = new Vector3(0f,   0f, 0f);  // south chair → faces table center

        // Table D center (3.5, 0, 1.5) → left/right chairs at X ±1.1
        [Header("BG CUSTOMERS — Table D (right-mid)")]
        public Vector3 bgTalk1Pos  = new Vector3( 2.34f, 0.40f,  1.50f);
        public Vector3 bgTalk1Rot  = new Vector3(0f,  90f, 0f);   // left chair → faces table center
        public Vector3 bgTalk2Pos  = new Vector3( 4.66f, 0.40f,  1.50f);
        public Vector3 bgTalk2Rot  = new Vector3(0f, 270f, 0f);   // right chair → faces table center

        // Table E center (−4.5, 0, 3.0) → back/front chairs at Z ±1.1
        [Header("BG CUSTOMERS — Table E (far-left corner)")]
        public Vector3 bgReaderPos   = new Vector3(-4.50f, 0.40f,  4.16f);
        public Vector3 bgReaderRot   = new Vector3(0f, 180f, 0f);  // north chair → faces table center
        public Vector3 bgReader2Pos  = new Vector3(-4.50f, 0.40f,  1.84f);
        public Vector3 bgReader2Rot  = new Vector3(0f,   0f, 0f);  // south chair → faces table center

        // Counter stools at Z=6.5, Y=0.78 (stool seat). rotY=0 → faces the counter (+Z)
        [Header("COUNTER STOOL SITTERS")]
        public Vector3 stool1Pos  = new Vector3(-2.40f, 0.78f,  5.95f);
        public Vector3 stool1Rot  = new Vector3(0f,   0f, 0f);
        public Vector3 stool2Pos  = new Vector3( 1.20f, 0.78f,  5.95f);
        public Vector3 stool2Rot  = new Vector3(0f,   0f, 0f);

        [Header("WAITERS — speed and routes")]
        [Range(0.5f, 5f)] public float waiterWalkSpeed = 1.6f;
        public Vector3 w1CounterPoint = new Vector3(4.15f, 0f,  6.15f);
        public Vector3 w1TablePoint   = new Vector3(4.95f, 0f,  1.50f);
        public Vector3 w2CounterPoint = new Vector3(5.35f, 0f,  6.05f);
        public Vector3 w2TablePoint   = new Vector3(4.65f, 0f, -3.00f);

        [Header("Player")]
        [Tooltip("Where the player spawns at game start")]
        public Vector3 playerStartPos = new Vector3(0f, 1f, -6f);

        // ── Live-tweak controls ────────────────────────────────────────────────
        [Header("--- LIVE TWEAKS ---")]
        [Tooltip("Teleport player to playerStartPos right now (while playing)")]
        public bool teleportPlayerNow = false;
        [Tooltip("Clears all saved positions — next Play uses the code defaults again")]
        public bool resetSavedPositions = false;

        // ── Internal references ────────────────────────────────────────────────
        private SceneBuilder           _builder;
        private SceneRefs              _refs;
        private SimplePlayerController _player;

        // Auto-rebuild debounce (fires 0.4 s after the last Inspector value change)
        private bool  _rebuildPending = false;
        private float _rebuildTimer   = 0f;

        // ── OnValidate fires every time ANY Inspector field changes ────────────
        // (works in Play mode in Unity 6 — drives the auto-rebuild below)
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            if (_refs == null || _builder == null) return;
            _rebuildPending = true;
            _rebuildTimer   = 0f;   // reset the debounce window
        }

        private void Update()
        {
            // ── Auto-rebuild: waits 0.4 s after last value change ─────────────
            if (_rebuildPending)
            {
                _rebuildTimer += Time.deltaTime;
                if (_rebuildTimer >= 0.4f)
                {
                    _rebuildPending = false;
                    _rebuildTimer   = 0f;
                    DoRebuildNPCs();
                }
            }

            // ── Teleport player ───────────────────────────────────────────────
            if (teleportPlayerNow)
            {
                teleportPlayerNow = false;
                if (_player != null)
                    _player.transform.position = playerStartPos;
            }

            // ── Reset saved positions ─────────────────────────────────────────
            if (resetSavedPositions)
            {
                resetSavedPositions = false;
                ClearSavedPositions();
                Debug.Log("[Bootstrap] Saved positions cleared — restart Play to use code defaults.");
            }
        }

        // ── Awake: load previously saved positions BEFORE Start() builds scene ─
        private void Awake()
        {
            LoadPositionsFromPrefs();
        }

        private void DoRebuildNPCs()
        {
            if (_refs == null || _builder == null) return;

            // ── Destroy old friends ───────────────────────────────────────────
            foreach (GameObject f in _refs.friends)
                if (f != null) Destroy(f);
            _refs.friends.Clear();

            // ── Destroy old background customers + props ──────────────────────
            string[] bgNames = { "C_Chat1","C_Chat2","C_Laptop1","C_Laptop2",
                                  "C_Talk1","C_Talk2","C_Reader","C_Reader2",
                                  "C_Stool1","C_Stool2" };
            foreach (string n in bgNames)
            {
                GameObject g = GameObject.Find(n);
                if (g != null) Destroy(g);
                foreach (string pfx in new[]{ "Laptop_","Screen_","Book_" })
                {
                    GameObject prop = GameObject.Find(pfx + n);
                    if (prop != null) Destroy(prop);
                }
            }

            // ── Destroy old waiters ───────────────────────────────────────────
            foreach (string wn in new[] { "Waiter1", "Waiter2" })
            {
                GameObject w = GameObject.Find(wn);
                if (w != null) Destroy(w);
            }

            // ── Rebuild ───────────────────────────────────────────────────────
            ApplyConfigToBuilder(_builder);
            _builder.CreateFriends(_refs);
            _builder.CreateBackgroundCustomers();
            _builder.CreateWaiters();

            // Show friends so you can judge positions while playing
            foreach (GameObject f in _refs.friends)
                if (f != null) f.SetActive(true);

            // Save these positions so they survive stopping Play mode
            SavePositionsToPrefs();
            Debug.Log("[Bootstrap] NPCs rebuilt & positions saved.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // PlayerPrefs save / load  — persists NPC positions across Play sessions
        // ══════════════════════════════════════════════════════════════════════

        private const string PrefKey = "npc_v8_saved";   // bumped — forces clear waiter routes + table service fixes

        private void SavePositionsToPrefs()
        {
            // Friends
            SP("emma",  emmaPos,  emmaRot);
            SP("jake",  jakePos,  jakeRot);
            SP("mia",   miaPos,   miaRot);
            PlayerPrefs.SetFloat("npc_friendScale", friendScale);
            // Background customers
            SP("bgChat1",   bgChat1Pos,   bgChat1Rot);
            SP("bgChat2",   bgChat2Pos,   bgChat2Rot);
            SP("bgLap1",    bgLaptop1Pos, bgLaptop1Rot);
            SP("bgLap2",    bgLaptop2Pos, bgLaptop2Rot);
            SP("bgTalk1",   bgTalk1Pos,   bgTalk1Rot);
            SP("bgTalk2",   bgTalk2Pos,   bgTalk2Rot);
            SP("bgRdr",     bgReaderPos,  bgReaderRot);
            SP("bgRdr2",    bgReader2Pos, bgReader2Rot);
            SP("stool1",    stool1Pos,    stool1Rot);
            SP("stool2",    stool2Pos,    stool2Rot);
            // Waiters
            SP3("w1c", w1CounterPoint); SP3("w1t", w1TablePoint);
            SP3("w2c", w2CounterPoint); SP3("w2t", w2TablePoint);
            PlayerPrefs.SetFloat("npc_waiterSpeed", waiterWalkSpeed);
            // Player start
            SP3("playerStart", playerStartPos);
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
        }

        private void LoadPositionsFromPrefs()
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 0) return;
            // Friends
            (emmaPos,  emmaRot)  = LP("emma");
            (jakePos,  jakeRot)  = LP("jake");
            (miaPos,   miaRot)   = LP("mia");
            friendScale = PlayerPrefs.GetFloat("npc_friendScale", friendScale);
            // Background customers
            (bgChat1Pos,   bgChat1Rot)   = LP("bgChat1");
            (bgChat2Pos,   bgChat2Rot)   = LP("bgChat2");
            (bgLaptop1Pos, bgLaptop1Rot) = LP("bgLap1");
            (bgLaptop2Pos, bgLaptop2Rot) = LP("bgLap2");
            (bgTalk1Pos,   bgTalk1Rot)   = LP("bgTalk1");
            (bgTalk2Pos,   bgTalk2Rot)   = LP("bgTalk2");
            (bgReaderPos,  bgReaderRot)  = LP("bgRdr");
            (bgReader2Pos, bgReader2Rot) = LP("bgRdr2");
            (stool1Pos,    stool1Rot)    = LP("stool1");
            (stool2Pos,    stool2Rot)    = LP("stool2");
            // Waiters
            w1CounterPoint = LP3("w1c"); w1TablePoint = LP3("w1t");
            w2CounterPoint = LP3("w2c"); w2TablePoint = LP3("w2t");
            waiterWalkSpeed = PlayerPrefs.GetFloat("npc_waiterSpeed", waiterWalkSpeed);
            // Player start
            playerStartPos = LP3("playerStart");
            Debug.Log("[Bootstrap] Loaded saved NPC positions.");
        }

        private void ClearSavedPositions()
        {
            PlayerPrefs.DeleteKey(PrefKey);
            PlayerPrefs.DeleteKey("npc_v1_saved");
            PlayerPrefs.DeleteKey("npc_v2_saved");
            PlayerPrefs.DeleteKey("npc_v3_saved");
            PlayerPrefs.DeleteKey("npc_v4_saved");
            PlayerPrefs.DeleteKey("npc_v5_saved");
            PlayerPrefs.DeleteKey("npc_v6_saved");
            PlayerPrefs.DeleteKey("npc_v7_saved");
            // Also clear per-NPC NPCPoseAdjust offsets so old tweaks don't apply to new positions
            string[] npcNames = {
                "Emma","Jake","Mia",
                "C_Chat1","C_Chat2","C_Laptop1","C_Laptop2",
                "C_Talk1","C_Talk2","C_Reader","C_Reader2",
                "C_Stool1","C_Stool2",
                "Barista1","Barista2","Barista3","CounterCustomer",
                "Waiter1","Waiter2"
            };
            foreach (string n in npcNames)
            {
                foreach (string prefix in new[] { "pose_", "pose_v2_", "pose_v3_" })
                {
                    string k = prefix + n;
                    PlayerPrefs.DeleteKey(k+"_px"); PlayerPrefs.DeleteKey(k+"_py"); PlayerPrefs.DeleteKey(k+"_pz");
                    PlayerPrefs.DeleteKey(k+"_rx"); PlayerPrefs.DeleteKey(k+"_ry"); PlayerPrefs.DeleteKey(k+"_rz");
                    PlayerPrefs.DeleteKey(k+"_has");
                }
            }
            PlayerPrefs.Save();
            Debug.Log("[Bootstrap] All saved positions cleared — restart Play to use code defaults.");
        }

        // ── Compact helpers ───────────────────────────────────────────────────
        // SP / LP save/load a world position + full euler rotation (3 axes each).
        private static void SP(string k, Vector3 pos, Vector3 rot)
        {
            PlayerPrefs.SetFloat(k+"_px", pos.x); PlayerPrefs.SetFloat(k+"_py", pos.y); PlayerPrefs.SetFloat(k+"_pz", pos.z);
            PlayerPrefs.SetFloat(k+"_rx", rot.x); PlayerPrefs.SetFloat(k+"_ry", rot.y); PlayerPrefs.SetFloat(k+"_rz", rot.z);
        }
        private static void SP3(string k, Vector3 p)
        {
            PlayerPrefs.SetFloat(k+"_x", p.x); PlayerPrefs.SetFloat(k+"_y", p.y); PlayerPrefs.SetFloat(k+"_z", p.z);
        }
        private static (Vector3 pos, Vector3 rot) LP(string k)
        {
            Vector3 pos = new Vector3(PlayerPrefs.GetFloat(k+"_px"), PlayerPrefs.GetFloat(k+"_py"), PlayerPrefs.GetFloat(k+"_pz"));
            Vector3 rot = new Vector3(PlayerPrefs.GetFloat(k+"_rx", 0f), PlayerPrefs.GetFloat(k+"_ry", 180f), PlayerPrefs.GetFloat(k+"_rz", 0f));
            return (pos, rot);
        }
        private static Vector3 LP3(string k)
        {
            return new Vector3(PlayerPrefs.GetFloat(k+"_x"), PlayerPrefs.GetFloat(k+"_y"), PlayerPrefs.GetFloat(k+"_z"));
        }

        private void Start()
        {
            ConfigureLighting();
            DisableExtraAudioListeners();

            // ── Core systems ──────────────────────────────────────────────────
            UIManager             ui     = CreateUI();
            SimplePlayerController player = CreatePlayer();
            _player = player;                        // store for live teleport
            SceneRefs             refs   = BuildScene();
            AudioManager          audio  = CreateAudio();

            DisableOtherCameras(player.Camera);

            // ── Street / outdoor scene ────────────────────────────────────────
            BuildStreetScene(refs);

            // ── New enhancement systems ───────────────────────────────────────
            PostProcessingController postProc = CreatePostProcessing(ui);
            NPCDialogue              npcDlg   = CreateNPCDialogue(ui);
            ThoughtJournal           journal  = CreateJournal(ui);
            SettingsManager          settings = CreateSettings(ui, player, audio);

            // ── Software collision obstacles (PhysX is empty in Unity 6 here) ─
            RegisterCollisionObstacles();

            // ── Game director (wired last so all references are ready) ────────
            GameObject dirObj   = new GameObject("GameDirector");
            GameDirector director = dirObj.AddComponent<GameDirector>();
            director.ui             = ui;
            director.player         = player;
            director.playerCamera   = player.Camera;
            director.scene          = refs;
            director.audioManager   = audio;
            director.postProcessing = postProc;
            director.npcDialogue    = npcDlg;
            director.journal        = journal;
            director.settingsManager = settings;
            director.Initialize();
        }

        // ── Factory helpers ───────────────────────────────────────────────────
        private void ConfigureLighting()
        {
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.60f);

            GameObject lightObj = new GameObject("Directional Light");
            Light light    = lightObj.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void DisableExtraAudioListeners()
        {
            AudioListener[] all  = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            bool kept = false;
            foreach (AudioListener al in all)
            {
                if (!kept) { kept = true; continue; }
                al.enabled = false;
            }
        }

        private UIManager CreateUI()
        {
            GameObject obj = new GameObject("UIManager");
            return obj.AddComponent<UIManager>();
        }

        private SimplePlayerController CreatePlayer()
        {
            GameObject obj = new GameObject("Player");
            obj.transform.position = playerStartPos;

            CharacterController cc = obj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            SimplePlayerController player = obj.AddComponent<SimplePlayerController>();

            GameObject camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(obj.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";

            // URP post-processing is enabled via the renderer asset — no extra component needed.

            player.BindCamera(cam);
            return player;
        }

        private AudioManager CreateAudio()
        {
            GameObject obj     = new GameObject("AudioManager");
            AudioManager audio = obj.AddComponent<AudioManager>();

            // ── Clips ────────────────────────────────────────────────────────
            audio.ambientLoop       = ambientLoop;
            audio.chatterLoop       = chatterLoop;
            audio.machineLoop       = machineLoop;
            audio.clinkLoop         = clinkLoop;
            audio.whisperLoop       = whisperLoop;
            audio.breathingLoop     = breathingLoop;
            audio.streetAmbientLoop = streetAmbientLoop;
            audio.footstepLoop      = footstepLoop;

            // ── Volumes ──────────────────────────────────────────────────────
            audio.ambientVolume   = ambientVolume;
            audio.chatterVolume   = chatterVolume;
            audio.machineVolume   = machineVolume;
            audio.clinkVolume     = clinkVolume;
            audio.whisperVolume   = whisperVolume;
            audio.breathingVolume = breathingVolume;
            audio.streetVolume    = streetVolume;
            audio.footstepVolume  = footstepVolume;

            // ── Burst timing ─────────────────────────────────────────────────
            audio.machineBurstDuration   = machineBurstDuration;
            audio.machineSilenceDuration = machineSilenceDuration;
            audio.clinkBurstDuration     = clinkBurstDuration;
            audio.clinkSilenceDuration   = clinkSilenceDuration;

            audio.Init();
            return audio;
        }

        private SceneRefs BuildScene()
        {
            CharacterBuilder.ResetSeeds();   // consistent palette every Play session
            _builder = new SceneBuilder(roomWidth, roomLength, roomHeight);
            _builder.SetTextures(floorTexture, wallTexture, tableTexture);
            ApplyConfigToBuilder(_builder);
            _refs = _builder.Build();
            return _refs;
        }

        private void ApplyConfigToBuilder(SceneBuilder b)
        {
            // Friends
            b.emmaPos    = emmaPos;    b.emmaRot    = emmaRot;
            b.jakePos    = jakePos;    b.jakeRot    = jakeRot;
            b.miaPos     = miaPos;     b.miaRot     = miaRot;
            b.friendScale = friendScale;

            // Background customers
            b.bgChat1Pos    = bgChat1Pos;    b.bgChat1Rot    = bgChat1Rot;
            b.bgChat2Pos    = bgChat2Pos;    b.bgChat2Rot    = bgChat2Rot;
            b.bgLaptop1Pos  = bgLaptop1Pos;  b.bgLaptop1Rot  = bgLaptop1Rot;
            b.bgLaptop2Pos  = bgLaptop2Pos;  b.bgLaptop2Rot  = bgLaptop2Rot;
            b.bgTalk1Pos    = bgTalk1Pos;    b.bgTalk1Rot    = bgTalk1Rot;
            b.bgTalk2Pos    = bgTalk2Pos;    b.bgTalk2Rot    = bgTalk2Rot;
            b.bgReaderPos   = bgReaderPos;   b.bgReaderRot   = bgReaderRot;
            b.bgReader2Pos  = bgReader2Pos;  b.bgReader2Rot  = bgReader2Rot;
            b.stool1Pos     = stool1Pos;     b.stool1Rot     = stool1Rot;
            b.stool2Pos     = stool2Pos;     b.stool2Rot     = stool2Rot;

            // Waiters
            b.waiterWalkSpeed = waiterWalkSpeed;
            b.w1CounterPoint  = w1CounterPoint;
            b.w1TablePoint    = w1TablePoint;
            b.w2CounterPoint  = w2CounterPoint;
            b.w2TablePoint    = w2TablePoint;
        }

        private void BuildStreetScene(SceneRefs refs)
        {
            // Indoor entrance is at roughly Z = -roomLength/2 + 1.2
            float indoorEntranceZ = -roomLength / 2f + 1.2f;
            new StreetSceneBuilder(indoorEntranceZ).Build(refs);
        }

        /// <summary>
        /// Register all static obstacles (walls, tables, buildings) into the
        /// software collision system.  Called AFTER both scenes are built so
        /// all positions are known.
        /// </summary>
        private void RegisterCollisionObstacles()
        {
            SoftCollision.Clear();

            float W = roomWidth;    // 15
            float L = roomLength;   // 18
            float streetLen = 28f;
            float indoorEntranceZ = -L / 2f + 1.2f;
            float zBase = indoorEntranceZ - streetLen; // ≈ -35.8

            // ══════════════════════════════════════════════════════════════════
            // INDOOR CAFÉ walls
            // ══════════════════════════════════════════════════════════════════
            float wallT = 1f;   // virtual wall thickness for collision (larger = safer)

            // Left wall  (X = -W/2)
            SoftCollision.AddBox(
                new Vector3(-W * 0.5f - wallT * 0.5f, 2f, 0f),
                new Vector3(wallT, 5f, L + 2f));

            // Right wall  (X = +W/2)
            SoftCollision.AddBox(
                new Vector3( W * 0.5f + wallT * 0.5f, 2f, 0f),
                new Vector3(wallT, 5f, L + 2f));

            // Back wall  (Z = +L/2)
            SoftCollision.AddBox(
                new Vector3(0f, 2f,  L * 0.5f + wallT * 0.5f),
                new Vector3(W + 2f, 5f, wallT));

            // Front wall left panel  (door opening is centre 9 m wide)
            SoftCollision.AddBox(
                new Vector3(-W * 0.5f + 1.5f, 2f, -L * 0.5f),
                new Vector3(3.2f, 5f, wallT));
            // Front wall right panel
            SoftCollision.AddBox(
                new Vector3( W * 0.5f - 1.5f, 2f, -L * 0.5f),
                new Vector3(3.2f, 5f, wallT));

            // ── Tables (footprint 1.7 × 1.25 + 0.15 clearance each side) ─────
            var tablePosns = new Vector3[]
            {
                new Vector3(-3.2f, 0f, -2.5f),   // friend table
                new Vector3( 3.0f, 0f, -3.0f),
                new Vector3(-1.5f, 0f,  1.5f),
                new Vector3( 3.5f, 0f,  1.5f),
                new Vector3(-4.5f, 0f,  3.0f),
            };
            foreach (Vector3 tp in tablePosns)
                SoftCollision.AddBox(
                    new Vector3(tp.x, 0.4f, tp.z),
                    new Vector3(2.1f, 1f, 1.6f));

            // ── Counter (right-back area) ─────────────────────────────────────
            float counterZ = L * 0.5f - 0.9f;  // ≈ 8.1
            SoftCollision.AddBox(
                new Vector3(W * 0.5f - 2.5f, 0.6f, counterZ),
                new Vector3(5.5f, 1.4f, 1.8f));

            // ── Friends as small NPC obstacles ────────────────────────────────
            foreach (Vector3 friendPos in new[] { emmaPos, jakePos, miaPos })
                SoftCollision.AddBox(
                    friendPos + Vector3.up * 0.42f,
                    new Vector3(0.7f, 1.8f, 0.7f));

            // ── Side service station beside the counter ─────────────────────
            SoftCollision.AddBox(
                new Vector3(6.15f, 0.5f, 5.95f),
                new Vector3(1.2f, 1.1f, 1.0f));

            // ══════════════════════════════════════════════════════════════════
            // OUTDOOR STREET buildings
            // ══════════════════════════════════════════════════════════════════
            float streetW   = 10f;   // StreetSceneBuilder.StreetWidth
            float sidewalkW = 3.5f;  // StreetSceneBuilder.SidewalkW
            float bldDepth  = 6f;    // StreetSceneBuilder.BuildingDepth

            float xLeft  = -(streetW * 0.5f + sidewalkW + bldDepth * 0.5f); // -11.5
            float xRight =  (streetW * 0.5f + sidewalkW + bldDepth * 0.5f); //  11.5
            float streetZCenter = zBase + streetLen * 0.5f;                  // ≈ -21.8

            // Left building facades
            SoftCollision.AddBox(
                new Vector3(xLeft, 2f, streetZCenter),
                new Vector3(bldDepth + 1f, 6f, streetLen + 4f));

            // Right building facades
            SoftCollision.AddBox(
                new Vector3(xRight, 2f, streetZCenter),
                new Vector3(bldDepth + 1f, 6f, streetLen + 4f));

            // Parked cars — rough AABB per car (4 cars added by CreateParkedCars)
            // Left side cars: X ≈ -(streetW/2 + 1.2) = -6.2
            // Right side cars: X ≈ (streetW/2 + 1.2) = 6.2
            // Placed at intervals along the street
            for (int i = 0; i < 4; i++)
            {
                float carZ = zBase + 6f + i * 6f;
                SoftCollision.AddBox(
                    new Vector3(-6.2f, 0.6f, carZ), new Vector3(2.4f, 1.4f, 4.4f));
                SoftCollision.AddBox(
                    new Vector3( 6.2f, 0.6f, carZ), new Vector3(2.4f, 1.4f, 4.4f));
            }
        }

        private void DisableOtherCameras(Camera playerCamera)
        {
            if (playerCamera == null) return;
            playerCamera.enabled = true;
            playerCamera.depth = 10f;
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam == playerCamera) continue;
                cam.enabled = false;
                AudioListener al = cam.GetComponent<AudioListener>();
                if (al != null) al.enabled = false;
            }
        }

        private PostProcessingController CreatePostProcessing(UIManager ui)
        {
            GameObject obj  = new GameObject("PostProcessing");
            PostProcessingController pp = obj.AddComponent<PostProcessingController>();
            // UIManager.Awake() has already run by the time Start() fires, so
            // GetCanvasTransform() is safe to call here.
            pp.Initialize(ui.GetCanvasTransform());
            return pp;
        }

        private NPCDialogue CreateNPCDialogue(UIManager ui)
        {
            GameObject obj = new GameObject("NPCDialogue");
            NPCDialogue dlg = obj.AddComponent<NPCDialogue>();
            dlg.Initialize(ui);
            return dlg;
        }

        private ThoughtJournal CreateJournal(UIManager ui)
        {
            GameObject obj = new GameObject("ThoughtJournal");
            ThoughtJournal journal = obj.AddComponent<ThoughtJournal>();
            journal.Initialize(ui);
            return journal;
        }

        private SettingsManager CreateSettings(UIManager ui,
                                                SimplePlayerController player,
                                                AudioManager audio)
        {
            GameObject obj = new GameObject("SettingsManager");
            SettingsManager sm = obj.AddComponent<SettingsManager>();
            sm.Initialize(ui, player, audio);
            return sm;
        }
    }
}
