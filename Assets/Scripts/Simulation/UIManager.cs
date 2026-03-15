using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OCDSimulation
{
    public class UIManager : MonoBehaviour
    {
        public Action OnStartClicked;
        public Action OnLookUnderTable;
        public Action OnLookBackUp;
        public Action OnRemoveGum;
        public Action OnAcceptCalming;
        public Action OnReturnHome;
        public Action OnTryAgain;

        private Canvas mainCanvas;
        private GameObject homePanel;
        private GameObject hudPanel;
        private GameObject breathingPanel;
        private GameObject gumPanel;
        private GameObject calmingPanel;
        private GameObject completionPanel;
        private GameObject celebrationPanel;

        private Text phaseText;
        private Text promptText;
        private Text messageText;
        private Text urgeText;
        private Text anxietyText;
        private Image anxietyBarFill;
        private Image vignette;
        private Text crosshairText;

        private Text breathingText;
        private Image breathingCircle;
        private Text breathingHint;

        private Text completionTitle;
        private Text completionSubtitle;
        private Text statsText;
        private Text badgeText;
        private Text insightsText;

        private Text celebrationTitle;
        private Text celebrationSubtitle;

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            GameObject canvasObj = new GameObject("UIRoot");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasObj.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            homePanel = BuildHomeScreen(canvasObj.transform);
            hudPanel = BuildHUD(canvasObj.transform);
            breathingPanel = BuildBreathing(canvasObj.transform);
            gumPanel = BuildGumPanel(canvasObj.transform);
            calmingPanel = BuildCalmingPanel(canvasObj.transform);
            celebrationPanel = BuildCelebration(canvasObj.transform);
            completionPanel = BuildCompletion(canvasObj.transform);

            ShowHome(true);
            ShowHUD(false);
            ShowBreathing(false);
            ShowGumPanel(false);
            ShowCalming(false);
            ShowCompletion(false);
            ShowCelebration(false);
        }

        private GameObject BuildHomeScreen(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0.04f, 0.08f, 0.1f, 0.95f));
            CreateText(panel.transform, "Title", "Through Their Eyes", 42, TextAnchor.UpperCenter, new Vector2(0, -40));
            CreateText(panel.transform, "Subtitle", "An immersive journey into the mind of someone living with OCD", 18, TextAnchor.UpperCenter, new Vector2(0, -95));

            CreateText(panel.transform, "Scenario", "Coffee Shop Scenario", 22, TextAnchor.UpperCenter, new Vector2(0, -150));
            CreateText(panel.transform, "Indicators", "Contamination OCD  •  Mindfulness Therapy", 16, TextAnchor.UpperCenter, new Vector2(0, -185));

            CreateText(panel.transform, "Steps",
                "1) Enter the coffee shop and find your friends\n" +
                "2) Press [E] to greet them and sit down\n" +
                "3) Notice the table isn't clean\n" +
                "4) Anxiety rises, resist the urge to clean\n" +
                "5) Press [SPACE] to practice mindfulness",
                16, TextAnchor.UpperCenter, new Vector2(0, -260));

            Button startButton = CreateButton(panel.transform, "StartButton", "Enter the Coffee Shop", new Vector2(0, -380));
            startButton.onClick.AddListener(() => OnStartClicked?.Invoke());

            return panel;
        }

        private GameObject BuildHUD(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0, 0, 0, 0));

            anxietyText = CreateText(panel.transform, "AnxietyText", "Anxiety: 0%", 16, TextAnchor.UpperLeft, new Vector2(15, -15));
            phaseText = CreateText(panel.transform, "PhaseText", "Phase: Entering", 16, TextAnchor.UpperLeft, new Vector2(15, -35));
            promptText = CreateText(panel.transform, "PromptText", "", 18, TextAnchor.UpperCenter, new Vector2(0, -15));
            messageText = CreateText(panel.transform, "MessageText", "", 18, TextAnchor.LowerCenter, new Vector2(0, 40));
            urgeText = CreateText(panel.transform, "UrgeText", "", 16, TextAnchor.UpperCenter, new Vector2(0, -45));
            urgeText.color = new Color(1f, 0.7f, 0.2f);

            crosshairText = CreateText(panel.transform, "Crosshair", "+", 18, TextAnchor.MiddleCenter, Vector2.zero);

            GameObject barBg = CreateImage(panel.transform, "AnxietyBarBg", new Vector2(0, 0), new Vector2(200, 14), new Color(0, 0, 0, 0.5f));
            RectTransform barRect = barBg.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1, 1);
            barRect.anchorMax = new Vector2(1, 1);
            barRect.pivot = new Vector2(1, 1);
            barRect.anchoredPosition = new Vector2(-15, -15);

            GameObject barFill = CreateImage(barBg.transform, "AnxietyBarFill", Vector2.zero, new Vector2(200, 14), new Color(0.2f, 0.8f, 0.3f));
            anxietyBarFill = barFill.GetComponent<Image>();
            RectTransform fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, 0.5f);

            GameObject vignetteObj = CreateImage(panel.transform, "Vignette", Vector2.zero, new Vector2(0, 0), new Color(0.6f, 0, 0, 0));
            vignette = vignetteObj.GetComponent<Image>();
            RectTransform vigRect = vignetteObj.GetComponent<RectTransform>();
            vigRect.anchorMin = new Vector2(0, 0);
            vigRect.anchorMax = new Vector2(1, 1);
            vigRect.offsetMin = Vector2.zero;
            vigRect.offsetMax = Vector2.zero;

            return panel;
        }

        private GameObject BuildBreathing(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0, 0, 0, 0.75f));
            breathingText = CreateText(panel.transform, "BreathingText", "Breathe In", 26, TextAnchor.MiddleCenter, new Vector2(0, 120));
            breathingHint = CreateText(panel.transform, "BreathingHint", "Press ESC to close", 14, TextAnchor.MiddleCenter, new Vector2(0, -180));

            GameObject circle = CreateImage(panel.transform, "BreathingCircle", Vector2.zero, new Vector2(200, 200), new Color(0.2f, 0.6f, 0.8f, 0.8f));
            breathingCircle = circle.GetComponent<Image>();
            return panel;
        }

        private GameObject BuildGumPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0, 0, 0, 0.55f));
            CreateText(panel.transform, "GumTitle", "Something under the table... is that GUM?!", 20, TextAnchor.UpperCenter, new Vector2(0, -120));
            Button lookUnder = CreateButton(panel.transform, "LookUnder", "Look Under Table", new Vector2(0, -200));
            Button lookBack = CreateButton(panel.transform, "LookBack", "Look Back Up", new Vector2(-140, -260));
            Button removeGum = CreateButton(panel.transform, "RemoveGum", "Remove Gum", new Vector2(140, -260));

            lookUnder.onClick.AddListener(() => OnLookUnderTable?.Invoke());
            lookBack.onClick.AddListener(() => OnLookBackUp?.Invoke());
            removeGum.onClick.AddListener(() => OnRemoveGum?.Invoke());
            return panel;
        }

        private GameObject BuildCalmingPanel(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0, 0, 0, 0.8f));
            CreateText(panel.transform, "CalmTitle", "Anxiety is at its peak", 24, TextAnchor.UpperCenter, new Vector2(0, -120));
            CreateText(panel.transform, "CalmSubtitle", "Begin calming therapy?", 18, TextAnchor.UpperCenter, new Vector2(0, -170));
            Button accept = CreateButton(panel.transform, "CalmAccept", "Begin Calming", new Vector2(0, -240));
            accept.onClick.AddListener(() => OnAcceptCalming?.Invoke());
            return panel;
        }

        private GameObject BuildCelebration(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0.02f, 0.02f, 0.04f, 0.95f));
            celebrationTitle = CreateText(panel.transform, "CelebrationTitle", "Excellent Work!", 36, TextAnchor.UpperCenter, new Vector2(0, -140));
            celebrationSubtitle = CreateText(panel.transform, "CelebrationSubtitle", "You resisted the urge to clean!", 20, TextAnchor.UpperCenter, new Vector2(0, -200));
            CreateText(panel.transform, "CelebrationHint", "Loading your results...", 14, TextAnchor.UpperCenter, new Vector2(0, -260));
            return panel;
        }

        private GameObject BuildCompletion(Transform parent)
        {
            GameObject panel = CreatePanel(parent, new Color(0.02f, 0.04f, 0.06f, 0.95f));
            completionTitle = CreateText(panel.transform, "CompletionTitle", "Your ERP Session Complete", 30, TextAnchor.UpperCenter, new Vector2(0, -40));
            completionSubtitle = CreateText(panel.transform, "CompletionSubtitle", "Here's your detailed progress report", 16, TextAnchor.UpperCenter, new Vector2(0, -85));
            badgeText = CreateText(panel.transform, "Badge", "OCD Champion", 20, TextAnchor.UpperCenter, new Vector2(0, -125));

            statsText = CreateText(panel.transform, "Stats", "", 16, TextAnchor.UpperCenter, new Vector2(0, -210));
            insightsText = CreateText(panel.transform, "Insights", "", 14, TextAnchor.UpperCenter, new Vector2(0, -360));

            Button returnHome = CreateButton(panel.transform, "ReturnHome", "Return Home", new Vector2(-120, -460));
            Button tryAgain = CreateButton(panel.transform, "TryAgain", "Try Again", new Vector2(120, -460));
            returnHome.onClick.AddListener(() => OnReturnHome?.Invoke());
            tryAgain.onClick.AddListener(() => OnTryAgain?.Invoke());

            return panel;
        }

        private GameObject CreatePanel(Transform parent, Color color)
        {
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = color;
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private Text CreateText(Transform parent, string name, string text, int size, TextAnchor anchor, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text t = go.AddComponent<Text>();
            Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtin == null)
            {
                builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            t.font = builtin;
            t.text = text;
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = anchor;
            RectTransform rect = t.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900, 200);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            return t;
        }

        private GameObject CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            RectTransform rect = img.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            return go;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.5f, 0.6f, 0.9f);
            Button btn = go.AddComponent<Button>();

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260, 44);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;

            CreateText(go.transform, name + "Text", label, 16, TextAnchor.MiddleCenter, new Vector2(0, -12));
            return btn;
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        public void ShowHome(bool show) => homePanel.SetActive(show);
        public void ShowHUD(bool show) => hudPanel.SetActive(show);
        public void ShowBreathing(bool show) => breathingPanel.SetActive(show);
        public void ShowGumPanel(bool show) => gumPanel.SetActive(show);
        public void ShowCalming(bool show) => calmingPanel.SetActive(show);
        public void ShowCompletion(bool show) => completionPanel.SetActive(show);
        public void ShowCelebration(bool show) => celebrationPanel.SetActive(show);

        public void SetPrompt(string text) => promptText.text = text;
        public void SetMessage(string text) => messageText.text = text;
        public void SetUrge(string text) => urgeText.text = text;
        public void SetPhase(string text) => phaseText.text = text;

        public void SetAnxiety(float pct)
        {
            anxietyText.text = $"Anxiety: {Mathf.RoundToInt(pct)}%";
            float t = Mathf.Clamp01(pct / 100f);
            anxietyBarFill.rectTransform.sizeDelta = new Vector2(200f * t, 14f);
            anxietyBarFill.color = Color.Lerp(new Color(0.2f, 0.8f, 0.3f), new Color(0.9f, 0.2f, 0.2f), t);

            Color v = vignette.color;
            v.a = Mathf.Clamp01(t * 0.6f);
            vignette.color = v;
        }

        public void UpdateBreathing(string label, float normalized)
        {
            breathingText.text = label;
            float size = Mathf.Lerp(140f, 260f, normalized);
            breathingCircle.rectTransform.sizeDelta = new Vector2(size, size);
        }

        public void SetCompletionStats(string title, string subtitle, string badge, string stats, string insights)
        {
            completionTitle.text = title;
            completionSubtitle.text = subtitle;
            badgeText.text = badge;
            statsText.text = stats;
            insightsText.text = insights;
        }

        public void SetCelebration(string title, string subtitle)
        {
            celebrationTitle.text = title;
            celebrationSubtitle.text = subtitle;
        }
    }
}
