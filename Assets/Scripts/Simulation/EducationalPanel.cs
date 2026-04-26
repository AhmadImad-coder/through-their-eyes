using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Static library of OCD educational facts shown on the completion screen.
    /// </summary>
    public static class EducationalPanel
    {
        private static readonly string[] facts =
        {
            "OCD affects ~1-2% of the world's population — over 70 million people globally.",
            "ERP (Exposure and Response Prevention) is the gold-standard therapy for OCD, with 60-80% effectiveness.",
            "OCD is not about being 'too clean' — it is a serious anxiety disorder driven by intrusive, unwanted thoughts.",
            "The anxiety from an OCD trigger naturally decreases over time if you resist the compulsion. This is called habituation.",
            "CBT helps people challenge the distorted meaning they assign to intrusive thoughts — thoughts are not facts.",
            "Most people with OCD experience significant relief within 12-16 weeks of ERP therapy.",
            "The 5-4-3-2-1 Grounding Technique redirects focus from anxiety to present-moment sensory experiences.",
            "Mindfulness teaches us to observe intrusive thoughts without judgment — noticing them without obeying them.",
            "Brain scans show that ERP therapy physically changes the neural pathways associated with OCD responses.",
            "Journaling intrusive thoughts helps externalize them, making them easier to challenge and reframe.",
            "Compulsions provide only short-term relief and reinforce the OCD cycle — resisting them breaks the loop.",
            "The average person with OCD waits 14-17 years before receiving a correct diagnosis and proper treatment."
        };

        public static string GetRandomFact()
        {
            return facts[Random.Range(0, facts.Length)];
        }
    }
}
