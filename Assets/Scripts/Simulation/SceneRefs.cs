using System.Collections.Generic;
using UnityEngine;

namespace OCDSimulation
{
    public class SceneRefs
    {
        public Transform friendTableCenter;
        public Transform friendSeatPoint;
        public Transform playerSeatPoint;
        public Transform entrancePoint;
        public Transform lookUnderTablePoint;
        public List<DirtSpot> stage0Dirt = new List<DirtSpot>();
        public List<DirtSpot> stage1Dirt = new List<DirtSpot>();
        public GumSpot gumSpot;
        public List<ScratchSpot> scratches = new List<ScratchSpot>();
        public List<GameObject> friends = new List<GameObject>();
    }
}
