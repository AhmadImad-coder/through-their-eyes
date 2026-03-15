using UnityEngine;

namespace OCDSimulation
{
    [RequireComponent(typeof(Collider))]
    public class ScratchSpot : MonoBehaviour
    {
        public bool hover = false;

        private void OnMouseEnter()
        {
            hover = true;
        }

        private void OnMouseExit()
        {
            hover = false;
        }
    }
}
