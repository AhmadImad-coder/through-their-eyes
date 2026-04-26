using System.Collections.Generic;
using UnityEngine;

namespace OCDSimulation
{
    public class JournalEntry
    {
        public float sessionTime;
        public string emotion;

        public JournalEntry(float t, string e) { sessionTime = t; emotion = e; }
    }

    /// <summary>
    /// CBT Thought Journal — press [J] during gameplay to log how you feel right now.
    /// Entries are summarised on the completion screen, modelling the CBT thought-recording
    /// technique used in real Exposure and Response Prevention therapy.
    /// </summary>
    public class ThoughtJournal : MonoBehaviour
    {
        private UIManager ui;
        private List<JournalEntry> entries = new List<JournalEntry>();
        private bool isOpen = false;
        private float sessionStartTime;

        public int  EntryCount => entries.Count;
        public bool IsOpen     => isOpen;

        private static readonly string[] emotions =
        {
            "I feel contaminated right now",
            "I'm embarrassed about my OCD",
            "The urge to clean is overwhelming",
            "I'm worried others will judge me",
            "I am actively resisting the urge",
            "This is really hard right now",
            "I'm starting to manage the anxiety",
            "I feel proud for resisting so far"
        };

        public void Initialize(UIManager uiManager)
        {
            ui = uiManager;
            ui.OnJournalEntry += OnEntrySelected;
            ui.OnJournalClose += OnJournalClose;
            sessionStartTime = Time.time;
        }

        public void ToggleJournal(float currentAnxiety)
        {
            isOpen = !isOpen;
            ui.ShowJournal(isOpen, currentAnxiety);
        }

        public void ResetJournal()
        {
            entries.Clear();
            sessionStartTime = Time.time;
        }

        private void OnEntrySelected(string emotion)
        {
            entries.Add(new JournalEntry(Time.time - sessionStartTime, emotion));
            isOpen = false;
            ui.ShowJournal(false, 0f);
        }

        private void OnJournalClose()
        {
            isOpen = false;
            ui.ShowJournal(false, 0f);
        }

        public static string[] GetEmotions() => emotions;
    }
}
