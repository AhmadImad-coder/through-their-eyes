using System.Collections.Generic;
using UnityEngine;

namespace OCDSimulation
{
    public class SceneBuilder
    {
        private readonly float roomWidth;
        private readonly float roomLength;
        private readonly float roomHeight;

        public SceneBuilder(float width, float length, float height)
        {
            roomWidth = width;
            roomLength = length;
            roomHeight = height;
        }

        public SceneRefs Build()
        {
            SceneRefs refs = new SceneRefs();
            CreateRoom();
            CreateCounter();
            CreateDecor();
            CreateWorkers();
            CreateTables(refs);
            CreateFriends(refs);
            CreateEntrance(refs);
            return refs;
        }

        private void CreateRoom()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(roomWidth / 10f, 1f, roomLength / 10f);
            floor.GetComponent<Renderer>().material = MakeMaterial(new Color(0.7f, 0.64f, 0.56f));

            GameObject wallBack = CreateWall("WallBack", new Vector3(0f, roomHeight / 2f, roomLength / 2f), new Vector3(roomWidth, roomHeight, 0.2f));
            GameObject wallFront = CreateWall("WallFront", new Vector3(0f, roomHeight / 2f, -roomLength / 2f), new Vector3(roomWidth, roomHeight, 0.2f));
            GameObject wallLeft = CreateWall("WallLeft", new Vector3(-roomWidth / 2f, roomHeight / 2f, 0f), new Vector3(0.2f, roomHeight, roomLength));
            GameObject wallRight = CreateWall("WallRight", new Vector3(roomWidth / 2f, roomHeight / 2f, 0f), new Vector3(0.2f, roomHeight, roomLength));

            wallBack.GetComponent<Renderer>().material = MakeMaterial(new Color(0.92f, 0.88f, 0.8f));
            wallFront.GetComponent<Renderer>().material = MakeMaterial(new Color(0.92f, 0.88f, 0.8f));
            wallLeft.GetComponent<Renderer>().material = MakeMaterial(new Color(0.92f, 0.88f, 0.8f));
            wallRight.GetComponent<Renderer>().material = MakeMaterial(new Color(0.92f, 0.88f, 0.8f));

            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.position = new Vector3(0f, roomHeight, 0f);
            ceiling.transform.localScale = new Vector3(roomWidth, 0.1f, roomLength);
            ceiling.GetComponent<Renderer>().material = MakeMaterial(new Color(0.95f, 0.93f, 0.9f));

            GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
            window.name = "Window";
            window.transform.position = new Vector3(roomWidth / 2f - 0.12f, roomHeight / 2f + 0.4f, 2f);
            window.transform.localScale = new Vector3(0.05f, 1.7f, 2.8f);
            window.GetComponent<Renderer>().material = MakeMaterial(new Color(0.55f, 0.75f, 0.9f));
        }

        private GameObject CreateWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            return wall;
        }

        private void CreateCounter()
        {
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            counter.transform.position = new Vector3(0f, 0.6f, roomLength / 2f - 1.2f);
            counter.transform.localScale = new Vector3(4.2f, 1.2f, 1.1f);
            counter.GetComponent<Renderer>().material = MakeMaterial(new Color(0.36f, 0.29f, 0.24f));

            GameObject menu = GameObject.CreatePrimitive(PrimitiveType.Cube);
            menu.name = "MenuBoard";
            menu.transform.position = new Vector3(0f, 2.6f, roomLength / 2f - 1.1f);
            menu.transform.localScale = new Vector3(3.8f, 1.3f, 0.1f);
            menu.GetComponent<Renderer>().material = MakeMaterial(new Color(0.15f, 0.18f, 0.2f));
        }

        private void CreateDecor()
        {
            GameObject plant = new GameObject("Plant");
            plant.transform.position = new Vector3(-4.8f, 0f, 0.5f);

            GameObject pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.transform.SetParent(plant.transform, false);
            pot.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            pot.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
            pot.GetComponent<Renderer>().material = MakeMaterial(new Color(0.45f, 0.25f, 0.18f));

            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.SetParent(plant.transform, false);
            leaves.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            leaves.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            leaves.GetComponent<Renderer>().material = MakeMaterial(new Color(0.25f, 0.5f, 0.3f));

            for (int i = 0; i < 3; i++)
            {
                GameObject light = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                light.name = "PendantLight";
                light.transform.position = new Vector3(-2f + i * 2f, roomHeight - 0.6f, -1f + i * 0.5f);
                light.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
                light.GetComponent<Renderer>().material = MakeMaterial(new Color(0.9f, 0.85f, 0.6f));
            }
        }

        private void CreateWorkers()
        {
            Vector3[] positions =
            {
                new Vector3(-1.2f, 0f, roomLength / 2f - 0.5f),
                new Vector3(1.2f, 0f, roomLength / 2f - 0.8f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject worker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                worker.name = "Worker";
                worker.transform.position = positions[i] + new Vector3(0f, 1f, 0f);
                worker.transform.localScale = new Vector3(0.6f, 1.1f, 0.6f);
                worker.GetComponent<Renderer>().material = MakeMaterial(new Color(0.2f, 0.5f, 0.3f));
                worker.AddComponent<NPCIdle>();
            }
        }

        private void CreateTables(SceneRefs refs)
        {
            Vector3[] tablePositions =
            {
                new Vector3(-3.2f, 0f, -2f),
                new Vector3(2.4f, 0f, -2.8f),
                new Vector3(-1.2f, 0f, 2.2f),
                new Vector3(3.4f, 0f, 2f),
                new Vector3(-4.2f, 0f, 2.4f)
            };

            for (int i = 0; i < tablePositions.Length; i++)
            {
                bool isFriendTable = i == 0;
                CreateTable(tablePositions[i], isFriendTable, refs);
            }
        }

        private void CreateTable(Vector3 position, bool isFriendTable, SceneRefs refs)
        {
            GameObject tableRoot = new GameObject(isFriendTable ? "FriendTable" : "Table");
            tableRoot.transform.position = position;

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.transform.SetParent(tableRoot.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            top.transform.localScale = new Vector3(1.6f, 0.1f, 1.2f);
            top.GetComponent<Renderer>().material = MakeMaterial(new Color(0.52f, 0.41f, 0.32f));

            float legHeight = 0.7f;
            Vector3[] legs =
            {
                new Vector3(0.7f, legHeight / 2f, 0.5f),
                new Vector3(-0.7f, legHeight / 2f, 0.5f),
                new Vector3(0.7f, legHeight / 2f, -0.5f),
                new Vector3(-0.7f, legHeight / 2f, -0.5f)
            };

            foreach (Vector3 legPos in legs)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.transform.SetParent(tableRoot.transform, false);
                leg.transform.localPosition = legPos;
                leg.transform.localScale = new Vector3(0.1f, legHeight, 0.1f);
                leg.GetComponent<Renderer>().material = MakeMaterial(new Color(0.35f, 0.28f, 0.22f));
            }

            CreateChair(tableRoot.transform, new Vector3(0f, 0f, -1.1f), Quaternion.Euler(0f, 0f, 0f));
            CreateChair(tableRoot.transform, new Vector3(0f, 0f, 1.1f), Quaternion.Euler(0f, 180f, 0f));

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

            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.transform.SetParent(chairRoot.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            seat.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
            seat.GetComponent<Renderer>().material = MakeMaterial(new Color(0.25f, 0.25f, 0.28f));

            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.transform.SetParent(chairRoot.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.9f, -0.35f);
            back.transform.localScale = new Vector3(0.8f, 0.9f, 0.1f);
            back.GetComponent<Renderer>().material = MakeMaterial(new Color(0.23f, 0.23f, 0.26f));
        }

        private void CreateFriendTableTrigger(Transform parent, SceneRefs refs)
        {
            GameObject trigger = new GameObject("FriendTrigger");
            trigger.transform.SetParent(parent, false);
            trigger.transform.localPosition = new Vector3(0f, 0.6f, -0.2f);
            BoxCollider col = trigger.AddComponent<BoxCollider>();
            col.size = new Vector3(3f, 1.4f, 3f);
            col.isTrigger = true;
            trigger.AddComponent<FriendTableTrigger>();
        }

        private void CreateSeatPoint(Transform parent, SceneRefs refs)
        {
            GameObject seat = new GameObject("PlayerSeatPoint");
            seat.transform.SetParent(parent, false);
            // Seat player on the far side with Mia (2v2 layout)
            seat.transform.localPosition = new Vector3(-0.4f, 0.55f, 0.95f);
            seat.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            refs.playerSeatPoint = seat.transform;

            GameObject under = new GameObject("LookUnderPoint");
            under.transform.SetParent(parent, false);
            under.transform.localPosition = new Vector3(0f, 0.3f, -0.2f);
            under.transform.localRotation = Quaternion.Euler(40f, 0f, 0f);
            refs.lookUnderTablePoint = under.transform;
        }

        private void CreateStageDirt(Transform parent, SceneRefs refs)
        {
            DirtSpot dirtA = CreateDirt(parent, new Vector3(0.25f, 0.81f, 0.15f), new Color(0.25f, 0.15f, 0.1f), 0.28f);
            DirtSpot dirtB = CreateDirt(parent, new Vector3(-0.25f, 0.81f, -0.12f), new Color(0.18f, 0.12f, 0.08f), 0.24f);
            refs.stage0Dirt.Add(dirtA);
            refs.stage0Dirt.Add(dirtB);

            DirtSpot dirtC = CreateDirt(parent, new Vector3(-0.35f, 0.81f, 0.3f), new Color(0.2f, 0.1f, 0.05f), 0.22f);
            DirtSpot dirtD = CreateDirt(parent, new Vector3(0.4f, 0.81f, -0.28f), new Color(0.3f, 0.2f, 0.12f), 0.26f);
            refs.stage1Dirt.Add(dirtC);
            refs.stage1Dirt.Add(dirtD);

            SetActiveList(refs.stage0Dirt, false);
            SetActiveList(refs.stage1Dirt, false);
        }

        private DirtSpot CreateDirt(Transform parent, Vector3 localPos, Color color, float scale)
        {
            GameObject stain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stain.name = "Dirt";
            stain.transform.SetParent(parent, false);
            stain.transform.localPosition = localPos + new Vector3(0f, 0.03f, 0f);
            stain.transform.localScale = new Vector3(scale, 0.01f, scale);
            stain.GetComponent<Renderer>().material = MakeMaterial(color);
            Collider existing = stain.GetComponent<Collider>();
            if (existing != null)
            {
                Object.Destroy(existing);
            }
            BoxCollider box = stain.AddComponent<BoxCollider>();
            box.size = new Vector3(1.6f, 0.2f, 1.6f);
            box.center = Vector3.zero;
            return stain.AddComponent<DirtSpot>();
        }

        private void CreateScratches(Transform parent, SceneRefs refs)
        {
            Vector3[] spots =
            {
                new Vector3(-0.4f, 0.82f, -0.1f),
                new Vector3(0.3f, 0.82f, 0.2f),
                new Vector3(0.1f, 0.82f, -0.25f),
                new Vector3(-0.2f, 0.82f, 0.3f),
                new Vector3(0.45f, 0.82f, -0.35f)
            };

            foreach (Vector3 pos in spots)
            {
                GameObject scratch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                scratch.name = "Scratch";
                scratch.transform.SetParent(parent, false);
                scratch.transform.localPosition = pos;
                scratch.transform.localScale = new Vector3(0.25f, 0.01f, 0.05f);
                scratch.GetComponent<Renderer>().material = MakeMaterial(new Color(0.4f, 0.3f, 0.25f));
                ScratchSpot spot = scratch.AddComponent<ScratchSpot>();
                refs.scratches.Add(spot);
                scratch.SetActive(false);
            }
        }

        private void CreateGum(Transform parent, SceneRefs refs)
        {
            GameObject gum = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gum.name = "Gum";
            gum.transform.SetParent(parent, false);
            gum.transform.localPosition = new Vector3(0.2f, 0.45f, 0.2f);
            gum.transform.localScale = new Vector3(0.2f, 0.08f, 0.2f);
            gum.GetComponent<Renderer>().material = MakeMaterial(new Color(0.8f, 0.4f, 0.7f));
            GumSpot spot = gum.AddComponent<GumSpot>();
            gum.SetActive(false);
            refs.gumSpot = spot;
        }

        private void CreateFriends(SceneRefs refs)
        {
            Vector3 basePos = new Vector3(-3.2f, 0f, -2f);
            string[] names = { "Emma", "Jake", "Mia" };
            Vector3[] offsets = { new Vector3(-0.6f, 0f, -0.8f), new Vector3(0.6f, 0f, -0.8f), new Vector3(0.45f, 0f, 0.8f) };

            for (int i = 0; i < names.Length; i++)
            {
                GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npc.name = names[i];
                npc.transform.position = basePos + offsets[i] + new Vector3(0f, 1f, 0f);
                npc.transform.localScale = new Vector3(0.6f, 1.1f, 0.6f);
                npc.GetComponent<Renderer>().material = MakeMaterial(new Color(0.5f + i * 0.1f, 0.4f, 0.5f));
                npc.AddComponent<NPCIdle>();

                GameObject label = new GameObject(names[i] + "Label");
                label.transform.SetParent(npc.transform, false);
                label.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                TextMesh tm = label.AddComponent<TextMesh>();
                tm.text = names[i];
                tm.color = new Color(0.3f, 0.6f, 1f);
                tm.characterSize = 0.1f;
                tm.anchor = TextAnchor.MiddleCenter;
                label.AddComponent<Billboard>();

                refs.friends.Add(npc);
            }
        }

        private void CreateEntrance(SceneRefs refs)
        {
            GameObject entry = new GameObject("EntrancePoint");
            entry.transform.position = new Vector3(0f, 0f, -roomLength / 2f + 1.2f);
            entry.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            refs.entrancePoint = entry.transform;
        }

        private void SetActiveList<T>(List<T> list, bool active) where T : Component
        {
            foreach (T item in list)
            {
                if (item != null) item.gameObject.SetActive(active);
            }
        }

        private Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            Material mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.2f);
            }
            if (mat.HasProperty("_Glossiness"))
            {
                mat.SetFloat("_Glossiness", 0.2f);
            }
            return mat;
        }
    }
}
