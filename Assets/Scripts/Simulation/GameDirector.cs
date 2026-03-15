using System.Collections;
using UnityEngine;

namespace OCDSimulation
{
    public class GameDirector : MonoBehaviour
    {
        [Header("References")]
        public UIManager ui;
        public SimplePlayerController player;
        public Camera playerCamera;
        public SceneRefs scene;
        public AudioManager audioManager;

        [Header("Anxiety")]
        public float anxiety = 5f;
        public float anxietyIncreaseHover = 5f;
        public float anxietyIncreaseClick = 10f;
        public float anxietyDecay = 1.5f;

        [Header("Breathing")]
        public float inhaleTime = 4f;
        public float holdTime = 2f;
        public float exhaleTime = 4f;

        [Header("Timers")]
        public float hugDuration = 2f;
        public float observeDelay = 2f;

        private GamePhase phase = GamePhase.Home;
        private SessionStats stats = new SessionStats();

        private bool nearFriends = false;
        private bool isSeated = false;
        private bool lookingUnderTable = false;
        private bool hasCompletedCalming = false;
        private bool inBreathing = false;

        private float phaseTimer = 0f;
        private float sessionStartTime = 0f;
        private float firstScratchTime = -1f;
        private float lastScratchTime = 0f;
        private int scratchClicksAfterCalming = 0;
        private bool maxAnxietyTriggered = false;
        private Vector3 camLocalPos;
        private Quaternion camLocalRot;
        private float friendProximity = 3.2f;

        private int stage = 0;
        private float intrusiveTimer = 0f;
        private readonly string[] intrusiveLines =
        {
            "Clean it! CLEAN IT NOW!",
            "You can't leave it like that...",
            "Everyone is watching you.",
            "It's disgusting. FIX IT.",
            "You touched it. Your hands are dirty.",
            "Do it again. Just to be sure."
        };

        private void Start()
        {
            if (ui == null) ui = FindFirstObjectByType<UIManager>();
            ui.OnStartClicked += StartSimulation;
            ui.OnLookUnderTable += LookUnderTable;
            ui.OnLookBackUp += LookBackUp;
            ui.OnRemoveGum += RemoveGum;
            ui.OnAcceptCalming += BeginCalming;
            ui.OnReturnHome += ReturnHome;
            ui.OnTryAgain += RestartSimulation;

            SetPhase(GamePhase.Home);
            ui.ShowHome(true);
            ui.ShowHUD(false);

            player?.SetInputEnabled(false);

            if (playerCamera != null)
            {
                camLocalPos = playerCamera.transform.localPosition;
                camLocalRot = playerCamera.transform.localRotation;
            }
            else
            {
                playerCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (phase == GamePhase.Home || phase == GamePhase.Complete) return;

            stats.totalSessionTime = Time.time - sessionStartTime;
            if (!inBreathing && anxiety > 0f && phase >= GamePhase.Stage3)
            {
                anxiety = Mathf.Max(0f, anxiety - anxietyDecay * Time.deltaTime);
            }

            UpdateAnxietyUI();
            HandleInput();
            UpdateAudio();
            UpdateIntrusiveMessages();
            UpdateFriendProximity();

            if (phase == GamePhase.Hugging)
            {
                phaseTimer += Time.deltaTime;
                if (phaseTimer >= hugDuration)
                {
                    SetPhase(GamePhase.Sitting);
                    ui.SetPrompt("Press [E] to sit with your friends");
                }
            }

            if (phase == GamePhase.Sitting)
            {
                // Wait for sit.
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
                HandleScratchHover();
                if (anxiety >= 40f && isSeated && !inBreathing)
                {
                    ui.SetPrompt("Press [SPACE] to practice mindfulness");
                }
            }

            if (phase == GamePhase.Recovery)
            {
                if (hasCompletedCalming)
                {
                    ui.SetPrompt("Press [E] to end session");
                }
            }
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (phase == GamePhase.Approaching && nearFriends)
                {
                    SetPhase(GamePhase.Hugging);
                    ui.SetMessage("Great to see you all!");
                    phaseTimer = 0f;
                }
                else if (phase == GamePhase.Sitting && nearFriends)
                {
                    SitDown();
                }
                else if (phase == GamePhase.Recovery && hasCompletedCalming)
                {
                    EndSession();
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isSeated && anxiety >= 40f && !inBreathing)
                {
                    StartBreathing(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (inBreathing)
                {
                    StopBreathing();
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }

        private void HandleClick()
        {
            if (phase == GamePhase.Stage0 || phase == GamePhase.Stage1)
            {
                DirtSpot dirt = RaycastFor<DirtSpot>();
                if (dirt == null)
                {
                    var list = (phase == GamePhase.Stage0) ? scene.stage0Dirt : scene.stage1Dirt;
                    dirt = FindNearestVisibleDirt(list);
                }
                if (dirt != null)
                {
                    OnDirtClicked(dirt);
                }
            }
            else if (phase == GamePhase.Stage3)
            {
                ScratchSpot scratch = RaycastFor<ScratchSpot>();
                if (scratch != null)
                {
                    RegisterScratchAttempt();
                }
            }
        }

        private void HandleScratchHover()
        {
            ScratchSpot scratch = RaycastFor<ScratchSpot>();
            if (scratch != null)
            {
                float rate = anxiety < 50f ? 3f : 8f;
                anxiety = Mathf.Min(100f, anxiety + rate * Time.deltaTime);
                ui.SetUrge("These scratches can't be cleaned... resist the urge");
            }
            else
            {
                ui.SetUrge("");
            }

            if (anxiety >= 100f && !maxAnxietyTriggered)
            {
                maxAnxietyTriggered = true;
                if (stats.timesReachedMaxAnxiety == 0) stats.timeToResist = Time.time - firstScratchTime;
                stats.timesReachedMaxAnxiety += 1;
                ui.ShowCalming(true);
                SetPhase(GamePhase.Calming);
            }
            if (anxiety < 100f)
            {
                maxAnxietyTriggered = false;
            }
        }

        private void RegisterScratchAttempt()
        {
            anxiety = Mathf.Min(100f, anxiety + anxietyIncreaseClick);
            stats.totalScratchAttempts += 1;

            if (firstScratchTime < 0f)
            {
                firstScratchTime = Time.time;
            }

            if (!hasCompletedCalming)
            {
                stats.scratchAttemptsBeforeCalming += 1;
            }
            else
            {
                stats.scratchAttemptsAfterCalming += 1;
                scratchClicksAfterCalming += 1;
                stats.relapseCount += 1;
                ui.SetMessage("You relapsed. Return to breathing.");
                hasCompletedCalming = false;
                SetPhase(GamePhase.Calming);
                ui.ShowCalming(true);
            }

            lastScratchTime = Time.time;
            ui.SetMessage("Just one more try...?");
        }

        private void StartSimulation()
        {
            ui.ShowHome(false);
            ui.ShowHUD(true);
            sessionStartTime = Time.time;
            stats = new SessionStats();
            anxiety = 5f;
            SetPhase(GamePhase.Entering);
            ui.SetPrompt("Find your friends at their table");

            player.LockMovement(false);
            player.SetInputEnabled(true);
            if (scene != null && scene.entrancePoint != null)
            {
                player.transform.position = scene.entrancePoint.position;
                player.transform.rotation = scene.entrancePoint.rotation;
            }
        }

        private void RestartSimulation()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnHome()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void SetPhase(GamePhase newPhase)
        {
            phase = newPhase;
            ui.SetPhase($"Phase: {newPhase}");
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
            if (scene == null || scene.friendTableCenter == null || player == null) return;
            if (phase != GamePhase.Entering && phase != GamePhase.Approaching) return;

            float dist = Vector3.Distance(player.transform.position, scene.friendTableCenter.position);
            bool near = dist <= friendProximity;
            if (near != nearFriends)
            {
                SetNearFriends(near);
            }
        }

        private void SitDown()
        {
            isSeated = true;
            player.LockMovement(true);
            player.ForceLockCursor();
            if (scene != null && scene.playerSeatPoint != null)
            {
                Quaternion rot = scene.playerSeatPoint.rotation;
                if (scene.friendTableCenter != null)
                {
                    Vector3 lookDir = (scene.friendTableCenter.position - scene.playerSeatPoint.position);
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.01f)
                    {
                        rot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
                    }
                }
                player.TeleportTo(scene.playerSeatPoint.position, rot);
            }
            ui.SetPrompt("Sit with the discomfort");
            SetPhase(GamePhase.Observing);
            phaseTimer = 0f;
        }

        private void ShowStage0()
        {
            stage = 0;
            SetActiveList(scene.stage0Dirt, true);
            ui.SetMessage("You notice dirt stains... Click to clean");
            ui.SetPrompt("Click to clean the stain");
        }

        private void ShowStage1()
        {
            stage = 1;
            SetActiveList(scene.stage1Dirt, true);
            ui.SetMessage("Wait... is that MORE dirt?");
            ui.SetPrompt("More dirt appeared! Clean it?");
        }

        private void ShowStage2()
        {
            stage = 2;
            if (scene.gumSpot != null) scene.gumSpot.gameObject.SetActive(true);
            ui.ShowGumPanel(true);
            ui.SetMessage("Something under the table... is that GUM?!");
            ui.SetPrompt("Look under the table");
            player.SetInputEnabled(false); // unlock cursor for UI buttons
        }

        private void ShowStage3()
        {
            stage = 3;
            SetActiveList(scene.scratches, true);
            ui.SetMessage("Those scratches on the table... they're bothering you.");
            SetPhase(GamePhase.Stage3);
            ui.ShowGumPanel(false);
            player.SetInputEnabled(true);
            player.ForceLockCursor();
        }

        private IEnumerator AdvanceStageAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (stage == 0)
            {
                ShowStage1();
                SetPhase(GamePhase.Stage1);
            }
            else if (stage == 1)
            {
                ShowStage2();
                SetPhase(GamePhase.Stage2);
            }
        }

        private void LookUnderTable()
        {
            lookingUnderTable = true;
            ui.ShowGumPanel(true);
            ui.SetMessage("You bend down... there it is. Pink gum stuck there.");
            playerCamera.transform.localPosition = new Vector3(0f, 1.0f, 0.2f);
            playerCamera.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
        }

        private void LookBackUp()
        {
            lookingUnderTable = false;
            ui.ShowGumPanel(true);
            playerCamera.transform.localPosition = camLocalPos;
            playerCamera.transform.localRotation = camLocalRot;
        }

        private void RemoveGum()
        {
            if (scene.gumSpot != null) scene.gumSpot.Remove();
            ui.ShowGumPanel(false);
            ui.SetMessage("You peeled it off... gross. Your hands feel dirty now.");
            StartCoroutine(AdvanceToStage3AfterDelay(2f));
        }

        private IEnumerator AdvanceToStage3AfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowStage3();
        }

        private void BeginCalming()
        {
            ui.ShowCalming(false);
            StartBreathing(true);
        }

        private void StartBreathing(bool isCalming)
        {
            inBreathing = true;
            stats.breathingExercisesUsed += 1;
            if (!stats.techniquesUsed.Contains("breathing")) stats.techniquesUsed.Add("breathing");
            ui.ShowBreathing(true);
            audioManager?.SetBreathing(true);
            StartCoroutine(BreathingRoutine(isCalming));
        }

        private IEnumerator BreathingRoutine(bool isCalming)
        {
            int cycles = isCalming ? 2 : 1;
            for (int i = 0; i < cycles; i++)
            {
                yield return DoBreath("Breathe In", 0f, 1f, inhaleTime);
                yield return DoBreath("Hold", 1f, 1f, holdTime);
                yield return DoBreath("Breathe Out", 1f, 0f, exhaleTime);
                stats.breathingCyclesCompleted += 1;
                anxiety = Mathf.Max(0f, anxiety - 12f);
            }

            if (isCalming)
            {
                hasCompletedCalming = true;
                SetPhase(GamePhase.Recovery);
            }

            StopBreathing();
        }

        private IEnumerator DoBreath(string label, float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float n = Mathf.Lerp(from, to, t / duration);
                ui.UpdateBreathing(label, n);
                yield return null;
            }
        }

        private void StopBreathing()
        {
            inBreathing = false;
            ui.ShowBreathing(false);
            audioManager?.SetBreathing(false);
        }

        private void EndSession()
        {
            stats.finalAnxiety = anxiety;
            stats.peakAnxiety = Mathf.Max(stats.peakAnxiety, anxiety);
            stats.resistanceSuccessful = true;
            StartCoroutine(ShowCompletionFlow());
        }

        private IEnumerator ShowCompletionFlow()
        {
            SetPhase(GamePhase.Complete);
            ui.ShowHUD(false);
            ui.ShowCelebration(true);
            ui.SetCelebration("Excellent Work!", "You resisted the urge to clean!");
            yield return new WaitForSeconds(3f);
            ui.ShowCelebration(false);

            ui.ShowCompletion(true);
            ui.SetCompletionStats(
                "Your ERP Session Complete",
                "Here's your detailed progress report",
                GetBadge(),
                BuildStatsText(),
                BuildInsightsText()
            );
        }

        private string GetBadge()
        {
            if (stats.relapseCount == 0) return "OCD Champion";
            if (stats.relapseCount <= 2) return "Recovery Warrior";
            if (stats.relapseCount <= 4) return "Progress Made";
            return "Keep Practicing";
        }

        private string BuildStatsText()
        {
            float anxietyReduced = stats.peakAnxiety > 0f ? (stats.peakAnxiety - stats.finalAnxiety) / stats.peakAnxiety * 100f : 0f;
            int impulseControl = Mathf.Clamp(100 - (stats.scratchAttemptsAfterCalming * 10) - (stats.relapseCount * 15), 0, 100);
            string mindfulnessGrade = GetMindfulnessGrade(stats.breathingCyclesCompleted);
            string erpGrade = GetErpGrade();
            return
                $"Total Clean Attempts: {stats.totalScratchAttempts}\n" +
                $"Peak Anxiety: {Mathf.RoundToInt(stats.peakAnxiety)}%\n" +
                $"Breathing Cycles: {stats.breathingCyclesCompleted}\n" +
                $"Relapses: {stats.relapseCount}\n\n" +
                $"Session Duration: {FormatTime(stats.totalSessionTime)}\n" +
                $"Time to Accept: {FormatTime(stats.timeToResist)}\n" +
                $"Final Anxiety: {Mathf.RoundToInt(stats.finalAnxiety)}%\n" +
                $"Hit Max Anxiety: {stats.timesReachedMaxAnxiety}\n\n" +
                $"Anxiety Reduced: {Mathf.RoundToInt(anxietyReduced)}%\n" +
                $"Impulse Control: {impulseControl}\n" +
                $"Mindfulness Grade: {mindfulnessGrade}\n" +
                $"Overall ERP Grade: {erpGrade}\n";
        }

        private string GetMindfulnessGrade(int cycles)
        {
            if (cycles >= 6) return "A+";
            if (cycles >= 4) return "A";
            if (cycles >= 3) return "B";
            if (cycles >= 1) return "C";
            return "C";
        }

        private string GetErpGrade()
        {
            if (stats.relapseCount == 0 && stats.totalScratchAttempts < 6) return "S";
            if (stats.relapseCount <= 2) return "A";
            if (stats.relapseCount <= 4) return "B";
            return "C";
        }

        private string BuildInsightsText()
        {
            if (stats.relapseCount == 0)
            {
                return "Excellent impulse control! You successfully sat with discomfort without acting on compulsions.";
            }
            if (stats.relapseCount <= 2)
            {
                return "You experienced some setbacks, but returning to mindfulness shows great awareness. Recovery isn't linear!";
            }
            return "The urge to clean can feel overwhelming. Each time you resist, you're building strength.";
        }

        private string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int min = s / 60;
            int sec = s % 60;
            return $"{min:00}:{sec:00}";
        }

        private void UpdateAnxietyUI()
        {
            if (anxiety > stats.peakAnxiety) stats.peakAnxiety = anxiety;
            ui.SetAnxiety(anxiety);
        }

        private void UpdateAudio()
        {
            if (audioManager == null) return;
            audioManager.SetWhispers(anxiety >= 60f);
        }

        private void UpdateIntrusiveMessages()
        {
            if (phase != GamePhase.Stage3) return;
            if (anxiety < 60f) return;

            intrusiveTimer -= Time.deltaTime;
            if (intrusiveTimer <= 0f)
            {
                string line = intrusiveLines[Random.Range(0, intrusiveLines.Length)];
                ui.SetMessage(line);
                intrusiveTimer = 3.5f;
            }
        }

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

        private DirtSpot FindNearestVisibleDirt(System.Collections.Generic.List<DirtSpot> list)
        {
            if (list == null || playerCamera == null) return null;

            DirtSpot best = null;
            float bestDist = 999f;

            Vector3 camPos = playerCamera.transform.position;
            Vector3 camFwd = playerCamera.transform.forward;

            foreach (DirtSpot d in list)
            {
                if (d == null || !d.gameObject.activeInHierarchy) continue;
                Vector3 to = d.transform.position - camPos;
                float dist = to.magnitude;
                if (dist > 2.5f) continue;

                to.Normalize();
                if (Vector3.Dot(camFwd, to) < 0.4f) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = d;
                }
            }
            return best;
        }

        private void SetActiveList<T>(System.Collections.Generic.List<T> list, bool active) where T : Component
        {
            if (list == null) return;
            foreach (T item in list)
            {
                if (item != null) item.gameObject.SetActive(active);
            }
        }
        public void OnDirtClicked(DirtSpot dirt)
        {
            if (phase != GamePhase.Stage0 && phase != GamePhase.Stage1) return;
            if (dirt == null) return;

            dirt.Clean();

            if (phase == GamePhase.Stage0)
                ui.SetMessage("You cleaned it. That felt good... but was it necessary?");
            else
                ui.SetMessage("Cleaned again. The urge is satisfied... for now.");

            StartCoroutine(AdvanceStageAfterDelay(2f));
        }

    }

}