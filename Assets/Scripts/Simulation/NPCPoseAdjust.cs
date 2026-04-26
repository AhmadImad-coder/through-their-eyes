using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Attach to any NPC to give it live Inspector controls for fine-tuning
    /// position and rotation without rebuilding the whole scene.
    ///
    /// HOW TO USE (while game is playing):
    ///   1. Click the NPC root object (Emma, Jake, C_Chat1, etc.) in the Hierarchy.
    ///   2. Find "NPC Pose Adjust (Script)" in the Inspector.
    ///   3. Change "Position Offset" or "Rotation Offset" — NPC moves within 1 frame.
    ///   4. Stop the game — your values are saved automatically and reload next time.
    ///
    /// Reset an NPC back to its base position: set both offsets to (0, 0, 0).
    /// </summary>
    public class NPCPoseAdjust : MonoBehaviour
    {
        [Header("Fine-tune this NPC (changes save automatically)")]
        [Tooltip("Move the NPC relative to its spawned position. " +
                 "Y = up/down into chair,  X = left/right,  Z = forward/back")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("Rotate the NPC on top of its base rotation. " +
                 "X = tilt fwd/back,  Y = turn left/right,  Z = lean sideways")]
        public Vector3 rotationOffset = Vector3.zero;

        // ── Internals ──────────────────────────────────────────────────────────
        private Vector3    _basePos;
        private Quaternion _baseRot;
        private string     _prefKey;

        // Track previous values so Update() only acts when something changed
        private Vector3    _prevPosOffset;
        private Vector3    _prevRotOffset;
        private bool       _initialized;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            Init();
        }

        /// <summary>
        /// Polls for Inspector changes every frame.
        /// More reliable than OnValidate() for runtime-added components in Unity 6.
        /// </summary>
        private void Update()
        {
            if (!_initialized) { Init(); return; }

            if (positionOffset != _prevPosOffset || rotationOffset != _prevRotOffset)
            {
                ApplyOffset();
                SaveToPrefs();
                _prevPosOffset = positionOffset;
                _prevRotOffset = rotationOffset;
            }
        }

        /// <summary>
        /// OnValidate fires in the Editor when Inspector values change.
        /// Kept as a fast-path complement to Update().
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying || !_initialized) return;
            ApplyOffset();
            SaveToPrefs();
            _prevPosOffset = positionOffset;
            _prevRotOffset = rotationOffset;
        }

        // ── Initialise once ────────────────────────────────────────────────────

        private void Init()
        {
            if (_initialized) return;
            _basePos  = transform.position;
            _baseRot  = transform.rotation;
            _prefKey  = "pose_v3_" + gameObject.name;
            LoadFromPrefs();
            ApplyOffset();
            _prevPosOffset = positionOffset;
            _prevRotOffset = rotationOffset;
            _initialized   = true;
        }

        // ── Apply / Save / Load ────────────────────────────────────────────────

        private void ApplyOffset()
        {
            if (!_initialized) return;
            transform.position = _basePos + positionOffset;
            transform.rotation = _baseRot * Quaternion.Euler(rotationOffset);
        }

        private void SaveToPrefs()
        {
            if (_prefKey == null) return;
            PlayerPrefs.SetFloat(_prefKey + "_px", positionOffset.x);
            PlayerPrefs.SetFloat(_prefKey + "_py", positionOffset.y);
            PlayerPrefs.SetFloat(_prefKey + "_pz", positionOffset.z);
            PlayerPrefs.SetFloat(_prefKey + "_rx", rotationOffset.x);
            PlayerPrefs.SetFloat(_prefKey + "_ry", rotationOffset.y);
            PlayerPrefs.SetFloat(_prefKey + "_rz", rotationOffset.z);
            PlayerPrefs.SetInt (_prefKey + "_has", 1);
            PlayerPrefs.Save();
        }

        private void LoadFromPrefs()
        {
            if (_prefKey == null) return;
            if (PlayerPrefs.GetInt(_prefKey + "_has", 0) == 0) return;

            positionOffset = new Vector3(
                PlayerPrefs.GetFloat(_prefKey + "_px", 0f),
                PlayerPrefs.GetFloat(_prefKey + "_py", 0f),
                PlayerPrefs.GetFloat(_prefKey + "_pz", 0f));

            rotationOffset = new Vector3(
                PlayerPrefs.GetFloat(_prefKey + "_rx", 0f),
                PlayerPrefs.GetFloat(_prefKey + "_ry", 0f),
                PlayerPrefs.GetFloat(_prefKey + "_rz", 0f));
        }
    }
}
