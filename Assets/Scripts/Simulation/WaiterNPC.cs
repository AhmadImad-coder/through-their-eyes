using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Moves a waiter NPC back and forth between the espresso counter and a
    /// customer table, simulating real café service.
    ///
    /// The route is a direct XZ path; SoftCollision.ResolveXZ keeps the waiter
    /// from clipping through walls or tables in the same way it does for the
    /// player.  Y is always snapped to 0 (flat indoor floor).
    /// </summary>
    public class WaiterNPC : MonoBehaviour
    {
        [Header("Route")]
        public Vector3 counterPoint;      // where the waiter picks up coffee
        public Vector3 tablePoint;        // where the waiter delivers
        public Vector3[] tablePoints;     // optional route through multiple tables

        [Header("Timing")]
        public float walkSpeed      = 1.6f;   // metres per second
        public float pauseAtTable   = 2.8f;   // seconds spent "serving"
        public float pauseAtCounter = 4.0f;   // seconds spent "restocking"

        [Header("Start position")]
        public bool startAtCounter = true;    // false = start halfway to table

        // ── Internal state ───────────────────────────────────────────────────
        private enum State { PausingAtCounter, WalkToTable, PausingAtTable, WalkToCounter }
        private State  _state;
        private float  _pauseTimer;
        private Vector3 _prevPos;
        private int _tableIndex = 0;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Start()
        {
            if (startAtCounter)
            {
                transform.position = new Vector3(counterPoint.x, 0f, counterPoint.z);
                _state      = State.WalkToTable;
                _pauseTimer = 0f;
                FacePoint(GetCurrentTablePoint());
            }
            else
            {
                // Start mid-journey toward the table so the two waiters are visually
                // offset from each other when the scene loads.
                Vector3 mid = Vector3.Lerp(counterPoint, GetCurrentTablePoint(), 0.5f);
                transform.position = new Vector3(mid.x, 0f, mid.z);
                _state = State.WalkToTable;
                FacePoint(GetCurrentTablePoint());
            }
            _prevPos = transform.position;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.PausingAtCounter:
                    _pauseTimer -= Time.deltaTime;
                    if (_pauseTimer <= 0f)
                        _state = State.WalkToTable;
                    break;

                case State.WalkToTable:
                    if (StepToward(GetCurrentTablePoint()))
                    {
                        _state      = State.PausingAtTable;
                        _pauseTimer = pauseAtTable;
                        // Face the table so the cup is "offered" to the seated customer
                        FacePoint(GetCurrentTablePoint() + new Vector3(0f, 0f, -0.5f));
                    }
                    break;

                case State.PausingAtTable:
                    _pauseTimer -= Time.deltaTime;
                    if (_pauseTimer <= 0f)
                        _state = State.WalkToCounter;
                    break;

                case State.WalkToCounter:
                    if (StepToward(counterPoint))
                    {
                        _state      = State.PausingAtCounter;
                        _pauseTimer = pauseAtCounter;
                        AdvanceTable();
                    }
                    break;
            }
        }

        // ── Movement helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Move one frame toward <paramref name="target"/> with SoftCollision
        /// avoidance. Returns true when the destination is reached.
        /// </summary>
        private bool StepToward(Vector3 target)
        {
            Vector3 pos = transform.position;
            float dx = target.x - pos.x;
            float dz = target.z - pos.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            if (dist < 0.12f)
            {
                // Snap exactly to destination
                transform.position = new Vector3(target.x, 0f, target.z);
                return true;
            }

            float step  = walkSpeed * Time.deltaTime;
            float ratio = step / dist;
            Vector3 proposed = new Vector3(
                pos.x + dx * ratio,
                0f,
                pos.z + dz * ratio);

            // Software collision — same system as the player
            Vector3 resolved = SoftCollision.ResolveXZ(_prevPos, proposed);
            _prevPos = resolved;
            transform.position = resolved;

            // Smoothly rotate to face the direction of travel
            float moveDx = resolved.x - pos.x;
            float moveDz = resolved.z - pos.z;
            if (moveDx * moveDx + moveDz * moveDz > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(
                    new Vector3(moveDx, 0f, moveDz), Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, 8f * Time.deltaTime);
            }

            return false;
        }

        private void FacePoint(Vector3 point)
        {
            Vector3 dir = new Vector3(
                point.x - transform.position.x,
                0f,
                point.z - transform.position.z);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        private Vector3 GetCurrentTablePoint()
        {
            if (tablePoints != null && tablePoints.Length > 0)
                return tablePoints[Mathf.Clamp(_tableIndex, 0, tablePoints.Length - 1)];
            return tablePoint;
        }

        private void AdvanceTable()
        {
            if (tablePoints == null || tablePoints.Length == 0) return;
            _tableIndex = (_tableIndex + 1) % tablePoints.Length;
        }
    }
}
