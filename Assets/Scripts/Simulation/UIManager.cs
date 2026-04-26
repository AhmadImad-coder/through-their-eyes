using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OCDSimulation
{
    public class UIManager : MonoBehaviour
    {
        // ── Action events (subscribed by GameDirector / other managers) ──────
        public Action OnStartClicked;
        public Action OnLookUnderTable;
        public Action OnLookBackUp;
        public Action OnRemoveGum;
        public Action OnAcceptCalming;
        public Action OnStartGrounding;
        public Action OnReturnHome;
        public Action OnTryAgain;
        public Action OnNarrativeComplete;
        public Action<string> OnOrderSelected;

        public Action<string>  OnJournalEntry;
        public Action          OnJournalClose;

        public Action<float>   OnSensitivityChanged;
        public Action<float>   OnVolumeChanged;
        public Action<bool>    OnComfortModeChanged;
        public Action<bool>    OnHighContrastChanged;
        public Action          OnSettingsClose;

        // ── Panels ────────────────────────────────────────────────────────────
        private Canvas     mainCanvas;
        private GameObject narrativePanel;
        private GameObject homePanel;
        private GameObject hudPanel;
        private GameObject breathingPanel;
        private GameObject groundingPanel;
        private GameObject orderPanel;
        private GameObject gumPanel;
        private GameObject calmingPanel;
        private GameObject celebrationPanel;
        private GameObject completionPanel;
        private GameObject npcDialoguePanel;
        private GameObject journalPanel;
        private GameObject settingsPanel;

        // ── HUD elements ─────────────────────────────────────────────────────
        private Text  phaseText;
        private Text  promptText;
        private Text  messageText;
        private Text  urgeText;
        private Text  anxietyText;
        private Image anxietyBarFill;
        private Image vignette;
        private Text  crosshairText;
        private Text  intrusiveThoughtText;   // red intrusive thought line
        private Text  rationalThoughtText;    // teal CBT counter-thought
        private Text  journalHintText;        // "Press [J] to journal"

        // ── Breathing panel ───────────────────────────────────────────────────
        private Text  breathingText;
        private Image breathingCircle;

        // ── Grounding panel ───────────────────────────────────────────────────
        private Text groundingInstText;
        private Text groundingItemsText;
        private Text groundingProgressText;

        // ── NPC dialogue (small corner bubble) ───────────────────────────────
        private Text npcSpeakerText;
        private Text npcLineText;

        // ── Journal panel ─────────────────────────────────────────────────────
        private Text journalAnxietyText;

        // ── Settings panel ────────────────────────────────────────────────────
        private Slider sensitivitySlider;
        private Slider volumeSlider;
        private Text   sensitivityValueText;
        private Text   volumeValueText;
        private Text   comfortBtnText;
        private Text   contrastBtnText;
        private bool   _localComfort  = false;
        private bool   _localContrast = false;

        // ── Completion panel ──────────────────────────────────────────────────
        private Text      completionTitle;
        private Text      completionSubtitle;
        private Text      badgeText;
        private Text      statsText;
        private Text      insightsText;
        private Text      educationalFactText;
        private Text      journalSummaryText;
        private Transform achievementContainer;
        private int       achievementCount = 0;

        // ── Gum panel interactive buttons ────────────────────────────────────
        private Button gumLookUnderBtn;
        private Button gumLookBackBtn;
        private Button gumRemoveBtn;
        private Text   gumStatusText;

        // ── Celebration panel ─────────────────────────────────────────────────
        private Text celebrationTitle;
        private Text celebrationSubtitle;

        // ── Narrative ─────────────────────────────────────────────────────────
        private Text       narrativeText;
        private Coroutine  typewriterCoroutine;

        private static readonly string[] narrativeLines =
        {
            "You are Alex.",
            "You have been diagnosed with OCD — contamination obsessions.",
            "Today you're meeting your friends Emma, Jake, and Mia at their favourite coffee shop.",
            "Like every day, your mind will try to control you.",
            "But today — you choose to resist."
        };

        // ══════════════════════════════════════════════════════════════════════
        private void Awake() { BuildUI(); }

        private void BuildUI()
        {
            GameObject root = new GameObject("UIRoot");
            mainCanvas = root.AddComponent<Canvas>();
            mainCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;
            mainCanvas.pixelPerfect = true;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode   = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.dynamicPixelsPerUnit = 2.5f;
            root.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            // Build order = z-depth (last child renders on top)
            homePanel       = BuildHomeScreen(root.transform);
            hudPanel        = BuildHUD(root.transform);
            breathingPanel  = BuildBreathing(root.transform);
            groundingPanel  = BuildGrounding(root.transform);
            orderPanel      = BuildOrderPanel(root.transform);
            gumPanel        = BuildGumPanel(root.transform);
            calmingPanel    = BuildCalmingPanel(root.transform);
            celebrationPanel = BuildCelebration(root.transform);
            completionPanel  = BuildCompletion(root.transform);
            npcDialoguePanel = BuildNPCDialogue(root.transform);
            journalPanel     = BuildJournalPanel(root.transform);
            settingsPanel    = BuildSettingsPanel(root.transform);
            narrativePanel   = BuildNarrative(root.transform);

            // Initial visibility — GameDirector calls StartNarrative() in Start()
            ShowHome(false);
            ShowHUD(false);
            ShowBreathing(false);
            ShowGrounding(false);
            ShowOrderPanel(false);
            ShowGumPanel(false);
            ShowCalming(false);
            ShowCompletion(false);
            ShowCelebration(false);
            HideNPCDialogue();
            ShowJournal(false, 0f);
            narrativePanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        // ── Narrative ─────────────────────────────────────────────────────────
        //
        // CUSTOMISE NARRATIVE SCREEN:
        //   NarrBgColor  — background colour (change to any dark Color)
        //   NarrFontSize — typewriter text size
        //
        private static readonly Color NarrBgColor  = new Color(0.04f, 0.06f, 0.16f, 1f);
        private const           int   NarrFontSize = 22;

        private GameObject BuildNarrative(Transform parent)
        {
            // Rich dark-blue background instead of pure black
            GameObject panel = CreatePanel(parent, NarrBgColor);

            // Subtitle at top
            Text hint = CreateText(panel.transform, "NarrHint",
                "Through Their Eyes  —  An Immersive OCD Simulation",
                12, TextAnchor.UpperCenter, new Vector2(0, -26));
            hint.color = new Color(0.45f, 0.55f, 0.70f);

            // Narrative text — truly centred on screen (uses center anchor)
            narrativeText = CreateText(panel.transform, "NarrText", "", NarrFontSize,
                                        TextAnchor.MiddleCenter, Vector2.zero);
            RectTransform nt = narrativeText.GetComponent<RectTransform>();
            nt.anchorMin        = new Vector2(0.5f, 0.5f);
            nt.anchorMax        = new Vector2(0.5f, 0.5f);
            nt.pivot            = new Vector2(0.5f, 0.5f);
            nt.sizeDelta        = new Vector2(840f, 300f);
            nt.anchoredPosition = new Vector2(0f, 30f);   // slightly above screen-centre

            // Skip button — centered below the text (also uses center anchor)
            Button skip = CreateButton(panel.transform, "SkipBtn", "[ Skip ]",
                                        Vector2.zero, 160f, 40f);
            RectTransform sr = skip.GetComponent<RectTransform>();
            sr.anchorMin        = new Vector2(0.5f, 0.5f);
            sr.anchorMax        = new Vector2(0.5f, 0.5f);
            sr.pivot            = new Vector2(0.5f, 0.5f);
            sr.anchoredPosition = new Vector2(0f, -150f);  // below text block

            skip.onClick.AddListener(FinishNarrative);
            return panel;
        }

        public void StartNarrative()
        {
            narrativePanel.SetActive(true);
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(TypewriterRoutine());
        }

        private IEnumerator TypewriterRoutine()
        {
            narrativeText.text = "";
            string full = string.Join("\n\n", narrativeLines);

            // Type at ~55 chars/sec — readable typewriter effect (~5 s total)
            foreach (char c in full)
            {
                narrativeText.text += c;
                yield return new WaitForSeconds(0.018f);
            }
            yield return new WaitForSeconds(1.5f);
            FinishNarrative();
        }

        private void FinishNarrative()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            narrativePanel.SetActive(false);
            OnNarrativeComplete?.Invoke();
        }

        public void ShowNarrative(bool show) => narrativePanel.SetActive(show);

        // ── Home screen ───────────────────────────────────────────────────────
        //
        // CUSTOMISE HOME SCREEN:
        //   HomeBgColor — background colour (alpha = 1.0 keeps 3D text from bleeding through)
        //
        private static readonly Color HomeBgColor = new Color(0.04f, 0.08f, 0.10f, 1.0f);

        private GameObject BuildHomeScreen(Transform parent)
        {
            // Alpha MUST be 1.0 — any transparency lets world-space TextMesh objects
            // (espresso prices, menu board) bleed through the overlay panel.
            GameObject panel = CreatePanel(parent, HomeBgColor);
            CreateText(panel.transform, "Title",
                "Through Their Eyes", 42, TextAnchor.UpperCenter, new Vector2(0, -40));
            CreateText(panel.transform, "Subtitle",
                "An immersive journey into the mind of someone living with OCD",
                17, TextAnchor.UpperCenter, new Vector2(0, -92));

            CreateText(panel.transform, "Scenario",
                "Street  →  Coffee Shop  •  Contamination OCD  •  ERP Therapy",
                16, TextAnchor.UpperCenter, new Vector2(0, -140));

            CreateText(panel.transform, "Steps",
                "1) Walk from the street to Brewed Grounds coffee shop\n" +
                "2) Press [E] to enter — then find and greet your friends\n" +
                "3) Press [E] again to sit down with them\n" +
                "4) Notice the table isn't perfectly clean — resist the urge!\n" +
                "5) Press [SPACE] for mindfulness  •  Press [J] to journal a thought\n" +
                "6) Press [ESC] at any time to access Settings",
                15, TextAnchor.UpperCenter, new Vector2(0, -215));

            Button startBtn = CreateButton(panel.transform, "StartBtn",
                "Begin the Day", new Vector2(0, -395), 200f, 46f);
            startBtn.onClick.AddListener(() => OnStartClicked?.Invoke());
            return panel;
        }

        // ── HUD ───────────────────────────────────────────────────────────────
        private GameObject BuildHUD(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0, 0, 0, 0));

            // Top-left info
            anxietyText = CreateText(panel.transform, "AnxietyText",
                "Anxiety: 0%", 15, TextAnchor.UpperLeft, new Vector2(15, -15));
            phaseText = CreateText(panel.transform, "PhaseText",
                "Phase: Entering", 13, TextAnchor.UpperLeft, new Vector2(15, -33));

            // Journal hint (top-left, subtle)
            journalHintText = CreateText(panel.transform, "JournalHint",
                "[J] Journal  |  [ESC] Settings", 11, TextAnchor.UpperLeft,
                new Vector2(15, -51));
            journalHintText.color = new Color(0.6f, 0.6f, 0.6f);

            // Prompts (top-center)
            promptText = CreateText(panel.transform, "PromptText",
                "", 17, TextAnchor.UpperCenter, new Vector2(0, -15));

            // Intrusive thought (upper-center, below prompt)
            intrusiveThoughtText = CreateText(panel.transform, "IntrusiveText",
                "", 18, TextAnchor.UpperCenter, new Vector2(0, -50));
            intrusiveThoughtText.color = new Color(1f, 0.25f, 0.20f);

            // Rational counter-thought (below intrusive)
            rationalThoughtText = CreateText(panel.transform, "RationalText",
                "", 13, TextAnchor.UpperCenter, new Vector2(0, -78));
            rationalThoughtText.color = new Color(0.25f, 0.85f, 0.75f);

            // Urge text (upper-center)
            urgeText = CreateText(panel.transform, "UrgeText",
                "", 15, TextAnchor.UpperCenter, new Vector2(0, -110));
            urgeText.color = new Color(1f, 0.70f, 0.20f);

            // Message (lower-center)
            messageText = CreateText(panel.transform, "MessageText",
                "", 16, TextAnchor.LowerCenter, new Vector2(0, 40));

            // Crosshair
            crosshairText = CreateText(panel.transform, "Crosshair",
                "+", 18, TextAnchor.MiddleCenter, Vector2.zero);

            // Anxiety bar (top-right)
            GameObject barBg = CreateImage(panel.transform, "AnxietyBarBg",
                Vector2.zero, new Vector2(200, 14), new Color(0, 0, 0, 0.5f));
            RectTransform barBgRect = barBg.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(1, 1);
            barBgRect.anchorMax = new Vector2(1, 1);
            barBgRect.pivot     = new Vector2(1, 1);
            barBgRect.anchoredPosition = new Vector2(-15, -15);

            GameObject barFill = CreateImage(barBg.transform, "AnxietyBarFill",
                Vector2.zero, new Vector2(200, 14), new Color(0.2f, 0.8f, 0.3f));
            anxietyBarFill = barFill.GetComponent<Image>();
            RectTransform fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot     = new Vector2(0, 0.5f);

            // Vignette
            GameObject vigObj = CreateImage(panel.transform, "Vignette",
                Vector2.zero, Vector2.zero, new Color(0.6f, 0f, 0f, 0f));
            vignette = vigObj.GetComponent<Image>();
            RectTransform vigRect = vigObj.GetComponent<RectTransform>();
            vigRect.anchorMin  = Vector2.zero;
            vigRect.anchorMax  = Vector2.one;
            vigRect.offsetMin  = Vector2.zero;
            vigRect.offsetMax  = Vector2.zero;
            vignette.raycastTarget = false;

            return panel;
        }

        // ── Breathing panel ───────────────────────────────────────────────────
        private GameObject BuildBreathing(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0f, 0f, 0f, 0.78f));
            breathingText = CreateText(panel.transform, "BreathText",
                "Breathe In", 26, TextAnchor.MiddleCenter, new Vector2(0, 130));
            CreateText(panel.transform, "BreathHint",
                "Press [ESC] to close early", 13, TextAnchor.MiddleCenter,
                new Vector2(0, -195));
            GameObject circle = CreateImage(panel.transform, "BreathCircle",
                Vector2.zero, new Vector2(200, 200), new Color(0.2f, 0.6f, 0.8f, 0.8f));
            breathingCircle = circle.GetComponent<Image>();
            return panel;
        }

        // ── Grounding panel ───────────────────────────────────────────────────
        private GameObject BuildGrounding(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0f, 0.04f, 0.08f, 0.88f));
            CreateText(panel.transform, "GroundTitle",
                "5-4-3-2-1  Grounding Technique",
                22, TextAnchor.UpperCenter, new Vector2(0, -50))
                .color = new Color(0.3f, 0.8f, 0.7f);

            groundingProgressText = CreateText(panel.transform, "GroundProgress",
                "Step 1 of 5", 16, TextAnchor.UpperCenter, new Vector2(0, -88));
            groundingProgressText.color = new Color(0.7f, 0.7f, 0.7f);

            groundingInstText = CreateText(panel.transform, "GroundInst",
                "", 20, TextAnchor.UpperCenter, new Vector2(0, -135));

            groundingItemsText = CreateText(panel.transform, "GroundItems",
                "", 16, TextAnchor.UpperCenter, new Vector2(0, -180));
            groundingItemsText.color = new Color(0.85f, 0.85f, 0.85f);
            groundingItemsText.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 300);

            CreateText(panel.transform, "GroundNote",
                "Focus on each item. Feel yourself returning to the present moment.",
                13, TextAnchor.UpperCenter, new Vector2(0, -430))
                .color = new Color(0.55f, 0.55f, 0.55f);

            return panel;
        }

        // ── Order panel ──────────────────────────────────────────────────────
        private GameObject BuildOrderPanel(Transform parent)
        {
            GameObject panel = new GameObject("OrderPanel");
            panel.transform.SetParent(parent, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.025f, 0.02f, 0.94f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560f, 430f);
            rt.anchoredPosition = Vector2.zero;

            CreateText(panel.transform, "OrderTitle",
                "Brewed Grounds Menu", 25, TextAnchor.UpperCenter,
                new Vector2(0f, -28f)).color = new Color(0.98f, 0.86f, 0.55f);

            CreateText(panel.transform, "OrderPrompt",
                "The server asks what you would like to order.", 14,
                TextAnchor.UpperCenter, new Vector2(0f, -72f))
                .color = new Color(0.86f, 0.82f, 0.72f);

            string[] labels =
            {
                "Espresso - rich and small",
                "Latte - warm milk coffee",
                "Cappuccino - foamy classic",
                "Nothing right now"
            };
            string[] values = { "Espresso", "Latte", "Cappuccino", "Nothing" };

            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                Button btn = CreateButton(panel.transform, "Order" + i,
                    labels[i], new Vector2(0f, -128f - i * 62f), 390f, 46f);
                btn.GetComponent<Image>().color = i == labels.Length - 1
                    ? new Color(0.22f, 0.26f, 0.28f, 0.96f)
                    : new Color(0.42f, 0.24f, 0.12f, 0.96f);
                btn.onClick.AddListener(() => OnOrderSelected?.Invoke(values[idx]));
            }

            CreateText(panel.transform, "OrderHint",
                "Choose one option to continue.", 12, TextAnchor.UpperCenter,
                new Vector2(0f, -380f)).color = new Color(0.62f, 0.62f, 0.58f);

            return panel;
        }

        // ── Gum panel ─────────────────────────────────────────────────────────
        // Compact strip anchored to the bottom of the screen so the 3-D view
        // remains visible when the player looks under the table.
        private GameObject BuildGumPanel(Transform parent)
        {
            // Container: bottom 22% of the screen
            GameObject panel = new GameObject("GumPanel");
            panel.transform.SetParent(parent, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.04f, 0.06f, 0.88f);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0.22f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Title / status text — centred child object
            GameObject titleGo = new GameObject("GumTitle");
            titleGo.transform.SetParent(panel.transform, false);
            gumStatusText = titleGo.AddComponent<Text>();
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                  ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            gumStatusText.font          = f;
            gumStatusText.fontSize      = 18;
            gumStatusText.color         = new Color(1f, 0.75f, 0.2f);
            gumStatusText.alignment     = TextAnchor.MiddleCenter;
            gumStatusText.raycastTarget = false;
            gumStatusText.text          = "Something is wrong under the table...  Is that GUM?!";
            RectTransform tr = gumStatusText.GetComponent<RectTransform>();
            tr.anchorMin        = new Vector2(0f, 0.55f);
            tr.anchorMax        = new Vector2(1f, 1f);
            tr.offsetMin        = new Vector2(10f, 0f);
            tr.offsetMax        = new Vector2(-10f, 0f);

            // ── "Look Under Table" button (shown first) ───────────────────────
            gumLookUnderBtn = CreateGumButton(panel.transform, "LookUnder",
                "[ Look Under Table ]", new Vector2(0f, 0.08f), new Vector2(0.3f, 0.52f));
            gumLookUnderBtn.onClick.AddListener(() => OnLookUnderTable?.Invoke());

            // ── "Look Back Up" button (shown after looking under) ─────────────
            gumLookBackBtn = CreateGumButton(panel.transform, "LookBack",
                "[ Look Back Up ]", new Vector2(0f, 0.08f), new Vector2(0.05f, 0.52f));
            gumLookBackBtn.onClick.AddListener(() => OnLookBackUp?.Invoke());
            gumLookBackBtn.gameObject.SetActive(false);

            // ── "Remove Gum" button (shown after looking under) ───────────────
            gumRemoveBtn = CreateGumButton(panel.transform, "RemoveGum",
                "[ Remove Gum ]", new Vector2(0f, 0.08f), new Vector2(0.57f, 0.52f));
            gumRemoveBtn.GetComponent<Image>().color = new Color(0.55f, 0.15f, 0.15f, 0.9f);
            gumRemoveBtn.onClick.AddListener(() => OnRemoveGum?.Invoke());
            gumRemoveBtn.gameObject.SetActive(false);

            return panel;
        }

        // Anchor-based button helper for the gum panel (uses normalised anchors)
        private Button CreateGumButton(Transform parent, string name, string label,
                                        Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.10f, 0.50f, 0.60f, 0.90f);
            Button btn = go.AddComponent<Button>();
            RectTransform r = go.GetComponent<RectTransform>();
            r.anchorMin  = anchorMin;
            r.anchorMax  = anchorMax;
            r.offsetMin  = new Vector2(6f, 4f);
            r.offsetMax  = new Vector2(-6f, -4f);

            GameObject textGo = new GameObject(name + "Text");
            textGo.transform.SetParent(go.transform, false);
            Text t = textGo.AddComponent<Text>();
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                  ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.font          = f;
            t.text          = label;
            t.fontSize      = 15;
            t.color         = Color.white;
            t.alignment     = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            RectTransform tr = t.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;

            return btn;
        }

        /// <summary>Switches gum panel between "not looked" and "under table" state.</summary>
        public void UpdateGumPanel(bool lookingUnder)
        {
            if (gumLookUnderBtn != null) gumLookUnderBtn.gameObject.SetActive(!lookingUnder);
            if (gumLookBackBtn  != null) gumLookBackBtn .gameObject.SetActive(lookingUnder);
            if (gumRemoveBtn    != null) gumRemoveBtn   .gameObject.SetActive(lookingUnder);
            if (gumStatusText   != null)
                gumStatusText.text = lookingUnder
                    ? "You can see it — pink gum stuck to the underside.  Remove it or look away?"
                    : "Something is wrong under the table...  Is that GUM?!";
        }

        // ── Calming panel (two-technique choice) ─────────────────────────────
        private GameObject BuildCalmingPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0f, 0f, 0.05f, 0.88f));
            CreateText(panel.transform, "CalmTitle",
                "Anxiety is at its peak.", 24, TextAnchor.UpperCenter,
                new Vector2(0, -100));
            CreateText(panel.transform, "CalmSub",
                "Choose a calming technique:", 17, TextAnchor.UpperCenter,
                new Vector2(0, -148));

            // Technique cards
            CreateText(panel.transform, "BreathCard",
                "Breathing Exercise\n4s inhale · 2s hold · 4s exhale\nReduces anxiety directly",
                14, TextAnchor.UpperCenter, new Vector2(-180, -200))
                .color = new Color(0.5f, 0.85f, 1.0f);
            CreateText(panel.transform, "GroundCard",
                "5-4-3-2-1 Grounding\nRefocus your five senses\nAnchors you in the present",
                14, TextAnchor.UpperCenter, new Vector2(180, -200))
                .color = new Color(0.4f, 0.9f, 0.75f);

            Button breathBtn = CreateButton(panel.transform, "BreathBtn",
                "Start Breathing", new Vector2(-180, -295));
            Button groundBtn = CreateButton(panel.transform, "GroundBtn",
                "Start Grounding", new Vector2(180, -295));
            breathBtn.onClick.AddListener(() => OnAcceptCalming?.Invoke());
            groundBtn.onClick.AddListener(() => OnStartGrounding?.Invoke());
            return panel;
        }

        // ── Celebration panel ─────────────────────────────────────────────────
        private GameObject BuildCelebration(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0.02f, 0.02f, 0.04f, 0.95f));
            celebrationTitle = CreateText(panel.transform, "CelebTitle",
                "Session Complete!", 36, TextAnchor.UpperCenter, new Vector2(0, -140));
            celebrationSubtitle = CreateText(panel.transform, "CelebSub",
                "You resisted the urge!", 20, TextAnchor.UpperCenter, new Vector2(0, -200));
            CreateText(panel.transform, "CelebHint",
                "Loading your results...", 13, TextAnchor.UpperCenter, new Vector2(0, -260));
            return panel;
        }

        // ── Completion panel ──────────────────────────────────────────────────
        private GameObject BuildCompletion(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0.02f, 0.04f, 0.06f, 0.97f));

            completionTitle = CreateText(panel.transform, "CompTitle",
                "ERP Session Complete", 28, TextAnchor.UpperCenter, new Vector2(0, -40));
            completionSubtitle = CreateText(panel.transform, "CompSub",
                "", 15, TextAnchor.UpperCenter, new Vector2(0, -76));
            completionSubtitle.color = new Color(0.8f, 0.8f, 0.6f);
            badgeText = CreateText(panel.transform, "Badge",
                "OCD Champion", 20, TextAnchor.UpperCenter, new Vector2(0, -105));
            badgeText.color = new Color(1f, 0.84f, 0f);

            statsText = CreateText(panel.transform, "Stats",
                "", 13, TextAnchor.UpperCenter, new Vector2(0, -143));
            statsText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 200);

            insightsText = CreateText(panel.transform, "Insights",
                "", 13, TextAnchor.UpperCenter, new Vector2(0, -290));
            insightsText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 80);
            insightsText.color = new Color(0.8f, 0.9f, 1.0f);

            // Educational fact box (teal tinted)
            educationalFactText = CreateText(panel.transform, "EduFact",
                "", 12, TextAnchor.UpperCenter, new Vector2(0, -365));
            educationalFactText.color = new Color(0.35f, 0.85f, 0.75f);
            educationalFactText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 60);
            educationalFactText.fontStyle = FontStyle.Italic;

            journalSummaryText = CreateText(panel.transform, "JournalSummary",
                "", 12, TextAnchor.UpperCenter, new Vector2(0, -422));
            journalSummaryText.color = new Color(0.7f, 0.8f, 1.0f);

            // Achievement row container
            GameObject achContainer = new GameObject("AchievementRow");
            achContainer.transform.SetParent(panel.transform, false);
            RectTransform acRect = achContainer.AddComponent<RectTransform>();
            acRect.anchorMin  = new Vector2(0.5f, 1f);
            acRect.anchorMax  = new Vector2(0.5f, 1f);
            acRect.pivot      = new Vector2(0.5f, 1f);
            acRect.sizeDelta  = new Vector2(900, 115);
            acRect.anchoredPosition = new Vector2(0, -445);
            achievementContainer = achContainer.transform;

            Button retBtn = CreateButton(panel.transform, "ReturnHome",
                "Return Home", new Vector2(-130, -585));
            Button tryBtn = CreateButton(panel.transform, "TryAgain",
                "Try Again",   new Vector2(130, -585));
            retBtn.onClick.AddListener(() => OnReturnHome?.Invoke());
            tryBtn.onClick.AddListener(() => OnTryAgain?.Invoke());

            return panel;
        }

        // ── NPC dialogue (small corner bubble) ───────────────────────────────
        private GameObject BuildNPCDialogue(Transform parent)
        {
            GameObject panel = new GameObject("NPCDialoguePanel");
            panel.transform.SetParent(parent, false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.10f, 0.15f, 0.82f);
            RectTransform pr = panel.GetComponent<RectTransform>();
            pr.anchorMin        = new Vector2(1f, 0f);
            pr.anchorMax        = new Vector2(1f, 0f);
            pr.pivot            = new Vector2(1f, 0f);
            pr.sizeDelta        = new Vector2(300, 60);
            pr.anchoredPosition = new Vector2(-20, 60);

            npcSpeakerText = CreateText(panel.transform, "NPCName", "",
                13, TextAnchor.UpperLeft, new Vector2(10, -8));
            npcSpeakerText.color = new Color(0.4f, 0.8f, 1f);
            npcSpeakerText.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 22);

            npcLineText = CreateText(panel.transform, "NPCLine", "",
                12, TextAnchor.UpperLeft, new Vector2(10, -28));
            npcLineText.color = Color.white;
            npcLineText.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 30);

            return panel;
        }

        // ── Journal panel ─────────────────────────────────────────────────────
        private GameObject BuildJournalPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0.04f, 0.06f, 0.12f, 0.93f));

            CreateText(panel.transform, "JournalTitle",
                "Thought Journal", 24, TextAnchor.UpperCenter, new Vector2(0, -50))
                .color = new Color(0.5f, 0.8f, 1f);
            CreateText(panel.transform, "JournalSub",
                "How are you feeling right now?", 15, TextAnchor.UpperCenter,
                new Vector2(0, -88));

            journalAnxietyText = CreateText(panel.transform, "JournalAnxiety",
                "", 13, TextAnchor.UpperCenter, new Vector2(0, -112));
            journalAnxietyText.color = new Color(0.8f, 0.5f, 0.3f);

            string[] emotions = ThoughtJournal.GetEmotions();
            float startY = -152f;
            float spacing = 46f;
            for (int i = 0; i < emotions.Length; i++)
            {
                string emo = emotions[i]; // capture for lambda
                Button btn = CreateButton(panel.transform, "Emo" + i, emo,
                    new Vector2(0, startY - i * spacing));
                btn.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 38);
                btn.onClick.AddListener(() => OnJournalEntry?.Invoke(emo));
            }

            Button closeBtn = CreateButton(panel.transform, "JournalClose",
                "Close Journal",
                new Vector2(0, startY - emotions.Length * spacing - 10f));
            closeBtn.onClick.AddListener(() => OnJournalClose?.Invoke());

            return panel;
        }

        // ── Settings panel ────────────────────────────────────────────────────
        private GameObject BuildSettingsPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0.03f, 0.05f, 0.10f, 0.94f));

            CreateText(panel.transform, "SettTitle",
                "Settings", 28, TextAnchor.UpperCenter, new Vector2(0, -55))
                .color = new Color(0.7f, 0.9f, 1f);

            // Sensitivity
            CreateText(panel.transform, "SensLabel",
                "Mouse Sensitivity", 15, TextAnchor.UpperCenter, new Vector2(-80, -125));
            sensitivitySlider = CreateSlider(panel.transform, "Sensitivity",
                0.5f, 4f, SettingsManager.Sensitivity,
                new Vector2(80, -120), v =>
                {
                    sensitivityValueText.text = v.ToString("F1");
                    OnSensitivityChanged?.Invoke(v);
                });
            sensitivityValueText = CreateText(panel.transform, "SensValue",
                SettingsManager.Sensitivity.ToString("F1"),
                14, TextAnchor.UpperCenter, new Vector2(260, -125));

            // Volume
            CreateText(panel.transform, "VolLabel",
                "Master Volume", 15, TextAnchor.UpperCenter, new Vector2(-80, -180));
            volumeSlider = CreateSlider(panel.transform, "Volume",
                0f, 1f, SettingsManager.Volume,
                new Vector2(80, -175), v =>
                {
                    volumeValueText.text = Mathf.RoundToInt(v * 100) + "%";
                    OnVolumeChanged?.Invoke(v);
                });
            volumeValueText = CreateText(panel.transform, "VolValue",
                Mathf.RoundToInt(SettingsManager.Volume * 100) + "%",
                14, TextAnchor.UpperCenter, new Vector2(260, -180));

            // Comfort Mode toggle
            _localComfort = SettingsManager.ComfortMode;
            Button comfortBtn = CreateButton(panel.transform, "ComfortBtn",
                "", new Vector2(0, -245));
            comfortBtnText = comfortBtn.transform.Find("ComfortBtnText")
                             ?.GetComponent<Text>();
            if (comfortBtnText == null)
                comfortBtnText = comfortBtn.GetComponentInChildren<Text>();
            comfortBtnText.text = _localComfort
                ? "Comfort Mode:  ON   (slower anxiety gain)"
                : "Comfort Mode:  OFF  (click to enable)";
            comfortBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 40);
            comfortBtn.onClick.AddListener(() =>
            {
                _localComfort = !_localComfort;
                comfortBtnText.text = _localComfort
                    ? "Comfort Mode:  ON   (slower anxiety gain)"
                    : "Comfort Mode:  OFF  (click to enable)";
                OnComfortModeChanged?.Invoke(_localComfort);
            });

            // High Contrast toggle
            _localContrast = SettingsManager.HighContrast;
            Button contrastBtn = CreateButton(panel.transform, "ContrastBtn",
                "", new Vector2(0, -300));
            contrastBtnText = contrastBtn.GetComponentInChildren<Text>();
            contrastBtnText.text = _localContrast
                ? "High Contrast:  ON   (accessible colours)"
                : "High Contrast:  OFF  (click to enable)";
            contrastBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 40);
            contrastBtn.onClick.AddListener(() =>
            {
                _localContrast = !_localContrast;
                contrastBtnText.text = _localContrast
                    ? "High Contrast:  ON   (accessible colours)"
                    : "High Contrast:  OFF  (click to enable)";
                OnHighContrastChanged?.Invoke(_localContrast);
            });

            CreateText(panel.transform, "SettHint",
                "Comfort Mode reduces anxiety gain rate by 50%.\nHigh Contrast mode adjusts UI colours for accessibility.",
                12, TextAnchor.UpperCenter, new Vector2(0, -358))
                .color = new Color(0.55f, 0.55f, 0.55f);

            Button resume = CreateButton(panel.transform, "ResumeBtn",
                "Resume", new Vector2(0, -415));
            resume.onClick.AddListener(() => OnSettingsClose?.Invoke());

            return panel;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Helper builders
        // ══════════════════════════════════════════════════════════════════════

        private Slider CreateSlider(Transform parent, string name, float min, float max,
                                     float value, Vector2 pos, Action<float> onChange)
        {
            GameObject root = new GameObject(name + "Slider");
            root.transform.SetParent(parent, false);
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            RectTransform rr = root.GetComponent<RectTransform>();
            rr.sizeDelta        = new Vector2(240, 18);
            rr.anchorMin        = new Vector2(0.5f, 1f);
            rr.anchorMax        = new Vector2(0.5f, 1f);
            rr.pivot            = new Vector2(0.5f, 1f);
            rr.anchoredPosition = pos;

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            RectTransform faRect = fillArea.AddComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0, 0.25f);
            faRect.anchorMax = new Vector2(1, 0.75f);
            faRect.offsetMin = new Vector2(5, 0);
            faRect.offsetMax = new Vector2(-15, 0);

            // Fill image
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.20f, 0.70f, 0.90f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Handle area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(root.transform, false);
            RectTransform haRect = handleArea.AddComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.offsetMin = new Vector2(10, 0);
            haRect.offsetMax = new Vector2(-10, 0);

            // Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18, 18);

            Slider slider = root.AddComponent<Slider>();
            slider.fillRect     = fillRect;
            slider.handleRect   = handleRect;
            slider.targetGraphic = handleImg;
            slider.minValue     = min;
            slider.maxValue     = max;
            slider.value        = value;
            slider.direction    = Slider.Direction.LeftToRight;
            if (onChange != null)
                slider.onValueChanged.AddListener(v => onChange(v));

            return slider;
        }

        private GameObject CreatePanel(Transform parent, Color color)
        {
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = color;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private Text CreateText(Transform parent, string name, string text,
                                int size, TextAnchor anchor, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.font          = builtin;
            t.text          = text;
            t.fontSize      = size;
            t.fontStyle     = size >= 18 ? FontStyle.Bold : FontStyle.Normal;
            t.color         = Color.white;
            t.alignment     = anchor;
            t.alignByGeometry = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            t.supportRichText    = false;
            t.raycastTarget = false; // prevent large text bounds blocking button clicks
            RectTransform rect = t.GetComponent<RectTransform>();
            rect.sizeDelta        = new Vector2(900, 200);
            rect.anchorMin        = new Vector2(0.5f, 1f);
            rect.anchorMax        = new Vector2(0.5f, 1f);
            rect.pivot            = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            return t;
        }

        private GameObject CreateImage(Transform parent, string name,
                                        Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            RectTransform rect = img.GetComponent<RectTransform>();
            rect.sizeDelta        = size;
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            return go;
        }

        /// <summary>
        /// Creates a button. The label text is stretched to fill the button rect
        /// so it is always centred and never overflows the colored background.
        ///
        /// CUSTOMISE:  change BtnColor to any color you like.
        ///             change BtnWidth / BtnHeight to resize all buttons at once.
        /// </summary>
        private static readonly Color  BtnColor  = new Color(0.10f, 0.45f, 0.58f, 0.92f);
        private const           float  BtnWidth  = 280f;
        private const           float  BtnHeight = 46f;
        private const           int    BtnFontSz = 15;

        private Button CreateButton(Transform parent, string name, string label, Vector2 pos,
                                    float width = BtnWidth, float height = BtnHeight)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = BtnColor;
            Button btn = go.AddComponent<Button>();
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta        = new Vector2(width, height);
            rect.anchorMin        = new Vector2(0.5f, 1f);
            rect.anchorMax        = new Vector2(0.5f, 1f);
            rect.pivot            = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;

            // ── Label text ──────────────────────────────────────────────────
            // Stretch-fill the button so the text is always centred inside it.
            // (Using sizeDelta=900 and top-anchor was the old broken approach.)
            GameObject textObj = new GameObject(name + "Text");
            textObj.transform.SetParent(go.transform, false);
            Text t = textObj.AddComponent<Text>();
            Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.font          = builtin;
            t.text          = label;
            t.fontSize      = BtnFontSz;
            t.fontStyle     = FontStyle.Bold;
            t.color         = Color.white;
            t.alignment     = TextAnchor.MiddleCenter;
            t.alignByGeometry = true;
            t.raycastTarget = false;
            RectTransform tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;          // fill the button entirely
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(6f, 2f);   // small horizontal padding
            tr.offsetMax = new Vector2(-6f, -2f);
            return btn;
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Public show / hide helpers
        // ══════════════════════════════════════════════════════════════════════

        public void ShowHome(bool show)       => homePanel.SetActive(show);
        public void ShowHUD(bool show)        => hudPanel.SetActive(show);
        public void ShowBreathing(bool show)  => breathingPanel.SetActive(show);
        public void ShowGrounding(bool show)  => groundingPanel.SetActive(show);
        public void ShowOrderPanel(bool show) => orderPanel.SetActive(show);
        public void ShowGumPanel(bool show)   => gumPanel.SetActive(show);
        public void ShowCalming(bool show)    => calmingPanel.SetActive(show);
        public void ShowCompletion(bool show) => completionPanel.SetActive(show);
        public void ShowCelebration(bool show)=> celebrationPanel.SetActive(show);

        public void ShowNPCDialogue(string speaker, string line)
        {
            npcSpeakerText.text = speaker + ":";
            npcLineText.text    = line;
            npcDialoguePanel.SetActive(true);
        }
        public void HideNPCDialogue() => npcDialoguePanel.SetActive(false);

        public void ShowJournal(bool show, float currentAnxiety)
        {
            journalPanel.SetActive(show);
            if (show && journalAnxietyText != null)
                journalAnxietyText.text = $"Current Anxiety: {Mathf.RoundToInt(currentAnxiety)}%";
        }

        public void ShowSettings(bool show, float sensitivity, float volume,
                                  bool comfortMode, bool highContrast)
        {
            settingsPanel.SetActive(show);
            if (!show) return;
            // Sync slider values WITHOUT triggering onChange callbacks
            if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(sensitivity);
            if (volumeSlider      != null) volumeSlider.SetValueWithoutNotify(volume);
            if (sensitivityValueText != null)
                sensitivityValueText.text = sensitivity.ToString("F1");
            if (volumeValueText != null)
                volumeValueText.text = Mathf.RoundToInt(volume * 100) + "%";
            _localComfort  = comfortMode;
            _localContrast = highContrast;
            if (comfortBtnText != null)
                comfortBtnText.text = _localComfort
                    ? "Comfort Mode:  ON   (slower anxiety gain)"
                    : "Comfort Mode:  OFF  (click to enable)";
            if (contrastBtnText != null)
                contrastBtnText.text = _localContrast
                    ? "High Contrast:  ON   (accessible colours)"
                    : "High Contrast:  OFF  (click to enable)";
        }

        // ══════════════════════════════════════════════════════════════════════
        // Public data setters
        // ══════════════════════════════════════════════════════════════════════

        public void SetPrompt(string text)   => promptText.text  = text;
        public void SetMessage(string text)  => messageText.text = text;
        public void SetUrge(string text)     => urgeText.text    = text;
        public void SetPhase(string text)    => phaseText.text   = text;

        /// <summary>
        /// Dual-display CBT: shows intrusive thought in red with a rational
        /// counter-thought in teal immediately below it.
        /// </summary>
        public void SetIntrusiveThought(string intrusive, string rational)
        {
            intrusiveThoughtText.text = intrusive;
            rationalThoughtText.text  = rational;
        }

        public void ClearIntrusiveThought()
        {
            intrusiveThoughtText.text = "";
            rationalThoughtText.text  = "";
        }

        public void SetAnxiety(float pct)
        {
            anxietyText.text = $"Anxiety: {Mathf.RoundToInt(pct)}%";
            float t = Mathf.Clamp01(pct / 100f);
            anxietyBarFill.rectTransform.sizeDelta = new Vector2(200f * t, 14f);
            anxietyBarFill.color = Color.Lerp(
                new Color(0.2f, 0.8f, 0.3f),
                new Color(0.9f, 0.2f, 0.2f), t);

            Color v = vignette.color;
            v.a = Mathf.Clamp01(t * 0.65f);
            vignette.color = v;
        }

        public void UpdateBreathing(string label, float normalized)
        {
            breathingText.text = label;
            float size = Mathf.Lerp(140f, 270f, normalized);
            breathingCircle.rectTransform.sizeDelta = new Vector2(size, size);
        }

        public void UpdateGrounding(string instruction, string items, int step, int total)
        {
            groundingInstText.text     = instruction;
            groundingItemsText.text    = items;
            groundingProgressText.text = $"Step {step} of {total}";
        }

        public void SetCompletionStats(string title, string subtitle, string badge,
                                        string stats, string insights)
        {
            completionTitle.text    = title;
            completionSubtitle.text = subtitle;
            badgeText.text          = badge;
            statsText.text          = stats;
            insightsText.text       = insights;
        }

        public void SetCelebration(string title, string subtitle)
        {
            celebrationTitle.text    = title;
            celebrationSubtitle.text = subtitle;
        }

        public void SetEducationalFact(string fact)
        {
            if (educationalFactText != null)
                educationalFactText.text = "Did you know? " + fact;
        }

        public void SetJournalSummary(string summary)
        {
            if (journalSummaryText != null)
                journalSummaryText.text = summary;
        }

        /// <summary>Adds a coloured achievement badge to the completion screen (max 8, 4 per row).</summary>
        public void AddAchievementToCompletion(string achName, string description, Color color)
        {
            if (achievementContainer == null) return;
            if (achievementCount >= 8) return; // cap for layout safety

            const int  perRow  = 4;
            const float badgeW = 148f;
            const float badgeH = 50f;
            const float gapX   = 6f;
            const float gapY   = 6f;

            int col = achievementCount % perRow;
            int row = achievementCount / perRow;
            float xOffset = col * (badgeW + gapX) - (perRow * (badgeW + gapX) - gapX) * 0.5f
                            + badgeW * 0.5f;
            float yOffset = -row * (badgeH + gapY);
            achievementCount++;

            GameObject badge = new GameObject("Badge_" + achName);
            badge.transform.SetParent(achievementContainer, false);
            Image bg = badge.AddComponent<Image>();
            bg.color = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.85f);
            RectTransform br = badge.GetComponent<RectTransform>();
            br.sizeDelta        = new Vector2(badgeW, badgeH);
            br.anchorMin        = new Vector2(0.5f, 1f);
            br.anchorMax        = new Vector2(0.5f, 1f);
            br.pivot            = new Vector2(0.5f, 1f);
            br.anchoredPosition = new Vector2(xOffset, yOffset);

            // Coloured circle dot
            GameObject dot = new GameObject("Dot");
            dot.transform.SetParent(badge.transform, false);
            Image dotImg = dot.AddComponent<Image>();
            dotImg.color = color;
            RectTransform dr = dot.GetComponent<RectTransform>();
            dr.sizeDelta        = new Vector2(10, 10);
            dr.anchorMin        = new Vector2(0, 0.5f);
            dr.anchorMax        = new Vector2(0, 0.5f);
            dr.pivot            = new Vector2(0, 0.5f);
            dr.anchoredPosition = new Vector2(6, 0);

            // Name text
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject nameObj = new GameObject("AchName");
            nameObj.transform.SetParent(badge.transform, false);
            Text nameT = nameObj.AddComponent<Text>();
            nameT.font      = font;
            nameT.text      = achName;
            nameT.fontSize  = 11;
            nameT.color     = color;
            nameT.alignment = TextAnchor.UpperLeft;
            RectTransform nr = nameT.GetComponent<RectTransform>();
            nr.anchorMin        = Vector2.zero;
            nr.anchorMax        = Vector2.one;
            nr.offsetMin        = new Vector2(20, 22);
            nr.offsetMax        = new Vector2(-4,  -4);

            // Description text
            GameObject descObj = new GameObject("AchDesc");
            descObj.transform.SetParent(badge.transform, false);
            Text descT = descObj.AddComponent<Text>();
            descT.font      = font;
            descT.text      = description;
            descT.fontSize  = 9;
            descT.color     = new Color(0.7f, 0.7f, 0.7f);
            descT.alignment = TextAnchor.UpperLeft;
            RectTransform deR = descT.GetComponent<RectTransform>();
            deR.anchorMin  = Vector2.zero;
            deR.anchorMax  = Vector2.one;
            deR.offsetMin  = new Vector2(20, 4);
            deR.offsetMax  = new Vector2(-4, -24);
        }

        public void ApplyHighContrast(bool on)
        {
            // Swap anxiety bar to high-contrast yellow/blue
            if (anxietyBarFill == null) return;
            // Colour will be re-applied on next SetAnxiety call
            // Additional contrast adjustments can be added here
        }

        /// <summary>Returns the main canvas Transform (used by PostProcessingController).</summary>
        public Transform GetCanvasTransform() => mainCanvas.transform;
    }
}
