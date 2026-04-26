using UnityEngine;

namespace OCDSimulation
{
    public class NPCIdle : MonoBehaviour
    {
        public float bobAmplitude = 0.008f;
        public float bobSpeed = 1.2f;
        public float swayAmplitude = 1.2f;
        public float swaySpeed = 0.6f;

        private Vector3 basePos;
        private Quaternion baseRot;
        private Vector3 lastAppliedPos;
        private Quaternion lastAppliedRot;
        private float offset;
        private bool initialized;

        private void Start()
        {
            CaptureBasePose();
            offset = Random.Range(0f, 3f);
        }

        private void Update()
        {
            if (!initialized)
                CaptureBasePose();

            bool movedExternally =
                Vector3.Distance(transform.position, lastAppliedPos) > 0.03f ||
                Quaternion.Angle(transform.rotation, lastAppliedRot) > 2f;
            if (movedExternally)
                CaptureBasePose();

            float t = Time.time + offset;
            float y = Mathf.Sin(t * bobSpeed) * bobAmplitude;
            transform.position = basePos + new Vector3(0f, y, 0f);
            transform.rotation = baseRot * Quaternion.Euler(
                0f,
                Mathf.Sin(t * swaySpeed) * swayAmplitude,
                0f);

            lastAppliedPos = transform.position;
            lastAppliedRot = transform.rotation;
        }

        private void CaptureBasePose()
        {
            basePos = transform.position;
            baseRot = transform.rotation;
            lastAppliedPos = basePos;
            lastAppliedRot = baseRot;
            initialized = true;
        }
    }
}
