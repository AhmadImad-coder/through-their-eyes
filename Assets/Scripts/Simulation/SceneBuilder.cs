using System.Collections.Generic;
using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Procedural coffee-shop interior. Creates every visible object at runtime —
    /// no prefabs, no external assets. All dimensions are driven by the room
    /// parameters passed from SimulationBootstrap.
    /// </summary>
    public class SceneBuilder
    {
        private readonly float W;   // room width  (x)
        private readonly float L;   // room length (z)
        private readonly float H;   // room height (y)

        // ── Inspector-driven configuration ─────────────────────────────────────
        // Each NPC has its own world-space Position and RotationY.
        // Set from SimulationBootstrap before Build(); also writable at runtime
        // for the live-rebuild feature (tick "Rebuild NPCs Now" checkbox).
        //
        // RotationY guide:  180 = face toward player (-Z)
        //                     0 = face away from player (+Z)
        //                    90 = face left  (-X)
        //                   -90 = face right (+X)

        // ── Friends ────────────────────────────────────────────────────────────
        // Rotation: X=tilt fwd/back  Y=turn left/right  Z=lean sideways
        public Vector3 emmaPos   = new Vector3(-3.20f, 0.40f, -3.66f); public Vector3 emmaRot   = new Vector3(0f,   0f, 0f);
        public Vector3 jakePos   = new Vector3(-2.04f, 0.40f, -2.50f); public Vector3 jakeRot   = new Vector3(0f, 270f, 0f);
        public Vector3 miaPos    = new Vector3(-4.36f, 0.40f, -2.50f); public Vector3 miaRot    = new Vector3(0f,  90f, 0f);
        public float   friendScale = 1.0f;

        // ── Background customers ───────────────────────────────────────────────
        // Table B center (3.0, 0, -3.0): side chairs ±1.1 on X axis
        public Vector3 bgChat1Pos    = new Vector3( 4.16f, 0.40f, -3.00f); public Vector3 bgChat1Rot    = new Vector3(0f, 270f, 0f);  // right chair → faces table center
        public Vector3 bgChat2Pos    = new Vector3( 1.84f, 0.40f, -3.00f); public Vector3 bgChat2Rot    = new Vector3(0f,  90f, 0f);  // left chair → faces table center
        // Table C center (-1.5, 0, 1.5): front/back chairs ±1.1 on Z axis
        public Vector3 bgLaptop1Pos  = new Vector3(-1.50f, 0.40f,  2.66f); public Vector3 bgLaptop1Rot  = new Vector3(0f, 180f, 0f);  // north chair → faces table center
        public Vector3 bgLaptop2Pos  = new Vector3(-1.50f, 0.40f,  0.34f); public Vector3 bgLaptop2Rot  = new Vector3(0f,   0f, 0f);  // south chair → faces table center
        // Table D center (3.5, 0, 1.5): side chairs ±1.1 on X axis
        public Vector3 bgTalk1Pos    = new Vector3( 2.34f, 0.40f,  1.50f); public Vector3 bgTalk1Rot    = new Vector3(0f,  90f, 0f);  // left chair → faces table center
        public Vector3 bgTalk2Pos    = new Vector3( 4.66f, 0.40f,  1.50f); public Vector3 bgTalk2Rot    = new Vector3(0f, 270f, 0f);  // right chair → faces table center
        // Table E center (-4.5, 0, 3.0): front/back chairs ±1.1 on Z axis
        public Vector3 bgReaderPos   = new Vector3(-4.50f, 0.40f,  4.16f); public Vector3 bgReaderRot   = new Vector3(0f, 180f, 0f);  // north chair → faces table center
        public Vector3 bgReader2Pos  = new Vector3(-4.50f, 0.40f,  1.84f); public Vector3 bgReader2Rot  = new Vector3(0f,   0f, 0f);  // south chair → faces table center
        // Counter stools at Z=6.5 (counterZ-1.1). Y=0.78 ≈ stool seat height. rotY=0 → faces counter.
        public Vector3 stool1Pos     = new Vector3(-2.40f, 0.78f,  5.95f); public Vector3 stool1Rot     = new Vector3(0f,   0f, 0f);
        public Vector3 stool2Pos     = new Vector3( 1.20f, 0.78f,  5.95f); public Vector3 stool2Rot     = new Vector3(0f,   0f, 0f);

        // ── Waiters ────────────────────────────────────────────────────────────
        public float   waiterWalkSpeed  = 1.6f;
        public Vector3 w1CounterPoint   = new Vector3(4.15f, 0f, 6.15f);
        public Vector3 w1TablePoint     = new Vector3(4.95f, 0f, 1.50f);
        public Vector3 w2CounterPoint   = new Vector3(5.35f, 0f, 6.05f);
        public Vector3 w2TablePoint     = new Vector3(4.65f, 0f,-3.00f);

        public SceneBuilder(float width, float length, float height)
        {
            W = width; L = length; H = height;
        }

        // ══════════════════════════════════════════════════════════════════════
        public SceneRefs Build()
        {
            SceneRefs refs = new SceneRefs();

            CreateRoom();
            CreateCounterArea();
            CreateAtmosphereDecor();
            CreateBarStaff();
            CreateTables(refs);
            CreateFriends(refs);
            CreateBackgroundCustomers();
            CreateWaiters();
            CreateEntrance(refs);

            return refs;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Room shell
        // ══════════════════════════════════════════════════════════════════════

        private void CreateRoom()
        {
            // ── Floor (warm hardwood tone) ────────────────────────────────────
            {
                // Floor slab made 0.5 units thick so raycasts reliably hit it.
                // Top surface stays at Y ≈ 0 (center at -0.225, half = 0.25 → top = 0.025).
                GameObject floor = MakeCube("Floor",
                    new Vector3(0f, -0.225f, 0f),
                    new Vector3(W, 0.5f, L),
                    new Color(0.55f, 0.42f, 0.30f), 0.4f);
                if (floorTex != null)
                {
                    Renderer r = floor.GetComponent<Renderer>();
                    r.material.mainTexture       = floorTex;
                    r.material.mainTextureScale  = new Vector2(W / 2f, L / 2f);
                }
            }

            // ── Ceiling ───────────────────────────────────────────────────────
            MakeCube("Ceiling",
                new Vector3(0f, H, 0f),
                new Vector3(W, 0.1f, L),
                new Color(0.96f, 0.94f, 0.90f));

            // ── Walls ─────────────────────────────────────────────────────────
            Color wallColor = new Color(0.93f, 0.89f, 0.82f);
            Color brickColor = new Color(0.58f, 0.38f, 0.28f);

            // Back wall — exposed-brick accent behind counter
            {
                GameObject backWall = MakeCube("WallBack",
                    new Vector3(0f, H * 0.5f, L * 0.5f),
                    new Vector3(W, H, 0.18f),
                    brickColor, 0.25f);
                if (wallTex != null)
                {
                    Renderer r = backWall.GetComponent<Renderer>();
                    r.material.mainTexture      = wallTex;
                    r.material.mainTextureScale = new Vector2(W / 2f, H / 2f);
                }
            }
            // Add lighter mortar grid lines suggestion (thin horizontal strips)
            for (int row = 0; row < 5; row++)
            {
                MakeCube("BrickLine",
                    new Vector3(0f, 0.45f + row * 0.72f, L * 0.5f + 0.1f),
                    new Vector3(W, 0.04f, 0.01f),
                    new Color(0.75f, 0.65f, 0.58f));
            }

            // Front wall (with door opening; two side panels)
            MakeCube("WallFrontL",
                new Vector3(-W * 0.5f + 1.5f, H * 0.5f, -L * 0.5f),
                new Vector3(3f, H, 0.18f), wallColor);
            MakeCube("WallFrontR",
                new Vector3( W * 0.5f - 1.5f, H * 0.5f, -L * 0.5f),
                new Vector3(3f, H, 0.18f), wallColor);
            // Door lintel
            MakeCube("DoorLintel",
                new Vector3(0f, H - 0.6f, -L * 0.5f),
                new Vector3(W - 6f, 1.2f, 0.18f), wallColor);

            // Large glass front — warm amber tint
            MakeCube("FrontGlass",
                new Vector3(0f, 1.3f, -L * 0.5f + 0.05f),
                new Vector3(W - 6f, 2.2f, 0.06f),
                new Color(0.65f, 0.82f, 0.92f, 0.22f));

            // Left wall
            MakeCube("WallLeft",
                new Vector3(-W * 0.5f, H * 0.5f, 0f),
                new Vector3(0.18f, H, L), wallColor);

            // Right wall — large windows
            MakeCube("WallRight",
                new Vector3(W * 0.5f, H * 0.5f, 0f),
                new Vector3(0.18f, H, L), wallColor);
            CreateRightWallWindows();

            // Skirting boards
            CreateSkirting();

            // Ceiling coving
            CreateCoving();
        }

        private void CreateRightWallWindows()
        {
            // Three tall windows on the right wall
            float[] zPositions = { -L * 0.3f, 0f, L * 0.3f };
            foreach (float z in zPositions)
            {
                MakeCube("Window",
                    new Vector3(W * 0.5f - 0.05f, 1.6f, z),
                    new Vector3(0.1f, 1.9f, 1.8f),
                    new Color(0.62f, 0.83f, 0.95f, 0.25f));
                // Window frame
                MakeCube("WinFrame",
                    new Vector3(W * 0.5f - 0.06f, 1.6f, z),
                    new Vector3(0.08f, 2.0f, 0.08f),
                    new Color(0.28f, 0.22f, 0.18f));
                // Windowsill plant
                CreatePlantSmall(new Vector3(W * 0.5f - 0.35f, 0.25f, z + 0.5f));
            }
        }

        private void CreateSkirting()
        {
            Color sk = new Color(0.82f, 0.76f, 0.68f);
            // Along all four walls at floor level
            MakeCube("SkirtBack",  new Vector3(0f, 0.07f, L*0.5f-0.09f),  new Vector3(W, 0.14f, 0.04f), sk);
            MakeCube("SkirtFront", new Vector3(0f, 0.07f, -L*0.5f+0.09f), new Vector3(W, 0.14f, 0.04f), sk);
            MakeCube("SkirtLeft",  new Vector3(-W*0.5f+0.09f, 0.07f, 0f), new Vector3(0.04f, 0.14f, L), sk);
            MakeCube("SkirtRight", new Vector3( W*0.5f-0.09f, 0.07f, 0f), new Vector3(0.04f, 0.14f, L), sk);
        }

        private void CreateCoving()
        {
            Color cov = new Color(0.88f, 0.86f, 0.82f);
            MakeCube("CovBack",  new Vector3(0f, H-0.08f, L*0.5f-0.09f),  new Vector3(W, 0.16f, 0.05f), cov);
            MakeCube("CovFront", new Vector3(0f, H-0.08f, -L*0.5f+0.09f), new Vector3(W, 0.16f, 0.05f), cov);
            MakeCube("CovLeft",  new Vector3(-W*0.5f+0.09f, H-0.08f, 0f), new Vector3(0.05f, 0.16f, L), cov);
            MakeCube("CovRight", new Vector3( W*0.5f-0.09f, H-0.08f, 0f), new Vector3(0.05f, 0.16f, L), cov);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Counter / bar area
        // ══════════════════════════════════════════════════════════════════════

        private void CreateCounterArea()
        {
            float counterZ = L * 0.5f - 1.4f;

            // ── Main counter bar ──────────────────────────────────────────────
            MakeCube("Counter",
                new Vector3(0f, 0.6f, counterZ),
                new Vector3(6.5f, 1.2f, 1.0f),
                new Color(0.32f, 0.24f, 0.18f), 0.5f);
            // Counter top (slightly lighter)
            MakeCube("CounterTop",
                new Vector3(0f, 1.22f, counterZ),
                new Vector3(6.6f, 0.05f, 1.1f),
                new Color(0.18f, 0.14f, 0.11f), 0.8f);

            // ── Counter stools for customers ──────────────────────────────────
            float[] stoolXs = { -2.4f, -1.2f, 0f, 1.2f, 2.4f };
            foreach (float sx in stoolXs)
                CreateStool(new Vector3(sx, 0f, counterZ - 1.1f));

            // ── Espresso machine (hero prop) ──────────────────────────────────
            CreateEspressoMachine(new Vector3(-1.8f, 1.22f, counterZ + 0.2f));

            // ── Second coffee station ─────────────────────────────────────────
            CreateDripStation(new Vector3(1.5f, 1.22f, counterZ + 0.2f));

            // ── Display shelf behind counter ──────────────────────────────────
            CreateDisplayShelf(counterZ);

            // ── Side prep / service station ──────────────────────────────────
            CreateServiceStation(new Vector3(6.15f, 0f, 5.95f));

            GameObject orderCounter = new GameObject("OrderCounterPoint");
            orderCounter.transform.position = new Vector3(4.8f, 0f, counterZ - 0.55f);
            orderCounter.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // ── Chalkboard menu ───────────────────────────────────────────────
            MakeCube("MenuBoard",
                new Vector3(0f, 2.7f, L * 0.5f - 0.1f),
                new Vector3(4.5f, 1.4f, 0.06f),
                new Color(0.12f, 0.14f, 0.15f));
            CreateWorldText("MenuHeader", "MENU",
                new Vector3(0f, 3.08f, L*0.5f - 0.18f), 0.036f, new Color(0.96f, 0.92f, 0.74f));
            CreateWorldText("MenuItem1", "Espresso",
                new Vector3(-0.88f, 2.90f, L*0.5f - 0.18f), 0.022f, new Color(0.95f, 0.92f, 0.75f));
            CreateWorldText("MenuPrice1", "3.20",
                new Vector3(0.94f, 2.90f, L*0.5f - 0.18f), 0.022f, new Color(0.95f, 0.92f, 0.75f));
            CreateWorldText("MenuItem2", "Latte",
                new Vector3(-0.88f, 2.70f, L*0.5f - 0.18f), 0.022f, new Color(0.95f, 0.92f, 0.75f));
            CreateWorldText("MenuPrice2", "4.50",
                new Vector3(0.94f, 2.70f, L*0.5f - 0.18f), 0.022f, new Color(0.95f, 0.92f, 0.75f));
            CreateWorldText("MenuItem3", "Cappuccino",
                new Vector3(-0.88f, 2.50f, L*0.5f - 0.18f), 0.021f, new Color(0.95f, 0.92f, 0.75f));
            CreateWorldText("MenuPrice3", "4.80",
                new Vector3(0.94f, 2.50f, L*0.5f - 0.18f), 0.022f, new Color(0.95f, 0.92f, 0.75f));
            CreateWorldText("MenuItem4", "Flat White",
                new Vector3(-0.88f, 2.30f, L*0.5f - 0.18f), 0.021f, new Color(0.95f, 0.92f, 0.75f));
            CreateWorldText("MenuPrice4", "4.20",
                new Vector3(0.94f, 2.30f, L*0.5f - 0.18f), 0.022f, new Color(0.95f, 0.92f, 0.75f));
        }

        private void CreateStool(Vector3 pos)
        {
            GameObject root = new GameObject("Stool");
            root.transform.position = pos;
            // Seat
            MakeChild(PrimitiveType.Cylinder, "Seat", root.transform,
                new Vector3(0f, 0.75f, 0f), new Vector3(0.28f, 0.04f, 0.28f),
                new Color(0.18f, 0.14f, 0.12f));
            // Post
            MakeChild(PrimitiveType.Cylinder, "Post", root.transform,
                new Vector3(0f, 0.38f, 0f), new Vector3(0.07f, 0.38f, 0.07f),
                new Color(0.55f, 0.55f, 0.58f));
            // Foot ring
            MakeChild(PrimitiveType.Cylinder, "Ring", root.transform,
                new Vector3(0f, 0.22f, 0f), new Vector3(0.22f, 0.015f, 0.22f),
                new Color(0.55f, 0.55f, 0.58f));
        }

        private void CreateEspressoMachine(Vector3 basePos)
        {
            GameObject root = new GameObject("EspressoMachine");
            root.transform.position = basePos;
            // Body
            MakeChild(PrimitiveType.Cube, "Body", root.transform,
                new Vector3(0f, 0.22f, 0f), new Vector3(0.8f, 0.44f, 0.45f),
                new Color(0.22f, 0.22f, 0.25f), 0.8f);
            // Group head (2 portafilter arms)
            foreach (float gx in new[] { -0.18f, 0.18f })
            {
                MakeChild(PrimitiveType.Cylinder, "GroupHead", root.transform,
                    new Vector3(gx, 0.04f, 0.2f), new Vector3(0.10f, 0.04f, 0.10f),
                    new Color(0.35f, 0.35f, 0.38f), 0.6f);
            }
            // Drip tray
            MakeChild(PrimitiveType.Cube, "DripTray", root.transform,
                new Vector3(0f, -0.01f, 0.18f), new Vector3(0.7f, 0.02f, 0.30f),
                new Color(0.45f, 0.45f, 0.48f), 0.6f);
            // Steam wand
            MakeChild(PrimitiveType.Cylinder, "SteamWand", root.transform,
                new Vector3(0.38f, 0.1f, 0.15f), new Vector3(0.03f, 0.18f, 0.03f),
                new Color(0.70f, 0.70f, 0.72f), 0.7f);
            // Pressure gauge (small disc)
            MakeChild(PrimitiveType.Cylinder, "Gauge", root.transform,
                new Vector3(0.22f, 0.32f, 0.23f), new Vector3(0.10f, 0.02f, 0.10f),
                new Color(0.85f, 0.80f, 0.30f));
            // Two espresso cups on drip tray
            foreach (float cx in new[] { -0.12f, 0.12f })
                CreateCup(root.transform, new Vector3(cx, 0.06f, 0.2f), new Color(0.94f, 0.91f, 0.86f));
        }

        private void CreateDripStation(Vector3 basePos)
        {
            GameObject root = new GameObject("DripStation");
            root.transform.position = basePos;
            MakeChild(PrimitiveType.Cylinder, "Carafe", root.transform,
                new Vector3(0f, 0.18f, 0f), new Vector3(0.18f, 0.18f, 0.18f),
                new Color(0.15f, 0.15f, 0.18f), 0.9f);
            MakeChild(PrimitiveType.Cube, "Base", root.transform,
                new Vector3(0f, 0.03f, 0f), new Vector3(0.28f, 0.06f, 0.28f),
                new Color(0.25f, 0.22f, 0.20f));
            // Cups beside it
            for (int i = 0; i < 3; i++)
                CreateCup(root.transform, new Vector3(0.25f + i * 0.18f, 0.04f, 0f),
                    new Color(0.94f, 0.91f, 0.86f));
        }

        private void CreateDisplayShelf(float counterZ)
        {
            // Shelf on back wall above counter
            MakeCube("DisplayShelf",
                new Vector3(2.5f, 2.2f, L*0.5f - 0.12f),
                new Vector3(2.2f, 0.06f, 0.35f),
                new Color(0.32f, 0.24f, 0.18f), 0.5f);
            // Decorative jars / canisters on shelf
            Color[] jarColors = {
                new Color(0.55f, 0.38f, 0.25f),
                new Color(0.72f, 0.62f, 0.48f),
                new Color(0.35f, 0.28f, 0.22f)
            };
            for (int i = 0; i < 3; i++)
            {
                MakeCube("Jar" + i,
                    new Vector3(1.65f + i * 0.45f, 2.42f, L*0.5f - 0.14f),
                    new Vector3(0.18f, 0.38f, 0.18f),
                    jarColors[i]);
            }
        }

        private void CreateServiceStation(Vector3 pos)
        {
            GameObject root = new GameObject("ServiceStation");
            root.transform.position = pos;

            MakeChild(PrimitiveType.Cube, "Base", root.transform,
                new Vector3(0f, 0.42f, 0f), new Vector3(1.25f, 0.84f, 0.72f),
                new Color(0.30f, 0.24f, 0.18f), 0.45f);
            MakeChild(PrimitiveType.Cube, "Top", root.transform,
                new Vector3(0f, 0.87f, 0f), new Vector3(1.32f, 0.06f, 0.82f),
                new Color(0.18f, 0.14f, 0.11f), 0.8f);
            MakeChild(PrimitiveType.Cube, "Shelf", root.transform,
                new Vector3(0f, 0.48f, 0f), new Vector3(1.1f, 0.05f, 0.62f),
                new Color(0.24f, 0.19f, 0.15f), 0.45f);

            CreateEspressoMachine(pos + new Vector3(-0.18f, 0.87f, 0.02f));
            CreateCup(root.transform, new Vector3(0.34f, 0.91f, -0.10f),
                new Color(0.94f, 0.91f, 0.86f));
            CreateCup(root.transform, new Vector3(0.50f, 0.91f, 0.10f),
                new Color(0.94f, 0.91f, 0.86f));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Atmosphere & Décor
        // ══════════════════════════════════════════════════════════════════════

        private void CreateAtmosphereDecor()
        {
            // ── Wall art (left wall) ──────────────────────────────────────────
            CreateWallArt(new Vector3(-W*0.5f+0.10f, 2.0f, -1.5f),
                          new Vector2(1.4f, 1.0f), new Color(0.55f, 0.38f, 0.25f), "Art1");
            CreateWallArt(new Vector3(-W*0.5f+0.10f, 2.0f, 1.5f),
                          new Vector2(1.0f, 0.8f), new Color(0.30f, 0.50f, 0.42f), "Art2");
            CreateWallArt(new Vector3(-W*0.5f+0.10f, 1.7f, 3.5f),
                          new Vector2(0.7f, 0.7f), new Color(0.65f, 0.45f, 0.25f), "Art3");

            // ── Pendant lamps over each table (actual point lights) ───────────
            float[][] lampData = {   // { x, z, warmth }
                new float[]{ -3.2f, -2.0f, 0.9f },
                new float[]{  2.4f, -2.8f, 0.85f },
                new float[]{ -1.5f,  2.0f, 0.95f },
                new float[]{  3.4f,  2.0f, 0.88f },
                new float[]{ -4.5f,  3.0f, 0.92f },
            };
            foreach (float[] d in lampData)
                CreatePendantLamp(new Vector3(d[0], H, d[1]), d[2]);

            // ── Corner plants ─────────────────────────────────────────────────
            CreatePlantLarge(new Vector3(-W*0.5f+0.5f, 0f, -L*0.5f+1.5f));
            CreatePlantLarge(new Vector3( W*0.5f-0.6f, 0f,  L*0.5f-1.8f));
            CreatePlantMedium(new Vector3(-W*0.5f+0.5f, 0f, L*0.5f-2.5f));

            // ── Welcome sign near entrance ────────────────────────────────────
            MakeCube("WelcomeSign",
                new Vector3(0f, 2.3f, -L*0.5f + 0.12f),
                new Vector3(2.5f, 0.55f, 0.06f),
                new Color(0.32f, 0.22f, 0.15f));
            CreateWorldText("WelcomeText", "BREWED GROUNDS COFFEE",
                new Vector3(0f, 2.3f, -L*0.5f + 0.18f), 0.048f, new Color(0.96f, 0.88f, 0.55f));

            // ── Coat hooks near entrance ──────────────────────────────────────
            for (int i = 0; i < 4; i++)
            {
                MakeCube("Hook"+i,
                    new Vector3(-W*0.5f+0.12f, 1.8f, -L*0.5f + 1.8f + i*0.55f),
                    new Vector3(0.14f, 0.06f, 0.14f),
                    new Color(0.40f, 0.30f, 0.22f), 0.6f);
            }

            // ── Ambient fill light ────────────────────────────────────────────
            RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.56f, 0.48f);
        }

        private void CreateWallArt(Vector3 pos, Vector2 size, Color col, string name)
        {
            // Frame (slightly larger, darker)
            MakeCube(name + "Frame", pos,
                new Vector3(0.06f, size.y + 0.12f, size.x + 0.12f),
                new Color(0.22f, 0.17f, 0.12f));
            // Canvas
            MakeCube(name + "Canvas", pos + new Vector3(0.04f, 0f, 0f),
                new Vector3(0.04f, size.y, size.x), col);
        }

        private void CreatePendantLamp(Vector3 ceilingPos, float warmth)
        {
            // Cable
            MakeCube("PendantCable",
                ceilingPos - new Vector3(0f, 0.45f, 0f),
                new Vector3(0.02f, 0.9f, 0.02f),
                new Color(0.15f, 0.12f, 0.10f));
            // Shade
            GameObject shade = MakeCube("PendantShade",
                ceilingPos - new Vector3(0f, 0.9f, 0f),
                new Vector3(0.32f, 0.22f, 0.32f),
                new Color(0.20f, 0.17f, 0.14f));
            // Inner glow
            MakeCube("PendantBulb",
                ceilingPos - new Vector3(0f, 0.88f, 0f),
                new Vector3(0.14f, 0.14f, 0.14f),
                new Color(0.98f, 0.92f, 0.65f));
            // Actual Unity point light
            GameObject lightObj = new GameObject("TableLight");
            lightObj.transform.position = ceilingPos - new Vector3(0f, 1.1f, 0f);
            Light lt = lightObj.AddComponent<Light>();
            lt.type      = LightType.Point;
            lt.range     = 4.5f;
            lt.intensity = 1.2f;
            lt.color     = new Color(0.98f, 0.88f + warmth * 0.05f, 0.60f + warmth * 0.1f);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Bar staff
        // ══════════════════════════════════════════════════════════════════════

        private void CreateBarStaff()
        {
            float counterZ = L * 0.5f - 0.9f;

            // Barista 1 — main espresso bar (making coffee)
            GameObject b1 = CharacterBuilder.CreateBarista("Barista1",
                new Vector3(-1.8f, 0.12f, counterZ + 0.36f),
                new Color(0.22f, 0.52f, 0.35f),
                faceTowardsNegZ: true);
            b1.AddComponent<NPCIdle>();

            // Barista 2 — serving at counter
            GameObject b2 = CharacterBuilder.CreateBarista("Barista2",
                new Vector3(0.5f, 0.12f, counterZ + 0.36f),
                new Color(0.25f, 0.48f, 0.32f),
                faceTowardsNegZ: true);
            b2.AddComponent<NPCIdle>();

            // Customer being served — standing at counter on the customer side
            GameObject cc = CharacterBuilder.CreateStandingHumanoid("CounterCustomer",
                new Vector3(1.75f, 0.02f, counterZ - 1.45f),
                new Color(0.55f, 0.45f, 0.62f),
                faceTowardsNegZ: false, scale: 1.02f);
            cc.AddComponent<NPCIdle>();

            // Barista 3 — restocking shelf in the back
            GameObject b3 = CharacterBuilder.CreateBarista("Barista3",
                new Vector3(2.5f, 0.12f, counterZ + 0.46f),
                new Color(0.20f, 0.48f, 0.30f),
                faceTowardsNegZ: true);
            b3.AddComponent<NPCIdle>();
        }

        // ══════════════════════════════════════════════════════════════════════
        // Tables
        // ══════════════════════════════════════════════════════════════════════

        private void CreateTables(SceneRefs refs)
        {
            // friend table = index 0, rest are background tables
            var tables = new (Vector3 pos, bool isFriend)[]
            {
                (new Vector3(-3.2f, 0f, -2.5f),  true ),  // friend table — near-left
                (new Vector3( 3.0f, 0f, -3.0f),  false),  // right near
                (new Vector3(-1.5f, 0f,  1.5f),  false),  // centre
                (new Vector3( 3.5f, 0f,  1.5f),  false),  // right mid
                (new Vector3(-4.5f, 0f,  3.0f),  false),  // far left corner
            };

            foreach (var t in tables)
                CreateTable(t.pos, t.isFriend, refs);
        }

        private void CreateTable(Vector3 position, bool isFriendTable, SceneRefs refs)
        {
            GameObject tableRoot = new GameObject(isFriendTable ? "FriendTable" : "Table");
            tableRoot.transform.position = position;

            // ── Table top ─────────────────────────────────────────────────────
            GameObject top = MakeChild(PrimitiveType.Cube, "Top", tableRoot.transform,
                new Vector3(0f, 0.75f, 0f), new Vector3(1.7f, 0.08f, 1.25f),
                new Color(0.50f, 0.38f, 0.28f), 0.5f);
            if (tableTex != null)
            {
                Renderer r = top.GetComponent<Renderer>();
                r.material.mainTexture      = tableTex;
                r.material.mainTextureScale = new Vector2(1.7f, 1.25f);
            }

            // Subtle table edge highlight
            MakeChild(PrimitiveType.Cube, "Edge", tableRoot.transform,
                new Vector3(0f, 0.72f, 0f), new Vector3(1.74f, 0.06f, 1.29f),
                new Color(0.38f, 0.28f, 0.20f));

            // ── Legs ──────────────────────────────────────────────────────────
            float lh = 0.72f;
            foreach (Vector3 lp in new[] {
                new Vector3( 0.72f, lh/2, 0.53f), new Vector3(-0.72f, lh/2, 0.53f),
                new Vector3( 0.72f, lh/2,-0.53f), new Vector3(-0.72f, lh/2,-0.53f) })
            {
                MakeChild(PrimitiveType.Cylinder, "Leg", tableRoot.transform,
                    lp, new Vector3(0.07f, lh/2, 0.07f),
                    new Color(0.33f, 0.25f, 0.18f));
            }

            // ── Four chairs around the table ──────────────────────────────────
            CreateChair(tableRoot.transform, new Vector3( 0f, 0f, -1.1f), Quaternion.identity);
            CreateChair(tableRoot.transform, new Vector3( 0f, 0f,  1.1f), Quaternion.Euler(0f,180f,0f));
            CreateChair(tableRoot.transform, new Vector3( 1.1f, 0f, 0f),  Quaternion.Euler(0f,-90f,0f));
            CreateChair(tableRoot.transform, new Vector3(-1.1f, 0f, 0f),  Quaternion.Euler(0f, 90f,0f));

            // ── Coffee cups on every table ────────────────────────────────────
            CreateCup(tableRoot.transform, new Vector3(-0.35f, 0.82f, -0.2f), new Color(0.94f, 0.91f, 0.86f));
            CreateCup(tableRoot.transform, new Vector3( 0.35f, 0.82f,  0.2f), new Color(0.94f, 0.91f, 0.86f));

            // ── Friend-table exclusive refs ───────────────────────────────────
            if (isFriendTable)
            {
                GameObject center = new GameObject("FriendTableCenter");
                center.transform.SetParent(tableRoot.transform, false);
                center.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                refs.friendTableCenter = center.transform;

                CreateFriendTableTrigger(tableRoot.transform, refs);
                CreateStageDirt(tableRoot.transform, refs);
                CreateScratches(tableRoot.transform, refs);
                CreateGum(tableRoot.transform, refs);
                CreateSeatPoint(tableRoot.transform, refs);
            }
        }

        private void CreateChair(Transform parent, Vector3 localPos, Quaternion localRot)
        {
            GameObject chairRoot = new GameObject("Chair");
            chairRoot.transform.SetParent(parent, false);
            chairRoot.transform.localPosition = localPos;
            chairRoot.transform.localRotation = localRot;

            Color seatCol = new Color(0.20f, 0.20f, 0.23f);
            Color legCol  = new Color(0.30f, 0.22f, 0.16f);

            MakeChild(PrimitiveType.Cube, "Seat", chairRoot.transform,
                new Vector3(0f, 0.46f, 0f), new Vector3(0.72f, 0.08f, 0.72f), seatCol);
            MakeChild(PrimitiveType.Cube, "Back", chairRoot.transform,
                new Vector3(0f, 0.92f, -0.32f), new Vector3(0.72f, 0.80f, 0.08f), seatCol);
            // Four chair legs
            foreach (Vector3 lp in new[] {
                new Vector3( 0.3f, 0.22f, 0.3f), new Vector3(-0.3f, 0.22f, 0.3f),
                new Vector3( 0.3f, 0.22f,-0.3f), new Vector3(-0.3f, 0.22f,-0.3f) })
                MakeChild(PrimitiveType.Cube, "CL", chairRoot.transform,
                    lp, new Vector3(0.06f, 0.44f, 0.06f), legCol);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Friends (Alex's group — always SEATED)
        // ══════════════════════════════════════════════════════════════════════

        public void CreateFriends(SceneRefs refs)
        {
            // Each friend uses a direct world position + Y rotation set from the Inspector.
            var friendData = new (string name, Vector3 pos, Vector3 rot, Color shirt)[]
            {
                ("Emma", emmaPos, emmaRot, new Color(0.72f, 0.52f, 0.62f)),
                ("Jake", jakePos, jakeRot, new Color(0.52f, 0.62f, 0.78f)),
                ("Mia",  miaPos,  miaRot,  new Color(0.60f, 0.74f, 0.55f)),
            };

            foreach (var fd in friendData)
            {
                GameObject npc = CharacterBuilder.CreateSeatedHumanoid(
                    fd.name, fd.pos, fd.shirt,
                    fd.rot.y, friendScale, fd.rot.x, fd.rot.z);
                npc.AddComponent<NPCIdle>();

                // Small floating name label — starts HIDDEN, revealed when player
                // actually approaches after entering (avoids labels bleeding
                // through café windows from the street scene).
                GameObject label = new GameObject(fd.name + "Label");
                label.transform.SetParent(npc.transform, false);
                label.transform.localPosition = new Vector3(0f, 0.86f, 0f);
                TextMesh tm = label.AddComponent<TextMesh>();
                tm.text          = fd.name;
                tm.characterSize = 0.020f;
                ConfigureWorldText(tm, new Color(0.35f, 0.70f, 1.00f), 34);
                label.AddComponent<Billboard>();
                label.SetActive(false);  // hidden until player is inside café

                // Hide the ENTIRE friend NPC until the player is inside the café
                // so they can't be seen through the window glass from the street.
                npc.SetActive(false);

                refs.friends.Add(npc);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // Background customers at all other tables
        // ══════════════════════════════════════════════════════════════════════

        public void CreateBackgroundCustomers()
        {
            // Every background customer uses their own direct world position + rotationY.
            // Change these on SimulationBootstrap in the Inspector, tick "Rebuild NPCs Now".

            SpawnSeatedCustomer("C_Chat1",   bgChat1Pos,   new Color(0.60f, 0.50f, 0.40f), bgChat1Rot);
            SpawnSeatedCustomer("C_Chat2",   bgChat2Pos,   new Color(0.50f, 0.40f, 0.58f), bgChat2Rot);

            SpawnSeatedCustomerWithLaptop("C_Laptop1", bgLaptop1Pos, new Color(0.45f, 0.55f, 0.68f), bgLaptop1Rot);
            SpawnSeatedCustomerWithLaptop("C_Laptop2", bgLaptop2Pos, new Color(0.65f, 0.48f, 0.45f), bgLaptop2Rot);

            SpawnSeatedCustomer("C_Talk1",   bgTalk1Pos,   new Color(0.58f, 0.65f, 0.50f), bgTalk1Rot);
            SpawnSeatedCustomer("C_Talk2",   bgTalk2Pos,   new Color(0.48f, 0.42f, 0.60f), bgTalk2Rot);

            SpawnSeatedCustomerWithBook("C_Reader",  bgReaderPos,  new Color(0.55f, 0.48f, 0.38f), bgReaderRot);
            SpawnSeatedCustomer("C_Reader2", bgReader2Pos, new Color(0.42f, 0.55f, 0.50f), bgReader2Rot);

            SpawnSeatedCustomer("C_Stool1",  stool1Pos,    new Color(0.50f, 0.60f, 0.65f), stool1Rot);
            SpawnSeatedCustomer("C_Stool2",  stool2Pos,    new Color(0.62f, 0.52f, 0.45f), stool2Rot);
        }

        private void SpawnSeatedCustomer(string name, Vector3 pos, Color shirtColor, Vector3 rot)
        {
            GameObject npc = CharacterBuilder.CreateSeatedHumanoid(
                name, pos, shirtColor, rot.y, 1.0f, rot.x, rot.z);
            npc.AddComponent<NPCIdle>();
        }

        private void SpawnSeatedCustomerWithLaptop(string name, Vector3 pos, Color color, Vector3 rot)
        {
            SpawnSeatedCustomer(name, pos, color, rot);
            // Place laptop in the direction the NPC faces (toward the table center).
            Vector3 fwd = Quaternion.Euler(0f, rot.y, 0f) * Vector3.forward;
            MakeCube("Laptop_" + name,
                new Vector3(pos.x + fwd.x * 0.32f, 0.80f, pos.z + fwd.z * 0.32f),
                new Vector3(0.30f, 0.18f, 0.22f),
                new Color(0.20f, 0.20f, 0.22f), 0.6f);
            MakeCube("Screen_" + name,
                new Vector3(pos.x + fwd.x * 0.42f, 1.02f, pos.z + fwd.z * 0.42f),
                new Vector3(0.28f, 0.18f, 0.02f),
                new Color(0.35f, 0.55f, 0.80f));
        }

        private void SpawnSeatedCustomerWithBook(string name, Vector3 pos, Color color, Vector3 rot)
        {
            SpawnSeatedCustomer(name, pos, color, rot);
            // Place book in the direction the NPC faces (toward the table center).
            Vector3 fwd = Quaternion.Euler(0f, rot.y, 0f) * Vector3.forward;
            MakeCube("Book_" + name,
                new Vector3(pos.x + fwd.x * 0.28f, 0.80f, pos.z + fwd.z * 0.28f),
                new Vector3(0.26f, 0.02f, 0.20f),
                new Color(0.92f, 0.88f, 0.80f));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Waiter NPCs — walking serving routes
        // ══════════════════════════════════════════════════════════════════════

        public void CreateWaiters()
        {
            // Counter pickup point: in front of the espresso counter (customer side).
            // Counter centre X ≈ W/2 - 2.5 = 5.0, Z = L/2 - 0.9 = 8.1.
            // Waiters start at the counter-front (Z ≈ 7.0) and walk to table service points
            // along the right side of the café (X ≈ 4.5–5.0), staying clear of all tables.

            // Waiter 1: counter ↔ Table 4
            CreateWaiter("Waiter1",
                new Color(0.22f, 0.52f, 0.35f),          // green apron
                counterPoint:   w1CounterPoint,
                tablePoint:     w1TablePoint,
                tablePoints:    new[]
                {
                    new Vector3(4.95f, 0f, 1.50f),
                    new Vector3(4.65f, 0f,-3.00f),
                    new Vector3(-1.35f, 0f, 2.95f)
                },
                walkSpeed:      waiterWalkSpeed,
                pauseAtTable:   2.8f,
                pauseAtCounter: 0.05f,
                startAtCounter: true);

            // Waiter 2: counter ↔ Table 2
            CreateWaiter("Waiter2",
                new Color(0.25f, 0.48f, 0.32f),
                counterPoint:   w2CounterPoint,
                tablePoint:     w2TablePoint,
                tablePoints:    new[]
                {
                    new Vector3(4.65f, 0f,-3.00f),
                    new Vector3(4.95f, 0f, 1.50f),
                    new Vector3(-4.10f, 0f, 3.00f)
                },
                walkSpeed:      waiterWalkSpeed,
                pauseAtTable:   2.2f,
                pauseAtCounter: 0.05f,
                startAtCounter: false);   // start mid-route for visual variety
        }

        private void CreateWaiter(string wName, Color apronColor,
                                   Vector3 counterPoint, Vector3 tablePoint,
                                   Vector3[] tablePoints,
                                   float walkSpeed,
                                   float pauseAtTable, float pauseAtCounter,
                                   bool startAtCounter)
        {
            GameObject npc = CharacterBuilder.CreateBarista(wName, counterPoint, apronColor,
                                                             faceTowardsNegZ: true,
                                                             scale: 1.12f);

            // Small coffee-cup prop held in right hand (approximate hand position)
            // Hand is at local offset (torsoW/2 + 0.07, handY, 0.03) ≈ (0.23, 0.13, 0.03)
            // after 180° rotation this becomes world (-0.23, handY+rootY, -0.03).
            // We just place a cup as a child near the hand.
            GameObject cupHolder = new GameObject("CupHolder");
            cupHolder.transform.SetParent(npc.transform, false);
            cupHolder.transform.localPosition = new Vector3(0.23f, 0.13f, 0.03f);

            MakeChildLocal(PrimitiveType.Cylinder, "Cup", cupHolder.transform,
                Vector3.zero, new Vector3(0.07f, 0.04f, 0.07f),
                new Color(0.92f, 0.88f, 0.82f));   // ceramic-white cup body
            MakeChildLocal(PrimitiveType.Cylinder, "CupLiquid", cupHolder.transform,
                new Vector3(0f, 0.04f, 0f), new Vector3(0.055f, 0.01f, 0.055f),
                new Color(0.25f, 0.15f, 0.08f));   // dark coffee surface

            WaiterNPC waiter = npc.AddComponent<WaiterNPC>();
            waiter.counterPoint   = counterPoint;
            waiter.tablePoint     = tablePoint;
            waiter.tablePoints    = tablePoints;
            waiter.walkSpeed      = walkSpeed;
            waiter.pauseAtTable   = pauseAtTable;
            waiter.pauseAtCounter = pauseAtCounter;
            waiter.startAtCounter = startAtCounter;
        }

        /// <summary>Helper: create a primitive as a child of a local transform (no parent offset).</summary>
        private static void MakeChildLocal(PrimitiveType type, string name, Transform parent,
                                            Vector3 localPos, Vector3 localScale, Color color)
        {
            GameObject go = RuntimePrimitive.Create(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            Object.Destroy(go.GetComponent<Collider>());
            Renderer rend = go.GetComponent<Renderer>();
            Shader shader = FindRuntimeShader();
            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            rend.material = mat;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Friend-table gameplay refs (unchanged logic)
        // ══════════════════════════════════════════════════════════════════════

        private void CreateFriendTableTrigger(Transform parent, SceneRefs refs)
        {
            GameObject trigger = new GameObject("FriendTrigger");
            trigger.transform.SetParent(parent, false);
            trigger.transform.localPosition = new Vector3(0f, 0.6f, -0.2f);
            BoxCollider col = trigger.AddComponent<BoxCollider>();
            col.size    = new Vector3(3.2f, 1.6f, 3.2f);
            col.isTrigger = true;
            trigger.AddComponent<FriendTableTrigger>();
        }

        private void CreateSeatPoint(Transform parent, SceneRefs refs)
        {
            GameObject seat = new GameObject("PlayerSeatPoint");
            seat.transform.SetParent(parent, false);
            seat.transform.localPosition = new Vector3(0f, 0.02f, 1.18f);
            seat.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            refs.playerSeatPoint = seat.transform;

            GameObject under = new GameObject("LookUnderPoint");
            under.transform.SetParent(parent, false);
            under.transform.localPosition = new Vector3(0.02f, 0.98f, 0.18f);
            under.transform.localRotation = Quaternion.Euler(50f, 0f, 0f);
            refs.lookUnderTablePoint = under.transform;

            GameObject order = new GameObject("OrderTablePoint");
            order.transform.SetParent(parent, false);
            order.transform.localPosition = new Vector3(1.42f, 0f, 0.15f);
            order.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);
            refs.orderTablePoint = order.transform;
        }

        private void CreateStageDirt(Transform parent, SceneRefs refs)
        {
            refs.stage0Dirt.Add(CreateDirt(parent, new Vector3( 0.25f, 0.81f, 0.15f), new Color(0.20f, 0.10f, 0.04f), 0.24f));
            refs.stage0Dirt.Add(CreateDirt(parent, new Vector3(-0.25f, 0.81f,-0.12f), new Color(0.16f, 0.08f, 0.035f), 0.22f));
            refs.stage1Dirt.Add(CreateDirt(parent, new Vector3(-0.35f, 0.81f, 0.30f), new Color(0.19f, 0.09f, 0.035f), 0.24f));
            refs.stage1Dirt.Add(CreateDirt(parent, new Vector3( 0.40f, 0.81f,-0.28f), new Color(0.27f, 0.13f, 0.05f), 0.26f));
            SetActiveList(refs.stage0Dirt, false);
            SetActiveList(refs.stage1Dirt, false);
        }

        private DirtSpot CreateDirt(Transform parent, Vector3 localPos, Color color, float scale)
        {
            GameObject root = new GameObject("CoffeeStain");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos + new Vector3(0f, 0.035f, 0f);

            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(scale * 2.2f, 0.16f, scale * 1.7f);
            box.center = Vector3.zero;

            Color dark = color;
            Color edge = Color.Lerp(color, new Color(0.42f, 0.22f, 0.10f), 0.28f);
            Color crumb = new Color(0.62f, 0.42f, 0.22f);

            GameObject main = MakeChild(PrimitiveType.Cylinder, "CoffeeRing", root.transform,
                Vector3.zero, new Vector3(scale * 0.78f, 0.006f, scale * 0.52f), edge, 0.45f);
            Object.Destroy(main.GetComponent<Collider>());

            GameObject center = MakeChild(PrimitiveType.Cylinder, "WetCoffee", root.transform,
                new Vector3(0.025f, 0.006f, -0.015f),
                new Vector3(scale * 0.48f, 0.007f, scale * 0.34f), dark, 0.65f);
            Object.Destroy(center.GetComponent<Collider>());

            GameObject smear = MakeChild(PrimitiveType.Cube, "Smear", root.transform,
                new Vector3(scale * 0.55f, 0.010f, -scale * 0.05f),
                new Vector3(scale * 0.55f, 0.006f, scale * 0.10f), dark, 0.5f);
            smear.transform.localRotation = Quaternion.Euler(0f, 28f, 0f);
            Object.Destroy(smear.GetComponent<Collider>());

            Vector3[] crumbOffsets =
            {
                new Vector3(-0.38f, 0.016f,  0.28f),
                new Vector3( 0.12f, 0.016f,  0.38f),
                new Vector3( 0.44f, 0.016f,  0.18f),
                new Vector3(-0.16f, 0.016f, -0.34f)
            };
            for (int i = 0; i < crumbOffsets.Length; i++)
            {
                GameObject c = MakeChild(PrimitiveType.Cube, "Crumb", root.transform,
                    new Vector3(crumbOffsets[i].x * scale, crumbOffsets[i].y, crumbOffsets[i].z * scale),
                    new Vector3(scale * 0.14f, scale * 0.055f, scale * 0.11f),
                    Color.Lerp(crumb, dark, i * 0.12f), 0.25f);
                c.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
                Object.Destroy(c.GetComponent<Collider>());
            }

            return root.AddComponent<DirtSpot>();
        }

        private void CreateScratches(Transform parent, SceneRefs refs)
        {
            Vector3[] spots = {
                new Vector3(-0.40f, 0.82f,-0.10f),
                new Vector3( 0.30f, 0.82f, 0.20f),
                new Vector3( 0.10f, 0.82f,-0.25f),
                new Vector3(-0.20f, 0.82f, 0.30f),
                new Vector3( 0.45f, 0.82f,-0.35f),
            };
            foreach (Vector3 pos in spots)
            {
                GameObject scratch = MakeChild(PrimitiveType.Cube, "Scratch", parent,
                    pos, new Vector3(0.28f, 0.008f, 0.05f),
                    new Color(0.38f, 0.28f, 0.22f));
                ScratchSpot sp = scratch.AddComponent<ScratchSpot>();
                refs.scratches.Add(sp);
                scratch.SetActive(false);
            }
        }

        private void CreateGum(Transform parent, SceneRefs refs)
        {
            // ── Realistic chewed-gum cluster pressed against the UNDERSIDE of
            //    the table top (table-top bottom face is at Y≈0.71 in table-local
            //    space).  We place the main blob just below the underside so that
            //    when the player looks up they see a lumpy, drippy wad hanging
            //    downward — not a flat pink dot embedded in the edge.
            //
            //    Colour: aged, slightly discoloured bubble-gum pink with a hint
            //    of grey (used-gum colour, not fresh candy pink).
            Color gumColor       = new Color(0.82f, 0.55f, 0.60f);   // faded pink
            Color gumColorDark   = new Color(0.58f, 0.38f, 0.42f);   // deeper shade
            Color gumColorDrip   = new Color(0.76f, 0.50f, 0.55f);

            // Main gum body — root hangs slightly BELOW the table-top underside
            // (table-top bottom ≈ Y 0.71; main blob centred at Y 0.69, ~3cm tall)
            GameObject gum = MakeChild(PrimitiveType.Sphere, "Gum", parent,
                new Vector3(0.18f, 0.69f, 0.15f),
                new Vector3(0.11f, 0.05f, 0.10f),
                gumColor);

            // Secondary lobe — gives the asymmetric "squished" look
            MakeChild(PrimitiveType.Sphere, "GumLobe1", gum.transform,
                new Vector3(0.45f, -0.15f, 0.3f),
                new Vector3(0.75f, 0.85f, 0.70f),
                gumColor);

            // Tertiary lobe — smaller, off-centre
            MakeChild(PrimitiveType.Sphere, "GumLobe2", gum.transform,
                new Vector3(-0.5f, 0.1f, -0.35f),
                new Vector3(0.55f, 0.65f, 0.55f),
                gumColorDark);

            // Darker shadowed spot on top (where it's stuck to wood)
            MakeChild(PrimitiveType.Sphere, "GumTopDark", gum.transform,
                new Vector3(0f, 0.35f, 0f),
                new Vector3(0.65f, 0.25f, 0.65f),
                gumColorDark);

            // Thin drippy string hanging down from the main blob
            MakeChild(PrimitiveType.Cylinder, "GumDrip", gum.transform,
                new Vector3(0.15f, -0.9f, 0f),
                new Vector3(0.08f, 0.7f, 0.08f),
                gumColorDrip);

            // Tiny bead at the end of the drip
            MakeChild(PrimitiveType.Sphere, "GumDripBead", gum.transform,
                new Vector3(0.15f, -1.7f, 0f),
                new Vector3(0.18f, 0.18f, 0.18f),
                gumColorDrip);

            GumSpot spot = gum.AddComponent<GumSpot>();
            gum.SetActive(false);
            refs.gumSpot = spot;

            GameObject residue = MakeChild(PrimitiveType.Cube, "GumResidue", parent,
                new Vector3(0.18f, 0.705f, 0.15f),
                new Vector3(0.18f, 0.01f, 0.14f),
                new Color(0.45f, 0.24f, 0.40f), 0.3f);
            residue.transform.localRotation = Quaternion.Euler(0f, 16f, 0f);
            residue.SetActive(false);
            refs.gumResidue = residue;
        }

        private void CreateEntrance(SceneRefs refs)
        {
            GameObject entry = new GameObject("EntrancePoint");
            // Spawn 1m above the floor to avoid CharacterController capsule
            // penetration against the indoor floor on teleport — gravity settles
            // the capsule cleanly within one frame.  Prevents the "Move called
            // on inactive controller" deadlock when re-entering the café.
            entry.transform.position  = new Vector3(0f, 1.0f, -L * 0.5f + 1.2f);
            entry.transform.rotation  = Quaternion.identity;
            refs.entrancePoint = entry.transform;

            // ── Entrance exterior backdrop ────────────────────────────────────
            // When the player is inside and turns around toward the entrance,
            // the outdoor scene root is deactivated and the glass shows nothing.
            // This backdrop plane sits just outside the front glass, providing a
            // simple sky + ground colour so the view looks complete instead of
            // showing the grey void.
            //
            // CUSTOMISE: change skyColor/groundColor below, or drag a texture onto
            //            the "EntranceBackdrop_Sky" object in the Hierarchy at runtime.

            // Sky portion (upper area)
            MakeCube("EntranceBackdrop_Sky",
                new Vector3(0f, 3.2f, -L * 0.5f - 0.20f),
                new Vector3(W + 2f, 5.0f, 0.06f),
                new Color(0.52f, 0.66f, 0.80f));   // ← CUSTOMISE sky colour here

            // Ground / pavement strip (lower area)
            MakeCube("EntranceBackdrop_Ground",
                new Vector3(0f, 0.0f, -L * 0.5f - 0.20f),
                new Vector3(W + 2f, 1.8f, 0.06f),
                new Color(0.35f, 0.35f, 0.37f));   // ← CUSTOMISE pavement colour here

            // A dark door-frame strip across the opening centre
            MakeCube("EntranceDoorFrame_Top",
                new Vector3(0f, 2.6f, -L * 0.5f - 0.02f),
                new Vector3(W, 0.25f, 0.06f),
                new Color(0.20f, 0.18f, 0.16f));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Small prop helpers
        // ══════════════════════════════════════════════════════════════════════

        private void CreateCup(Transform parent, Vector3 localPos, Color color)
        {
            MakeChild(PrimitiveType.Cylinder, "Cup", parent,
                localPos, new Vector3(0.09f, 0.06f, 0.09f), color);
            // Saucer
            MakeChild(PrimitiveType.Cylinder, "Saucer", parent,
                localPos - new Vector3(0f, 0.045f, 0f),
                new Vector3(0.14f, 0.015f, 0.14f),
                new Color(color.r * 0.9f, color.g * 0.9f, color.b * 0.85f));
        }

        private void CreatePlantLarge(Vector3 pos)
        {
            GameObject root = new GameObject("PlantLarge");
            root.transform.position = pos;
            MakeChild(PrimitiveType.Cylinder,"Pot",root.transform,
                new Vector3(0f,0.22f,0f),new Vector3(0.52f,0.22f,0.52f),new Color(0.45f,0.28f,0.18f));
            MakeChild(PrimitiveType.Sphere,"Leaves",root.transform,
                new Vector3(0f,0.85f,0f),new Vector3(1.0f,1.1f,1.0f),new Color(0.22f,0.48f,0.25f));
            MakeChild(PrimitiveType.Sphere,"Leaves2",root.transform,
                new Vector3(0.3f,0.95f,0.2f),new Vector3(0.65f,0.75f,0.65f),new Color(0.26f,0.52f,0.28f));
        }

        private void CreatePlantMedium(Vector3 pos)
        {
            GameObject root = new GameObject("PlantMed");
            root.transform.position = pos;
            MakeChild(PrimitiveType.Cylinder,"Pot",root.transform,
                new Vector3(0f,0.15f,0f),new Vector3(0.38f,0.15f,0.38f),new Color(0.50f,0.32f,0.22f));
            MakeChild(PrimitiveType.Sphere,"Leaves",root.transform,
                new Vector3(0f,0.55f,0f),new Vector3(0.65f,0.72f,0.65f),new Color(0.20f,0.45f,0.22f));
        }

        private void CreatePlantSmall(Vector3 pos)
        {
            GameObject root = new GameObject("PlantSmall");
            root.transform.position = pos;
            MakeChild(PrimitiveType.Cylinder,"Pot",root.transform,
                new Vector3(0f,0.08f,0f),new Vector3(0.18f,0.08f,0.18f),new Color(0.48f,0.30f,0.20f));
            MakeChild(PrimitiveType.Sphere,"Leaves",root.transform,
                new Vector3(0f,0.22f,0f),new Vector3(0.22f,0.25f,0.22f),new Color(0.28f,0.52f,0.30f));
        }

        private void CreateWorldText(string name, string text, Vector3 pos,
                                      float charSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text          = text;
            tm.characterSize = charSize;
            ConfigureWorldText(tm, color, 40);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Primitive factory helpers
        // ══════════════════════════════════════════════════════════════════════

        private GameObject MakeCube(string name, Vector3 pos, Vector3 scale,
                                     Color color, float smoothness = 0.15f)
        {
            GameObject go = RuntimePrimitive.Create(PrimitiveType.Cube);
            go.name = name;
            go.transform.position   = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = MakeMaterial(color, smoothness);
            return go;
        }

        private GameObject MakeChild(PrimitiveType type, string name, Transform parent,
                                      Vector3 localPos, Vector3 localScale,
                                      Color color, float smoothness = 0.2f)
        {
            GameObject go = RuntimePrimitive.Create(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            go.GetComponent<Renderer>().material = MakeMaterial(color, smoothness);
            return go;
        }

        private void SetActiveList<T>(List<T> list, bool active) where T : Component
        {
            foreach (T item in list)
                if (item != null) item.gameObject.SetActive(active);
        }

        // Texture slots injected from SimulationBootstrap via SetTextures()
        public Texture2D floorTex;
        public Texture2D wallTex;
        public Texture2D tableTex;
        private static Font _worldFont;

        /// <summary>Called by SimulationBootstrap before Build() to inject optional textures.</summary>
        public void SetTextures(Texture2D floor, Texture2D wall, Texture2D table)
        {
            floorTex = floor;
            wallTex  = wall;
            tableTex = table;
        }

        private Material MakeMaterial(Color color, float smoothness = 0.2f,
                                       Texture2D tex = null)
        {
            Shader shader = FindRuntimeShader();
            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness", smoothness);

            // Apply texture if provided
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))    mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex"))    mat.SetTexture("_MainTex", tex);
            }

            // Enable alpha transparency for glass / translucent surfaces
            if (color.a < 0.99f)
            {
                if (mat.HasProperty("_Surface"))  mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend"))    mat.SetFloat("_Blend",   0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
            }
            return mat;
        }

        private static Shader FindRuntimeShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Hidden/Internal-Colored")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Diffuse");
        }

        private static void ConfigureWorldText(TextMesh textMesh, Color color, int fontSize)
        {
            if (_worldFont == null)
                _worldFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                          ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (_worldFont != null)
            {
                textMesh.font = _worldFont;
                Renderer renderer = textMesh.GetComponent<Renderer>();
                renderer.sharedMaterial = _worldFont.material;
                renderer.material.color = color;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            textMesh.color = color;
            textMesh.fontSize = Mathf.Clamp(fontSize, 28, 48);
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.richText = false;
            textMesh.lineSpacing = 0.9f;
        }
    }
}
