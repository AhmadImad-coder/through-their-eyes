using System.Collections.Generic;
using UnityEngine;

namespace OCDSimulation
{
    public struct Achievement
    {
        public string name;
        public string description;
        public Color color;

        public Achievement(string n, string d, Color c)
        {
            name = n;
            description = d;
            color = c;
        }
    }

    /// <summary>
    /// Evaluates which achievements the player has earned at the end of a session.
    /// </summary>
    public static class AchievementSystem
    {
        private static readonly Color Gold   = new Color(1f,    0.84f, 0f,   1f);
        private static readonly Color Silver = new Color(0.75f, 0.75f, 0.75f, 1f);
        private static readonly Color Bronze = new Color(0.80f, 0.50f, 0.20f, 1f);

        public static List<Achievement> Evaluate(SessionStats stats)
        {
            var unlocked = new List<Achievement>();

            // Bronze — First Steps: complete the scenario at all
            if (stats.resistanceSuccessful)
                unlocked.Add(new Achievement(
                    "First Steps",
                    "Completed a full ERP session",
                    Bronze));

            // Gold — Iron Will: zero relapses
            if (stats.resistanceSuccessful && stats.relapseCount == 0)
                unlocked.Add(new Achievement(
                    "Iron Will",
                    "Finished with zero relapses",
                    Gold));

            // Silver — Deep Breather: 3+ breathing cycles
            if (stats.breathingCyclesCompleted >= 3)
                unlocked.Add(new Achievement(
                    "Deep Breather",
                    "Completed 3 or more breathing cycles",
                    Silver));

            // Gold — Mindful Mind: used both breathing AND grounding
            if (stats.bothTechniquesUsed)
                unlocked.Add(new Achievement(
                    "Mindful Mind",
                    "Used both breathing and 5-4-3-2-1 grounding in one session",
                    Gold));

            // Bronze — Speed Run: finished in under 3 minutes
            if (stats.resistanceSuccessful && stats.totalSessionTime < 180f)
                unlocked.Add(new Achievement(
                    "Speed Run",
                    "Completed the session in under 3 minutes",
                    Bronze));

            // Silver — Steady Hands: anxiety below 50% when Stage 3 begins
            if (stats.resistanceSuccessful && stats.anxietyAtStage3Entry < 50f)
                unlocked.Add(new Achievement(
                    "Steady Hands",
                    "Reached Stage 3 with anxiety below 50%",
                    Silver));

            // Gold — Therapist's Pick: S-grade (no relapses, few attempts)
            if (stats.resistanceSuccessful &&
                stats.relapseCount == 0 &&
                stats.totalScratchAttempts < 6)
                unlocked.Add(new Achievement(
                    "Therapist's Pick",
                    "Earned the highest ERP grade: S",
                    Gold));

            // Bronze — Resilient: 3+ relapses but still finished
            if (stats.resistanceSuccessful && stats.relapseCount >= 3)
                unlocked.Add(new Achievement(
                    "Resilient",
                    "Recovered from 3+ relapses and completed the session",
                    Bronze));

            // Silver — Reflective: logged 3+ journal entries
            if (stats.journalEntries >= 3)
                unlocked.Add(new Achievement(
                    "Reflective",
                    "Logged 3 or more thoughts in the Thought Journal",
                    Silver));

            return unlocked;
        }
    }
}
