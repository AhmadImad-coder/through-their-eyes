using System.Collections.Generic;
using UnityEngine;

namespace OCDSimulation
{
    [System.Serializable]
    public class SceneRefs
    {
        // ── Outdoor / street ──────────────────────────────────────────────────
        public Transform streetSpawnPoint;     // where player starts on the street
        public Transform coffeeShopDoorPoint;  // transition point inside the coffee shop
        public GameObject streetSceneRoot;     // parent of ALL street objects — deactivated on café entry

        // ── Indoor coffee shop ────────────────────────────────────────────────
        public Transform friendTableCenter;
        public Transform friendSeatPoint;
        public Transform playerSeatPoint;
        public Transform entrancePoint;
        public Transform lookUnderTablePoint;
        public Transform orderCounterPoint;
        public Transform orderTablePoint;
        public List<DirtSpot> stage0Dirt = new List<DirtSpot>();
        public List<DirtSpot> stage1Dirt = new List<DirtSpot>();
        public GumSpot gumSpot;
        public GameObject gumResidue;
        public List<ScratchSpot> scratches = new List<ScratchSpot>();
        public List<GameObject> friends = new List<GameObject>();
    }
}
