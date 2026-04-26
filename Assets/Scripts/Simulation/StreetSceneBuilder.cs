using System.Collections.Generic;
using UnityEngine;

namespace OCDSimulation
{
    /// <summary>
    /// Builds a procedural outdoor street scene that appears before the coffee shop.
    /// The street is offset at Z = -streetLength so it sits behind the coffee shop's
    /// entrance wall. When the player walks into the CafeEntryTrigger zone they are
    /// teleported inside.
    /// Layout (top-down, +Z = north / into coffee shop):
    ///
    ///   [Street start] ─────── shops ─────── [Coffee shop front] ──► indoor scene
    ///    Z = -streetLen                         Z = -coffeeOffset
    /// </summary>
    public class StreetSceneBuilder
    {
        // Street is placed so its far (north) end meets the indoor entrance wall
        private const float StreetLength  = 28f;  // length of walkable road
        private const float StreetWidth   = 10f;  // carriageway width
        private const float SidewalkW     = 3.5f;
        private const float BuildingDepth = 6f;
        private const float CoffeeOffsetZ = -1f;  // coffee shop front Z in street coords

        // The entire outdoor set is offset on the global Z axis so it sits
        // behind the indoor entrance (indoor entrance is near Z ≈ -roomLen/2 + 1.2)
        private readonly float zBase;

        // Root parent for every street object — deactivating this hides the entire
        // street scene the moment the player enters the café.
        private Transform streetRoot;
        private static Font worldFont;

        public StreetSceneBuilder(float indoorEntranceZ)
        {
            // Push street behind the indoor entrance so the two scenes don't overlap
            zBase = indoorEntranceZ - StreetLength;
        }

        public void Build(SceneRefs refs)
        {
            // All street objects are children of this root so we can hide the
            // entire outdoor scene in one call once the player is indoors.
            GameObject rootGo = new GameObject("StreetSceneRoot");
            streetRoot = rootGo.transform;
            refs.streetSceneRoot = rootGo;

            CreateGround();
            CreateSkybox();
            CreateBuildings(refs);
            CreateStreetFurniture();
            CreateParkedCars();
            CreateStreetTrees();
            CreateTrashBins();
            CreateCrosswalk();
            CreateSpawnPoint(refs);
        }

        // ── Road and pavement ─────────────────────────────────────────────────
        private void CreateGround()
        {
            // ── IMPORTANT: all ground slabs are made 0.5 units THICK ─────────
            // The original scale.Y values (0.02, 0.03, 0.08) produced BoxColliders
            // only 0.01–0.04 world-units tall.  PhysX line-raycasts frequently miss
            // such near-zero-height slabs (allHits.Length = 0), causing the player
            // to fall through the world.  Shifting the center DOWN by (0.5-old)/2
            // keeps every top surface at exactly the same visual height while making
            // the collider 10–25× thicker — reliably detected by Physics.RaycastAll.
            //
            //   Road:     top was at Y = 0.01  → center -0.24, scale 0.5  → top 0.01  ✓
            //   Sidewalk: top was at Y = 0.03  → center -0.22, scale 0.5  → top 0.03  ✓
            //   Kerb:     top was at Y = 0.08  → center -0.17, scale 0.5  → top 0.08  ✓

            // Road surface — visible top at Y ≈ 0
            Create("StreetRoad",
                   new Vector3(0f, -0.24f, zBase + StreetLength * 0.5f),
                   new Vector3(StreetWidth, 0.5f, StreetLength),
                   new Color(0.20f, 0.20f, 0.22f));

            // Left pavement — visible top at Y ≈ 0.03
            Create("SidewalkL",
                   new Vector3(-(StreetWidth * 0.5f + SidewalkW * 0.5f), -0.22f,
                               zBase + StreetLength * 0.5f),
                   new Vector3(SidewalkW, 0.5f, StreetLength),
                   new Color(0.68f, 0.66f, 0.62f));

            // Right pavement — visible top at Y ≈ 0.03
            Create("SidewalkR",
                   new Vector3( (StreetWidth * 0.5f + SidewalkW * 0.5f), -0.22f,
                               zBase + StreetLength * 0.5f),
                   new Vector3(SidewalkW, 0.5f, StreetLength),
                   new Color(0.68f, 0.66f, 0.62f));

            // Kerb lines
            CreateKerb(-1, zBase);
            CreateKerb( 1, zBase);
        }

        private void CreateKerb(int side, float z0)
        {
            float x = side * (StreetWidth * 0.5f + 0.1f);
            // Kerb top at Y = 0.08 → center = 0.08 - 0.5/2 = -0.17
            Create("Kerb" + (side < 0 ? "L" : "R"),
                   new Vector3(x, -0.17f, z0 + StreetLength * 0.5f),
                   new Vector3(0.2f, 0.5f, StreetLength),
                   new Color(0.55f, 0.54f, 0.52f));
        }

        // ── Sky backdrop with clouds, sun, distant skyline ────────────────────
        private void CreateSkybox()
        {
            // Overhead sky ceiling — pale blue, flipped so underside renders
            Create("OutdoorSky",
                   new Vector3(0f, 22f, zBase + StreetLength * 0.5f),
                   new Vector3(80f, 0.1f, 80f),
                   new Color(0.53f, 0.75f, 0.92f));

            // ── Clouds — soft white flattened spheres at high altitude ────────
            var cloudSeeds = new (float x, float z, float s)[]
            {
                (-18f,  5f, 4.5f), ( 12f,  8f, 3.8f), (-6f,  16f, 5.2f),
                ( 20f, 18f, 4.0f), (-22f, 22f, 4.6f), (  4f, 25f, 5.0f),
                ( 15f, 26f, 3.6f), (-14f, 14f, 3.9f), (  0f,  2f, 5.4f),
            };
            foreach (var c in cloudSeeds)
            {
                Vector3 cpos   = new Vector3(c.x, 17f, zBase + StreetLength * 0.5f + c.z);
                Vector3 cscale = new Vector3(c.s, 1.2f, c.s * 0.7f);
                Create("Cloud", cpos, cscale, new Color(1f, 1f, 1f, 0.85f));
                // Add a second overlapping lobe for a puffier silhouette
                Create("CloudLobe",
                       cpos + new Vector3(c.s * 0.4f, 0.2f, c.s * 0.2f),
                       cscale * 0.72f,
                       new Color(1f, 1f, 1f, 0.8f));
            }

            // ── Sun disc — a warm white sphere high in the sky ────────────────
            Create("Sun",
                   new Vector3(-14f, 18f, zBase + StreetLength - 2f),
                   new Vector3(3.2f, 3.2f, 3.2f),
                   new Color(1f, 0.95f, 0.72f));
            // Soft sun glow (larger, semi-transparent)
            Create("SunGlow",
                   new Vector3(-14f, 18f, zBase + StreetLength - 2f),
                   new Vector3(5.5f, 5.5f, 5.5f),
                   new Color(1f, 0.88f, 0.55f, 0.28f));

            // ── Distant city skyline silhouettes (far horizon, behind spawn) ──
            // Gives the outdoor scene visual depth as the player looks out over
            // the street — looks like a city is stretching away behind them.
            Color farCity = new Color(0.38f, 0.45f, 0.55f);
            for (int i = 0; i < 12; i++)
            {
                float fx = -28f + i * 5.2f + (i % 2 == 0 ? 0.5f : -0.5f);
                float fh = 6f + (i * 1.7f) % 5f;
                Create("FarBuilding",
                    new Vector3(fx, fh * 0.5f, zBase - 12f),
                    new Vector3(3.5f, fh, 2f),
                    Color.Lerp(farCity, new Color(0.4f, 0.5f, 0.65f), (i % 3) * 0.25f));
                // Occasional taller spire
                if (i % 3 == 0)
                {
                    Create("FarSpire",
                        new Vector3(fx, fh + 1.2f, zBase - 12f),
                        new Vector3(0.5f, 2.5f, 0.5f),
                        farCity);
                }
            }

            // Same silhouette band on the far side (beyond the café)
            for (int i = 0; i < 10; i++)
            {
                float fx = -22f + i * 5f;
                float fh = 5f + (i * 2.3f) % 6f;
                Create("FarBuildingN",
                    new Vector3(fx, fh * 0.5f, zBase + StreetLength + 8f),
                    new Vector3(3.2f, fh, 1.8f),
                    Color.Lerp(farCity, new Color(0.35f, 0.42f, 0.52f), (i % 4) * 0.2f));
            }

            // Warm ambient fill
            RenderSettings.ambientLight = Color.Lerp(
                RenderSettings.ambientLight,
                new Color(0.72f, 0.70f, 0.65f), 0.5f);
        }

        // ── Shop buildings on each side ───────────────────────────────────────
        private void CreateBuildings(SceneRefs refs)
        {
            // ── Left side shops ───────────────────────────────────────────────
            float xLeft = -(StreetWidth * 0.5f + SidewalkW + BuildingDepth * 0.5f);

            var leftShops = new (string name, Color facade, Color sign, string signText)[]
            {
                ("Bookshop",   new Color(0.55f, 0.45f, 0.35f), new Color(0.85f, 0.70f, 0.30f), "BOOKS"),
                ("Bakery",     new Color(0.82f, 0.72f, 0.60f), new Color(0.90f, 0.35f, 0.20f), "BAKERY"),
                ("Florist",    new Color(0.45f, 0.65f, 0.45f), new Color(0.95f, 0.85f, 0.90f), "FLOWERS"),
                ("Pharmacy",   new Color(0.88f, 0.92f, 0.88f), new Color(0.20f, 0.65f, 0.35f), "PHARMACY"),
            };

            for (int i = 0; i < leftShops.Length; i++)
            {
                float z = zBase + 3f + i * 6f;
                CreateShop(leftShops[i].name, new Vector3(xLeft, 0f, z),
                           leftShops[i].facade, leftShops[i].sign, leftShops[i].signText,
                           facingRight: true);
            }

            // ── Right side shops + coffee shop at far end ─────────────────────
            float xRight = (StreetWidth * 0.5f + SidewalkW + BuildingDepth * 0.5f);

            var rightShops = new (string name, Color facade, Color sign, string signText)[]
            {
                ("Barber",      new Color(0.35f, 0.45f, 0.65f), new Color(0.92f, 0.92f, 0.95f), "BARBER"),
                ("Newsagent",   new Color(0.72f, 0.62f, 0.50f), new Color(0.95f, 0.75f, 0.20f), "NEWS"),
                ("Optician",    new Color(0.60f, 0.70f, 0.78f), new Color(0.25f, 0.50f, 0.80f), "OPTICIAN"),
            };

            for (int i = 0; i < rightShops.Length; i++)
            {
                float z = zBase + 3f + i * 6f;
                CreateShop(rightShops[i].name, new Vector3(xRight, 0f, z),
                           rightShops[i].facade, rightShops[i].sign, rightShops[i].signText,
                           facingRight: false);
            }

            // Coffee shop — positioned at the far (north) end, centred on the street
            CreateCoffeeShop(refs);
        }

        private void CreateShop(string name, Vector3 pos, Color facadeColor,
                                  Color signColor, string signText, bool facingRight)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(streetRoot, false);
            root.transform.position = pos;

            // ── Main building body ────────────────────────────────────────────
            CreatePrim(PrimitiveType.Cube, "Body", root.transform,
                new Vector3(0f, 2f, 0f),
                new Vector3(BuildingDepth, 4f, 5f), facadeColor);

            // Upper-floor band (slightly lighter, gives the building 2-storey look)
            CreatePrim(PrimitiveType.Cube, "UpperBand", root.transform,
                new Vector3(0f, 3.1f, 0f),
                new Vector3(BuildingDepth + 0.05f, 0.08f, 5.02f),
                Color.Lerp(facadeColor, Color.white, 0.25f));

            // Cornice (decorative top trim)
            CreatePrim(PrimitiveType.Cube, "Cornice", root.transform,
                new Vector3(0f, 3.95f, 0f),
                new Vector3(BuildingDepth + 0.15f, 0.15f, 5.15f),
                Color.Lerp(facadeColor, Color.black, 0.45f));

            // Roof slab
            CreatePrim(PrimitiveType.Cube, "Roof", root.transform,
                new Vector3(0f, 4.1f, 0f),
                new Vector3(BuildingDepth + 0.3f, 0.2f, 5.3f),
                Color.Lerp(facadeColor, Color.black, 0.55f));

            // ── Storefront: the face that looks toward the street ────────────
            float signDir = facingRight ? 1f : -1f;
            float winX    = signDir * (BuildingDepth * 0.5f + 0.02f);

            // Stone plinth at ground level (below the window)
            CreatePrim(PrimitiveType.Cube, "Plinth", root.transform,
                new Vector3(winX, 0.35f, 0f),
                new Vector3(0.12f, 0.7f, 4.9f),
                Color.Lerp(facadeColor, Color.black, 0.55f));

            // Main shop window (large, semi-transparent glass)
            CreatePrim(PrimitiveType.Cube, "Window", root.transform,
                new Vector3(winX, 1.55f, -0.8f),
                new Vector3(0.06f, 1.7f, 2.4f),
                new Color(0.45f, 0.68f, 0.82f, 0.55f));

            // Window mullions (black cross dividers on the glass)
            CreatePrim(PrimitiveType.Cube, "WindowMullionV", root.transform,
                new Vector3(winX + signDir * 0.01f, 1.55f, -0.8f),
                new Vector3(0.04f, 1.7f, 0.06f),
                new Color(0.15f, 0.12f, 0.10f));
            CreatePrim(PrimitiveType.Cube, "WindowMullionH", root.transform,
                new Vector3(winX + signDir * 0.01f, 1.55f, -0.8f),
                new Vector3(0.04f, 0.06f, 2.4f),
                new Color(0.15f, 0.12f, 0.10f));

            // Window sill
            CreatePrim(PrimitiveType.Cube, "WindowSill", root.transform,
                new Vector3(winX, 0.75f, -0.8f),
                new Vector3(0.22f, 0.08f, 2.55f),
                Color.Lerp(facadeColor, Color.white, 0.35f));

            // Window frame top (lintel)
            CreatePrim(PrimitiveType.Cube, "WindowLintel", root.transform,
                new Vector3(winX, 2.45f, -0.8f),
                new Vector3(0.18f, 0.12f, 2.55f),
                Color.Lerp(facadeColor, Color.black, 0.4f));

            // Interior warm glow visible through the glass
            GameObject interiorLightGo = new GameObject("InteriorGlow");
            interiorLightGo.transform.SetParent(root.transform, false);
            interiorLightGo.transform.localPosition = new Vector3(
                winX - signDir * 1.0f, 1.6f, -0.8f);
            Light glow = interiorLightGo.AddComponent<Light>();
            glow.type      = LightType.Point;
            glow.range     = 3.5f;
            glow.intensity = 1.3f;
            glow.color     = new Color(1.0f, 0.88f, 0.60f);

            // Second window (upper floor — residential feel)
            CreatePrim(PrimitiveType.Cube, "UpperWindow", root.transform,
                new Vector3(winX, 3.45f, -1.5f),
                new Vector3(0.06f, 0.8f, 0.9f),
                new Color(0.55f, 0.72f, 0.85f, 0.55f));
            CreatePrim(PrimitiveType.Cube, "UpperWindow2", root.transform,
                new Vector3(winX, 3.45f, 0.7f),
                new Vector3(0.06f, 0.8f, 0.9f),
                new Color(0.55f, 0.72f, 0.85f, 0.55f));

            // ── Shop door on the street side (adjacent to window) ─────────────
            CreatePrim(PrimitiveType.Cube, "DoorFrame", root.transform,
                new Vector3(winX, 1.1f, 1.35f),
                new Vector3(0.08f, 2.25f, 1.0f),
                Color.Lerp(facadeColor, Color.black, 0.6f));
            // Door slab (slightly recessed)
            CreatePrim(PrimitiveType.Cube, "Door", root.transform,
                new Vector3(winX - signDir * 0.02f, 1.05f, 1.35f),
                new Vector3(0.04f, 2.1f, 0.85f),
                Color.Lerp(facadeColor, Color.black, 0.35f));
            // Door handle (tiny gold ball)
            CreatePrim(PrimitiveType.Sphere, "DoorHandle", root.transform,
                new Vector3(winX - signDir * 0.04f, 1.05f, 1.70f),
                new Vector3(0.08f, 0.08f, 0.08f),
                new Color(0.85f, 0.70f, 0.35f));

            // ── Awning / canopy over the storefront ───────────────────────────
            Color awnColor = signColor;
            CreatePrim(PrimitiveType.Cube, "Awning", root.transform,
                new Vector3(winX + signDir * 0.6f, 2.75f, -0.4f),
                new Vector3(1.2f, 0.08f, 3.6f),
                awnColor);
            // Awning fringe (striped accent)
            CreatePrim(PrimitiveType.Cube, "AwningFringe", root.transform,
                new Vector3(winX + signDir * 1.2f, 2.65f, -0.4f),
                new Vector3(0.02f, 0.18f, 3.6f),
                Color.Lerp(awnColor, Color.white, 0.6f));

            // ── Shop sign ─────────────────────────────────────────────────────
            CreatePrim(PrimitiveType.Cube, "Sign", root.transform,
                new Vector3(winX, 3.25f, 0f),
                new Vector3(0.08f, 0.55f, 2.2f),
                signColor);

            // Sign text
            GameObject signTextGo = new GameObject(name + "SignText");
            signTextGo.transform.SetParent(root.transform, false);
            signTextGo.transform.localPosition = new Vector3(
                winX + signDir * 0.08f, 3.25f, 0f);
            signTextGo.transform.localRotation = Quaternion.Euler(0f, facingRight ? -90f : 90f, 0f);
            TextMesh tm = signTextGo.AddComponent<TextMesh>();
            tm.text          = signText;
            tm.characterSize = 0.052f;
            ConfigureWorldText(tm, Color.white, 38);
        }

        // ── Coffee shop ───────────────────────────────────────────────────────
        private void CreateCoffeeShop(SceneRefs refs)
        {
            // Sits at the far end of the street, centred, facing south (toward player)
            float z = zBase + StreetLength - 3f;
            Vector3 centre = new Vector3(0f, 0f, z);

            GameObject root = new GameObject("CoffeeShopExterior");
            root.transform.SetParent(streetRoot, false);
            root.transform.position = centre;

            float w = 8f, h = 4.5f, d = 6f;

            // Side walls
            CreatePrim(PrimitiveType.Cube, "WallL", root.transform,
                new Vector3(-w * 0.5f, h * 0.5f, d * 0.5f), new Vector3(0.2f, h, d),
                new Color(0.36f, 0.29f, 0.24f));
            CreatePrim(PrimitiveType.Cube, "WallR", root.transform,
                new Vector3( w * 0.5f, h * 0.5f, d * 0.5f), new Vector3(0.2f, h, d),
                new Color(0.36f, 0.29f, 0.24f));
            // Back wall
            CreatePrim(PrimitiveType.Cube, "WallBack", root.transform,
                new Vector3(0f, h * 0.5f, d), new Vector3(w, h, 0.2f),
                new Color(0.36f, 0.29f, 0.24f));
            // Roof
            CreatePrim(PrimitiveType.Cube, "Roof", root.transform,
                new Vector3(0f, h + 0.1f, d * 0.5f), new Vector3(w + 0.4f, 0.2f, d + 0.4f),
                new Color(0.22f, 0.18f, 0.15f));

            // Glass front wall (south face, facing player) — semi-transparent
            CreatePrim(PrimitiveType.Cube, "GlassFront", root.transform,
                new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, 0.12f),
                new Color(0.55f, 0.78f, 0.92f, 0.55f));

            CreateExteriorWindowDisplays(root.transform);

            // Awning above door
            CreatePrim(PrimitiveType.Cube, "Awning", root.transform,
                new Vector3(0f, h - 0.3f, -0.6f), new Vector3(w + 0.6f, 0.18f, 1.4f),
                new Color(0.55f, 0.22f, 0.18f));

            // Name sign on awning
            GameObject sign = new GameObject("CafeSign");
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, h - 0.1f, -0.55f);
            TextMesh tm = sign.AddComponent<TextMesh>();
            tm.text          = "BREWED GROUNDS COFFEE";
            tm.characterSize = 0.070f;
            ConfigureWorldText(tm, new Color(0.98f, 0.92f, 0.70f), 46);

            // Door frame
            CreatePrim(PrimitiveType.Cube, "DoorFrameL", root.transform,
                new Vector3(-0.7f, 1.1f, 0f), new Vector3(0.12f, 2.2f, 0.15f),
                new Color(0.30f, 0.22f, 0.16f));
            CreatePrim(PrimitiveType.Cube, "DoorFrameR", root.transform,
                new Vector3( 0.7f, 1.1f, 0f), new Vector3(0.12f, 2.2f, 0.15f),
                new Color(0.30f, 0.22f, 0.16f));
            CreatePrim(PrimitiveType.Cube, "DoorFrameTop", root.transform,
                new Vector3(0f, 2.3f, 0f), new Vector3(1.4f, 0.12f, 0.15f),
                new Color(0.30f, 0.22f, 0.16f));

            CreatePrim(PrimitiveType.Cube, "CafeDoorLeft", root.transform,
                new Vector3(-0.36f, 1.08f, -0.04f), new Vector3(0.64f, 2.08f, 0.06f),
                new Color(0.25f, 0.18f, 0.13f, 0.86f));
            CreatePrim(PrimitiveType.Cube, "CafeDoorRight", root.transform,
                new Vector3(0.36f, 1.08f, -0.04f), new Vector3(0.64f, 2.08f, 0.06f),
                new Color(0.25f, 0.18f, 0.13f, 0.86f));
            CreatePrim(PrimitiveType.Sphere, "DoorBell", root.transform,
                new Vector3(0f, 2.55f, -0.12f), new Vector3(0.16f, 0.16f, 0.16f),
                new Color(0.95f, 0.78f, 0.30f));

            // ── Entrance trigger ──────────────────────────────────────────────
            GameObject trigger = new GameObject("CafeEntryTrigger");
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.localPosition = new Vector3(0f, 1f, -0.5f);
            BoxCollider col = trigger.AddComponent<BoxCollider>();
            col.size      = new Vector3(2f, 2.2f, 1.8f);
            col.isTrigger = true;
            trigger.AddComponent<CafeEntryTrigger>();

            // ── Door sign ─────────────────────────────────────────────────────
            GameObject hint = new GameObject("EnterHint");
            hint.transform.SetParent(root.transform, false);
            hint.transform.localPosition = new Vector3(0f, 0.9f, -0.7f);
            TextMesh ht = hint.AddComponent<TextMesh>();
            ht.text          = "[E]  Enter";
            ht.characterSize = 0.038f;
            ConfigureWorldText(ht, new Color(0.95f, 0.90f, 0.60f), 34);

            // ── Street spawn point ref ────────────────────────────────────────
            refs.coffeeShopDoorPoint = trigger.transform;
        }

        // ── Street lamps + benches ────────────────────────────────────────────
        private void CreateStreetFurniture()
        {
            float[] lampZ = { zBase + 5f, zBase + 11f, zBase + 17f, zBase + 23f };
            foreach (float z in lampZ)
            {
                CreateLamp(-1, z);
                CreateLamp( 1, z);
            }

            CreateBench(-1, zBase + 8f);
            CreateBench( 1, zBase + 14f);
            CreateBench(-1, zBase + 20f);
        }

        private void CreateLamp(int side, float z)
        {
            float x = side * (StreetWidth * 0.5f + SidewalkW * 0.5f + 0.3f);
            GameObject lamp = new GameObject("StreetLamp");
            lamp.transform.SetParent(streetRoot, false);
            lamp.transform.position = new Vector3(x, 0f, z);

            CreatePrim(PrimitiveType.Cylinder, "Post", lamp.transform,
                new Vector3(0f, 2.5f, 0f), new Vector3(0.12f, 2.5f, 0.12f),
                new Color(0.30f, 0.30f, 0.32f));
            CreatePrim(PrimitiveType.Sphere, "Head", lamp.transform,
                new Vector3(0f, 5.2f, 0f), new Vector3(0.35f, 0.35f, 0.35f),
                new Color(0.98f, 0.95f, 0.75f));

            // Attach a light source
            GameObject lightGo = new GameObject("LampLight");
            lightGo.transform.SetParent(lamp.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 5.2f, 0f);
            Light lt = lightGo.AddComponent<Light>();
            lt.type      = LightType.Point;
            lt.range     = 7f;
            lt.intensity = 0.7f;
            lt.color     = new Color(0.98f, 0.92f, 0.70f);
        }

        private void CreateBench(int side, float z)
        {
            float x = side * (StreetWidth * 0.5f + SidewalkW * 0.3f);
            GameObject bench = new GameObject("Bench");
            bench.transform.SetParent(streetRoot, false);
            bench.transform.position = new Vector3(x, 0f, z);

            CreatePrim(PrimitiveType.Cube, "Seat", bench.transform,
                new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.1f, 0.5f),
                new Color(0.45f, 0.30f, 0.22f));
            CreatePrim(PrimitiveType.Cube, "Back", bench.transform,
                new Vector3(0f, 0.85f, -0.2f), new Vector3(1.4f, 0.7f, 0.08f),
                new Color(0.42f, 0.28f, 0.20f));
            // Legs
            foreach (float lx in new[] { -0.6f, 0.6f })
            {
                CreatePrim(PrimitiveType.Cube, "Leg", bench.transform,
                    new Vector3(lx, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.5f),
                    new Color(0.22f, 0.20f, 0.18f));
            }
        }

        // ── Parked cars along the kerb ────────────────────────────────────────
        private void CreateParkedCars()
        {
            // Each car: (z-position, side, bodyColor)
            var cars = new (float z, int side, Color body)[]
            {
                (zBase + 6f,  -1, new Color(0.80f, 0.20f, 0.18f)),   // red
                (zBase + 12f,  1, new Color(0.25f, 0.35f, 0.55f)),   // navy blue
                (zBase + 19f, -1, new Color(0.92f, 0.88f, 0.80f)),   // cream
                (zBase + 19.8f,  1, new Color(0.18f, 0.18f, 0.20f)), // black, clear of crosswalk
            };
            foreach (var c in cars) CreateCar(c.z, c.side, c.body);
        }

        private void CreateCar(float z, int side, Color body)
        {
            // Park along the inside edge of the street, near the kerb
            float x = side * (StreetWidth * 0.5f - 1.0f);
            GameObject car = new GameObject("ParkedCar");
            car.transform.SetParent(streetRoot, false);
            car.transform.position = new Vector3(x, 0f, z);
            // Cars face along the street (Z axis)
            car.transform.rotation = Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f);

            Color darkBody = Color.Lerp(body, Color.black, 0.45f);

            // Lower chassis (wider, longer)
            CreatePrim(PrimitiveType.Cube, "Chassis", car.transform,
                new Vector3(0f, 0.45f, 0f), new Vector3(4.0f, 0.6f, 1.7f), body);
            // Upper cabin (narrower, centered)
            CreatePrim(PrimitiveType.Cube, "Cabin", car.transform,
                new Vector3(0.1f, 1.05f, 0f), new Vector3(2.2f, 0.55f, 1.55f),
                Color.Lerp(body, Color.white, 0.15f));
            // Windshield (tinted glass)
            CreatePrim(PrimitiveType.Cube, "Windshield", car.transform,
                new Vector3(0.9f, 1.05f, 0f), new Vector3(0.08f, 0.55f, 1.45f),
                new Color(0.15f, 0.20f, 0.30f, 0.75f));
            // Rear window
            CreatePrim(PrimitiveType.Cube, "RearWindow", car.transform,
                new Vector3(-0.85f, 1.05f, 0f), new Vector3(0.08f, 0.55f, 1.45f),
                new Color(0.15f, 0.20f, 0.30f, 0.75f));
            // Side windows (left and right)
            CreatePrim(PrimitiveType.Cube, "SideWinL", car.transform,
                new Vector3(0.1f, 1.05f,  0.78f), new Vector3(2.1f, 0.5f, 0.04f),
                new Color(0.15f, 0.20f, 0.30f, 0.75f));
            CreatePrim(PrimitiveType.Cube, "SideWinR", car.transform,
                new Vector3(0.1f, 1.05f, -0.78f), new Vector3(2.1f, 0.5f, 0.04f),
                new Color(0.15f, 0.20f, 0.30f, 0.75f));
            // Headlights (two small glowing spheres at the front)
            CreatePrim(PrimitiveType.Sphere, "HeadlightL", car.transform,
                new Vector3(2.02f, 0.5f,  0.55f), new Vector3(0.25f, 0.25f, 0.25f),
                new Color(1.0f, 0.98f, 0.80f));
            CreatePrim(PrimitiveType.Sphere, "HeadlightR", car.transform,
                new Vector3(2.02f, 0.5f, -0.55f), new Vector3(0.25f, 0.25f, 0.25f),
                new Color(1.0f, 0.98f, 0.80f));
            // Tail lights (red)
            CreatePrim(PrimitiveType.Sphere, "TailL", car.transform,
                new Vector3(-2.02f, 0.5f,  0.55f), new Vector3(0.22f, 0.22f, 0.22f),
                new Color(0.90f, 0.12f, 0.12f));
            CreatePrim(PrimitiveType.Sphere, "TailR", car.transform,
                new Vector3(-2.02f, 0.5f, -0.55f), new Vector3(0.22f, 0.22f, 0.22f),
                new Color(0.90f, 0.12f, 0.12f));
            // Four wheels
            var wheelPos = new[]
            {
                new Vector3( 1.35f, 0.25f,  0.85f), new Vector3( 1.35f, 0.25f, -0.85f),
                new Vector3(-1.35f, 0.25f,  0.85f), new Vector3(-1.35f, 0.25f, -0.85f)
            };
            foreach (var wp in wheelPos)
            {
                GameObject wheel = CreatePrim(PrimitiveType.Cylinder, "Wheel", car.transform,
                    wp, new Vector3(0.42f, 0.12f, 0.42f), new Color(0.10f, 0.10f, 0.10f));
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            // Bumper accents
            CreatePrim(PrimitiveType.Cube, "FrontBumper", car.transform,
                new Vector3(2.0f, 0.3f, 0f), new Vector3(0.12f, 0.2f, 1.6f), darkBody);
            CreatePrim(PrimitiveType.Cube, "RearBumper", car.transform,
                new Vector3(-2.0f, 0.3f, 0f), new Vector3(0.12f, 0.2f, 1.6f), darkBody);
        }

        // ── Street trees in planters ──────────────────────────────────────────
        private void CreateStreetTrees()
        {
            float[] treeZ = { zBase + 2.5f, zBase + 8f, zBase + 14f, zBase + 20f, zBase + 25.5f };
            foreach (float z in treeZ)
            {
                CreateTree(-1, z);
                CreateTree( 1, z);
            }
        }

        private void CreateTree(int side, float z)
        {
            float x = side * (StreetWidth * 0.5f + SidewalkW * 0.7f);
            GameObject tree = new GameObject("StreetTree");
            tree.transform.SetParent(streetRoot, false);
            tree.transform.position = new Vector3(x, 0f, z);

            // Stone planter box
            CreatePrim(PrimitiveType.Cube, "Planter", tree.transform,
                new Vector3(0f, 0.2f, 0f), new Vector3(1.0f, 0.4f, 1.0f),
                new Color(0.55f, 0.54f, 0.52f));
            // Soil
            CreatePrim(PrimitiveType.Cube, "Soil", tree.transform,
                new Vector3(0f, 0.42f, 0f), new Vector3(0.85f, 0.05f, 0.85f),
                new Color(0.28f, 0.20f, 0.14f));
            // Trunk
            CreatePrim(PrimitiveType.Cylinder, "Trunk", tree.transform,
                new Vector3(0f, 1.6f, 0f), new Vector3(0.18f, 1.2f, 0.18f),
                new Color(0.33f, 0.22f, 0.15f));
            // Foliage — three overlapping green spheres
            Color leaf = new Color(0.25f, 0.55f, 0.28f);
            CreatePrim(PrimitiveType.Sphere, "Leaves1", tree.transform,
                new Vector3(0f, 3.0f, 0f), new Vector3(1.7f, 1.6f, 1.7f), leaf);
            CreatePrim(PrimitiveType.Sphere, "Leaves2", tree.transform,
                new Vector3(0.3f, 3.3f, 0.2f), new Vector3(1.3f, 1.3f, 1.3f),
                Color.Lerp(leaf, Color.white, 0.12f));
            CreatePrim(PrimitiveType.Sphere, "Leaves3", tree.transform,
                new Vector3(-0.3f, 2.9f, -0.2f), new Vector3(1.2f, 1.2f, 1.2f),
                Color.Lerp(leaf, Color.black, 0.12f));
        }

        // ── Trash bins along sidewalks ────────────────────────────────────────
        private void CreateTrashBins()
        {
            CreateTrashBin(-1, zBase + 10f);
            CreateTrashBin( 1, zBase + 22f);
        }

        private void CreateTrashBin(int side, float z)
        {
            float x = side * (StreetWidth * 0.5f + SidewalkW * 0.9f);
            GameObject bin = new GameObject("TrashBin");
            bin.transform.SetParent(streetRoot, false);
            bin.transform.position = new Vector3(x, 0f, z);

            // Body
            CreatePrim(PrimitiveType.Cylinder, "Body", bin.transform,
                new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.55f, 0.5f),
                new Color(0.30f, 0.35f, 0.33f));
            // Lid
            CreatePrim(PrimitiveType.Cylinder, "Lid", bin.transform,
                new Vector3(0f, 1.15f, 0f), new Vector3(0.55f, 0.04f, 0.55f),
                new Color(0.18f, 0.22f, 0.20f));
            // Opening
            CreatePrim(PrimitiveType.Cube, "Slot", bin.transform,
                new Vector3(0f, 1.0f, 0f), new Vector3(0.35f, 0.06f, 0.15f),
                new Color(0.05f, 0.05f, 0.05f));
        }

        // ── Crosswalk stripes at the cafe end of the street ───────────────────
        private void CreateCrosswalk()
        {
            // Place crosswalk just south of the coffee shop entrance
            float baseZ = zBase + StreetLength - 5f;
            int stripes = 6;
            float stripeW = 0.6f;
            float stripeGap = 0.45f;
            for (int i = 0; i < stripes; i++)
            {
                float z = baseZ + i * (stripeW + stripeGap);
                Create("CrosswalkStripe",
                    new Vector3(0f, 0.012f, z),
                    new Vector3(StreetWidth - 0.4f, 0.01f, stripeW),
                    new Color(0.92f, 0.90f, 0.85f));
            }
            // Centre dashed line along the road
            float centreStart = zBase + 1f;
            float centreEnd   = baseZ - 1f;
            int   dashes      = 10;
            for (int i = 0; i < dashes; i++)
            {
                float t = (float)i / (dashes - 1);
                float z = Mathf.Lerp(centreStart, centreEnd, t);
                Create("CentreDash",
                    new Vector3(0f, 0.011f, z),
                    new Vector3(0.15f, 0.01f, 1.2f),
                    new Color(0.90f, 0.88f, 0.75f));
            }
        }

        private void CreateExteriorWindowDisplays(Transform root)
        {
            CreatePrim(PrimitiveType.Cube, "WindowWarmthL", root,
                new Vector3(-2.45f, 1.7f, -0.08f), new Vector3(1.6f, 1.6f, 0.04f),
                new Color(0.88f, 0.60f, 0.35f, 0.45f));
            CreatePrim(PrimitiveType.Cube, "WindowWarmthR", root,
                new Vector3(2.45f, 1.7f, -0.08f), new Vector3(1.6f, 1.6f, 0.04f),
                new Color(0.35f, 0.55f, 0.52f, 0.45f));
            CreatePrim(PrimitiveType.Cylinder, "WindowCoffeeIcon", root,
                new Vector3(-2.45f, 1.65f, -0.12f), new Vector3(0.35f, 0.05f, 0.35f),
                new Color(0.32f, 0.18f, 0.10f));
            CreatePrim(PrimitiveType.Cube, "WindowPosterR", root,
                new Vector3(2.45f, 1.65f, -0.12f), new Vector3(0.95f, 1.15f, 0.04f),
                new Color(0.94f, 0.88f, 0.72f, 0.70f));
        }

        // ── Street spawn point ────────────────────────────────────────────────
        private void CreateSpawnPoint(SceneRefs refs)
        {
            GameObject sp = new GameObject("StreetSpawnPoint");
            sp.transform.SetParent(streetRoot, false);
            // IMPORTANT: spawn a comfortable 1m ABOVE the road surface so the
            // CharacterController capsule does NOT spawn penetrating the road
            // collider (road top is at Y≈0.01).  A small overlap on spawn puts
            // PhysX into a depenetration-deadlock and every Move() call prints
            // "CharacterController.Move called on inactive controller".  Gravity
            // will settle the capsule onto the road surface within one frame.
            sp.transform.position = new Vector3(0f, 1.0f, zBase + 1.5f);
            sp.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            refs.streetSpawnPoint = sp.transform;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private GameObject CreatePrim(PrimitiveType type, string name,
                                       Transform parent, Vector3 worldPos,
                                       Vector3 scale, Color color)
        {
            GameObject go = RuntimePrimitive.Create(type);
            go.name = name;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = worldPos;
            }
            else
            {
                go.transform.position = worldPos;
            }
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = MakeMaterial(color);
            return go;
        }

        private void Create(string name, Vector3 pos, Vector3 scale, Color color)
        {
            GameObject go = RuntimePrimitive.Create(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(streetRoot, false);
            go.transform.position   = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = MakeMaterial(color);
        }

        private Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Hidden/Internal-Colored")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness", 0.15f);
            if (mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness", 0.15f);
            // Enable transparency for glass elements (alpha < 0.99)
            if (color.a < 0.99f)
            {
                mat.SetFloat("_Surface", 1f);          // transparent surface
                mat.SetFloat("_Blend",   0f);          // alpha blend
                mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite",    0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
            }
            return mat;
        }

        private static void ConfigureWorldText(TextMesh textMesh, Color color, int fontSize)
        {
            if (worldFont == null)
                worldFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            if (worldFont != null)
            {
                textMesh.font = worldFont;
                Renderer renderer = textMesh.GetComponent<Renderer>();
                renderer.sharedMaterial = worldFont.material;
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
