using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OCDSimulation
{
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        public float moveSpeed = 3.5f;
        public float lookSpeed = 2f;
        public float gravity  = -9.81f;

        private CharacterController controller;
        private Rigidbody           rb;
        private Camera              cam;
        private float               pitch = 0f;
        private bool                movementLocked = false;
        private bool                inputEnabled   = true;
        private float               verticalVelocity = 0f;

        // When true, Escape key no longer unlocks cursor (used by SettingsManager)
        public  bool                suppressEscapeKey = false;

        public  Camera              Camera     => cam;
        public  CharacterController Controller => controller;
        public  bool                InputEnabled => inputEnabled;

        // ── DIAGNOSTIC STATE (for on-screen HUD) ─────────────────────────────
        private float   _dbgH, _dbgV;
        private Vector3 _dbgMove;
        private int     _dbgMoveCalls;
        private bool    _dbgGrounded;
        private string  _dbgInputSource  = "?";
        private string  _dbgGroundHitObj = "(none)";
        private int     _dbgRescues;
        private float   _dbgGroundNormalY;
        private float   _dbgGroundY;
        private int     _dbgOverlapCount;
        private string  _dbgOverlapNames = "";
        private string  _dbgRoadStatus   = "waiting...";

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            // ── CRITICAL Unity 6 physics fix ─────────────────────────────────
            // autoSyncTransforms: when true, every Transform.position write is
            // immediately flushed to the PhysX broadphase, so the NEXT line of
            // code's raycast/spherecast sees an up-to-date world.
            // The Unity 6 default for new projects is FALSE, which means static
            // colliders created at runtime (road, floor) may not be present in
            // the broadphase until the next FixedUpdate — Physics.RaycastAll
            // therefore returns 0 hits every frame and the player free-falls.
            Physics.autoSyncTransforms = true;

            // ── CharacterController bypass ───────────────────────────────────
            // Unity 6's CharacterController has a known bug where it becomes
            // "inactive" in PhysX after AddComponent at runtime, silently
            // rejecting every Move() call (confirmed by on-screen HUD:
            // 8248 Move calls, CollisionFlags.None, position never changed).
            // We keep the CC for its capsule collider (so friend-table trigger
            // volumes still detect the player) but DO NOT call controller.Move.
            // Movement uses transform.position directly with a raycast ground
            // snap — rock-solid and bypasses the CC bug entirely.
            //
            // Add a kinematic Rigidbody so OnTriggerEnter still fires on the
            // CafeEntryTrigger when we walk into it — PhysX requires at least
            // one Rigidbody on either side of a trigger contact.
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic              = true;
                rb.useGravity               = false;
                // Interpolation OFF — with transform-driven movement, Unity's
                // physics interpolator fights our writes and produces a visible
                // up/down "jump" every frame as it lerps between cached states.
                rb.interpolation            = RigidbodyInterpolation.None;
                rb.collisionDetectionMode   = CollisionDetectionMode.ContinuousSpeculative;
                rb.constraints              = RigidbodyConstraints.FreezeRotation;
            }

            UnlockCursor();
        }

        private void Start()
        {
            // ── GUARANTEED PHYSICS FLOOR ────────────────────────────────────
            // Unity 6 does not register static colliders created via
            // GameObject.CreatePrimitive() during Start() into PhysX until an
            // indeterminate future physics step.  Physics.OverlapSphere with a
            // 10 m radius returned 0 colliders even though road geometry is
            // visually present — confirming the PhysX broadphase is completely
            // empty at gameplay time.
            //
            // Fix: create ONE large invisible BoxCollider here, in Start().
            // Objects created in Start() ARE immediately available to this
            // script's own Update() on the same frame.  A single 150×150 m
            // slab at Y = -0.25 (top surface ≈ Y 0) covers the entire street
            // AND indoor café floor (both scenes use Y ≈ 0 as ground level).
            // The SphereCast in Update() will snap the player to its surface.
            GameObject physGround = new GameObject("_PhysicsGround");
            physGround.transform.position = new Vector3(0f, -0.25f, -20f);
            BoxCollider physBC = physGround.AddComponent<BoxCollider>();
            physBC.size = new Vector3(150f, 0.5f, 150f);
            // No Renderer — invisible slab, physics only.

            _dbgRoadStatus = "PhysicsGround created at Start()";
        }

        public void BindCamera(Camera camera)
        {
            cam = camera;
        }

        public void LockMovement(bool locked)
        {
            movementLocked = locked;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (enabled) LockCursor();
            else         UnlockCursor();
        }

        public void ForceLockCursor()
        {
            if (!inputEnabled) return;
            LockCursor();
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            // We use transform-based movement so teleport is a plain assignment.
            transform.position = position;
            transform.rotation = rotation;
            verticalVelocity   = 0f;
            Physics.SyncTransforms();
        }

        public void SetCameraPose(Vector3 localPosition, Quaternion localRotation)
        {
            if (cam == null) return;

            cam.transform.localPosition = localPosition;
            cam.transform.localRotation = localRotation;

            float eulerPitch = localRotation.eulerAngles.x;
            if (eulerPitch > 180f) eulerPitch -= 360f;
            pitch = Mathf.Clamp(eulerPitch, -80f, 80f);
        }

        // ── Input helpers: support BOTH legacy Input and new Input System ─────
        // The on-screen HUD proved Input.GetAxis("Horizontal") returns 0 in this
        // project (new Input System is active).  Input.GetKey and the new
        // Keyboard.current APIs still work — so we try both every frame.
        private bool KeyHeld(KeyCode legacy, string newSystemKeyName)
        {
            // Legacy Input (works when Active Input Handling = Both/Legacy)
            bool legacyHeld = false;
            try { legacyHeld = Input.GetKey(legacy); } catch { legacyHeld = false; }

            #if ENABLE_INPUT_SYSTEM
            // New Input System (works when Active Input Handling = Both/New)
            if (!legacyHeld && Keyboard.current != null)
            {
                var control = Keyboard.current[newSystemKeyName] as UnityEngine.InputSystem.Controls.KeyControl;
                if (control != null && control.isPressed) return true;
            }
            #endif
            return legacyHeld;
        }

        private Vector2 ReadMoveAxis()
        {
            float h = 0f, v = 0f;
            bool usedLegacy = false, usedNew = false;

            // --- Legacy keyboard read ---
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    { v += 1f; usedLegacy = true; }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  { v -= 1f; usedLegacy = true; }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) { h += 1f; usedLegacy = true; }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  { h -= 1f; usedLegacy = true; }

            #if ENABLE_INPUT_SYSTEM
            // --- New Input System keyboard read ---
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    { if (v <  1f) v =  1f; usedNew = true; }
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  { if (v > -1f) v = -1f; usedNew = true; }
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) { if (h <  1f) h =  1f; usedNew = true; }
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  { if (h > -1f) h = -1f; usedNew = true; }
            }
            #endif

            _dbgInputSource =
                (usedLegacy && usedNew)  ? "Both" :
                 usedLegacy              ? "Legacy" :
                 usedNew                 ? "NewIS"  :
                                           "none-pressed";
            return new Vector2(h, v);
        }

        private Vector2 ReadMouseDelta()
        {
            // Try new Input System first (more reliable in Unity 6)
            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 d = Mouse.current.delta.ReadValue();
                if (d.sqrMagnitude > 0.0001f) return d * 0.08f;
            }
            #endif
            // Fall back to legacy
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        private bool MouseLeftPressedThisFrame()
        {
            if (Input.GetMouseButtonDown(0)) return true;
            #if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            #endif
            return false;
        }

        private bool EscapePressedThisFrame()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) return true;
            #if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            #endif
            return false;
        }

        private void Update()
        {
            if (cam == null) return;

            if (inputEnabled && MouseLeftPressedThisFrame())
                LockCursor();

            if (!suppressEscapeKey && EscapePressedThisFrame())
                UnlockCursor();

            // Mouse look
            if (inputEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouseDelta = ReadMouseDelta();
                float mouseX = mouseDelta.x * lookSpeed;
                float mouseY = mouseDelta.y * lookSpeed;
                pitch = Mathf.Clamp(pitch - mouseY, -80f, 80f);
                cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                transform.Rotate(Vector3.up * mouseX);
            }

            if (movementLocked || !inputEnabled) return;

            // Read keyboard input
            Vector2 axis = ReadMoveAxis();
            float h = axis.x, v = axis.y;
            _dbgH = h; _dbgV = v;

            // ── 1. Horizontal movement (transform-based, no CC) ───────────────
            Vector3 horizontalMove = (transform.right * h + transform.forward * v) * moveSpeed;
            // Start newPos at current position — ground snap below will fix Y.
            Vector3 newPos = new Vector3(
                transform.position.x + horizontalMove.x * Time.deltaTime,
                transform.position.y,
                transform.position.z + horizontalMove.z * Time.deltaTime);

            // ── Software collision (PhysX queries are empty in this project) ──
            // Resolve XZ movement against all registered AABB obstacles so the
            // player cannot walk through walls, tables, NPCs, or buildings.
            newPos = SoftCollision.ResolveXZ(transform.position, newPos);

            _dbgMove = horizontalMove;
            _dbgMoveCalls++;

            // ── 2. Ground snap ────────────────────────────────────────────────
            //
            // CONFIRMED (via OverlapSphere with r=10 returning 0): Unity 6 does
            // not register procedurally-created static colliders into the PhysX
            // broadphase in this project configuration, so ALL physics queries
            // (RaycastAll, SphereCastAll, OverlapSphere) return 0 results
            // regardless of collider thickness, layer mask, or SyncTransforms.
            //
            // The entire game takes place on flat ground at Y ≈ 0:
            //   • Outdoor street  — road/pavement top at Y ≈ 0
            //   • Indoor café     — floor top at Y ≈ 0.025
            //
            // We bypass physics entirely and snap the player to the known
            // flat ground height directly.  Physics is still attempted first
            // (so the fix automatically upgrades if the broadphase starts
            // working), but the guaranteed fallback is always Y = 0.
            //
            // DIAGNOSTIC: keep OverlapSphere so the HUD shows if physics
            // ever starts working.
            if (_dbgMoveCalls % 30 == 0)
            {
                Collider[] nearby = Physics.OverlapSphere(
                    transform.position, 10f, ~0, QueryTriggerInteraction.Ignore);
                _dbgOverlapCount = nearby.Length;
                _dbgOverlapNames = "";
                for (int i = 0; i < Mathf.Min(4, nearby.Length); i++)
                    _dbgOverlapNames += nearby[i].gameObject.name + " ";
            }

            // ── Try physics first (SphereCast) ────────────────────────────────
            const float SphereRadius = 0.3f;
            Vector3 castStart = newPos + Vector3.up * 5f;
            RaycastHit[] allHits = Physics.SphereCastAll(
                castStart, SphereRadius, Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);

            bool hitGround = false;
            float targetGroundY = 0f;   // default flat-ground fallback
            float bestDist = float.MaxValue;
            for (int i = 0; i < allHits.Length; i++)
            {
                Transform t = allHits[i].collider.transform;
                if (t == transform || t.IsChildOf(transform) ||
                    (transform.parent != null && t.IsChildOf(transform.parent))) continue;
                if (allHits[i].normal.y < 0.7f) continue;
                if (allHits[i].distance < bestDist)
                {
                    bestDist     = allHits[i].distance;
                    targetGroundY = allHits[i].point.y;
                    hitGround    = true;
                    _dbgGroundHitObj  = allHits[i].collider.gameObject.name;
                    _dbgGroundNormalY = allHits[i].normal.y;
                }
            }

            if (!hitGround)
            {
                // Physics returned nothing — use flat-ground fallback.
                // Y = 0 is the correct ground level for both outdoor and indoor.
                targetGroundY    = 0f;
                _dbgGroundHitObj = "FLAT_FALLBACK_Y0 (physics empty=" + allHits.Length + ")";
                _dbgGroundNormalY = 1f;
            }

            // Smooth approach to target ground Y — eliminates any "jump" from
            // sudden snaps when transitioning between heights.
            float curY = transform.position.y;
            if (Mathf.Abs(curY - targetGroundY) < 0.015f)
                newPos.y = targetGroundY;
            else
                newPos.y = Mathf.Lerp(curY, targetGroundY, 12f * Time.deltaTime);

            verticalVelocity    = 0f;
            _dbgGrounded        = true;
            _dbgGroundY         = targetGroundY;
            _dbgRescues         = 0;   // no more falling — reset counter

            transform.position = newPos;
            // *** Do NOT call Physics.SyncTransforms() here. ***
            // Calling it every Update frame triggers a full PhysX broadphase
            // rebuild every ~16 ms, and RaycastAll/SphereCastAll during the
            // rebuild window return 0 hits → player falls forever.
            // autoSyncTransforms=true (set in Awake) handles sync automatically.
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // ── On-screen diagnostic HUD ─────────────────────────────────────────
        public static bool ShowDebugHud = false;  // walking is fixed — hide HUD
        private void OnGUI()
        {
            if (!ShowDebugHud) return;
            GUI.Box(new Rect(8, 8, 560, 360), "");
            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.fontSize = 13;
            s.normal.textColor = Color.white;
            Vector3 p = transform.position;
            string text =
              "=== WALKING DEBUG  (press F1 to hide) ===\n" +
              "inputSource      = " + _dbgInputSource + "\n" +
              "inputEnabled     = " + inputEnabled + "\n" +
              "movementLocked   = " + movementLocked + "\n" +
              "Cursor.lockState = " + Cursor.lockState + "\n" +
              "isGrounded (ray) = " + _dbgGrounded + "\n" +
              "ground hit       = " + _dbgGroundHitObj + "\n" +
              "ground normalY   = " + _dbgGroundNormalY.ToString("F2") + "  (>=0.70 accepted)\n" +
              "ground Y target  = " + _dbgGroundY.ToString("F3") + "\n" +
              "cast mode        = SphereCastAll r=0.3 from posY+5, dist 60m\n" +
              "OverlapSphere(r=10)= " + _dbgOverlapCount + " colliders: " + _dbgOverlapNames + "\n" +
              "physGround status  = " + _dbgRoadStatus + "\n" +
              "position         = (" + p.x.ToString("F2") + ", " + p.y.ToString("F2") + ", " + p.z.ToString("F2") + ")\n" +
              "Horizontal(A/D)  = " + _dbgH.ToString("F2") + "     Vertical(W/S) = " + _dbgV.ToString("F2") + "\n" +
              "moveVector       = (" + _dbgMove.x.ToString("F2") + ", " + _dbgMove.y.ToString("F2") + ", " + _dbgMove.z.ToString("F2") + ")\n" +
              "verticalVel      = " + verticalVelocity.ToString("F2") + "\n" +
              "Move() calls     = " + _dbgMoveCalls + "\n" +
              "rescues          = " + _dbgRescues + "\n" +
              "movement mode    = transform.position + ray snap";
            GUI.Label(new Rect(16, 14, 550, 350), text, s);

            if (Event.current != null && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F1)
                ShowDebugHud = false;
        }
    }
}
