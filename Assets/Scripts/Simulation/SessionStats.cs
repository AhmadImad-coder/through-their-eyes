using System.Collections.Generic;

namespace OCDSimulation
{
    public class SessionStats
    {
        public int totalScratchAttempts = 0;
        public int scratchAttemptsBeforeCalming = 0;
        public int scratchAttemptsAfterCalming = 0;
        public int breathingExercisesUsed = 0;
        public int breathingCyclesCompleted = 0;
        public float timeToResist = 0f;
        public float totalSessionTime = 0f;
        public float peakAnxiety = 0f;
        public bool resistanceSuccessful = false;
        public List<string> techniquesUsed = new List<string>();
        public int relapseCount = 0;
        public float finalAnxiety = 0f;
        public int timesReachedMaxAnxiety = 0;
        public int attemptsAfterCalming = 0;

        // Enhanced tracking fields
        public bool groundingUsed = false;
        public bool bothTechniquesUsed = false;
        public float anxietyAtStage3Entry = 0f;
        public int journalEntries = 0;
    }
}
