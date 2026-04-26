using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Placed on the coffee-shop door trigger zone in the street scene.
    /// When the player walks into range, GameDirector.SetNearCafe() is called.
    /// The player then presses [E] to enter, which GameDirector handles.
    /// </summary>
    public class CafeEntryTrigger : MonoBehaviour
    {
        private GameDirector director;

        private void Start()
        {
            director = Object.FindFirstObjectByType<GameDirector>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<SimplePlayerController>() != null)
                director?.SetNearCafe(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SimplePlayerController>() != null)
                director?.SetNearCafe(false);
        }
    }
}
