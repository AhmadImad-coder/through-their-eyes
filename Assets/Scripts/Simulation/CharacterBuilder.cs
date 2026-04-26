using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Builds multi-part humanoid characters entirely from Unity primitives.
    /// No prefabs, no external assets — all shapes created at runtime.
    /// </summary>
    public static class CharacterBuilder
    {
        // ── Skin tone palette ──────────────────────────────────────────────────
        private static readonly Color[] SkinTones = {
            new Color(0.96f, 0.82f, 0.70f),  // fair
            new Color(0.90f, 0.72f, 0.58f),  // light
            new Color(0.80f, 0.62f, 0.46f),  // medium-light
            new Color(0.68f, 0.48f, 0.32f),  // medium
            new Color(0.50f, 0.34f, 0.22f),  // medium-dark
            new Color(0.32f, 0.22f, 0.14f),  // dark
        };

        private static readonly Color[] HairColors = {
            new Color(0.12f, 0.08f, 0.06f),  // black
            new Color(0.28f, 0.18f, 0.10f),  // dark brown
            new Color(0.50f, 0.30f, 0.12f),  // brown
            new Color(0.70f, 0.45f, 0.18f),  // light brown / auburn
            new Color(0.88f, 0.72f, 0.28f),  // blonde
            new Color(0.78f, 0.28f, 0.18f),  // red
            new Color(0.55f, 0.55f, 0.58f),  // grey
        };

        private static int _skinSeed  = 0;
        private static int _hairSeed  = 3;
        private static int _shirtSeed = 7;

        /// <summary>Call once before spawning a new scene so palette seeds reset.</summary>
        public static void ResetSeeds()
        {
            _skinSeed  = 0;
            _hairSeed  = 3;
            _shirtSeed = 7;
            _shader    = null;   // re-find shader in case pipeline changed
        }

        // ── Public entry points ────────────────────────────────────────────────

        /// <summary>
        /// Create a standing humanoid at world-space <paramref name="pos"/>.
        /// Y=0 is floor level — feet touch the floor.
        /// <paramref name="shirtColor"/> drives the shirt; skin/hair auto-vary.
        /// </summary>
        public static GameObject CreateStandingHumanoid(
            string characterName, Vector3 pos, Color shirtColor,
            bool faceTowardsNegZ = true, float scale = 1.0f)
        {
            Color skin = NextSkin();
            Color hair = NextHair();

            GameObject root = new GameObject(characterName);
            root.transform.position   = pos;
            root.transform.localScale = Vector3.one * scale;
            if (faceTowardsNegZ)
                root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            BuildBody(root.transform, skin, shirtColor, hair,
                      seated: false, isBarista: false);
            root.AddComponent<NPCPoseAdjust>();
            return root;
        }

        /// <summary>
        /// Create a seated humanoid whose hips sit at world-space <paramref name="pos"/>.
        /// Pass the same world position you previously used for the capsule NPC.
        /// </summary>
        public static GameObject CreateSeatedHumanoid(
            string characterName, Vector3 pos, Color shirtColor,
            float rotationY = 180f, float scale = 1.0f,
            float rotationX = 0f,   float rotationZ = 0f)
        {
            Color skin = NextSkin();
            Color hair = NextHair();

            GameObject root = new GameObject(characterName);
            root.transform.position   = pos;
            root.transform.localScale = Vector3.one * scale;
            root.transform.rotation   = Quaternion.Euler(rotationX, rotationY, rotationZ);

            BuildBody(root.transform, skin, shirtColor, hair,
                      seated: true, isBarista: false);
            root.AddComponent<NPCPoseAdjust>();
            return root;
        }

        /// <summary>
        /// Create a standing barista (green apron overlay).
        /// </summary>
        public static GameObject CreateBarista(
            string characterName, Vector3 pos, Color apronColor,
            bool faceTowardsNegZ = true, float scale = 1.10f)   // default true → faces customers / player
        {
            Color skin = NextSkin();
            Color hair = NextHair();
            Color shirt = new Color(0.88f, 0.86f, 0.82f);  // off-white shirt under apron

            GameObject root = new GameObject(characterName);
            root.transform.position   = pos;
            root.transform.localScale = Vector3.one * scale;
            if (faceTowardsNegZ)
                root.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            BuildBody(root.transform, skin, shirt, hair,
                      seated: false, isBarista: true, apronColor: apronColor);
            root.AddComponent<NPCPoseAdjust>();
            return root;
        }

        // ── Body builder ──────────────────────────────────────────────────────

        private static void BuildBody(Transform root, Color skin, Color shirt,
                                      Color hair, bool seated, bool isBarista,
                                      Color apronColor = default)
        {
            // All measurements in local-space units (1 unit = 1 metre).
            // Standing total height ≈ 1.75 m from feet (Y=0) to top of head.

            // ── Torso ────────────────────────────────────────────────────────
            float torsoY      = seated ? 0.45f : 0.75f;
            float torsoZ      = seated ? -0.05f : 0f;
            float torsoH      = seated ? 0.48f : 0.50f;
            float torsoW      = 0.32f;
            float torsoD      = 0.22f;

            MakePart(PrimitiveType.Cube, "Torso", root,
                new Vector3(0f, torsoY, torsoZ),
                new Vector3(torsoW, torsoH, torsoD),
                shirt);

            // ── Hips ─────────────────────────────────────────────────────────
            float hipsY = seated ? 0.15f : torsoY - torsoH * 0.5f - 0.08f;
            MakePart(PrimitiveType.Cube, "Hips", root,
                new Vector3(0f, hipsY, seated ? -0.03f : 0f),
                new Vector3(torsoW * 1.05f, seated ? 0.16f : 0.18f, torsoD * 1.10f),
                Darker(shirt, 0.15f));

            // ── Legs (only drawn for seated; standing uses trouser cylinders) ─
            if (seated)
            {
                // For seated NPCs, keep the thighs under the table and the shins
                // dropping close to the chair front so silhouettes stay readable
                // from every chair orientation.
                float thighY = hipsY - 0.03f;
                float thighZ = 0.16f;
                float kneeY = hipsY - 0.23f;
                float shinZ = 0.32f;
                float footY = hipsY - 0.43f;
                float footZ = 0.38f;

                MakePart(PrimitiveType.Cylinder, "LegL_Upper", root,
                    new Vector3(-0.10f, thighY, thighZ),
                    new Vector3(0.09f, 0.16f, 0.09f),
                    Darker(shirt, 0.25f),
                    Quaternion.Euler(90f, 0f, 0f));
                MakePart(PrimitiveType.Cylinder, "LegR_Upper", root,
                    new Vector3( 0.10f, thighY, thighZ),
                    new Vector3(0.09f, 0.16f, 0.09f),
                    Darker(shirt, 0.25f),
                    Quaternion.Euler(90f, 0f, 0f));

                MakePart(PrimitiveType.Cylinder, "LegL_Lower", root,
                    new Vector3(-0.10f, kneeY, shinZ),
                    new Vector3(0.08f, 0.18f, 0.08f),
                    Darker(shirt, 0.25f));
                MakePart(PrimitiveType.Cylinder, "LegR_Lower", root,
                    new Vector3( 0.10f, kneeY, shinZ),
                    new Vector3(0.08f, 0.18f, 0.08f),
                    Darker(shirt, 0.25f));

                MakePart(PrimitiveType.Cube, "FootL", root,
                    new Vector3(-0.10f, footY, footZ),
                    new Vector3(0.10f, 0.06f, 0.18f),
                    new Color(0.18f, 0.14f, 0.10f));
                MakePart(PrimitiveType.Cube, "FootR", root,
                    new Vector3( 0.10f, footY, footZ),
                    new Vector3(0.10f, 0.06f, 0.18f),
                    new Color(0.18f, 0.14f, 0.10f));
            }
            else
            {
                Color trouser = Darker(shirt, 0.30f);
                MakePart(PrimitiveType.Cylinder, "LegL", root,
                    new Vector3(-0.10f, 0.32f, 0f),
                    new Vector3(0.10f, 0.32f, 0.10f),
                    trouser);
                MakePart(PrimitiveType.Cylinder, "LegR", root,
                    new Vector3( 0.10f, 0.32f, 0f),
                    new Vector3(0.10f, 0.32f, 0.10f),
                    trouser);
                MakePart(PrimitiveType.Cube, "FootL", root,
                    new Vector3(-0.10f, 0.05f, 0.08f),
                    new Vector3(0.10f, 0.07f, 0.18f),
                    new Color(0.18f, 0.14f, 0.10f));
                MakePart(PrimitiveType.Cube, "FootR", root,
                    new Vector3( 0.10f, 0.05f, 0.08f),
                    new Vector3(0.10f, 0.07f, 0.18f),
                    new Color(0.18f, 0.14f, 0.10f));
            }

            // ── Arms ─────────────────────────────────────────────────────────
            float shoulderY  = torsoY + torsoH * 0.35f;
            float upperArmH  = 0.22f;
            float lowerArmH  = 0.20f;
            float elbowY     = shoulderY - upperArmH;

            // Upper arms
            MakePart(PrimitiveType.Cylinder, "ArmL_Upper", root,
                new Vector3(-(torsoW * 0.5f + 0.07f), shoulderY - upperArmH * 0.5f, 0f),
                new Vector3(0.09f, upperArmH * 0.5f, 0.09f),
                shirt);
            MakePart(PrimitiveType.Cylinder, "ArmR_Upper", root,
                new Vector3( torsoW * 0.5f + 0.07f,  shoulderY - upperArmH * 0.5f, 0f),
                new Vector3(0.09f, upperArmH * 0.5f, 0.09f),
                shirt);

            // Forearms — slightly angled forward when seated
            float fwdTilt = seated ? 12f : 5f;
            MakePart(PrimitiveType.Cylinder, "ArmL_Lower", root,
                new Vector3(-(torsoW * 0.5f + 0.07f), elbowY - lowerArmH * 0.5f, seated ? 0.03f : 0.02f),
                new Vector3(0.08f, lowerArmH * 0.5f, 0.08f),
                skin,
                Quaternion.Euler(fwdTilt, 0f, 0f));
            MakePart(PrimitiveType.Cylinder, "ArmR_Lower", root,
                new Vector3( torsoW * 0.5f + 0.07f,  elbowY - lowerArmH * 0.5f, seated ? 0.03f : 0.02f),
                new Vector3(0.08f, lowerArmH * 0.5f, 0.08f),
                skin,
                Quaternion.Euler(fwdTilt, 0f, 0f));

            // Hands
            float handY = elbowY - lowerArmH - 0.02f;
            MakePart(PrimitiveType.Sphere, "HandL", root,
                new Vector3(-(torsoW * 0.5f + 0.07f), handY, seated ? 0.05f : 0.03f),
                new Vector3(0.09f, 0.07f, 0.09f), skin);
            MakePart(PrimitiveType.Sphere, "HandR", root,
                new Vector3( torsoW * 0.5f + 0.07f,  handY, seated ? 0.05f : 0.03f),
                new Vector3(0.09f, 0.07f, 0.09f), skin);

            // ── Neck ─────────────────────────────────────────────────────────
            float neckY = torsoY + torsoH * 0.50f;
            float headZ = seated ? -0.02f : 0f;
            MakePart(PrimitiveType.Cylinder, "Neck", root,
                new Vector3(0f, neckY + 0.06f, headZ),
                new Vector3(0.10f, 0.06f, 0.10f),
                skin);

            // ── Head ─────────────────────────────────────────────────────────
            float headY = neckY + 0.20f;
            MakePart(PrimitiveType.Sphere, "Head", root,
                new Vector3(0f, headY, headZ),
                new Vector3(0.22f, 0.24f, 0.22f),
                skin);

            // Eyes — placed at local +Z so that the 180° Y-rotation applied by
            // faceTowardsNegZ=true sends them to world -Z (facing the player).
            MakePart(PrimitiveType.Sphere, "EyeL", root,
                new Vector3(-0.055f, headY + 0.02f, headZ + 0.10f),
                new Vector3(0.038f, 0.038f, 0.025f),
                new Color(0.10f, 0.08f, 0.06f));
            MakePart(PrimitiveType.Sphere, "EyeR", root,
                new Vector3( 0.055f, headY + 0.02f, headZ + 0.10f),
                new Vector3(0.038f, 0.038f, 0.025f),
                new Color(0.10f, 0.08f, 0.06f));

            // Eye whites — sit slightly behind the pupils (less positive Z)
            MakePart(PrimitiveType.Sphere, "EyeWhiteL", root,
                new Vector3(-0.055f, headY + 0.02f, headZ + 0.095f),
                new Vector3(0.048f, 0.048f, 0.025f),
                Color.white);
            MakePart(PrimitiveType.Sphere, "EyeWhiteR", root,
                new Vector3( 0.055f, headY + 0.02f, headZ + 0.095f),
                new Vector3(0.048f, 0.048f, 0.025f),
                Color.white);

            // Nose
            MakePart(PrimitiveType.Sphere, "Nose", root,
                new Vector3(0f, headY - 0.02f, headZ + 0.112f),
                new Vector3(0.025f, 0.020f, 0.025f),
                Darker(skin, 0.08f));

            // Mouth
            MakePart(PrimitiveType.Cube, "Mouth", root,
                new Vector3(0f, headY - 0.065f, headZ + 0.108f),
                new Vector3(0.065f, 0.012f, 0.012f),
                new Color(0.55f, 0.28f, 0.25f));

            // Ears
            MakePart(PrimitiveType.Sphere, "EarL", root,
                new Vector3(-0.112f, headY, headZ),
                new Vector3(0.030f, 0.050f, 0.030f),
                skin);
            MakePart(PrimitiveType.Sphere, "EarR", root,
                new Vector3( 0.112f, headY, headZ),
                new Vector3(0.030f, 0.050f, 0.030f),
                skin);

            // ── Hair ─────────────────────────────────────────────────────────
            // Hair cap: top of head (neutral Z ≈ 0)
            MakePart(PrimitiveType.Sphere, "Hair", root,
                new Vector3(0f, headY + 0.05f, headZ),
                new Vector3(0.235f, 0.175f, 0.235f),
                hair);
            // Hair back — at local -Z so it stays behind the head (on the
            // opposite side from the face) regardless of Y-rotation.
            MakePart(PrimitiveType.Sphere, "HairBack", root,
                new Vector3(0f, headY - 0.04f, headZ - 0.07f),
                new Vector3(0.215f, 0.145f, 0.22f),
                hair);

            // ── Barista apron ─────────────────────────────────────────────────
            if (isBarista)
            {
                // Apron body — at local +Z (same side as face/eyes) so it faces the player
                MakePart(PrimitiveType.Cube, "Apron", root,
                    new Vector3(0f, torsoY - 0.06f, +(torsoD * 0.5f + 0.005f)),
                    new Vector3(torsoW * 0.88f, torsoH * 0.78f, 0.015f),
                    apronColor == default ? new Color(0.22f, 0.52f, 0.35f) : apronColor);
                // Apron strings at waist
                MakePart(PrimitiveType.Cube, "ApronTieL", root,
                    new Vector3(-(torsoW * 0.5f + 0.04f), torsoY - 0.14f, +(torsoD * 0.5f)),
                    new Vector3(0.06f, 0.03f, 0.015f),
                    apronColor == default ? new Color(0.22f, 0.52f, 0.35f) : apronColor);
                MakePart(PrimitiveType.Cube, "ApronTieR", root,
                    new Vector3( torsoW * 0.5f + 0.04f, torsoY - 0.14f, +(torsoD * 0.5f)),
                    new Vector3(0.06f, 0.03f, 0.015f),
                    apronColor == default ? new Color(0.22f, 0.52f, 0.35f) : apronColor);
            }
        }

        // ── Primitive factory ──────────────────────────────────────────────────

        private static GameObject MakePart(PrimitiveType type, string name,
                                            Transform parent, Vector3 localPos,
                                            Vector3 localScale, Color color,
                                            Quaternion? localRot = null)
        {
            GameObject go = RuntimePrimitive.Create(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            // Only override rotation when explicitly requested — avoids degenerate zero-quaternion
            if (localRot.HasValue)
                go.transform.localRotation = localRot.Value;

            go.GetComponent<Renderer>().material = MakeMat(color);

            // Remove colliders from sub-parts — they are on the NPC root or not needed
            Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        // Cached shader — found once, reused for all 300+ character parts
        private static Shader _shader;

        private static Material MakeMat(Color color)
        {
            if (_shader == null)
                _shader = Shader.Find("Universal Render Pipeline/Lit")
                       ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                       ?? Shader.Find("Standard")
                       ?? Shader.Find("Diffuse")
                       ?? Shader.Find("Unlit/Color")
                       ?? Shader.Find("Sprites/Default")
                       ?? Shader.Find("Hidden/Internal-Colored");

            Material mat = new Material(_shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness", 0.25f);
            if (mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness", 0.25f);
            return mat;
        }

        // ── Palette helpers ────────────────────────────────────────────────────

        private static Color NextSkin()
        {
            Color c = SkinTones[_skinSeed % SkinTones.Length];
            _skinSeed = (_skinSeed + 3) % SkinTones.Length;
            return c;
        }

        private static Color NextHair()
        {
            Color c = HairColors[_hairSeed % HairColors.Length];
            _hairSeed = (_hairSeed + 2) % HairColors.Length;
            return c;
        }

        private static Color Darker(Color c, float amount) =>
            new Color(
                Mathf.Clamp01(c.r - amount),
                Mathf.Clamp01(c.g - amount),
                Mathf.Clamp01(c.b - amount),
                c.a);
    }
}
