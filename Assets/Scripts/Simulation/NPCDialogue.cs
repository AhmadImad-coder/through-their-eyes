using System.Collections;
using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Displays contextual speech-bubble dialogue from friend NPCs at key game moments.
    /// Dialogue lines are chosen based on the current GamePhase and mirror real OCD
    /// social situations to add immersion and emotional depth.
    /// </summary>
    public class NPCDialogue : MonoBehaviour
    {
        private UIManager ui;
        private Coroutine currentDialogue;

        public void Initialize(UIManager uiManager)
        {
            ui = uiManager;
        }

        /// <summary>Show a dialogue bubble for <paramref name="duration"/> seconds.</summary>
        public void ShowDialogue(string speakerName, string message, float duration)
        {
            if (currentDialogue != null) StopCoroutine(currentDialogue);
            currentDialogue = StartCoroutine(DialogueRoutine(speakerName, message, duration));
        }

        /// <summary>Called by GameDirector whenever the game phase changes.</summary>
        public void OnPhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Stage0:
                    ShowDialogue("Emma",
                        "Isn't it cozy in here? The coffee smells amazing!", 4f);
                    break;
                case GamePhase.Stage1:
                    ShowDialogue("Jake",
                        "So — how's everything going with you lately?", 4f);
                    break;
                case GamePhase.Stage2:
                    ShowDialogue("Mia",
                        "You seem a little distracted. Everything okay?", 4f);
                    break;
                case GamePhase.Stage3:
                    ShowDialogue("Emma",
                        "Just focus on us, not the table. We're here for you.", 5f);
                    break;
                case GamePhase.Calming:
                    ShowDialogue("Jake",
                        "Hey — breathe. You've got this, take your time.", 4f);
                    break;
                case GamePhase.Recovery:
                    ShowDialogue("Mia",
                        "You did it! We're so proud of you. Breathe easy.", 5f);
                    break;
            }
        }

        private IEnumerator DialogueRoutine(string speakerName, string message, float duration)
        {
            ui.ShowNPCDialogue(speakerName, message);
            yield return new WaitForSeconds(duration);
            ui.HideNPCDialogue();
            currentDialogue = null;
        }
    }
}
