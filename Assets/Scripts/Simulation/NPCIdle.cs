using UnityEngine;

namespace OCDSimulation
{
    public class NPCIdle : MonoBehaviour
    {
        public float bobAmplitude = 0.02f;
        public float bobSpeed = 1.2f;
        public float swayAmplitude = 2f;
        public float swaySpeed = 0.6f;

        private Vector3 startPos;
        private float offset;

        private void Start()
        {
            startPos = transform.position;
            offset = Random.Range(0f, 3f);
        }

        private void Update()
        {
            float t = Time.time + offset;
            float y = Mathf.Sin(t * bobSpeed) * bobAmplitude;
            transform.position = startPos + new Vector3(0f, y, 0f);
            transform.rotation = Quaternion.Euler(0f, Mathf.Sin(t * swaySpeed) * swayAmplitude, 0f);
        }
    }
}
