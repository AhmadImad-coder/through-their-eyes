using UnityEngine;

namespace OCDSimulation
{
    [RequireComponent(typeof(Collider))]
    public class DirtSpot : MonoBehaviour
    {
        public bool cleaned = false;

        private void OnMouseDown()
        {
            GameDirector director = FindFirstObjectByType<GameDirector>();
            if (director != null)
            {
                director.OnDirtClicked(this);
            }
        }

        public void Clean()
        {
            if (cleaned) return;
            cleaned = true;
            gameObject.SetActive(false);
        }
    }
}
