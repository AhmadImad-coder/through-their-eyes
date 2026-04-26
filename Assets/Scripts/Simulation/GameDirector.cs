using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OCDSimulation
{
    public class GameDirector : MonoBehaviour
    {
        [Header("References")]
        public UIManager                ui;
        public SimplePlayerController   player;
        public Camera                   playerCamera;
        public SceneRefs                scene;
        public AudioManager             audioManager;
        public PostProcessingController postProcessing;
        public NPCDialogue              npcDialogue;
        public ThoughtJournal           journal;
        public SettingsManager          settingsManager;

        [Header("Anxiety")]
        public float anxiety              = 5f;
        public float anxietyIncreaseHover = 5f;
        public float anxietyIncreaseClick = 10f;
        public float anxietyDecay         = 1.5f;

        [Header("Breathing")]
        public float inhaleTime = 4f;
        public float holdTime   = 2f;
        public float exhaleTime = 4f;

        [Header("Timers")]
        public float hugDuration   = 2f;
        public float observeDelay  = 2f;

        // ── State ─────────────────────────────────────────────────────────────
        private GamePhase phase      = GamePhase.Home;
        private SessionStats stats   = new SessionStats();

        private bool nearFriends        = false;
        private bool nearCafe           = false;  // player is at the coffee shop door
        private bool isSeated           = false;
        private bool isLookingUnderTable = false;
        private bool hasCompletedCalming = false;
        private bool inBreathing        = false;
        private bool inGrounding        = false;
        private bool _stageAdvancePending = false; // Bug-fix 1: prevents double-advance

        private float phaseTimer            = 0f;
        private float sessionStartTime      = 0f;
        private float firstScratchTime      = -1f;
        private float lastScratchClickTime  = -1f;
        private const float ScratchCooldown = 0.45f; // Bug-fix 2: debounce
        private bool  maxAnxietyTriggered   = false;
        private int   scratchAttemptCount   = 0;   // counts clicks on uncleanable scratch
        private bool  whisperEnabled        = false;
        private float friendProximity       = 3.2f;
        private int   stage                 = 0;
        private bool  orderInProgress       = false;
        private string selectedOrder        = null;
        private GameObject orderWaiter      = null;
        private int   recoveryScratchAttempts = 0;

        private Vector3    camLocalPos;
        private Quaternion camLocalRot;
        private readonly Vector3 seatedCamLocalPos     = new Vector3(0f, 1.24f, -0.06f);
        private readonly Quaternion seatedCamLocalRot  = Quaternion.Euler(0f, 0f, 0f);
        private readonly Vector3 underTableCamLocalPos = new Vector3(0.02f, 0.98f, 0.18f);
        private readonly Quaternion underTableCamLocalRot = Quaternion.Euler(50f, 0f, 0f);
        private bool gumRemovalInProgress = false;

        // ── CBT / Intrusive thoughts ──────────────────────────────────────────
        private float intrusiveTimer = 0f;
        private int   lastIntrusiveIndex = 0;

        private readonly string[] intrusiveLines =
        {
            "Clean it! CLEAN IT NOW!",
            "You can't leave it like that...",
            "Everyone is watching you.",
            "It's disgusting. FIX IT.",
            "You touched it. Your hands are dirty.",
            "Do it again. Just to be sure."
        };
        private readonly string[] rationalLines =
        {
            "Notice the thought. You don't have to act on it.",
            "This discomfort is temporary. You are safe.",
            "Your friends don't notice. This thought is OCD talking.",
            "Resisting this urge makes you stronger each time.",
            "Your hands are clean. This is an intrusive thought.",
            "One time is enough. The OCD wants you to doubt."
        };

        // ══════════════════════════════════════════════════════════════════════
        private void Start()
        {
            if (ui == null) ui = FindFirstObjectByType<UIManager>();

            ui.OnStartClicked     += StartSimulation;
            ui.OnLookUnderTable   += LookUnderTable;
            ui.OnLookBackUp       += LookBackUp;
            ui.OnRemoveGum        += RemoveGum;
            ui.OnAcceptCalming    += BeginCalming;
            ui.OnStartGrounding   += BeginGrounding;
            ui.OnReturnHome       += ReturnHome;
            ui.OnTryAgain         += RestartSimulation;
            ui.OnNarrativeComplete += OnNarrativeComplete;
            ui.OnOrderSelected    += OnOrderSelected;

            settingsManager.OnSettingsClosed += RestoreInputAfterSettings;

            if (playerCamera != null)
            {
                camLocalPos = playerCamera.transform.localPosition;
                camLocalRot = playerCamera.transform.localRotation;
            }
            else
            {
                playerCamera = Camera.main;
            }

            player?.SetInputEnabled(false);

            // Show narrative first; home screen shown after narrative completes.
            SetPhase(GamePhase.Home);
            ui.StartNarrative();
        }

        private void OnNarrativeComplete()
        {
            ui.ShowHome(true);
            ui.ShowHUD(false);
        }

        // ══════════════════════════════════════════════════════════════════════
        private void Update()
        {
            EnsureSceneRefs();
            SyncPlayerInputState();
            if (phase == GamePhase.Home)
            {
                if (KeyDown(KeyCode.Return, "enter") ||
                    KeyDown(KeyCode.Space, "space"))
                    StartSimulation();
                return;
            }
            if (phase == GamePhase.Complete) return;
            // Outdoor phase: movement + distance-based nearCafe detection + input
            if (phase == GamePhase.Outdoor)
            {
                // Physics triggers are non-functional in this project (broadphase
                // empty — confirmed by OverlapSphere returning 0).  Detect café
                // proximity by distance instead of OnTriggerEnter.
                if (scene?.coffeeShopDoorPoint != null && player != null)
                {
                    float dist = Vector3.Distance(
                        player.transform.position,
                        scene.coffeeShopDoorPoint.position);
                    bool isNear = dist < 5f;
                    if (isNear != nearCafe) SetNearCafe(isNear);
                }
                HandleInput();
                return;
            }

            stats.totalSessionTime = Time.time - sessionStartTime;

            // ── Bug-fix 3: anxiety decays in ALL active gameplay phases,
            //   faster during Recovery so it doesn't stay stubbornly high ──
            if (!inBreathing && !inGrounding && phase >= GamePhase.Stage0)
            {
                float rate = (phase == GamePhase.Recovery)
                    ? anxietyDecay * 3f
                    : anxietyDecay;
                anxiety = Mathf.Max(0f, anxiety - rate * Time.deltaTime);
            }

            UpdateAnxietyUI();
            HandleInput();
            UpdateAudio();
            UpdateIntrusiveMessages();
            UpdateFriendProximity();
            postProcessing?.UpdateEffects(anxiety, playerCamera);

            // ── Phase-specific ticks ──────────────────────────────────────────
            if (phase == GamePhase.Hugging)
            {
                phaseTimer += Time.deltaTime;
                if (phaseTimer >= hugDuration)
                {
                    SetPhase(GamePhase.Sitting);
                    ui.SetPrompt("Press [E] to sit with your friends");
                }
            }

            if (phase == GamePhase.Observing)
            {
                phaseTimer += Time.deltaTime;
                if (phaseTimer >= observeDelay)
                {
                    SetPhase(GamePhase.Stage0);
                    ShowStage0();
                }
            }

            if (phase == GamePhase.Stage3)
            {
                float rateMultiplier = SettingsManager.ComfortMode ? 0.5f : 1f;

                // Anxiety rises ONLY from player actions (hover/click on scratch),
                // NOT passively by itself.  Passive auto-increase was removed so
                // the player's stress level is entirely driven by their behaviour.
                if (!inBreathing && !inGrounding)
                    HandleScratchHover(rateMultiplier);
                else
                    ui.SetUrge("");

                // Show SPACE hint at a low threshold so the player can always trigger it
                if (anxiety >= 15f && isSeated && !inBreathing && !inGrounding)
                    ui.SetPrompt("Press [SPACE] for mindfulness  •  [J] to journal");
            }

            if (phase == GamePhase.Recovery && hasCompletedCalming)
                ui.SetPrompt("Press [E] to finish, or click the scratches if the urge pulls you back");
        }

        // ── Input helpers: dual legacy + new Input System (same pattern as
        //    SimplePlayerController — Input.GetKeyDown returns false when the
        //    project uses "New Input System Only" handling) ───────────────────
        private bool KeyDown(KeyCode legacy, string newSystemKeyName)
        {
            if (Input.GetKeyDown(legacy)) return true;
            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                var ctrl = Keyboard.current[newSystemKeyName]
                           as UnityEngine.InputSystem.Controls.KeyControl;
                if (ctrl != null && ctrl.wasPressedThisFrame) return true;
            }
            #endif
            return false;
        }

        private bool MouseDown0()
        {
            if (Input.GetMouseButtonDown(0)) return true;
            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            #endif
            return false;
        }

        // ── Input ─────────────────────────────────────────────────────────────
        private void HandleInput()
        {
            EnsureSceneRefs();
            #if UNITY_EDITOR
            if (HandleEditorDebugInput()) return;
            #endif

            if (KeyDown(KeyCode.Escape, "escape"))
            {
                if (inBreathing) { StopBreathing(); return; }
                if (inGrounding) return;
                if (journal != null && journal.IsOpen)
                {
                    journal.ToggleJournal(anxiety);
                    SyncPlayerInputState();
                    return;
                }
                if (settingsManager != null &&
                    phase >= GamePhase.Entering && phase != GamePhase.Complete)
                {
                    settingsManager.ToggleSettings();
                    SyncPlayerInputState();
                }
                return;
            }

            // Journal — available while seated and not in technique
            if (KeyDown(KeyCode.J, "j"))
            {
                if (isSeated && journal != null &&
                    !inBreathing && !inGrounding && !settingsManager.IsOpen)
                {
                    journal.ToggleJournal(anxiety);
                    SyncPlayerInputState();
                }
                return;
            }

            if (KeyDown(KeyCode.E, "e"))
            {
                // ── Street: enter the coffee shop ─────────────────────────────
                if (phase == GamePhase.Outdoor && nearCafe)
                {
                    EnterCoffeeShop();
                    return;
                }
                if (phase == GamePhase.Approaching && nearFriends)
                {
                    SetPhase(GamePhase.Hugging);
                    ui.SetMessage("Great to see you all!");
                    npcDialogue?.ShowDialogue("Friends", "Hey! Great to see you!", 3f);
                    phaseTimer = 0f;
                }
                else if (phase == GamePhase.Sitting && nearFriends) { SitDown(); }
                else if (phase == GamePhase.Recovery && hasCompletedCalming) { EndSession(); }
            }

            if (KeyDown(KeyCode.Space, "space"))
            {
                if (isSeated && anxiety >= 15f && !inBreathing && !inGrounding &&
                    !settingsManager.IsOpen)
                    StartBreathing(false);
            }

            if (MouseDown0())
                HandleClick();
        }

        private void OnOrderSelected(string order)
        {
            if (phase != GamePhase.Ordering) return;
            selectedOrder = string.IsNullOrWhiteSpace(order) ? "Nothing" : order;
        }

        #if UNITY_EDITOR
        private bool HandleEditorDebugInput()
        {
            EnsureSceneRefs();

            if (KeyDown(KeyCode.F1, "f1") && phase == GamePhase.Outdoor && scene?.coffeeShopDoorPoint != null)
            {
                Vector3 target = scene.coffeeShopDoorPoint.position + new Vector3(0f, 0f, -1.0f);
                player.TeleportTo(target, scene.coffeeShopDoorPoint.rotation);
                SetNearCafe(true);
                return true;
            }

            if (KeyDown(KeyCode.F2, "f2") &&
                (phase == GamePhase.Entering || phase == GamePhase.Approaching || phase == GamePhase.Sitting) &&
                scene?.friendTableCenter != null)
            {
                Vector3 target = scene.friendTableCenter.position + new Vector3(0f, 0.25f, -1.8f);
                player.TeleportTo(target, Quaternion.LookRotation(Vector3.forward, Vector3.up));
                SetNearFriends(true);
                return true;
            }

            if (KeyDown(KeyCode.F3, "f3") && (phase == GamePhase.Stage0 || phase == GamePhase.Stage1))
            {
                var list = phase == GamePhase.Stage0 ? scene.stage0Dirt : scene.stage1Dirt;
                DirtSpot dirt = FindNearestVisibleDirt(list);
                if (dirt != null)
                {
                    OnDirtClicked(dirt);
                    return true;
                }
            }

            if (KeyDown(KeyCode.F4, "f4") && phase == GamePhase.Stage2)
            {
                RemoveGum();
                return true;
            }

            if (KeyDown(KeyCode.F5, "f5") && (phase == GamePhase.Stage3 || (phase == GamePhase.Recovery && hasCompletedCalming)))
            {
                RegisterScratchAttempt();
                return true;
            }

            if (KeyDown(KeyCode.F6, "f6") && phase == GamePhase.Calming)
            {
                BeginCalming();
                return true;
            }

            if (KeyDown(KeyCode.F7, "f7") && phase == GamePhase.Calming)
            {
                BeginGrounding();
                return true;
            }

            if (KeyDown(KeyCode.F9, "f9") && phase == GamePhase.Ordering)
            {
                OnOrderSelected("Latte");
                return true;
            }

            if (KeyDown(KeyCode.F8, "f8") && isSeated && !isLookingUnderTable)
            {
                player.SetCameraPose(seatedCamLocalPos, seatedCamLocalRot);
                return true;
            }

            return false;
        }
        #endif

        private void EnsureSceneRefs()
        {
            scene ??= new SceneRefs();

            scene.streetSceneRoot ??= GameObject.Find("StreetSceneRoot");
            scene.coffeeShopDoorPoint ??= GameObject.Find("CafeEntryTrigger")?.transform;
            scene.friendTableCenter ??= GameObject.Find("FriendTableCenter")?.transform;
            scene.playerSeatPoint ??= GameObject.Find("PlayerSeatPoint")?.transform;
            scene.lookUnderTablePoint ??= GameObject.Find("LookUnderPoint")?.transform;
            scene.entrancePoint ??= GameObject.Find("EntrancePoint")?.transform;
            scene.orderCounterPoint ??= GameObject.Find("OrderCounterPoint")?.transform;
            scene.orderTablePoint ??= GameObject.Find("OrderTablePoint")?.transform;
            scene.gumResidue ??= GameObject.Find("GumResidue");
            scene.gumSpot ??= FindFirstObjectByType<GumSpot>(FindObjectsInactive.Include);

            if (scene.scratches == null || scene.scratches.Count == 0)
                scene.scratches = new List<ScratchSpot>(FindObjectsByType<ScratchSpot>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None));

            if (scene.friends == null || scene.friends.Count < 3)
            {
                scene.friends = new List<GameObject>();
                foreach (string friendName in new[] { "Emma", "Jake", "Mia" })
                {
                    GameObject friend = GameObject.Find(friendName);
                    if (friend != null)
                        scene.friends.Add(friend);
                }
            }

            if (scene.stage0Dirt == null || scene.stage0Dirt.Count == 0 ||
                scene.stage1Dirt == null || scene.stage1Dirt.Count == 0)
            {
                scene.stage0Dirt = new List<DirtSpot>();
                scene.stage1Dirt = new List<DirtSpot>();

                DirtSpot[] allDirt = FindObjectsByType<DirtSpot>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (DirtSpot dirt in allDirt)
                {
                    Vector3 local = dirt.transform.localPosition;
                    if (Mathf.Abs(local.z) > 0.2f)
                        scene.stage1Dirt.Add(dirt);
                    else
                        scene.stage0Dirt.Add(dirt);
                }
            }
        }

        private void HandleClick()
        {
            if (phase == GamePhase.Stage0 || phase == GamePhase.Stage1)
            {
                // Physics.Raycast is broken (broadphase empty) — use distance+angle fallback directly
                var list = (phase == GamePhase.Stage0)
                    ? scene.stage0Dirt : scene.stage1Dirt;
                DirtSpot dirt = FindNearestVisibleDirt(list);
                if (dirt != null) OnDirtClicked(dirt);
            }
            else if (phase == GamePhase.Stage3 || (phase == GamePhase.Recovery && hasCompletedCalming))
            {
                // Physics.Raycast is broken — use physics-free scratch detection
                ScratchSpot scratch = FindNearestVisibleScratch();
                if (scratch != null) RegisterScratchAttempt();
            }
        }

        private void HandleScratchHover(float rateMultiplier = 1f)
        {
            // Physics.Raycast always returns null here — use distance+angle detection
            ScratchSpot scratch = FindNearestVisibleScratch();
            if (scratch != null)
            {
                float rate = (anxiety < 50f ? 3f : 8f) * rateMultiplier;
                anxiety = Mathf.Min(100f, anxiety + rate * Time.deltaTime);
                ui.SetUrge("These scratches can't be cleaned… resist the urge.");
            }
            else
            {
                ui.SetUrge("");
            }

            if (anxiety >= 100f && !maxAnxietyTriggered)
            {
                maxAnxietyTriggered = true;
                if (stats.timesReachedMaxAnxiety == 0)
                    stats.timeToResist = Time.time - firstScratchTime;
                stats.timesReachedMaxAnxiety++;
                SetPhase(GamePhase.Calming);
                ui.SetMessage("Your anxiety peaked. Choose a calming technique, then decide for yourself when to finish.");
                ui.ShowCalming(true);
            }
            if (anxiety < 100f) maxAnxietyTriggered = false;
        }

        private void RegisterScratchAttempt()
        {
            // Bug-fix 2: debounce rapid clicks
            if (Time.time - lastScratchClickTime < ScratchCooldown) return;
            lastScratchClickTime = Time.time;

            scratchAttemptCount++;   // track for whisper threshold
            anxiety = Mathf.Min(100f, anxiety + anxietyIncreaseClick);
            stats.totalScratchAttempts++;

            if (firstScratchTime < 0f) firstScratchTime = Time.time;

            if (!hasCompletedCalming)
            {
                stats.scratchAttemptsBeforeCalming++;
            }
            else
            {
                stats.scratchAttemptsAfterCalming++;
                recoveryScratchAttempts++;

                if (anxiety >= 70f && recoveryScratchAttempts >= 2)
                {
                    stats.relapseCount++;
                    hasCompletedCalming = false;
                    recoveryScratchAttempts = 0;
                    SetPhase(GamePhase.Calming);
                    ui.ShowCalming(true);
                    ui.SetMessage("The urge surged again. Choose a technique, then decide when you are ready to finish.");
                    npcDialogue?.ShowDialogue("Mia", "Hey — breathe. You've got this.", 3f);
                    return;
                }

                ui.SetMessage("The urge is back, but you can keep accepting that the scratches cannot be cleaned.");
                ui.SetPrompt("Press [E] to finish, or keep resisting the urge");
                return;
            }
            ui.SetMessage("The urge is so strong… resist.");
        }

        // ── Simulation start / restart ────────────────────────────────────────
        private void StartSimulation()
        {
            ui.ShowHome(false);
            ui.ShowHUD(true);
            sessionStartTime     = Time.time;
            stats                = new SessionStats();
            anxiety              = 5f;
            nearCafe             = false;
            _stageAdvancePending = false;
            firstScratchTime     = -1f;
            lastScratchClickTime = -1f;
            scratchAttemptCount  = 0;
            whisperEnabled       = false;
            nearFriends          = false;
            isSeated             = false;
            hasCompletedCalming  = false;
            inBreathing          = false;
            inGrounding          = false;
            gumRemovalInProgress = false;
            orderInProgress      = false;
            selectedOrder        = null;
            recoveryScratchAttempts = 0;
            ui.ShowOrderPanel(false);
            if (orderWaiter != null)
            {
                Destroy(orderWaiter);
                orderWaiter = null;
            }
            SetInteriorWindowTextVisible(false);

            journal?.ResetJournal();
            player?.SetCameraPose(camLocalPos, camLocalRot);

            // ── Start on the street ───────────────────────────────────────────
            if (scene?.streetSpawnPoint != null)
            {
                SetPhase(GamePhase.Outdoor);
                player.LockMovement(false);
                player.SetInputEnabled(true);
                // Spawn exactly at the street spawn marker — CharacterController.center=(0,0.9,0)
                // already places the capsule feet at Y=0, so no extra offset is needed.
                player.TeleportTo(scene.streetSpawnPoint.position,
                                  scene.streetSpawnPoint.rotation);
                ui.SetPrompt("Walk to Brewed Grounds — you're meeting Emma, Jake and Mia");
                audioManager?.SetOutdoorMode();
            }
            else
            {
                // Fallback: no street scene, go straight inside
                SpawnInsideCafe();
            }
        }

        private void SpawnInsideCafe()
        {
            SetInteriorWindowTextVisible(true);
            SetPhase(GamePhase.Entering);
            ui.SetPrompt("Find your friends at their table");
            player.LockMovement(false);
            player.SetInputEnabled(true);
            if (scene?.entrancePoint != null)
                player.TeleportTo(scene.entrancePoint.position,
                                  scene.entrancePoint.rotation);

            // Reveal the entire friend NPC (body + label) now that the player
            // is inside.  The NPCs start SetActive(false) so they can't be seen
            // through the café window glass from the street.
            if (scene?.friends != null)
            {
                foreach (GameObject friend in scene.friends)
                {
                    if (friend == null) continue;
                    friend.SetActive(true);   // reveal full body

                    // Also activate the floating name label (first TextMesh child)
                    foreach (Transform child in friend.transform)
                    {
                        if (child.GetComponent<TextMesh>() != null)
                        {
                            child.gameObject.SetActive(true);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>Called by CafeEntryTrigger when player is at the door.</summary>
        public void SetNearCafe(bool near)
        {
            nearCafe = near;
            if (near && phase == GamePhase.Outdoor)
                ui.SetPrompt("Press [E] to enter Brewed Grounds");
            else if (!near && phase == GamePhase.Outdoor)
                ui.SetPrompt("Walk to Brewed Grounds — you're meeting Emma, Jake and Mia");
        }

        private void EnterCoffeeShop()
        {
            nearCafe = false;
            audioManager?.TransitionToIndoor();
            StartCoroutine(FadeInToCafe());
        }

        private IEnumerator FadeInToCafe()
        {
            ui.SetMessage("The door chime rings as you step inside.");
            yield return OpenExteriorDoorRoutine();

            // Hide the entire street scene — deactivating the root removes all
            // outdoor geometry, signs and TextMesh labels in one call so nothing
            // bleeds through the café walls.
            if (scene?.streetSceneRoot != null)
                scene.streetSceneRoot.SetActive(false);

            SpawnInsideCafe();
            npcDialogue?.ShowDialogue("Jake", "Alex! Over here — we saved you a seat!", 4f);
        }

        private IEnumerator OpenExteriorDoorRoutine()
        {
            Transform left = GameObject.Find("CafeDoorLeft")?.transform;
            Transform right = GameObject.Find("CafeDoorRight")?.transform;
            Transform bell = GameObject.Find("DoorBell")?.transform;

            if (left == null || right == null)
            {
                yield return new WaitForSeconds(0.45f);
                yield break;
            }

            Vector3 leftStart = left.localPosition;
            Vector3 rightStart = right.localPosition;
            Quaternion leftRot = left.localRotation;
            Quaternion rightRot = right.localRotation;
            Vector3 bellStart = bell != null ? bell.localScale : Vector3.one;

            float t = 0f;
            const float duration = 0.45f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float e = Mathf.SmoothStep(0f, 1f, t / duration);
                left.localPosition = Vector3.Lerp(leftStart, leftStart + new Vector3(-0.42f, 0f, 0.05f), e);
                right.localPosition = Vector3.Lerp(rightStart, rightStart + new Vector3(0.42f, 0f, 0.05f), e);
                left.localRotation = Quaternion.Slerp(leftRot, Quaternion.Euler(0f, -14f, 0f), e);
                right.localRotation = Quaternion.Slerp(rightRot, Quaternion.Euler(0f, 14f, 0f), e);
                if (bell != null)
                {
                    float pulse = 1f + Mathf.Sin(e * Mathf.PI * 4f) * 0.12f;
                    bell.localScale = bellStart * pulse;
                }
                yield return null;
            }
        }

        private void SetInteriorWindowTextVisible(bool visible)
        {
            string[] names =
            {
                "WelcomeText",
                "MenuHeader",
                "MenuItem1", "MenuItem2", "MenuItem3", "MenuItem4",
                "MenuPrice1", "MenuPrice2", "MenuPrice3", "MenuPrice4"
            };

            foreach (string objectName in names)
            {
                GameObject go = GameObject.Find(objectName);
                if (go == null)
                {
                    foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
                    {
                        if (candidate.name == objectName)
                        {
                            go = candidate;
                            break;
                        }
                    }
                }
                if (go != null) go.SetActive(visible);
            }
        }

        private void RestartSimulation() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

        private void ReturnHome() =>
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

        // ── Phase management ──────────────────────────────────────────────────
        private void SetPhase(GamePhase newPhase)
        {
            phase = newPhase;
            ui.SetPhase($"Phase: {newPhase}");
            npcDialogue?.OnPhaseChanged(newPhase);
        }

        public void SetNearFriends(bool near)
        {
            nearFriends = near;
            if (phase == GamePhase.Entering && nearFriends)
            {
                SetPhase(GamePhase.Approaching);
                ui.SetPrompt("Press [E] to greet your friends");
            }
            else if (phase == GamePhase.Approaching && !nearFriends)
            {
                SetPhase(GamePhase.Entering);
                ui.SetPrompt("Find your friends at their table");
            }
        }

        private void UpdateFriendProximity()
        {
            if (scene?.friendTableCenter == null || player == null) return;
            if (phase != GamePhase.Entering && phase != GamePhase.Approaching) return;
            float dist = Vector3.Distance(player.transform.position,
                                          scene.friendTableCenter.position);
            bool near = dist <= friendProximity;
            if (near != nearFriends) SetNearFriends(near);
        }

        // ── Sit down ──────────────────────────────────────────────────────────
        private void SitDown()
        {
            isSeated = true;
            isLookingUnderTable = false;
            player.LockMovement(true);
            player.ForceLockCursor();
            if (scene?.playerSeatPoint != null)
            {
                Quaternion rot = scene.playerSeatPoint.rotation;
                if (scene.friendTableCenter != null)
                {
                    Vector3 dir = scene.friendTableCenter.position
                                  - scene.playerSeatPoint.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.01f)
                        rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
                player.TeleportTo(scene.playerSeatPoint.position, rot);
            }
            player.SetCameraPose(seatedCamLocalPos, seatedCamLocalRot);
            ui.SetPrompt("The server is coming to take your order");
            npcDialogue?.ShowDialogue("Jake", "It's so nice to finally sit down!", 3f);
            SetPhase(GamePhase.Ordering);
            StartCoroutine(OrderAtTableRoutine());
        }

        private IEnumerator OrderAtTableRoutine()
        {
            if (orderInProgress) yield break;
            orderInProgress = true;
            selectedOrder = null;

            Vector3 counterPos = scene?.orderCounterPoint != null
                ? scene.orderCounterPoint.position
                : new Vector3(4.8f, 0f, 7.5f);
            Vector3 tablePos = scene?.orderTablePoint != null
                ? scene.orderTablePoint.position
                : player.transform.position + player.transform.right * 1.4f;

            orderWaiter = CharacterBuilder.CreateBarista("OrderServer", counterPos,
                new Color(0.20f, 0.50f, 0.34f), faceTowardsNegZ: false, scale: 1.12f);
            AddMenuProp(orderWaiter.transform);

            yield return MoveOrderWaiter(counterPos, tablePos, 1.45f);
            FaceToward(orderWaiter.transform, scene?.friendTableCenter != null
                ? scene.friendTableCenter.position
                : player.transform.position);

            npcDialogue?.ShowDialogue("Server", "Hi, what can I get started for you?", 5f);
            ui.SetPrompt("Choose a coffee order from the menu");
            ui.ShowOrderPanel(true);
            player.SetInputEnabled(false);
            player.LockMovement(true);

            while (selectedOrder == null)
                yield return null;

            ui.ShowOrderPanel(false);
            string response = selectedOrder == "Nothing"
                ? "No problem. I'll give you a few minutes."
                : $"Great, I'll start the {selectedOrder}.";
            npcDialogue?.ShowDialogue("Server", response, 3f);
            ui.SetMessage("The server heads back to prepare the order.");

            yield return new WaitForSeconds(0.65f);
            yield return MoveOrderWaiter(tablePos, counterPos, 1.45f);
            if (orderWaiter != null)
            {
                Destroy(orderWaiter);
                orderWaiter = null;
            }

            orderInProgress = false;
            player.SetInputEnabled(true);
            player.ForceLockCursor();
            SetPhase(GamePhase.Observing);
            ui.SetPrompt("Sit with the discomfort");
            phaseTimer = 0f;
        }

        private IEnumerator MoveOrderWaiter(Vector3 from, Vector3 to, float seconds)
        {
            if (orderWaiter == null) yield break;
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                float e = Mathf.SmoothStep(0f, 1f, t / seconds);
                Vector3 pos = Vector3.Lerp(from, to, e);
                orderWaiter.transform.position = pos;
                FaceToward(orderWaiter.transform, to);
                yield return null;
            }
            orderWaiter.transform.position = to;
        }

        private void FaceToward(Transform actor, Vector3 target)
        {
            if (actor == null) return;
            Vector3 dir = target - actor.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                actor.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void AddMenuProp(Transform waiter)
        {
            if (waiter == null) return;
            GameObject menu = GameObject.CreatePrimitive(PrimitiveType.Cube);
            menu.name = "HeldMenu";
            menu.transform.SetParent(waiter, false);
            menu.transform.localPosition = new Vector3(-0.22f, 0.58f, 0.24f);
            menu.transform.localRotation = Quaternion.Euler(18f, 0f, -10f);
            menu.transform.localScale = new Vector3(0.22f, 0.02f, 0.30f);
            Destroy(menu.GetComponent<Collider>());
            Renderer rend = menu.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = new Color(0.92f, 0.82f, 0.58f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mat.color);
            rend.material = mat;
        }

        // ── Stage transitions ─────────────────────────────────────────────────
        private void ShowStage0()
        {
            stage = 0;
            SetActiveList(scene.stage0Dirt, true);
            // Bug-fix 1: prompt shown immediately (not inside a delayed coroutine)
            ui.SetPrompt("You notice dirt stains on the table. Click to clean… or resist?");
            ui.SetMessage("You notice dark stains on the table surface.");
            anxiety = Mathf.Min(100f, anxiety + 15f); // seeing dirt raises anxiety
        }

        private void ShowStage1()
        {
            stage = 1;
            SetActiveList(scene.stage1Dirt, true);
            ui.SetPrompt("More dirt appeared. Do you have to clean it?");
            ui.SetMessage("Wait — there's more. Your chest tightens.");
            anxiety = Mathf.Min(100f, anxiety + 10f);
        }

        private void ShowStage2()
        {
            stage = 2;
            if (scene.gumSpot != null) scene.gumSpot.gameObject.SetActive(true);
            ui.ShowGumPanel(true);
            ui.SetPrompt("Something is wrong under the table…");
            ui.SetMessage("Is that GUM stuck under the table?!");
            player.SetInputEnabled(false);
        }

        private void ShowStage3()
        {
            stage = 3;
            stats.anxietyAtStage3Entry = anxiety;
            SetActiveList(scene.scratches, true);
            ui.SetMessage("Those scratches on the table… they're really bothering you.");
            ui.SetPrompt("Resist the urge.  [SPACE] = mindfulness  |  [J] = journal");
            SetPhase(GamePhase.Stage3);
            ui.ShowGumPanel(false);
            player.SetInputEnabled(true);
            player.ForceLockCursor();
        }

        private IEnumerator AdvanceStageAfterDelay(float delay)
        {
            // Bug-fix 1: immediate prompt update so UI never looks stuck
            ui.SetPrompt("Processing…");
            yield return new WaitForSeconds(delay);
            _stageAdvancePending = false;
            if (stage == 0) { ShowStage1(); SetPhase(GamePhase.Stage1); }
            else if (stage == 1) { ShowStage2(); SetPhase(GamePhase.Stage2); }
        }

        public void OnDirtClicked(DirtSpot dirt)
        {
            if (phase != GamePhase.Stage0 && phase != GamePhase.Stage1) return;
            if (dirt == null || dirt.cleaned) return;
            // Bug-fix 1+2: prevent multiple coroutine starts
            if (_stageAdvancePending) return;

            dirt.Clean();
            _stageAdvancePending = true;

            ui.SetMessage(phase == GamePhase.Stage0
                ? "You cleaned it. That felt good… but did it really help?"
                : "Cleaned again. The urge is satisfied… for now.");

            StartCoroutine(AdvanceStageAfterDelay(2f));
        }

        // ── Gum interactions ──────────────────────────────────────────────────
        private void LookUnderTable()
        {
            isLookingUnderTable = true;
            player.SetCameraPose(underTableCamLocalPos, underTableCamLocalRot);
            ui.UpdateGumPanel(true);
            npcDialogue?.ShowDialogue("Emma", "Are you ok? You look uncomfortable…", 3f);
        }

        private void LookBackUp()
        {
            isLookingUnderTable = false;
            player.SetCameraPose(seatedCamLocalPos, seatedCamLocalRot);
            ui.UpdateGumPanel(false);
        }

        private void RemoveGum()
        {
            if (gumRemovalInProgress) return;
            StartCoroutine(RemoveGumRoutine());
        }

        private IEnumerator RemoveGumRoutine()
        {
            gumRemovalInProgress = true;
            if (scene?.gumSpot == null)
            {
                gumRemovalInProgress = false;
                yield break;
            }

            Transform gum = scene.gumSpot.transform;
            Vector3 startPos = gum.position;
            Vector3 startScale = gum.localScale;
            Vector3 targetPos = playerCamera.transform.position
                                + playerCamera.transform.forward * 0.24f
                                - playerCamera.transform.up * 0.10f;

            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float eased = Mathf.SmoothStep(0f, 1f, t / 0.35f);
                gum.position = Vector3.Lerp(startPos, targetPos, eased);
                gum.localScale = Vector3.Lerp(startScale, startScale * 0.35f, eased);
                yield return null;
            }

            scene.gumSpot.Remove();
            if (scene.gumResidue != null)
                scene.gumResidue.SetActive(true);

            isLookingUnderTable = false;
            player.SetCameraPose(seatedCamLocalPos, seatedCamLocalRot);
            ui.UpdateGumPanel(false);
            ui.ShowGumPanel(false);
            ui.SetMessage("You peeled the gum off, but the sticky residue still reminds you it was there.");
            gumRemovalInProgress = false;
            StartCoroutine(AdvanceToStage3AfterDelay(1.6f));
        }

        private IEnumerator AdvanceToStage3AfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowStage3();
        }

        // ── Calming techniques ────────────────────────────────────────────────
        private void BeginCalming()
        {
            ui.ShowCalming(false);
            StartBreathing(true);
            SyncPlayerInputState();
        }

        private void BeginGrounding()
        {
            ui.ShowCalming(false);
            StartGrounding();
            SyncPlayerInputState();
        }

        private void StartBreathing(bool isCalming)
        {
            inBreathing = true;
            // Silence whispers immediately when the player starts a technique
            whisperEnabled = false;
            audioManager?.SetWhispers(false);

            stats.breathingExercisesUsed++;
            if (!stats.techniquesUsed.Contains("breathing"))
            {
                stats.techniquesUsed.Add("breathing");
                if (stats.techniquesUsed.Contains("grounding"))
                    stats.bothTechniquesUsed = true;
            }
            ui.ShowBreathing(true);
            audioManager?.EnterCalmingMode();   // fade out all café sounds
            audioManager?.SetBreathing(true);   // start breathing audio alone
            StartCoroutine(BreathingRoutine(isCalming));
        }

        private IEnumerator BreathingRoutine(bool isCalming)
        {
            int cycles = isCalming ? 2 : 1;
            for (int i = 0; i < cycles; i++)
            {
                yield return DoBreath("Breathe In",  0f, 1f, inhaleTime);
                yield return DoBreath("Hold",        1f, 1f, holdTime);
                yield return DoBreath("Breathe Out", 1f, 0f, exhaleTime);
                stats.breathingCyclesCompleted++;
                anxiety = Mathf.Max(0f, anxiety - 12f);
            }
            if (isCalming)
            {
                // Bug-fix 3: force anxiety to a manageable level after calming
                anxiety = Mathf.Min(anxiety, 28f);
                hasCompletedCalming = true;
                recoveryScratchAttempts = 0;
                SetPhase(GamePhase.Recovery);
                ui.SetMessage("Your breathing slowed the panic. Stay with the discomfort until you decide you're ready to finish.");
                ui.SetPrompt("Press [E] to finish, or click the scratches if OCD pulls you back");
            }
            StopBreathing();
        }

        private IEnumerator DoBreath(string label, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                ui.UpdateBreathing(label, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }
        }

        private void StopBreathing()
        {
            inBreathing = false;
            ui.ShowBreathing(false);
            audioManager?.SetBreathing(false);  // stop breathing audio
            audioManager?.ExitCalmingMode();    // fade café sounds back in
        }

        // ── 5-4-3-2-1 Grounding technique ────────────────────────────────────
        private void StartGrounding()
        {
            inGrounding = true;
            stats.groundingUsed = true;
            if (!stats.techniquesUsed.Contains("grounding"))
            {
                stats.techniquesUsed.Add("grounding");
                if (stats.techniquesUsed.Contains("breathing"))
                    stats.bothTechniquesUsed = true;
            }
            player.SetInputEnabled(false);
            ui.ShowGrounding(true);
            audioManager?.EnterCalmingMode();   // fade out all café sounds
            StartCoroutine(GroundingRoutine());
        }

        private IEnumerator GroundingRoutine()
        {
            var steps = new (int count, string sense, string items)[]
            {
                (5, "SEE",
                 "  • A wooden table\n  • Coffee mugs on the counter\n" +
                 "  • Your friends' faces\n  • Sunlight through the window\n" +
                 "  • The menu board on the wall"),
                (4, "TOUCH",
                 "  • Your chair beneath you\n  • The table surface\n" +
                 "  • Your own hands\n  • Your clothes against your skin"),
                (3, "HEAR",
                 "  • The coffee machine humming\n  • Quiet chatter around you\n" +
                 "  • Your own steady breathing"),
                (2, "SMELL",
                 "  • Fresh coffee brewing\n  • Pastries from the counter"),
                (1, "TASTE",
                 "  • The flavour of your coffee")
            };

            for (int i = 0; i < steps.Length; i++)
            {
                var s = steps[i];
                ui.UpdateGrounding(
                    $"Name {s.count} things you can {s.sense}",
                    s.items,
                    i + 1,
                    steps.Length);
                anxiety = Mathf.Max(0f, anxiety - 8f);
                yield return new WaitForSeconds(4.5f);
            }

            // Bug-fix 3 (for grounding path): force anxiety down
            anxiety = Mathf.Min(anxiety, 28f);
            inGrounding = false;
            hasCompletedCalming = true;
            recoveryScratchAttempts = 0;
            ui.ShowGrounding(false);
            ui.SetMessage("You've anchored yourself in the present. You can finish when you genuinely accept the discomfort.");
            ui.SetPrompt("Press [E] to finish, or click the scratches if OCD pulls you back");
            audioManager?.ExitCalmingMode();    // fade café sounds back in
            SetPhase(GamePhase.Recovery);
        }

        // ── Session end ───────────────────────────────────────────────────────
        private void EndSession()
        {
            stats.finalAnxiety        = anxiety;
            stats.peakAnxiety         = Mathf.Max(stats.peakAnxiety, anxiety);
            stats.resistanceSuccessful = true;
            stats.totalSessionTime    = Time.time - sessionStartTime;
            if (journal != null) stats.journalEntries = journal.EntryCount;

            // Read the previous run's relapse count BEFORE overwriting it
            int prevRunRelapses = PlayerPrefs.GetInt("LastRunRelapses", -1);

            int prevBest   = PlayerPrefs.GetInt("BestRelapseCount", 9999);
            bool newRecord = stats.relapseCount < prevBest;
            if (newRecord) PlayerPrefs.SetInt("BestRelapseCount", stats.relapseCount);
            PlayerPrefs.SetInt("TotalRuns",        PlayerPrefs.GetInt("TotalRuns", 0) + 1);
            PlayerPrefs.SetInt("LastRunRelapses",  stats.relapseCount);   // persist for next run
            PlayerPrefs.Save();

            StartCoroutine(ShowCompletionFlow(newRecord, prevRunRelapses));
        }

        private IEnumerator ShowCompletionFlow(bool newRecord, int prevRunRelapses)
        {
            SetPhase(GamePhase.Complete);
            player?.LockMovement(true);
            player?.SetInputEnabled(false);
            ui.ShowHUD(false);
            postProcessing?.ResetEffects(playerCamera);

            ui.ShowCelebration(true);
            ui.SetCelebration(
                newRecord ? "★  New Personal Best!  ★" : "Session Complete!",
                GetBadge() + "  —  " + GetErpGrade() + " Grade");
            yield return new WaitForSeconds(3f);
            ui.ShowCelebration(false);

            // Evaluate achievements
            List<Achievement> unlocked = AchievementSystem.Evaluate(stats);

            int totalRuns = PlayerPrefs.GetInt("TotalRuns", 1);

            // Build the subtitle: run number + optional improvement trend
            string subtitle = newRecord ? "★ Personal Best!  Run #" + totalRuns
                                        : "Run #" + totalRuns;

            if (prevRunRelapses >= 0 && totalRuns > 1)
            {
                int delta = prevRunRelapses - stats.relapseCount;
                if (delta > 0)
                    subtitle += $"    |    {prevRunRelapses} → {stats.relapseCount} relapses  ▲ Improved!";
                else if (delta < 0)
                    subtitle += $"    |    {prevRunRelapses} → {stats.relapseCount} relapses";
                else
                    subtitle += $"    |    Same as last run  ({stats.relapseCount} relapse{(stats.relapseCount == 1 ? "" : "s")})";
            }

            ui.ShowCompletion(true);
            ui.SetCompletionStats(
                "Your ERP Session Complete",
                subtitle,
                GetBadge(),
                BuildStatsText(),
                BuildInsightsText());

            foreach (var ach in unlocked)
                ui.AddAchievementToCompletion(ach.name, ach.description, ach.color);

            ui.SetEducationalFact(EducationalPanel.GetRandomFact());

            if (journal != null && journal.EntryCount > 0)
                ui.SetJournalSummary(
                    $"You logged {journal.EntryCount} thought(s) in the Thought Journal this session.");
        }

        // ── Stats / grading helpers ───────────────────────────────────────────
        private string GetBadge()
        {
            if (stats.relapseCount == 0)  return "OCD Champion";
            if (stats.relapseCount <= 2)  return "Recovery Warrior";
            if (stats.relapseCount <= 4)  return "Progress Made";
            return "Keep Practicing";
        }

        private string GetErpGrade()
        {
            if (stats.relapseCount == 0 && stats.totalScratchAttempts < 6) return "S";
            if (stats.relapseCount <= 2) return "A";
            if (stats.relapseCount <= 4) return "B";
            return "C";
        }

        private string GetMindfulnessGrade(int cycles)
        {
            if (cycles >= 6) return "A+";
            if (cycles >= 4) return "A";
            if (cycles >= 2) return "B";
            return "C";
        }

        private string BuildStatsText()
        {
            float reduced = stats.peakAnxiety > 0f
                ? (stats.peakAnxiety - stats.finalAnxiety) / stats.peakAnxiety * 100f
                : 0f;
            int impulse = Mathf.Clamp(
                100 - stats.scratchAttemptsAfterCalming * 10 - stats.relapseCount * 15,
                0, 100);
            string techniques = stats.techniquesUsed.Count > 0
                ? string.Join(", ", stats.techniquesUsed) : "none";

            return
                $"Urge Attempts: {stats.totalScratchAttempts}    Relapses: {stats.relapseCount}\n" +
                $"Peak Anxiety: {Mathf.RoundToInt(stats.peakAnxiety)}%    Final Anxiety: {Mathf.RoundToInt(stats.finalAnxiety)}%\n" +
                $"Anxiety Reduced: {Mathf.RoundToInt(reduced)}%    Impulse Control: {impulse}/100\n\n" +
                $"Breathing Cycles: {stats.breathingCyclesCompleted}    Techniques: {techniques}\n" +
                $"Journal Entries: {stats.journalEntries}    Session: {FormatTime(stats.totalSessionTime)}\n\n" +
                $"Mindfulness Grade: {GetMindfulnessGrade(stats.breathingCyclesCompleted)}    ERP Grade: {GetErpGrade()}";
        }

        private string BuildInsightsText()
        {
            if (stats.relapseCount == 0)
                return "Excellent impulse control! You sat with the discomfort without acting on compulsions — " +
                       "this is exactly what successful ERP therapy looks like.";
            if (stats.relapseCount <= 2)
                return "You experienced setbacks but returned to mindfulness each time. " +
                       "Recovery is not linear — every moment of awareness counts.";
            return "The urge to clean can feel overwhelming. Every time you try to resist, " +
                   "even briefly, you are retraining your brain to tolerate uncertainty.";
        }

        private string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }

        // ── Audio / Intrusive updates ─────────────────────────────────────────
        private void UpdateAnxietyUI()
        {
            if (anxiety > stats.peakAnxiety) stats.peakAnxiety = anxiety;
            ui.SetAnxiety(anxiety);
        }

        private void UpdateAudio()
        {
            // Whispers activate ONLY when ALL of:
            //   1. Player is in Stage3 (the uncleanable scratch phase)
            //   2. Anxiety is above 30 %
            //   4. Player is NOT in a calming technique (breathing / grounding)
            // They are turned off immediately when the player starts a technique.
            bool shouldWhisper = phase == GamePhase.Stage3
                                 && anxiety >= 30f
                                 && !inBreathing
                                 && !inGrounding;

            if (shouldWhisper != whisperEnabled)
            {
                whisperEnabled = shouldWhisper;
                audioManager?.SetWhispers(whisperEnabled);
            }
        }

        private void UpdateIntrusiveMessages()
        {
            if (phase != GamePhase.Stage3)
            {
                ui.ClearIntrusiveThought();
                return;
            }
            if (anxiety < 60f)
            {
                ui.ClearIntrusiveThought();
                return;
            }
            intrusiveTimer -= Time.deltaTime;
            if (intrusiveTimer <= 0f)
            {
                lastIntrusiveIndex = Random.Range(0, intrusiveLines.Length);
                ui.SetIntrusiveThought(
                    intrusiveLines[lastIntrusiveIndex],
                    rationalLines[lastIntrusiveIndex]);
                intrusiveTimer = 4f;
            }
        }

        // ── Input restoration after settings close ────────────────────────────
        private void RestoreInputAfterSettings()
        {
            SyncPlayerInputState();
        }

        private void SyncPlayerInputState()
        {
            if (player == null) return;

            bool uiOwnsCursor =
                phase == GamePhase.Home ||
                phase == GamePhase.Stage2 ||
                phase == GamePhase.Ordering ||
                phase == GamePhase.Calming ||
                phase == GamePhase.Complete ||
                gumRemovalInProgress ||
                (journal != null && journal.IsOpen) ||
                (settingsManager != null && settingsManager.IsOpen) ||
                inBreathing ||
                inGrounding;

            if (uiOwnsCursor)
            {
                if (player.InputEnabled)
                    player.SetInputEnabled(false);

                if (isSeated || phase == GamePhase.Complete)
                    player.LockMovement(true);

                return;
            }

            if (!player.InputEnabled)
                player.SetInputEnabled(true);

            if (isSeated)
            {
                player.LockMovement(true);
                player.ForceLockCursor();
            }
            else if (phase >= GamePhase.Outdoor && phase < GamePhase.Complete)
            {
                player.LockMovement(false);
                player.ForceLockCursor();
            }
        }

        // ── Raycast helpers ───────────────────────────────────────────────────
        private T RaycastFor<T>() where T : Component
        {
            if (playerCamera == null) return null;
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 5f);
            if (hits == null || hits.Length == 0) return null;
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                T comp = hit.collider.GetComponentInParent<T>();
                if (comp != null) return comp;
            }
            return null;
        }

        private DirtSpot FindNearestVisibleDirt(List<DirtSpot> list)
        {
            if (list == null || playerCamera == null) return null;
            DirtSpot best   = null;
            float bestDist  = 999f;
            Vector3 camPos  = playerCamera.transform.position;
            Vector3 camFwd  = playerCamera.transform.forward;
            foreach (DirtSpot d in list)
            {
                if (d == null || !d.gameObject.activeInHierarchy) continue;
                Vector3 to = d.transform.position - camPos;
                float dist = to.magnitude;
                if (dist > 2.5f) continue;
                to.Normalize();
                if (Vector3.Dot(camFwd, to) < 0.4f) continue;
                if (dist < bestDist) { bestDist = dist; best = d; }
            }
            return best;
        }

        /// <summary>
        /// Physics-free scratch detection: replaces the broken Physics.RaycastAll call.
        /// Finds the nearest active ScratchSpot that is within 2.5 m AND roughly
        /// centred in the player's view (dot product threshold mimics looking at it).
        /// </summary>
        private ScratchSpot FindNearestVisibleScratch()
        {
            if (scene?.scratches == null || playerCamera == null) return null;
            ScratchSpot best  = null;
            float bestDist    = 999f;
            Vector3 camPos    = playerCamera.transform.position;
            Vector3 camFwd    = playerCamera.transform.forward;
            foreach (ScratchSpot s in scene.scratches)
            {
                if (s == null || !s.gameObject.activeInHierarchy) continue;
                Vector3 to   = s.transform.position - camPos;
                float dist   = to.magnitude;
                if (dist > 2.5f) continue;           // too far
                to.Normalize();
                // 0.55 ≈ within ~56° of screen centre  (tighter than DirtSpot to
                // avoid triggering while just walking past the table)
                if (Vector3.Dot(camFwd, to) < 0.55f) continue;
                if (dist < bestDist) { bestDist = dist; best = s; }
            }
            return best;
        }

        private void SetActiveList<T>(List<T> list, bool active) where T : Component
        {
            if (list == null) return;
            foreach (T item in list)
                if (item != null) item.gameObject.SetActive(active);
        }
    }
}
