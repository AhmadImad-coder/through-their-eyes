using UnityEngine;

namespace OCDSimulation
{
    [RequireComponent(typeof(Collider))]
    public class FriendTableTrigger : MonoBehaviour
    {
        private GameDirector director;

        private void Awake()
        {
            director = FindFirstObjectByType<GameDirector>();
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<SimplePlayerController>() == null) return;
            director?.SetNearFriends(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SimplePlayerController>() == null) return;
            director?.SetNearFriends(false);
        }
    }
}
