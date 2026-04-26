using UnityEngine;

namespace OCDSimulation
{
    // Bug fix: removed OnMouseDown — GameDirector handles all click detection
    // via raycasting to prevent double-invocation of OnDirtClicked.
    [RequireComponent(typeof(Collider))]
    public class DirtSpot : MonoBehaviour
    {
        public bool cleaned = false;

        public void Clean()
        {
            if (cleaned) return;
            cleaned = true;
            gameObject.SetActive(false);
        }
    }
}
