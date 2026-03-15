using UnityEngine;

namespace OCDSimulation
{
    public class GumSpot : MonoBehaviour
    {
        public bool removed = false;

        public void Remove()
        {
            if (removed) return;
            removed = true;
            gameObject.SetActive(false);
        }
    }
}
